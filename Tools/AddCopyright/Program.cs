//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System.Security.Cryptography;
using System.Text;
using UtilitiesLibrary;

namespace AddCopyright;

static internal class Program
{
    // args:
    //   root=path
    //   exts=.cs,.csproj
    //   copyright=textfilepath

    private static bool isTest = true;
    private static bool force;
    private static bool backup;
    private static readonly HashSet<string> supportedExtensions = new HashSet<string>(new string[] { ".cs", ".csproj" }, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ignoreFolders = new HashSet<string>(new string[] { "bin", "obj" }, StringComparer.OrdinalIgnoreCase);
    private const string prefix = "<@$&<";
    private const string postfix = ">&$@>";
    private const string header = $" {prefix} copyright begin {postfix} ";
    private const string footer = $" {prefix} copyright end {postfix}";
    private const string version = "20241017.A";
    private const string dashLine = "=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=";
    private static readonly Dictionary<string, int> stats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> fileErrors = new List<string>();
    private static HashSet<string> exts;
    private static string[] copyrightLines;
    private static string newHash;
    private static Dictionary<string, string> commands;

    static void Main(string[] args)
    {
        bool showHelp = args.Length == 0;

        commands = ParseArgs(args);
        showHelp |= commands.ContainsKey("help");
        string root = null;
        string copyrightTextFile = null;
        StringBuilder errors = new StringBuilder();

        Console.WriteLine();

        StringBuilder sb = new StringBuilder();
        sb.AppendJoin(" ", args);

        Console.WriteLine($"{nameof(AddCopyright)} : {sb}");

        // validate inputs
        if (!showHelp)
        {
            root = GetCommand("root")?.Trim();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                errors.AppendLine($"Could not find root: \"{root ?? "null"}\"");
                showHelp = true;
            }

            string extsText = GetCommand("exts");
            if (extsText == null) extsText = "";

            if (extsText.Length == 0)
            {
                exts = supportedExtensions;
            }
            else
            {
                exts = new HashSet<string>(extsText.Split(',', StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);
            }

            foreach (string ext in exts)
            {
                if (!ext.StartsWith("."))
                {
                    errors.AppendLine($"Extension, \"{ext}\", must start with '.' ");
                    showHelp = true;
                }
                else if (!supportedExtensions.Contains(ext))
                {
                    errors.AppendLine($"Extension, \"{ext}\", is not supported");
                    showHelp = true;
                }
            }

            copyrightTextFile = GetCommand("text")?.Trim();
            if (string.IsNullOrEmpty(copyrightTextFile) || !File.Exists(copyrightTextFile))
            {
                errors.AppendLine($"Could not find copyright text file: \"{copyrightTextFile ?? "null"}\"");
                showHelp = true;
            }

            if (commands.ContainsKey("force")) force = true;
            if (commands.ContainsKey("change")) isTest = false;
            if (commands.ContainsKey("backup")) backup = true;
        }

        if (showHelp)
        {
            Console.WriteLine();
            if (errors.Length > 0)
            {
                Console.Write(errors.ToString());
                Console.WriteLine();
            }
            Console.WriteLine($"usage: {nameof(AddCopyright)} --root=folder --exts=.ext1,.ext2 --text=textfilepath [--force] [--change] [--backup]");
            Console.WriteLine();
            Console.WriteLine("where:");
            Console.WriteLine();
            Console.WriteLine("  root  : The path to start of the source code tree.");
            Console.WriteLine("  exts  : Limit the file extensions to this set to add the copyright text.");
            Console.WriteLine("  text  : The path to the file containing the copyright text.");
            Console.WriteLine();
            Console.WriteLine("options:");
            Console.WriteLine();
            Console.WriteLine("  force  : force a rewrite of the copyright text to all files with matching extensions.");
            Console.WriteLine("  change : add copyright to files. does not change files by default.");
            Console.WriteLine();
            return;
        }

        if (copyrightTextFile == null) return;
        if (exts == null) return;

        // read, trim, clean, and create hash for the copyright text
        string copyrightText = ReadCopyrightText(copyrightTextFile).Trim();
        copyrightLines = copyrightText.Split("\n", StringSplitOptions.TrimEntries);
        newHash = SHA256.HashData(Encoding.UTF8.GetBytes(copyrightText)).ToHex();

        Console.WriteLine();
        Console.WriteLine("Cleaned copyright text:");
        Console.WriteLine("----");
        Console.Write(copyrightText);
        Console.WriteLine("----");
        Console.WriteLine($"Text hash: {newHash}");
        Console.WriteLine("----");
        Console.WriteLine($"Extensions: {string.Join(", ", exts)}");
        Console.WriteLine("----");

        stats["folder"] = 0;
        stats["skipped"] = 0;
        stats["total"] = 0;
        stats["matched"] = 0;
        stats["changed"] = 0;
        stats["ignored"] = 0;
        stats["current"] = 0;

        ProcessFolder(root);

        Console.WriteLine("Stats:");
        Console.WriteLine($"  all folders:{stats["folder"]}");
        Console.WriteLine($"  skipped folders:{stats["skipped"]}");
        Console.WriteLine($"  total files:{stats["total"]}");
        Console.WriteLine($"  matched files:{stats["matched"]}");
        Console.WriteLine($"  changed files:{stats["changed"]}");
        Console.WriteLine($"  ignored files:{stats["ignored"]}");
        Console.WriteLine($"  current files:{stats["current"]}");

        if (fileErrors.Count > 0)
        {
            Console.WriteLine("----");
            Console.WriteLine($"Errors: ({fileErrors.Count})");
            for (int ii = 0; ii < fileErrors.Count; ii++)
            {
                string errorText = fileErrors[ii];
                Console.WriteLine($"{ii}. {errorText}");
            }
        }

        Console.WriteLine();
    }

    private static string GetCommand(string key)
    {
        commands.TryGetValue(key, out string value);
        return value;
    }

    private static void ProcessFolder(string folder)
    {
        stats["folder"]++;

        string[] parts = folder.Split(Path.DirectorySeparatorChar);
        string last = parts.LastOrDefault();
        if (last?.StartsWith('.') != false || ignoreFolders.Contains(last))
        {
            stats["skipped"]++;
            return;
        }

        foreach (string file in Directory.EnumerateFiles(folder))
        {
            ProcessFile(file);
        }

        foreach (string dir in Directory.EnumerateDirectories(folder))
        {
            ProcessFolder(dir);
        }
    }

    private static void ProcessFile(string file)
    {
        stats["total"]++;

        string name = Path.GetFileName(file);
        string ext = Path.GetExtension(file);

        if (!exts.Contains(ext) || name.EndsWith("assemblyinfo.cs", StringComparison.OrdinalIgnoreCase) || name.Equals("globalsuppressions.cs", StringComparison.OrdinalIgnoreCase))
        {
            stats["ignored"]++;
            return;
        }

        stats["matched"]++;

        switch (ext.ToLower())
        {
            case ".cs":
                ProcessCSharp(file);
                break;

            case ".csproj":
            case ".resx":
                ProcessXmlFile(file);
                break;
        }
    }

    private static void ProcessCSharp(string file)
    {
        DoFileWork(file, "//", "");
    }

    private static void DoFileWork(string file, string startCommentText, string endCommentText)
    {
        if (startCommentText == null || endCommentText == null) return;

        bool isXml = ".xml".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase);
        bool isCS = ".cs".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase);

        string headerText = null;
        string bakFile = null;
        bool restore = true;
        string newFile = null;

        if (isXml)
        {
            // this line needs to be first
            // <?xml version="1.0" encoding="utf-8"?>
        }

        try
        {
            bakFile = MakeBackupFileName(file);
            newFile = $"{file}.tmp";
            string[] lines = File.ReadAllLines(file);
            int copyrightHeader = -1;
            int copyrightFooter = -1;
            int firstLineOfCode = -1;
            bool hasCopyright = false;

            if (File.Exists(newFile)) File.Delete(newFile);

            // find and keep comment lines up to a non-comment line or the 
            // copyright text

            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                string line = lines[lineNumber];
                string lineText = line.Trim();

                if (lineText.Length == 0)
                {
                    continue;
                }

                if (!lineText.StartsWith(startCommentText))
                {
                    if (firstLineOfCode < 0)
                    {
                        firstLineOfCode = lineNumber;
                    }

                    break;
                }

                // check for copyright comment begin and end lines
                if (line.Contains(prefix) && line.Contains(postfix))
                {
                    int pos = line.IndexOf(postfix) + postfix.Length;

                    if (copyrightHeader < 0)
                    {
                        headerText = line.Substring(pos).Trim();
                        copyrightHeader = lineNumber;
                    }
                    else
                    {
                        copyrightFooter = lineNumber;
                    }
                }
            }

            hasCopyright = copyrightHeader >= 0 && copyrightFooter >= 0;
            if (copyrightHeader < 0) copyrightHeader = firstLineOfCode;

            bool overWrite = hasCopyright && TestHash(newHash, headerText) || force;
            bool mustAdd = !hasCopyright || overWrite;

            if (!mustAdd)
            {
                stats["current"]++;
                return;
            }

            stats["changed"]++;

            if (isTest)
            {
                return;
            }

            if (bakFile != null) File.Move(file, bakFile);

            using (StreamWriter writer = File.CreateText(newFile))
            {
                int index = 0;

                if (overWrite)
                {
                    // copy the leading content prior to the copyright
                    while (index < copyrightHeader)
                    {
                        string line = lines[index++];
                        writer.WriteLine(line);
                    }

                    index += copyrightFooter - copyrightHeader + 1;
                }
                else
                {
                    // copy the leading content without overwrite
                    while (index < firstLineOfCode)
                    {
                        string line = lines[index++];
                        writer.WriteLine(line);
                    }
                }

                // write the new copyright content
                DateTimeOffset now = DateTimeOffset.Now;
                writer.WriteLine($"{startCommentText} {header}{newHash}:{version}:{now.Year}:{now.Month}:{now.Day}:{now.Hour}:{now.Minute}{endCommentText}");
                writer.WriteLine($"{startCommentText} {dashLine}{endCommentText}");

                foreach (string line in copyrightLines)
                {
                    writer.WriteLine($"{startCommentText} {line}{endCommentText}");
                }

                writer.WriteLine($"{startCommentText} {dashLine}{endCommentText}");
                writer.WriteLine($"{startCommentText}{footer}{endCommentText}");

                // put a blank line after the copyright and before the code text
                if (isCS) writer.WriteLine();

                bool skipLeadingBlankLines = isCS;

                while (index < lines.Length)
                {
                    if (skipLeadingBlankLines)
                    {
                        skipLeadingBlankLines &= lines[index].Trim() == string.Empty;
                        if (skipLeadingBlankLines)
                        {
                            index++;
                            continue;
                        }
                    }

                    writer.WriteLine(lines[index]);
                    index++;
                }
            }

            if (File.Exists(file)) File.Delete(file);
            File.Move(newFile, file);

            restore = false;
        }
        catch (Exception e)
        {
            fileErrors.Add($"{file} error='{e.Message}'");
        }
        finally
        {
            if (restore && File.Exists(bakFile))
            {
                if (File.Exists(file)) File.Delete(file);
                File.Move(bakFile, file);
            }

            if (File.Exists(newFile)) File.Delete(newFile);
        }
    }

    private static string MakeBackupFileName(string file)
    {
        if (!backup) return null;

        string name = Path.GetFileName(file);
        string folder = Path.GetDirectoryName(file) ?? "";
        List<string> bakFiles = Directory.GetFiles(folder, $"{name}.bak*").ToList();
        bakFiles.Sort();
        string lastBak = bakFiles.LastOrDefault();

        int next = 0;
        if (lastBak != null)
        {
            string lastBakName = Path.GetExtension(lastBak);
            _ = int.TryParse(lastBakName.AsSpan(4), out next);
        }

        return $"{file}.bak{next + 1}";
    }

    private static bool TestHash(string newHash, string headerText)
    {
        bool result = false;
        string[] parts = headerText.Trim().Split(':');
        if (parts.Length > 0)
        {
            result = string.Compare(parts[0], newHash) != 0;
            if (parts.Length > 1)
            {
                result |= string.Compare(parts[1], version, StringComparison.OrdinalIgnoreCase) != 0;
            }
        }
        return result;
    }

    private static void ProcessXmlFile(string file)
    {
        DoFileWork(file, "<!--", " -->");
    }

    private static string ReadCopyrightText(string textfile)
    {
        // read copyright file
        string[] lines = File.ReadAllLines(textfile);

        StringBuilder copyrightText = new StringBuilder();

        // ignore leading blank lines
        int start = 0;
        while (start < lines.Length)
        {
            if (lines[start].Trim() != string.Empty) break;
            start++;
        }

        // ignore trailing blank lines
        int end = lines.Length - 1;
        while (end > start)
        {
            if (lines[end].Trim() != string.Empty) break;
            end--;
        }

        // ignore leading and trailing whitespace for each line
        // keeping blank lines in the middle of the text
        for (int ii = start; ii <= end; ii++)
        {
            string line = lines[ii].Replace("/*", "*").Replace("*/", "*").Replace("//", "*").Replace("/", "*").Replace("(c)", "©").Replace("--", "-=").Trim();
            copyrightText.AppendLine(line);
        }

        return copyrightText.ToString();
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        Dictionary<string, string> commands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string[] prefixes = { "--", "/", "-" };

        foreach (string arg in args)
        {
            string cmd = null;
            string value = null;
            foreach (string prefix in prefixes)
            {
                if (arg.StartsWith(prefix))
                {
                    string foo = arg.Substring(prefix.Length);
                    string[] parts = foo.Split(new char[] { '=' }, 2);

                    cmd = parts[0];
                    if (parts.Length > 1)
                    {
                        value = parts[1];
                    }

                    break;
                }
            }

            if (!string.IsNullOrEmpty(cmd))
            {
                string c = cmd.Trim();
                string v = value?.Trim();

                if (string.Compare(c, "?") == 0) c = "help";
                commands[c] = v;
            }
        }

        return commands;
    }
}
