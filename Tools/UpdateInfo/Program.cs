//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using LibGit2Sharp;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace BuildSetups;

internal static class Program
{
    private static readonly string[] cmdKeys =
    [
        "root",
        "setup",
        "auto",
        "master",
        "change",
        "url",
        "publish",
        "sign",
        "cert",
        "pwd",
        "company",
        "git"
    ];
    private static string isccexe;
    private static EnumerationOptions eo;
    private static string signTool;
    private readonly static Commands commands = new Commands();

    private static void Main(string[] args)
    {
        Console.WriteLine(nameof(BuildSetups));
        bool showHelp = false;

        eo = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true
        };

        commands.ParseArgs(args, ["root"]);
        showHelp |= commands.ContainsKey("help") || commands.Count == 0;

        // validate inputs
        if (!showHelp)
        {
            foreach (string key in cmdKeys)
            {
                string test = commands.GetCommand(key)?.Trim();
                if (string.IsNullOrEmpty(test) && !key.Equals("git", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Required parameter '{key}': \"{test ?? "null"}\"");
                    showHelp = true;
                }
            }
        }

        if (!showHelp)
        {
            string[] folders =
            [
                "root",
                "publish"
            ];

            foreach (string key in folders)
            {
                string dir = commands.GetCommand(key);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Console.WriteLine($">> Error << Could not find folder '{key}' = '{dir}'");
                    showHelp = true;
                }
            }
        }

        if (!showHelp)
        {
            string git = commands.GetCommand("git");
            if (!string.IsNullOrEmpty(git))
            {
                try
                {
                    // checking if git repo is valid
                    using (Repository repo = new Repository(git))
                    {
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($">> Error << 'Git' was specified but: \"{ex.Message}\"");
                    showHelp = true;
                }
            }
        }

        if (!showHelp)
        {
            string url = commands.GetCommand("url");
            showHelp |= string.IsNullOrEmpty(url);
            if (!showHelp)
            {
                if (!url.EndsWith('/'))
                {
                    commands.SetCommand("url", $"{url}/");
                }
            }
        }

        string pgmX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string pgm = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        if (!showHelp)
        {
            // find iscc.exe in Program Files (both 32 and 64 bit)
            Console.WriteLine("Locating required InnoSoft compiler: iscc.exe");
            string[] isccFiles = Directory.EnumerateFiles(pgmX86, "iscc.exe", eo).ToArray();
            if (isccFiles.Length == 0)
            {
                isccFiles = Directory.EnumerateFiles(pgm, "iscc.exe", eo).ToArray();
            }
            else if (isccFiles.Length == 1)
            {
                isccexe = isccFiles[0];
            }

            if (string.IsNullOrEmpty(isccexe))
            {
                Console.WriteLine(">> Error << Could not find iscc.exe");
                showHelp = true;
            }
            else
            {
                Console.WriteLine($"Found compiler: {isccexe}");
            }
        }

        if (!showHelp)
        {
            Console.WriteLine("Locating latest signtool.exe");
            // look for the latest sign tool
            List<string> signTools = Directory.EnumerateFiles(pgmX86, "signtool.exe", eo).ToList();
            signTools.AddRange(Directory.EnumerateFiles(pgm, "signtool.exe", eo));

            signTool = GetLatestVersion(signTools);
            if (string.IsNullOrEmpty(signTool))
            {
                Console.WriteLine(">> Error << Could not find signtool.exe");
                showHelp = true;
            }
            else
            {
                Console.WriteLine($"Signing tool: {signTool}");
            }
        }

        if (!showHelp)
        {
            Console.WriteLine("Validating commandline parameters");

            // validate signing parameters and values
            string cert = commands.GetCommand("cert");
            if (cert == null || !File.Exists(cert) || !string.Equals(".pfx", Path.GetExtension(cert), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(">> Error << Could not find cert or 'cert' not specified");
                showHelp = true;
            }
        }

        if (!showHelp)
        {
            string pwd = commands.GetCommand("pwd");
            if (pwd == null || !File.Exists(pwd))
            {
                Console.WriteLine(">> Error << Could not find pwd file or 'pwd' not specified");
                showHelp = true;
            }
        }

        if (!showHelp)
        {
            string text = commands.GetCommand("sign");
            if (string.IsNullOrEmpty(text))
            {
                Console.WriteLine(">> Error << Could not get sign text or 'sign' not specified");
                showHelp = true;
            }
        }

        if (!showHelp)
        {
            string extra = commands.GetCommand("extra");
            if (!string.IsNullOrEmpty(extra) && !Directory.Exists(extra))
            {
                Console.WriteLine(">> Error << 'Extra' was specified but could not be found.");
                showHelp = true;
            }
        }

        if (showHelp)
        {
            Console.WriteLine();
            Console.WriteLine($"usage: {nameof(BuildSetups)} -args=file -root=folder -extra=folder -changes=file -setup=*setup.exe");
            Console.WriteLine("    -auto=*update.xml -change=*changelog.txt -url=my.url -publish=folder -sign=text -cert=file");
            Console.WriteLine("    -pwd=file -company=text");
            Console.WriteLine();
            Console.WriteLine("where:");
            Console.WriteLine();
            Console.WriteLine("  root    : Path to root folder of the source code tree. Folder must exist.");
            Console.WriteLine("  setup   : Filter for finding setup executables.");
            Console.WriteLine("  master  : Name of master set of changes to be applied to auto update change log files.");
            Console.WriteLine("  auto    : Filter for finding auto update xml files.");
            Console.WriteLine("  change  : Filter for finding auto update change log files.");
            Console.WriteLine("  git     : Optional: path to git repository to use git comments with change log files.");
            Console.WriteLine("  url     : URL path to the auto update data files.");
            Console.WriteLine("  publish : Folder path to place all the files (changelog, update xml, setup executables). Folder must exist.");
            Console.WriteLine("  sign    : Comma separated list of folders to sign files or @value for a file with folder values.");
            Console.WriteLine("  cert    : PFX Cert file to sign executables with.");
            Console.WriteLine("  pwd     : Text file with the Cert password.");
            Console.WriteLine("  company : Company name.");
            Console.WriteLine();
            Console.WriteLine("optional:");
            Console.WriteLine();
            Console.WriteLine("  args    : A text file with the commandline parameters - values are overriden commandline.");
            Console.WriteLine("  extra   : A folder of file content to copy to publish folder.");
            Console.WriteLine();
            Console.WriteLine("notes:");
            Console.WriteLine();
            Console.WriteLine(" 1) Inno Setup must be installed");
            Console.WriteLine(" 2) Use the single tick \"'\" character to surround arguments with spaces)");
            Console.WriteLine(" 3) Args are prefixed with '/' or '-' characters.");
            Console.WriteLine(" 4) All files and folders must be explicitly pathed");
            Console.WriteLine(" 5) $[root] may start any file or folder path");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Parameters:");
        foreach (string key in commands.Keys)
        {
            Console.WriteLine($"  {key} = '{commands.GetCommand(key) ?? "null"}'");
        }

        ProcessAll(DateTime.Now);
    }

    private static CommitInfo[] ReadGit(string git)
    {
        try
        {
            List<CommitInfo> gitCommitMessages = new List<CommitInfo>();

            using (Repository repo = new Repository(git))
            {
                foreach (Commit commit in repo.Commits)
                {
                    // arranged newest to oldest
                    gitCommitMessages.Add(new CommitInfo { Id = commit.Id.ToString(), Message = commit.Message.Trim() });
                }
            }

            return gitCommitMessages.ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine($">> Error << Could not read git repository. \"{e.Message}\"");
            return null;
        }
    }

    private static void ParseStrings(string text, HashSet<string> strings, HashSet<string> files)
    {
        if (files == null) files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith('@'))
            {
                // handle file
                string name = part.Substring(1);
                if (name.Length > 0 && File.Exists(name))
                {
                    string full = Path.GetFullPath(name);
                    if (files.Contains(full)) continue;
                    files.Add(full);

                    foreach (string line in File.ReadLines(name))
                    {
                        ParseStrings(line, strings, files);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(part))
            {
                strings.Add(part);
            }
        }
    }

    private static bool ProcessAll(DateTime now)
    {
        string root = commands.GetCommand("root");
        Console.WriteLine($"Processing root folder: {root}");

        string certFile = commands.GetCommand("cert");
        string pwdFile = commands.GetCommand("pwd");
        string companyNameLC = commands.GetCommand("company")?.ToLower();

        string[] pwdLines = File.ReadAllLines(pwdFile);
        string pwd = null;
        foreach (string line in pwdLines)
        {
            if (string.IsNullOrEmpty(line?.Trim())) continue;
            pwd = line;
            break;
        }

        if (pwd == null)
        {
            Console.WriteLine(">> Error << Could not locate signing password - quitting");
            return false;
        }

        // find the setup source files
        HashSet<string> sourceDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> setupSourceFiles = Directory.GetFiles(commands.GetCommand("root"), "*.iss", SearchOption.AllDirectories).ToList();
        List<int> toRemove = new List<int>();

        for (int ii = 0; ii < setupSourceFiles.Count; ii++)
        {
            string setupSourceFile = setupSourceFiles[ii];
            if (setupSourceFile.Contains("\\includes\\", StringComparison.OrdinalIgnoreCase) ||
                setupSourceFile.Contains("\\dependencies\\", StringComparison.OrdinalIgnoreCase))
            {
                toRemove.Add(ii);
                continue;
            }

            string dir = Path.GetDirectoryName(setupSourceFile);
            if (string.IsNullOrEmpty(dir)) continue;

            sourceDirs.Add(dir);
        }

        if (toRemove.Count > 0)
        {
            toRemove.Reverse();
            foreach (int index in toRemove)
            {
                setupSourceFiles.RemoveAt(index);
            }
        }

        if (setupSourceFiles.Count == 0) return false;

        string[] projFiles = Directory.EnumerateFiles(root, "*.csproj", eo).ToArray();
        if (projFiles.Length == 0) return false;

        string sign = commands.GetCommand("sign");
        HashSet<string> projectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ParseStrings(sign, projectNames, null);

        Signing signing = new Signing
        {
            CertFile = certFile,
            CompanyName = companyNameLC,
            Pwd = pwd,
        };

        foreach (string project in projFiles)
        {
            if (!projectNames.Contains(Path.GetFileNameWithoutExtension(project))) continue;
            string projectDir = Path.GetDirectoryName(project);
            if (string.IsNullOrEmpty(projectDir)) continue;

            string bin = Path.Combine(projectDir, "bin", "Release");
            if (!Directory.Exists(bin))
            {
                Console.WriteLine($">> Error << Could not find directory: \"{bin}\"");
                return false;
            }

            List<string> allAssemblies = Directory.EnumerateFiles(bin, "*.dll", eo).ToList();
            allAssemblies.AddRange(Directory.EnumerateFiles(bin, "*.exe", eo));

            foreach (string assembly in allAssemblies)
            {
                SignAssembly(assembly, signing);
            }
        }

        // All lines that start with plus ('+') and then one or more name keys (unique
        // portion of the setup file names) separated by comma (',') characters and at
        // least one equals ('=') character will be included in the changelog file. The
        // key  can be "all" for all projects. All leading plus ('+') characters are
        // changed to a dash ('-') characters.

        string masterChangesTxt = commands.GetCommand("master");
        string[] changes = Directory.GetFiles(root, masterChangesTxt, SearchOption.AllDirectories);
        string changesFile = changes.Length == 1 ? changes[0] : null;
        Dictionary<string, List<string>> changeLines = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (string sourceDir in sourceDirs)
        {
            string name = Path.GetFileNameWithoutExtension(sourceDir);
            changeLines[name] = new List<string>();
        }

        CommitInfo[] commits = null;
        string git = commands.GetCommand("git");
        if (!string.IsNullOrEmpty(git))
        {
            commits = ReadGit(git);
            if (commits == null)
            {
                return false;
            }
        }

        if (changesFile != null)
        {
            const char sep = '=';
            const char lead = '+';
            const char used = '-';

            HashSet<string> oldIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string changesFileNew = $"{changesFile}.tmp";
            using (StreamWriter writer = new StreamWriter(changesFileNew))
            {
                using (StreamReader reader = new StreamReader(changesFile))
                {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;

                        string text = line.Trim();

                        if (!text.StartsWith(lead) || !text.Contains(sep))
                        {
                            int indexOfClose = text.IndexOf(')');
                            if (text.StartsWith("-git(") && indexOfClose > 5)
                            {
                                string id = text.Substring(5, indexOfClose - 5);
                                if (!string.IsNullOrEmpty(id))
                                {
                                    oldIds.Add(id);
                                }
                            }
                            writer.WriteLine(text);
                            continue;
                        }

                        StringBuilder prefix = new StringBuilder();
                        int startIndex = 1;

                        // collect the project prefixes
                        for (int ii = startIndex; ii < text.Length; ii++)
                        {
                            char ch = text[ii];
                            startIndex++;
                            if (ch == sep) break;
                            prefix.Append(ch);
                        }

                        string[] installerKeys = prefix.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (startIndex > 0) text = text.Substring(startIndex).Trim();

                        bool isAll = !string.IsNullOrEmpty(Array.Find(installerKeys, x => x.Equals("all", StringComparison.OrdinalIgnoreCase)));
                        if (isAll)
                        {
                            string[] keys = changeLines.Keys.ToArray();
                            foreach (string key in keys)
                            {
                                changeLines[key].Add(text);
                            }
                        }
                        else
                        {
                            foreach (string installerKey in installerKeys)
                            {
                                foreach (string key in changeLines.Keys)
                                {
                                    if (key.Contains(installerKey, StringComparison.OrdinalIgnoreCase))
                                    {
                                        changeLines[key].Add(text);
                                    }
                                }
                            }
                        }

                        writer.WriteLine($"{used}{string.Join(',', installerKeys)}{sep}{text}");
                    }
                }

                // add commits (by default 'all')
                if (commits?.Length > 0)
                {
                    foreach (CommitInfo commit in commits)
                    {
                        if (oldIds.Contains(commit.Id)) continue;

                        string[] keys = changeLines.Keys.ToArray();
                        foreach (string key in keys)
                        {
                            changeLines[key].Add(commit.Message);
                        }

                        writer.WriteLine($"-git({commit.Id})={commit.Message}");
                    }
                }
            }

            File.Move(changesFile, $"{changesFile}.bak", true);
            File.Move(changesFileNew, changesFile, true);
        }

        Dictionary<string, FileData> preSetupFileData = new Dictionary<string, FileData>();
        GetSetupInfo(sourceDirs, preSetupFileData);

        foreach (string installFile in setupSourceFiles)
        {
            string outPath = CompileInstallFile(installFile);
            if (!string.IsNullOrEmpty(outPath))
            {
                if (File.Exists(outPath))
                {
                    foreach (string line in File.ReadLines(outPath))
                    {
                        Console.WriteLine(line.Trim());
                    }

                    File.Delete(outPath);
                }
                return false;
            }
        }

        // find the setup executable file dates post-compile
        Dictionary<string, FileData> setupFileData = new Dictionary<string, FileData>();
        GetSetupInfo(sourceDirs, setupFileData);

        foreach (FileData fileData in setupFileData.Values)
        {
            Console.WriteLine($"Collect files for setup: {fileData.FilePath}");

            // make sure each setup file was created/updated correctly
            if (preSetupFileData.TryGetValue(fileData.FilePath, out FileData preData) && preData.LastWrite >= fileData.LastWrite)
            {
                Console.WriteLine($">> Error << Not compiled: {fileData.FilePath}");
                return false;
            }

            string key = Path.GetFileNameWithoutExtension(fileData.SourceDir);
            bool result = ProcessFileData(fileData, changeLines[key], now, signing);
            if (!result) return false;
        }

        if (commands.ContainsKey("extra"))
        {
            string extra = commands.GetCommand("extra");
            string pub = commands.GetCommand("publish");

            if (Directory.Exists(extra))
            {
                foreach (string file in Directory.EnumerateFiles(extra, "*.*", eo))
                {
                    PublishFile(file, pub, true);
                }
            }
        }

        return true;
    }

    private static int SignAssembly(string filePath, Signing signing)
    {
        int exitCode = -1;
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);
        if (fvi.CompanyName == null || signing.CompanyName == null || !fvi.CompanyName.Trim().Equals(signing.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            _ = X509CertificateLoader.LoadCertificateFromFile(filePath);
            Console.WriteLine($"Already signed: \"{filePath}\"");
            //already signed - skip this one
            return 0;
        }
        catch (CryptographicException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($">> Error << {ex.Message}");
            return 2;
        }

        // sign the assembly
        Console.WriteLine($"Signing: \"{filePath}\"");

        string name = Path.GetFileNameWithoutExtension(filePath);
        string outPath = Path.Combine(Path.GetTempPath(), $"{name}.txt");

        using (Output outputData = new Output(outPath))
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = signTool,
                Arguments = $"sign /fd SHA256 /f {signing.CertFile} /p {signing.Pwd} {filePath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;

                process.OutputDataReceived += outputData.Handler;
                process.ErrorDataReceived += outputData.Handler;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
        }

        return exitCode;
    }

    private static bool ProcessFileData(FileData fileData, List<string> changeLines, DateTime now, Signing signing)
    {
        // sign setup install .exe file
        SignAssembly(fileData.FilePath, signing);

        string dir = Path.GetDirectoryName(fileData.FilePath);
        if (dir == null)
        {
            Console.WriteLine($">> Error << Directory for: {fileData.FilePath} cannot be null");
            return false;
        }

        string parent = Path.GetDirectoryName(dir);
        if (parent == null)
        {
            Console.WriteLine($">> Error << Directory for {dir} cannot be null");
            return false;
        }

        string[] changelogs = Directory.GetFiles(parent, commands.GetCommand("change"));
        if (changelogs.Length > 1)
        {
            Console.WriteLine($">> Error << Cannot have more than one changelog file for {parent}");
            return false;
        }

        string[] updateXmls = Directory.GetFiles(parent, commands.GetCommand("auto"));
        if (updateXmls.Length > 1)
        {
            Console.WriteLine($">> Error << Cannot have more than one update xml file for {parent}");
            return false;
        }

        XmlDocument xmlDoc = new XmlDocument();
        XmlElement xmlElement;

        string updateXmlFile;
        string changeLog;

        if (changelogs.Length == 0)
        {
            string fileName = Path.GetFileName(fileData.FilePath);
            string changeTxt = commands.GetCommand("change").Replace("*", string.Empty).Replace("?", string.Empty);
            fileName = fileName.Replace("setup.exe", changeTxt, StringComparison.OrdinalIgnoreCase);
            changeLog = Path.Combine(fileData.SourceDir, fileName);

            Console.WriteLine($"Creating changelog: {changeLog}");
            File.Create(changeLog).Close();
        }
        else
        {
            changeLog = changelogs[0];
        }

        if (updateXmls.Length == 0)
        {
            string fileName = Path.GetFileName(fileData.FilePath);
            string updateTxt = commands.GetCommand("auto").Replace("*", string.Empty).Replace("?", string.Empty);
            fileName = fileName.Replace("setup.exe", updateTxt, StringComparison.OrdinalIgnoreCase);
            updateXmlFile = Path.Combine(fileData.SourceDir, fileName);

            Console.WriteLine($"Creating update xml file: {updateXmlFile}");

            //(1) the xml declaration is recommended, but not mandatory
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlElement root = xmlDoc.DocumentElement;
            xmlDoc.InsertBefore(xmlDeclaration, root);

            xmlElement = xmlDoc.CreateElement("item");
            xmlDoc.AppendChild(xmlElement);

            XmlNode nodeVersion = xmlDoc.CreateElement("version");
            xmlElement.AppendChild(nodeVersion);

            XmlNode nodeUrl = xmlDoc.CreateElement("url");
            xmlElement.AppendChild(nodeUrl);

            XmlNode nodeChangeLog = xmlDoc.CreateElement("changelog");
            xmlElement.AppendChild(nodeChangeLog);

            XmlNode manditoryNode = xmlDoc.CreateElement("manditory");
            manditoryNode.InnerText = "false";
            xmlElement.AppendChild(manditoryNode);
        }
        else
        {
            updateXmlFile = updateXmls[0];
            Console.WriteLine($"Updating update xml file: {updateXmlFile}");

            xmlDoc.Load(updateXmlFile);

            xmlElement = xmlDoc.DocumentElement;
            if (xmlElement == null)
            {
                Console.WriteLine($">> Error << Could not read xml file {updateXmls[0]}");
                return false;
            }
        }

        foreach (XmlNode node in xmlElement.ChildNodes)
        {
            switch (node.Name.ToLower())
            {
                case "url":
                {
                    node.InnerText = $"{commands.GetCommand("url")}{Path.GetFileName(fileData.FilePath)}";
                    break;
                }

                case "version":
                {
                    node.InnerText = fileData.ProductVersion?.Trim() ?? "0.0.0.0";
                    break;
                }

                case "changelog":
                {
                    node.InnerText = $"{commands.GetCommand("url")}{Path.GetFileName(changeLog)}";
                    break;
                }
            }
        }

        xmlDoc.Save(updateXmlFile);

        string outputPath = Path.GetDirectoryName(fileData.FilePath) ?? ".\\";

        string xmlFileName = Path.GetFileName(updateXmlFile);
        File.Copy(updateXmlFile, Path.Combine(outputPath, xmlFileName), true);

        Console.WriteLine($"Updating changelog: {changeLog}");
        using (FileStream newChangeLog = File.Create($"{changeLog}.tmp"))
        {
            string[] lines = File.ReadLines(changeLog).ToArray();

            using (StreamWriter textWriter = new StreamWriter(newChangeLog))
            {
                // header
                textWriter.WriteLine($"{fileData.ProductVersion} - {now:yyyy/MM/dd hh:mm:ss}");
                if (changeLines.Count > 0)
                {
                    for (int ii = 0; ii < changeLines.Count; ii++)
                    {
                        string change = changeLines[ii];
                        textWriter.WriteLine($" {ii + 1,4}) {change}");
                    }
                }
                else
                {
                    textWriter.WriteLine("    1) Minor bug fixes, performance improvements, or setup changes.");
                }

                // put a blank line if there are already lines in the file
                if (lines.Length > 0) textWriter.WriteLine();

                // write the remaining lines
                foreach (string line in lines)
                {
                    textWriter.WriteLine(line);
                }
            }
        }

        if (File.Exists(changeLog))
        {
            File.Move(changeLog, $"{changeLog}.delete");
        }

        File.Move($"{changeLog}.tmp", changeLog, true);

        string deleteName = $"{changeLog}.delete";
        if (File.Exists(deleteName))
        {
            File.Delete(deleteName);
        }

        string changeLogName = Path.GetFileName(changeLog);
        File.Copy(changeLog, Path.Combine(outputPath, changeLogName), true);

        if (commands.ContainsKey("extra"))
        {
            string extra = commands.GetCommand("extra");
            if (Directory.Exists(extra))
            {
                foreach (string file in Directory.EnumerateFiles(extra, "*.*", eo))
                {
                    File.Copy(file, Path.Combine(outputPath, Path.GetFileName(file)), true);
                }
            }
        }

        string pub = commands.GetCommand("publish");
        if (Directory.Exists(pub))
        {
            Console.WriteLine("Publishing files.");
            PublishFile(fileData.FilePath, pub, true);
            PublishFile(changeLog, pub, true);
            PublishFile(updateXmlFile, pub, true);
        }

        return true;
    }

    private static void PublishFile(string filePath, string pubDir, bool overwrite)
    {
        File.Copy(filePath, Path.Combine(pubDir, Path.GetFileName(filePath)), overwrite);
    }

    private static void GetSetupInfo(HashSet<string> sourceDirs, Dictionary<string, FileData> setupFileData)
    {
        // find the setup executable file dates pre-compile
        foreach (string file in Directory.GetFiles(commands.GetCommand("root"), commands.GetCommand("setup"), SearchOption.AllDirectories))
        {
            string source = null;
            foreach (string sourceDir in sourceDirs)
            {
                if (file.StartsWith(sourceDir, StringComparison.OrdinalIgnoreCase))
                {
                    source = sourceDir;
                    break;
                }
            }

            if (!setupFileData.TryGetValue(file, out FileData value))
            {
                value = new FileData { FilePath = file };
                setupFileData.Add(file, value);
            }

            FileData data = value;
            data.SourceDir = source;

            try
            {
                FileInfo fileInfo = new FileInfo(file);
                data.LastWrite = fileInfo.LastWriteTime;
            }
            catch { }

            try
            {
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(file);
                data.FileVersion = version.FileVersion?.Trim();
                data.ProductVersion = version.ProductVersion?.Trim();
            }
            catch { }
        }
    }

    private static string CompileInstallFile(string file)
    {
        Console.WriteLine($"Compile: \"{file}\"");

        string name = Path.GetFileNameWithoutExtension(file);
        string outPath = Path.Combine(Path.GetTempPath(), $"{name}.txt");

        int exitCode = 0;
        using (Output outputData = new Output(outPath))
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = isccexe,
                Arguments = file,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;

                process.OutputDataReceived += outputData.Handler;
                process.ErrorDataReceived += outputData.Handler;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
        }

        if (exitCode == 0)
        {
            if (File.Exists(outPath)) File.Delete(outPath);
            return null;
        }

        return outPath;
    }

    private static string GetLatestVersion(ICollection<string> assemblies)
    {
        System.Version version = System.Version.Parse("0.0.0.0");
        string result = null;

        foreach (string assembly in assemblies)
        {
            if (File.Exists(assembly))
            {
                if (assembly.Contains("\\arm", StringComparison.OrdinalIgnoreCase)) continue;

                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly);
                System.Version v = System.Version.Parse(fvi.ProductVersion ?? "0.0.0.0");
                if (v > version)
                {
                    result = assembly;
                    version = v;
                }
            }
        }

        return result;
    }
}

public class FileData
{
    public string FilePath { get; set; }
    public string FileVersion { get; set; }
    public string ProductVersion { get; set; }
    public DateTime? LastWrite { get; set; }
    public string SourceDir { get; set; }
}

public class Output : IDisposable
{
    private readonly StreamWriter writer;
    private bool disposedValue;

    public Output(string path)
    {
        writer = new StreamWriter(path);
    }

    public void Handler(object sendingProcess, DataReceivedEventArgs outLine)
    {
        //* Do your stuff with the output (write to console/log/StringBuilder)
        writer.WriteLine(outLine.Data);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                writer?.Close();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Output()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class Signing
{
    public string CertFile { get; set; }
    public string Pwd { get; set; }
    public string CompanyName { get; set; }
}

public class Commands
{
    private readonly Dictionary<string, string> cmds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> subs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public void ParseArgs(ICollection<string> args, string[] pathSubstitutes)
    {
        if (args == null || args.Count == 0) return;
        HashSet<string> pathSubs = pathSubstitutes?.Length > 0 ? new HashSet<string>(pathSubstitutes, StringComparer.OrdinalIgnoreCase) : null;

        string allArgs = $"{string.Join(' ', args)} ";
        ParseArgText(allArgs, true, pathSubs);

        string argFile = GetCommand("args");
        if (!string.IsNullOrEmpty(argFile))
        {
            // handle a file with commandline arguments (ignore all commands already in the dictionary)
            ParseFile(argFile, pathSubs);
        }
    }

    public string GetCommand(string key)
    {
        if (cmds.TryGetValue(key, out string value))
        {
            if (value == null) return null;

            if (!value.Contains("$[")) return value;
            foreach (string subst in subs.Keys)
            {
                value = value.Replace(subst, subs[subst]);
            }
            return value;
        }

        return null;
    }

    public int Count { get { return cmds.Count; } }

    public bool ContainsKey(string key)
    {
        return cmds.ContainsKey(key);
    }

    private void ParseArgText(string line, bool keepLatest, HashSet<string> pathSubs)
    {
        bool inArg = false;
        bool inTick = false;
        StringBuilder argSB = new StringBuilder();

        foreach (char ch in line)
        {
            if (!inArg)
            {
                // skipping everything except for prefix characters
                if (ch == '-' || ch == '/')
                {
                    inArg = true;
                    argSB.Clear();
                }
                continue;
            }

            if (!inTick)
            {
                if (char.IsWhiteSpace(ch))
                {
                    // parse the accumulated arg
                    if (!ParseArg(argSB, pathSubs, keepLatest)) continue;

                    inArg = false;
                    continue;
                }

                if (ch == '\'')
                {
                    inTick = true;
                    continue;
                }

                argSB.Append(ch);
            }
            else
            {
                if (ch == '\'')
                {
                    inTick = false;
                    continue;
                }

                argSB.Append(ch);
            }
        }

        if (argSB.Length > 0)
        {
            ParseArg(argSB, pathSubs, keepLatest);
        }
    }

    private bool ParseArg(StringBuilder argSB, HashSet<string> pathSubs, bool keepLatest)
    {
        string arg = argSB.ToString();
        string value = null;
        string[] parts = arg.Split(['='], 2);

        string cmd = parts[0];
        if (parts.Length > 1)
        {
            value = parts[1].Trim('\'');
        }

        if (!string.IsNullOrEmpty(cmd))
        {
            string c = cmd.Trim();
            string v = value?.Trim();
            if (v == null) return false;

            if (string.Compare(c, "?") == 0) c = "help";

            if (pathSubs?.Count > 0 && pathSubs.Contains(c) && !v.Contains("$["))
            {
                string substKey = $"$[{c}]";
                subs[substKey] = v;
                pathSubs.Remove(c);
            }

            if (cmds.ContainsKey(c))
            {
                if (keepLatest) cmds[c] = v;
            }
            else
            {
                cmds[c] = v.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : v;
            }
        }

        return true;
    }

    private void ParseFile(string v, HashSet<string> pathSubs)
    {
        if (string.IsNullOrEmpty(v)) return;
        if (!File.Exists(v)) return;
        ParseArgText(string.Join(' ', File.ReadAllLines(v)), false, pathSubs);
    }

    public void SetCommand(string key, string value)
    {
        cmds[key] = value;
    }

    public ICollection<string> Keys { get { return cmds.Keys; } }
}

public class CommitInfo
{
    public string Id { get; set; }
    public string Message { get; set; }
}
