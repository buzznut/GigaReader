//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

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
    private CancellationTokenSource cts = new CancellationTokenSource();
    private int bom;
    private int colCount;
    private long fileSize;
    private bool disposedValue;
    private BackgroundWorker loadWorker;
    private Search find;
    private readonly MRUCache<long, string> lineToText = new MRUCache<long, string>(2000);
    private readonly Stopwatch stopwatch = new Stopwatch();
    private string filePath;
    private StateChangedDelegate sendState;
    private long lineCount;
    private string indexPath;
    private Decoder decoder = null;
    private FileStream textFile = null;
    private Microsoft.Win32.SafeHandles.SafeFileHandle textHandle = null;
    private FileStream indexFile = null;
    private Microsoft.Win32.SafeHandles.SafeFileHandle indexHandle = null;
    private int eolLen = 0;

    public delegate void StateChangedDelegate(IDictionary<string, object> state);

    public const string LinesStateKey = "Lines.State";
    public const string LinesProgressKey = "Lines.Progress";
    public const string LinesFileKey = "Lines.File";
    public const string LinesErrorKey = "Lines.Error";
    public const string LinesReasonKey = "Lines.Reason";
    public const string LinesElapsedKey = "Lines.Elapsed";
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
        { LinesElapsedKey, typeof(TimeSpan) },
        { LinesLoadedKey, typeof(bool) },
        { LinesMaxLineKey, typeof(long) },
    };

    public Lines(StateChangedDelegate stateChangedDelegate)
    {
        sendState = stateChangedDelegate;
    }

    public void Load(string file)
    {
        loadWorker?.Dispose();

        loadWorker = new BackgroundWorker();
        loadWorker.WorkerSupportsCancellation = true;
        loadWorker.WorkerReportsProgress = true;

        loadWorker.DoWork += DoLoad;
        loadWorker.WorkerReportsProgress = true;
        loadWorker.ProgressChanged += LoadProgress;
        loadWorker.RunWorkerCompleted += LoadCompleted;

        filePath = file;

        if (sendState != null)
        {
            Dictionary<string, object> state = new Dictionary<string, object>();
            state[LinesStateKey] = "Loading";
            state[LinesProgressKey] = 0;
            state[LinesFileKey] = file;
            sendState(state);
            state[LinesLoadedKey] = false;
        }

        stopwatch.Restart();
        loadWorker.RunWorkerAsync(file);
    }

    private void LoadCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        stopwatch.Stop();
        loadWorker?.Dispose();
        loadWorker = null;

        if (sendState == null) return;

        Dictionary<string, object> state = new Dictionary<string, object>();
        state[LinesElapsedKey] = stopwatch.Elapsed;
        state[LinesLoadedKey] = false;

        try
        {
            if (e.Error != null)
            {
                state[LinesStateKey] = "Error";
                state[LinesErrorKey] = e.Error;
                state[LinesReasonKey] = e.Error.Message;
                return;
            }

            if (e.Cancelled)
            {
                state[LinesStateKey] = "Cancelled";
                state[LinesReasonKey] = "File load cancelled";
                return;
            }

            state[LinesStateKey] = "Done";
            state[LinesProgressKey] = 1000;
            state[LinesLoadedKey] = true;
        }
        finally
        {
            sendState(state);
        }


    }

    private void LoadProgress(object sender, ProgressChangedEventArgs e)
    {
        if (sendState == null) return;

        Dictionary<string, object> state = new Dictionary<string, object>();
        state[LinesStateKey] = "Loading";
        state[LinesProgressKey] = e.ProgressPercentage;
        state[LinesElapsedKey] = stopwatch.Elapsed;
        state[LinesMaxLineKey] = Rows;
        sendState(state);
    }

    private void DoLoad(object sender, DoWorkEventArgs e)
    {
        if (sender is not BackgroundWorker bw || e.Argument is not string path)
        {
            throw new ArgumentException("Invalid argument");
        }

        textFile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
        textHandle = textFile.SafeFileHandle;

        indexPath = Path.GetTempFileName();
        indexFile = new FileStream(
            indexPath, 
            FileMode.OpenOrCreate, 
            FileAccess.ReadWrite, 
            FileShare.None, 
            32768, 
            FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose);
        indexHandle = indexFile.SafeFileHandle;

        using (MemoryStream block = new MemoryStream())
        {
            Interlocked.Exchange(ref lineCount, 0);

            using (FileStream tf = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous))
            {
                try
                {
                    Interlocked.Exchange(ref colCount, 0);
                    Interlocked.Exchange(ref bom, (int)BOMType.ANSI);
                    Interlocked.Exchange(ref fileSize, textFile.Length);

                    byte[] buffer = new byte[BufferSize];
                    long pos = 0;

                    long start = 0;
                    long next;
                    int lastProgress = -1;
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
                                    charLen = 2;
                                }
                                else if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                                {
                                    // utf-16 LE
                                    Interlocked.Exchange(ref bom, (int)BOMType.UTF16LE);
                                    pos = 2;
                                    decoder = Encoding.Unicode.GetDecoder();
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
                    while (textBytesRead > 0 && !bw.CancellationPending && !cts.IsCancellationRequested)
                    {
                        while (bufferIndex < textBytesRead && !bw.CancellationPending && !cts.IsCancellationRequested)
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
                                        blockIndexTask.GetAwaiter().GetResult();
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

                            // percent complete calculation
                            float currentPercent = 1000F * (start + bufferIndex) / fileSize;
                            int percent = (int)currentPercent;
                            if (lastProgress != percent)
                            {
                                lastProgress = percent;
                                bw.ReportProgress(percent);
                            }
                        }

                        start += textBytesRead;

                        bufferIndex = 0;
                        if (bytesLeft == 0) break;

                        textBytesRead = tf.Read(buffer, 0, buffer.Length);
                        bytesLeft -= textBytesRead;
                    }

                    if (blockCount > 0)
                    {
                        if (blockIndexTask != default && !blockIndexTask.IsCompleted)
                        {
                            blockIndexTask.GetAwaiter().GetResult();
                        }

                        block.Position = 0;
                        RandomAccess.Write(indexHandle, block.ToROSpan(), indexPos);
                        Interlocked.Add(ref lineCount, blockCount);
                    }

                    bw.ReportProgress(100);
                }
                catch (Exception ex)
                {
                    if (!bw.CancellationPending && Debugger.IsAttached && !cts.IsCancellationRequested)
                    {
                        Debugger.Log(4, "Exception", ex.Message);
                        Debugger.Break();
                    }
                }
            }
        }

        // Collect all generations
        GC.Collect();

        // Wait for finalizers to complete
        GC.WaitForPendingFinalizers();
    }

    private void AppendLineInfo(Stream block, long lineStartPos)
    {
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

                string text = ReadString(lineNumber);
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
                return null;
            }
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

    private string ReadString(long lineNumber)
    {
        if (textFile == null) return string.Empty;
        long linePos = ReadIndex(lineNumber);
        if (linePos == -1) return string.Empty;

        byte[] bytes = new byte[colCount + eolLen];
        StringBuilder sb = new StringBuilder();

        while (!cts.IsCancellationRequested)
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

                if (!string.IsNullOrEmpty(indexPath) && File.Exists(indexPath))
                {
                    try
                    {
                        File.Delete(indexPath);
                    }
                    catch (Exception ex)
                    {
                        if (Debugger.IsAttached)
                        {
                            Debugger.Log(4, "Exception", ex.Message);
                            Debugger.Break();
                        }
                    }

                    indexPath = null;
                }

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
            cts.Cancel();
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
        Dictionary<string, object> state = new Dictionary<string, object>();
        foreach (string key in findState.Keys)
        {
            state[key] = findState[key];
        }
        sendState(state);
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