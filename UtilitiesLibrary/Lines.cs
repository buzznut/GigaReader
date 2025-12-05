//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UtilitiesLibrary;

public enum BOMType
{
    ANSI,
    UTF8,
    UTF16BE,
    UTF16LE
}

public class Lines : IDisposable
{
    private const int BlockSize = 32 * 1024;
    private const int BufferSize = 8 * 1024 * 1024;
    private CancellationTokenSource ctsX = new CancellationTokenSource();
    private int bom;
    private int colCount;
    private long fileSize;
    private bool disposedValue;
    private BackgroundWorker loadWorker;
    private BackgroundWorker fullHashWorker;
    private Search find;
    private readonly MRUCache<long, string> lineToText = new MRUCache<long, string>(2000);
    private readonly Stopwatch stopwatch = new Stopwatch();
    private string filePath;
    private long lineCount;
    private string indexPath;
    private Decoder decoder = null;
    private string decoderText = null;
    private FileStream textFile = null;
    private Microsoft.Win32.SafeHandles.SafeFileHandle textHandle = null;
    private FileStream indexFile = null;
    private Microsoft.Win32.SafeHandles.SafeFileHandle indexHandle = null;
    private string jsonPath;
    private int eolLen = 0;
    private ConcurrentDictionary<string, object> state;
    private readonly string productName;
    private Dictionary<string, string> headerInfo;

    public const string LinesStatusKey = "Lines.Status";
    public const string LinesStateKey = "Lines.State";
    public const string LinesProgressKey = "Lines.Progress";
    public const string LinesFileKey = "Lines.File";
    public const string LinesErrorKey = "Lines.Error";
    public const string LinesReasonKey = "Lines.Reason";
    public const string LinesLoadedKey = "Lines.Loaded";
    public const string LinesMaxLineKey = "Lines.MaxLine";
    public int Cols { get { return Interlocked.Add(ref colCount, 0); } }
    public long FileSize { get { return Interlocked.Add(ref fileSize, 0); } }
    public BOMType BOM { get { return (BOMType)Interlocked.Add(ref bom, 0); } }
    public long Rows { get { return Interlocked.Add(ref lineCount, 0); } }
    public bool Loading { get { return (loadWorker?.IsBusy).GetValueOrDefault(); } }
    public static readonly Dictionary<string, Type> LinesKeyTypes = new Dictionary<string, Type>()
    {
        { LinesStateKey, typeof(string) },
        { LinesProgressKey, typeof(int) },
        { LinesFileKey, typeof(string) },
        { LinesErrorKey, typeof(Exception) },
        { LinesReasonKey, typeof(string) },
        { LinesLoadedKey, typeof(bool) },
        { LinesMaxLineKey, typeof(long) },
    };

    public Lines(ConcurrentDictionary<string, object> stateChanged, string productName = null)
    {
        state = stateChanged;
        this.productName = string.IsNullOrEmpty(productName) ? AssemblyName : productName;
    }

    private string AssemblyName
    {
        get
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            string file = entryAssembly?.Location ?? "unknown";
            string app = Path.GetFileNameWithoutExtension(file);
            return app;
        }
    }

    public void Clear()
    {
        StopLoad();
        filePath = null;
        
        indexFile?.Dispose();
        indexFile = null;

        indexHandle?.Dispose();
        indexHandle = null;

        textFile?.Dispose();
        textFile = null;

        textHandle?.Dispose();
        textHandle = null;

        indexPath = null;

        indexHandle?.Dispose();
        indexHandle = null;

        lineToText.Clear();
        
        loadWorker?.Dispose();
        loadWorker = null;

        fullHashWorker?.Dispose();
        fullHashWorker = null;

        if (headerInfo != null)
        {
            headerInfo.Clear();
            headerInfo = null;
        }

        if (lineToText != null)
        {
            lineToText.Clear();
        }

        Interlocked.Exchange(ref lineCount, 0);
        Interlocked.Exchange(ref colCount, 0);
        Interlocked.Exchange(ref bom, (int)BOMType.ANSI);
        Interlocked.Exchange(ref fileSize, 0);
    }

    public void Load(string file)
    {
        if (!File.Exists(file))
        {
            throw new FileNotFoundException("File not found", file);
        }

        Clear();

        loadWorker = new BackgroundWorker();
        loadWorker.WorkerSupportsCancellation = true;

        loadWorker.DoWork += DoLoad;
        loadWorker.RunWorkerCompleted += LoadCompleted;

        fullHashWorker = new BackgroundWorker();
        fullHashWorker.WorkerSupportsCancellation = true;
        fullHashWorker.DoWork += CheckFullHash;
        fullHashWorker.RunWorkerCompleted += FullHashCheck;

        filePath = file;

        AddState(LinesStateKey, "Loading");
        AddState(LinesProgressKey, 0);
        AddState(LinesFileKey, file);
        AddState(LinesLoadedKey, false);

        stopwatch.Restart();
        loadWorker.RunWorkerAsync(file);
    }

    private void FullHashCheck(object sender, RunWorkerCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            AddState(LinesStateKey, "Error");
            AddState(LinesErrorKey, e.Error);
            AddState(LinesReasonKey, e.Error.Message);
            return;
        }
    }

    private void AddState(string key, object value)
    {
        if (state == null) return;
        state[key] = value;
    }

    private void LoadCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        stopwatch.Stop();
        loadWorker?.Dispose();
        loadWorker = null;

        AddState(LinesStatusKey, $"Loading Complete. Elapsed:{stopwatch.Elapsed}");

        if (e.Error != null)
        {
            AddState(LinesStateKey, "Error");
            AddState(LinesErrorKey, e.Error);
            AddState(LinesReasonKey, e.Error.Message);

            // clean up
            ErrorCleanUp();

            return;
        }

        if (e.Cancelled)
        {
            ctsX.Cancel();
            AddState(LinesStateKey, "Cancelled");
            AddState(LinesReasonKey, "File load cancelled");
            return;
        }

        AddState(LinesStateKey, "Done");
        AddState(LinesProgressKey, 1000);
        AddState(LinesLoadedKey, true);
    }

    private void ErrorCleanUp()
    {
        if (indexFile != null)
        {
            string name = indexFile.Name;
            if (File.Exists(name))
            {
                indexFile.Dispose();
                indexFile = null;
                File.Delete(name);
            }
        }

        if (jsonPath != null)
        {
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }

            jsonPath = null;
        }

        Clear();
    }

    private void LoadProgress(object sender, ProgressChangedEventArgs e)
    {
        AddState(LinesStateKey, "Loading");
        AddState(LinesProgressKey, e.ProgressPercentage);
        AddState(LinesMaxLineKey, Rows);
    }

    public static byte[] GetFastSha512(Stream stream, string name, DateTime date)
    {
        // Validate file existence
        ArgumentNullException.ThrowIfNull(stream);
        if (name == null) name = string.Empty;

        try
        {
            int blenMin = 4096;
            long spacedCount = Math.Max(0, (long)Math.Min(128, (float)stream.Length / blenMin));

            MemoryStream msChunks = new MemoryStream();

            msChunks.Write(BitConverter.GetBytes(stream.Length));
            msChunks.Write(Encoding.UTF8.GetBytes(name));
            msChunks.Write(BitConverter.GetBytes(date.ToBinary()));

            int bufferLen = (int)Math.Min(blenMin, stream.Length);
            float spacing = (float)stream.Length / spacedCount;

            for (long ii = 0; ii < spacedCount; ii++)
            {
                byte[] buffer = new byte[bufferLen];
                long pos = (long)(ii * spacing);
                stream.Position = pos;
                _ = stream.Read(buffer, 0, bufferLen);
                msChunks.Write(buffer);
            }

            // read last chunk
            byte[] bufferC = new byte[bufferLen];
            stream.Position = Math.Max(0, stream.Length - bufferLen);
            _ = stream.Read(bufferC, 0, bufferLen);
            msChunks.Write(bufferC);

            msChunks.Position = 0;
            return SHA512.HashData(msChunks);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Access to the file is denied.");
        }
        catch (IOException ex)
        {
            throw new IOException("An I/O error occurred while reading the file.", ex);
        }
    }

    private void DoLoad(object sender, DoWorkEventArgs e)
    {
        if (sender is not BackgroundWorker bw || e.Argument is not string path)
        {
            throw new ArgumentException("Invalid argument");
        }

        try
        {
            FileInfo fileInfo = new FileInfo(path);

            textFile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
            textHandle = textFile.SafeFileHandle;

            Interlocked.Exchange(ref fileSize, textFile.Length);

            string tmpDir = Path.Combine(Path.GetTempPath(), productName);
            Directory.CreateDirectory(tmpDir);

            string fname = Path.GetFileNameWithoutExtension(path);
            string metaDataRoot = path.ToLower().MD5();

            jsonPath = Path.Combine(tmpDir, metaDataRoot + ".json");
            indexPath = Path.Combine(tmpDir, metaDataRoot + ".index");

            textFile.Position = 0;
            string[] headers = { "FastHash", "FullHash", "Rows", "Cols", "Bom", "Decoder", "EolLen" };

            if (File.Exists(jsonPath))
            {
                // load header file
                string jsonText = File.ReadAllText(jsonPath);
                headerInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);
            }

            if (headerInfo == null)
            {
                // create header file
                headerInfo = new Dictionary<string, string>();
            }

            byte[] fastHash = GetFastSha512(textFile, Path.GetFileName(path), fileInfo.CreationTimeUtc);
            string fastHashString = Convert.ToBase64String(fastHash);

            bool mustRebuildIndex = !File.Exists(indexPath);
            bool changedInfoHeader = false;
            foreach (string header in headers)
            {
                switch (header)
                {
                    case "FastHash":
                    {
                        if (headerInfo.TryGetValue(header, out string text) == false)
                        {
                            headerInfo[header] = Convert.ToBase64String(fastHash);
                            changedInfoHeader = true;
                            mustRebuildIndex = true;
                            break;
                        }

                        if (fastHashString != text)
                        {
                            // hashes don't match, need to reprocess index
                            if (File.Exists(indexPath)) File.Delete(indexPath);
                            headerInfo[header] = fastHashString;
                            changedInfoHeader = true;
                            mustRebuildIndex = true;
                        }
                        break;
                    }

                    case "Rows":
                    {
                        if (headerInfo.TryGetValue(header, out string text) == false)
                        {
                            headerInfo[header] = "0";
                            changedInfoHeader = true;
                            mustRebuildIndex = true;
                            break;
                        }

                        long lc = Int64.Parse(text);
                        Interlocked.Exchange(ref lineCount, lc);

                        break;
                    }

                    case "Cols":
                    {
                        if (headerInfo.TryGetValue(header, out string text) == false)
                        {
                            headerInfo[header] = "0";
                            changedInfoHeader = true;
                            mustRebuildIndex = true;
                            break;
                        }

                        int cc = int.Parse(text);
                        Interlocked.Exchange(ref colCount, cc);

                        break;
                    }

                    case "Bom":
                    {
                        if (headerInfo.TryGetValue(header, out string text) == false)
                        {
                            headerInfo[header] = $"{(int)BOMType.ANSI}";
                            changedInfoHeader = true;
                            mustRebuildIndex = true;
                            break;
                        }

                        int b = int.Parse(text);
                        Interlocked.Exchange(ref bom, b);
                        break;
                    }

                    case "EolLen":
                    {
                        if (headerInfo.TryGetValue(header, out string text) == false)
                        {
                            headerInfo[header] = "0";
                            changedInfoHeader = true;
                            mustRebuildIndex = true;
                            break;
                        }

                        eolLen = int.Parse(text);
                        break;
                    }

                    case "Decoder":
                    {
                        if (headerInfo.TryGetValue(header, out string text) == false)
                        {
                            headerInfo[header] = "Default";
                            changedInfoHeader = true;
                            mustRebuildIndex = true;
                            break;
                        }

                        string decoderName = text;
                        switch (decoderName)
                        {
                            case "UTF8":
                                decoder = Encoding.UTF8.GetDecoder();
                                break;
                            case "UTF16BE":
                                decoder = Encoding.BigEndianUnicode.GetDecoder();
                                break;
                            case "UTF16LE":
                                decoder = Encoding.Unicode.GetDecoder();
                                break;
                            default:
                                decoder = Encoding.Default.GetDecoder();
                                break;
                        }
                        break;
                    }
                }
            }

            indexFile = new FileStream(
                indexPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                32768,
                FileOptions.Asynchronous | FileOptions.RandomAccess);

            indexHandle = indexFile.SafeFileHandle;

            bool goodIndex = !mustRebuildIndex;
            if (mustRebuildIndex)
            {
                AddState(LinesStatusKey, "Building index.");
                using (FileStream tf = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous))
                {
                    using (Task<bool> indexTask = RebuildIndexAsync(tf, ctsX.Token))
                    {
                        bool finished = false;
                        int lastProgress = -1;

                        while (!ctsX.IsCancellationRequested && indexTask?.Wait(150, ctsX.Token) == false)
                        {
                            int progress = 0;

                            if (indexTask != default && tf != null)
                            {
                                if (indexTask.IsCompleted)
                                {
                                    goodIndex = indexTask.Result;
                                    finished = true;
                                }
                                else
                                {
                                    progress += (int)(1000 * (tf.Position / (float)fileSize));
                                }

                                if (progress != lastProgress)
                                {
                                    lastProgress = progress;
                                    AddState(LinesProgressKey, progress);
                                    AddState(LinesStatusKey, $"Validating index file. Elapsed:{stopwatch.Elapsed}");
                                }
                            }

                            if (finished) break;
                        }
                    }
                }

                headerInfo["Rows"] = $"{Rows}";
                headerInfo["Cols"] = $"{Cols}";
                headerInfo["Bom"] = $"{(int)BOM}";
                headerInfo["Decoder"] = decoderText;
                headerInfo["EolLen"] = $"{eolLen}";
                headerInfo["FastHash"] = Convert.ToBase64String(fastHash);

                AddState(LinesProgressKey, 1000);
            }
            else
            {
                lineToText.Clear();
            }

            if (goodIndex && changedInfoHeader)
            {
                // save header info
                string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(headerInfo, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(jsonPath, jsonText);
            }

            // double check the full hash to verify file integrity - may take a long time
            fullHashWorker.RunWorkerAsync(path);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Log(4, "Exception", ex.Message);
                Debugger.Break();
            }
            throw;
        }
    }

    private void CheckFullHash(object sender, DoWorkEventArgs e)
    {
        if (sender is not BackgroundWorker bw || e.Argument is not string path)
        {
            throw new ArgumentException("Invalid argument");
        }

        if (headerInfo == null) throw new InvalidDataException("Header information is missing.");

        FileStream hashStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
        ValueTask<byte[]> hashTask = SHA512.HashDataAsync(hashStream, ctsX.Token);

        int progress = 0;
        float progressAmount = 1000f;

        if (hashTask == default || hashStream == null)
        {
            throw new InvalidDataException("Unable to compute full file hash.");
        }

        while (!ctsX.IsCancellationRequested)
        {
            Task.Delay(150, ctsX.Token).Wait(ctsX.Token);

            if (hashTask.IsCompleted)
            {
                if (hashTask.IsCompletedSuccessfully)
                {
                    progress += (int)progressAmount;
                    string fullHashText = Convert.ToBase64String(hashTask.Result);

                    if (headerInfo != null)
                    {
                        if (headerInfo.TryGetValue("FullHash", out string text) && text != null)
                        {
                            if (text != fullHashText)
                            {
                                ErrorCleanUp();

                                // hashes don't match, need to reprocess index
                                throw new InvalidDataException("File integrity check failed. The file has changed since it was last indexed.");
                            }
                        }
                        else
                        {
                            // store full hash
                            headerInfo["FullHash"] = fullHashText;

                            // save header info
                            string tmpDir = Path.Combine(Path.GetTempPath(), productName);
                            string metaDataRoot = path.ToLower().MD5();

                            if (jsonPath == null)
                            {
                                jsonPath = Path.Combine(tmpDir, metaDataRoot + ".json");
                            }

                            string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(headerInfo, Newtonsoft.Json.Formatting.Indented);
                            File.WriteAllText(jsonPath, jsonText);
                        }
                    }

                    break;
                }
            }
            else
            {
                progress += (int)(progressAmount * (hashStream.Position / (float)fileSize));
            }
        }
    }

    private bool ByteCompare(byte[] byteArrayA, byte[] byteArrayB)
    {
        if (byteArrayA == null && byteArrayB == null) return true;
        if (byteArrayA == null || byteArrayB == null) return false;
        if (byteArrayA.Length != byteArrayB.Length) return false;

        for (int ii = 0; ii < byteArrayA.Length; ii++)
        {
            if (byteArrayA[ii] != byteArrayB[ii]) return false;
        }

        return true;
    }

    //private static string MD5(string input)
    //{
    //    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
    //    byte[] hashBytes = MD5.HashData(inputBytes);

    //    // Convert byte array to hexadecimal string
    //    StringBuilder sb = new StringBuilder();
    //    foreach (byte b in hashBytes)
    //    {
    //        sb.Append(b.ToString("x2")); // Lowercase hex format
    //    }

    //    return sb.ToString();
    //}

    private async Task<bool> RebuildIndexAsync(FileStream tf, CancellationToken token)
    {
        bool success = false;
        using (MemoryStream block = new MemoryStream())
        {
            try
            {
                Interlocked.Exchange(ref lineCount, 0);
                Interlocked.Exchange(ref colCount, 0);
                Interlocked.Exchange(ref bom, (int)BOMType.ANSI);

                byte[] buffer = new byte[BufferSize];
                long pos = 0;

                long start = 0;
                long next;
                long lineStartPos = 0;
                byte lineTerminator = default;
                long bytesLeft = textFile.Length;

                int textBytesRead = tf.Read(buffer, 0, buffer.Length);
                bytesLeft -= textBytesRead;

                int bufferIndex = 0;
                if (pos == 0 && textBytesRead > 0)
                {
                    int charLen = 0;
                    bool hasLF = false;
                    bool hasCR = false;

                    if (textBytesRead >= 4)
                    {
                        bool notASCII = false;
                        int nullCount = 0;
                        for (int ii = 0; ii < textBytesRead; ii++)
                        {

                            byte b = buffer[ii];
                            if (b == 0) nullCount++;
                            if (b >= 128) notASCII = true;
                            if (b == 0x0a)
                            {
                                hasLF = true;
                            }
                            if (b == 0x0d)
                            {
                                hasCR = true;
                            }
                        }

                        if (hasLF)
                        {
                            // linefeeds were found
                            lineTerminator = 0x0a;
                        }
                        else if (hasCR)
                        {
                            // no linefeeds found, but carriage returns were found
                            lineTerminator = 0x0d;
                        }

                        if (nullCount > 0)
                        {
                            // determine the BOM
                            if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                            {
                                // utf-16 BE
                                Interlocked.Exchange(ref bom, (int)BOMType.UTF16BE);
                                pos = 2;
                                decoder = Encoding.BigEndianUnicode.GetDecoder();
                                decoderText = "UTF16BE";
                                charLen = 2;
                            }
                            else if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                            {
                                // utf-16 LE
                                Interlocked.Exchange(ref bom, (int)BOMType.UTF16LE);
                                pos = 2;
                                decoder = Encoding.Unicode.GetDecoder();
                                decoderText = "UTF16LE";
                                charLen = 2;
                            }
                        }
                        else if (notASCII)
                        {
                            // might be utf-8 with BOM
                            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                            {
                                // utf-8 with BOM
                                Interlocked.Exchange(ref bom, (int)BOMType.UTF8);
                                pos = 3;
                                decoder = Encoding.UTF8.GetDecoder();
                                decoderText = "UTF8";
                                charLen = 1;
                            }
                        }
                    }

                    if (decoder == null)
                    {
                        // Default
                        Interlocked.Exchange(ref bom, (int)BOMType.ANSI);
                        pos = 0;
                        decoder = Encoding.Default.GetDecoder();
                        decoderText = "ASCII";
                        charLen = 1;
                    }

                    eolLen = (hasLF ? charLen : 0) + (hasCR ? charLen : 0);

                    // first row
                    bufferIndex = (int)pos;
                    lineStartPos = pos;
                }

                long indexPos = 0;
                ValueTask blockIndexTask = default;
                int blockCount = 0;
                while (textBytesRead > 0 && !token.IsCancellationRequested)
                {
                    while (bufferIndex < textBytesRead && !token.IsCancellationRequested)
                    {
                        long lineIndex = Interlocked.Add(ref lineCount, 0);

                        if (buffer[bufferIndex] == lineTerminator)
                        {
                            // calculate the offset from the start of the file 
                            // to start of the line

                            next = start + bufferIndex + 1;

                            AppendLineInfo(block, lineStartPos);
                            blockCount++;
                            if (blockCount >= BlockSize)
                            {
                                block.Position = 0;

                                if (blockIndexTask != default && !blockIndexTask.IsCompleted)
                                {
                                    await blockIndexTask.ConfigureAwait(false);
                                }

                                RandomAccess.Write(indexHandle, block.ToROSpan(), indexPos);
                                indexPos += block.Length;

                                Interlocked.Add(ref lineCount, blockCount);
                                blockCount = 0;
                                block.SetLength(0);
                            }

                            int width = (int)(next - lineStartPos - eolLen);
                            lineStartPos = next;

                            if (width > Interlocked.Add(ref colCount, 0))
                            {
                                Interlocked.Exchange(ref colCount, width);
                            }

                            pos = next;
                        }

                        bufferIndex++;
                    }

                    start += textBytesRead;

                    bufferIndex = 0;
                    if (bytesLeft == 0) break;

                    textBytesRead = await tf.ReadAsync(buffer, token);
                    bytesLeft -= textBytesRead;
                }

                if (blockCount > 0)
                {
                    if (blockIndexTask != default && !blockIndexTask.IsCompleted)
                    {
                        await blockIndexTask.ConfigureAwait(false);
                    }

                    block.Position = 0;
                    RandomAccess.Write(indexHandle, block.ToROSpan(), indexPos);
                    Interlocked.Add(ref lineCount, blockCount);
                }

                success = true;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached && !token.IsCancellationRequested)
                {
                    Debugger.Log(4, "Exception", ex.Message);
                    Debugger.Break();
                }
            }
        }

        // Collect all generations
        GC.Collect();

        // Wait for finalizers to complete
        GC.WaitForPendingFinalizers();

        return success;
    }

    private static void AppendLineInfo(Stream block, long lineStartPos)
    {
        ArgumentNullException.ThrowIfNull(block);
        block.Write(BitConverter.GetBytes(lineStartPos));
    }

    public string this[long lineNumber]
    {
        get
        {
            try
            {
                long localCount = Interlocked.Add(ref lineCount, 0);

                if (Interlocked.Add(ref fileSize, 0) == 0) return null;
                if (string.IsNullOrEmpty(filePath)) return null;
                if (lineNumber < 0 || lineNumber > localCount) return string.Empty;

                if (lineToText.ContainsKey(lineNumber))
                {
                    return lineToText[lineNumber].TrimEnd();
                }

                if (lineNumber > Rows && Loading)
                {
                    return null;
                }

                string text = ReadString(lineNumber, ctsX.Token).GetAwaiter().GetResult();
                if (text == null) return null;

                lineToText[lineNumber] = text.TrimEnd();
                return text;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Log(4, "Exception", ex.Message);
                    Debugger.Break();
                }
                //return null;
            }

            return null;
        }
    }

    private long ReadIndex(long lineNumber)
    {
        if (indexFile == null) return -1;
        if (lineNumber < 0 || lineNumber > Rows) return -1;

        byte[] buffer = new byte[sizeof(long)];
        long pos = lineNumber * sizeof(long);

        long read = RandomAccess.Read(indexHandle, buffer, pos);

        
        if (read == sizeof(long))
        {
            long linePos = BitConverter.ToInt64(buffer, 0);
            return linePos;
        }

        return -1;
    }

    private async Task<string> ReadString(long lineNumber, CancellationToken token)
    {
        if (textFile == null) return string.Empty;
        long linePos = ReadIndex(lineNumber);
        if (linePos == -1) return string.Empty;

        byte[] bytes = new byte[colCount + eolLen];
        StringBuilder sb = new StringBuilder();

        while (!token.IsCancellationRequested)
        {
            int read = RandomAccess.Read(textHandle, bytes, linePos);
            if (read <= 0)
            {
                break;
            }

            linePos += read;

            char[] chars = new char[colCount * 2];
            decoder.Convert(bytes, 0, read, chars, 0, colCount * 2, false, out int bytesUsed, out int charsUsed, out bool completed);

            for (int i = 0; i < charsUsed; i++)
            {
                char ch = chars[i];
                if (ch == '\n' || ch == '\r')
                {
                    return sb.ToString().TrimEnd();
                }

                sb.Append(ch);
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string GetText(byte[] bytes, int index, int read)
    {
        switch (BOM)
        {
            case BOMType.ANSI:
            {
                // ANSI
                return Encoding.Default.GetString(bytes, index, read);
            }

            case BOMType.UTF8:
            {
                // UTF8
                return Encoding.UTF8.GetString(bytes, index, read);
            }

            case BOMType.UTF16BE:
            {
                // UTF16BE
                return Encoding.BigEndianUnicode.GetString(bytes, index, read);
            }

            case BOMType.UTF16LE:
            {
                // UFT16LE
                return Encoding.Unicode.GetString(bytes, index, read);
            }
        }

        return null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed viewState (managed objects)
                StopLoad();

                filePath = null;

                if (find != null)
                {
                    find.StopSearch();
                    find.Dispose();
                    find = null;
                }

                indexFile?.Dispose();
                indexFile = null;
                indexHandle = null;

                textFile?.Dispose();
                textFile = null;
                textHandle = null;

                indexPath = null;
                indexHandle = null;
                lineToText.Clear();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Lines()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void StopLoad()
    {
        if (loadWorker != null && !loadWorker.CancellationPending)
        {
            ctsX.Cancel();
            loadWorker.CancelAsync();
        }

        loadWorker?.Dispose();
        loadWorker = null;
    }

    public void StopSearch()
    {
        if (find == null)
        {
            return;
        }

        find.StopSearch();
        find.Dispose();
        find = null;
    }

    public void StartSearch(long startRow, int startCol, bool useCase, bool searchDown, string searchText)
    {
        if (find == null)
        {
            find = new Search(this, SearchStateDelegate);
        }

        find.StartSearch(startRow, startCol, searchText, useCase, searchDown);
    }

    private void SearchStateDelegate(IDictionary<string, object> findState)
    {
        if (state == null) return;

        foreach (KeyValuePair<string, object> kv in findState)
        {
            AddState(kv.Key, kv.Value);
        }
    }

    public bool IsSearching()
    {
        return find != null && find.Searching;
    }
}

public static class LinesExtensions
{
    public static ReadOnlyMemory<byte> ToROM(this MemoryStream ms)
    {
        if (ms.TryGetBuffer(out ArraySegment<byte> segment))
        {
            return new ReadOnlyMemory<byte>(segment.Array, segment.Offset, segment.Count);
        }
        else
        {
            return new ReadOnlyMemory<byte>(ms.ToArray());
        }
    }

    public static ReadOnlySpan<byte> ToROSpan(this MemoryStream ms)
    {
        if (ms.TryGetBuffer(out ArraySegment<byte> segment))
        {
            return new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        }
        else
        {
            byte[] arr = ms.ToArray();
            return new ReadOnlySpan<byte>(arr, 0, arr.Length);
        }
    }
}

public class CRC32
{
    // Precomputed CRC-32 table for polynomial 0xEDB88320
    private static readonly uint[] Table = new uint[256];

    static CRC32()
    {
        const uint polynomial = 0xEDB88320;
        for (uint i = 0; i < Table.Length; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            Table[i] = crc;
        }
    }

    /// <summary>
    /// Computes CRC-32 checksum for a byte array.
    /// </summary>
    public static uint Compute(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            byte index = (byte)((crc & 0xFF) ^ b);
            crc = (crc >> 8) ^ Table[index];
        }
        return ~crc; // Final XOR
    }

    /// <summary>
    /// Computes CRC-32 checksum for a file.
    /// </summary>
    public static uint ComputeFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        using (FileStream fs = File.OpenRead(filePath))
        {
            return ComputeStream(fs);
        }
    }

    public static uint ComputeStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        uint crc = 0xFFFFFFFF;
        byte[] buffer = new byte[32768];
        while (stream.Read(buffer, 0, buffer.Length) is int bytesRead && bytesRead > 0)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                byte index = (byte)((crc & 0xFF) ^ buffer[i]);
                crc = (crc >> 8) ^ Table[index];
            }
        }
        return ~crc;
    }
}
