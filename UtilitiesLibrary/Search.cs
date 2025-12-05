//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System.ComponentModel;
using System.Diagnostics;

namespace UtilitiesLibrary;

public class Search : IDisposable
{
    private long searchRow = -1;
    private int searchCol = -1;
    private bool searchDown = true;
    private string searchText;
    private bool searchCase;
    private BackgroundWorker searchWorker;
    private Lines lines;
    private bool disposedValue;
    private SearchStateDelegate sendState;
    private Stopwatch stopwatch = new Stopwatch();
    private bool found = false;
    private bool canceled;

    public const string SearchStateKey = "Search.State";
    public const string SearchProgressKey = "Search.Progress";
    public const string SearchErrorKey = "Search.Error";
    public const string SearchReasonKey = "Search.Reason";
    public const string SearchElapsedKey = "Search.Elapsed";
    public const string SearchCursorKey = "Search.Cursor";
    public const string FinishedKey = "Search.Found";
    public static readonly Dictionary<string, Type> SearchKeyTypes = new Dictionary<string, Type>()
    {
        { SearchStateKey, typeof(string) }, 
        { SearchErrorKey, typeof(Exception) },
        { SearchCursorKey, typeof(long[]) },
        { FinishedKey, typeof(string) },
        { SearchProgressKey, typeof(int) },
        { SearchElapsedKey, typeof(TimeSpan) },
        { SearchReasonKey, typeof(string) }
    };
    public bool Searching { get { return (searchWorker?.IsBusy).GetValueOrDefault(); } }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public delegate void SearchStateDelegate(IDictionary<string, object> searchState);

    public Search(Lines lines, SearchStateDelegate searchStateDelegate)
    {
        this.sendState = searchStateDelegate;
        this.lines = lines;
    }

    public void StartSearch(long row, int col, string text, bool useCase, bool isDown)
    {
        if (row < 0 || row >= lines.Rows)
        {
            row = isDown ? 0 : lines.Rows;
        }

        found = false;
        searchRow = row;
        searchCol = col;
        searchText = text;
        searchCase = useCase;
        searchDown = isDown;

        StartSearchWorker();
    }

    private void StartSearchWorker()
    {
        BackgroundWorker worker = searchWorker;
        worker?.Dispose();

        searchWorker = new BackgroundWorker();
        searchWorker.WorkerSupportsCancellation = true;
        searchWorker.WorkerReportsProgress = true;

        searchWorker.DoWork += DoSearch;
        searchWorker.RunWorkerCompleted += SearchCompleted;
        searchWorker.ProgressChanged += SearchProgress;

        if (sendState != null)
        {
            Dictionary<string, object> state = new Dictionary<string, object>();
            state[SearchStateKey] = "Searching";
            state[SearchProgressKey] = 0;
            sendState(state);
        }

        stopwatch.Restart();
        searchWorker.RunWorkerAsync();
    }

    private Search() { }

    public void StopSearch()
    {
        if (searchWorker != null && searchWorker.IsBusy)
        {
            canceled = true;
            searchWorker.CancelAsync();
        }
    }

    private void SearchProgress(object sender, ProgressChangedEventArgs e)
    {
        if (sendState == null)
        {
            return;
        }

        if (sendState == null) return;

        Dictionary<string, object> state = new Dictionary<string, object>();
        state[SearchStateKey] = "Searching";
        state[SearchProgressKey] = e.ProgressPercentage;

        sendState(state);
    }

    private void SearchCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        BackgroundWorker bw = sender as BackgroundWorker;
        if (bw == null)
        {
            throw new ArgumentException("Invalid sender for LoadCompleted");
        }

        bw.Dispose();
        searchWorker = null;

        stopwatch.Stop();

        if (sendState == null) return;

        Dictionary<string, object> state = new Dictionary<string, object>();
        state[SearchElapsedKey] = stopwatch.Elapsed;

        try
        {
            if (e.Error != null)
            {
                state[SearchErrorKey] = e.Error;
                state[SearchReasonKey] = e.Error.Message;
                state[FinishedKey] = "Error";
                return;
            }

            if (e.Cancelled)
            {
                state[SearchStateKey] = "Cancelled";
                state[SearchReasonKey] = "File load cancelled";
                state[FinishedKey] = "Cancelled";
                return;
            }

            state[SearchCursorKey] = (long[])[searchRow, searchCol];
            state[SearchStateKey] = "Done";
            state[SearchReasonKey] = found ? "Found" : "Not found";
            state[SearchProgressKey] = 1000;
            state[FinishedKey] = found ? "Found" : "Not found";
        }
        finally
        {
            sendState(state);
        }
        return;
    }

    private void DoSearch(object sender, DoWorkEventArgs e)
    {
        if (sender is not BackgroundWorker bw)
        {
            return;
        }

        StringComparison comparisonType = searchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int increment = searchDown ? 1 : -1;
        int testLength = searchText.Length;

        int lastPercent = -1;
        long searchCount = 0;

        if (searchRow == 0 && !searchDown) searchRow = lines.Rows - 1;

        long ii = searchRow;
        while (!bw.CancellationPending && !canceled)
        {
            string t = lines[ii];
            string line = searchCase ? t : t.ToUpper();

            searchCount++;

            int percent = (int)(searchCount * 1000 / lines.Rows);
            if (percent != lastPercent)
            {
                lastPercent = percent;
                bw.ReportProgress(percent);
            }

            int col;
            int len;
            if (searchDown)
            {
                col = Math.Min(searchCol, line.Length - 1);
                len = line.Length - col;
            }
            else
            {
                col = Math.Min(searchCol, line.Length - 1);
                len = col;
            }

            if (col >= 0 && len > 0 && testLength <= len)
            {
                int index = searchDown ? line.IndexOf(searchText, col, len, comparisonType) : line.LastIndexOf(searchText, col, len, comparisonType);
                if (index >= 0)
                {
                    searchRow = ii;
                    searchCol = index;
                    found = true;
                    return;
                }
            }

            searchCol = searchDown ? 0 : lines.Cols - 1;

            ii += increment;
            if (ii >= lines.Rows && searchDown)
            {
                ii = 0;
            }
            else if (ii < 0 && !searchDown)
            {
                ii = lines.Rows - 1;
            }

            // did the find wrap around?
            if (searchCount == lines.Rows)
            {
                // searched every line - no match found
                return;
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                searchWorker?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Find()
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
