using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using UtilitiesLibrary;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
public partial class HFViewer : Form
{
    private string baseTitle;
    private bool titleSet;
    private int cursorCol = -1;
    private long cursorRow = -1;
    private int screenCol = -1;
    private long screenRow = -1;
    private bool hasFile;

    public HFViewer(string file)
    {
        SuspendLayout();
        InitializeComponent();

        screenPos.Text = string.Empty;
        screenPos.Visible = false;

        cursorText.Text = string.Empty;
        cursorText.Visible = false;

        findToolStripMenuItem.Enabled = false;

        textControl.RegisterStateHandler(ErrorHandler, Lines.LinesErrorKey);
        textControl.RegisterStateHandler(ProgressHandler, Lines.LinesProgressKey);
        textControl.RegisterStateHandler(LinesLoadedHandler, Lines.LinesLoadedKey);
        textControl.RegisterStateHandler(FileChangedHandler, Lines.LinesFileKey);
        textControl.RegisterStateHandler(ElapsedHandler, Lines.LinesElapsedKey);
        textControl.RegisterStateHandler(ElapsedHandler, Search.SearchElapsedKey);
        textControl.RegisterStateHandler(SearchFinishedHandler, Search.FinishedKey);
        textControl.RegisterStateHandler(ProgressHandler, Search.SearchProgressKey);
        textControl.RegisterStateHandler(ErrorHandler, Search.SearchErrorKey);
        textControl.RegisterStateHandler(CursorHandler, TextControl.CursorKey);
        textControl.RegisterStateHandler(ProgressHandler, Lines.LinesMaxLineKey);
        textControl.RegisterStateHandler(SetStatus, Lines.LinesStatusKey);
        textControl.RegisterStateHandler(ScreenPosHandler, TextControl.ScreenKey);

        if (file != null)
        {
            hasFile = true;
            textControl.Open(file);
            textControl.Focus();
        }

        ResumeLayout();
    }

    private void ScreenPosHandler(KeyValuePair<string, object> state)
    {
        if (state.Value is long[] v && v != null && v.Length == 2)
        {
            if (screenRow >= 0 && screenCol >= 0 && screenRow == v[0] && screenCol == v[1]) return;

            screenRow = v[0];
            screenCol = (int)v[1];

            screenPos.Text = string.Format("Screen: {0},{1}", (screenRow + 1).ToString("0"), (screenCol + 1).ToString("0"));
        }
    }

    private void SetStatus(KeyValuePair<string, object> state)
    {
        if (state.Value != null && state.Value != null)
        {
            AddStatus(state.Value.ToString() ?? "Error");
        }
    }

    private void ElapsedHandler(KeyValuePair<string, object> state)
    {
        if (state.Value is TimeSpan span)
        {
            AddStatus($"Elapsed: {span}");
        }
    }

    private void CursorHandler(KeyValuePair<string, object> state)
    {
        if (state.Value is long[] v && v != null && v.Length == 2)
        {
            if (cursorRow >= 0 && cursorCol >= 0 && cursorRow == v[0] && cursorCol == v[1]) return;

            cursorRow = v[0];
            cursorCol = (int)v[1];

            cursorText.Text = string.Format("Ln {0}, Col {1}", (cursorRow + 1).ToString("0"), (cursorCol + 1).ToString("0"));
        }
    }

    private void SearchFinishedHandler(KeyValuePair<string, object> state)
    {
        progressBar.Visible = false;
    }

    private void FileChangedHandler(KeyValuePair<string, object> kvp)
    {
        if (kvp.Value is string s)
        {
            if (!titleSet)
            {
                Text = $"{baseTitle} - {s}";
                titleSet = true;
            }
            findToolStripMenuItem.Enabled = textControl.Rows > 0;
        }
    }

    private void LinesLoadedHandler(KeyValuePair<string, object> kvp)
    {
        bool loaded = kvp.Value is bool f && f;
        progressBar.Visible = !loaded;
        cursorText.Visible = loaded;
        screenPos.Visible = loaded;
    }

    private void ProgressHandler(KeyValuePair<string, object> state)
    {
        if ((state.Key == Search.SearchProgressKey || state.Key == Lines.LinesProgressKey) && state.Value is int progress)
        {
            try
            {
                progressBar.Visible = progress < 1000;
                progressBar.Value = progress;
            }
            catch { }
        }

        if (hasFile && state.Key == Lines.LinesMaxLineKey && state.Value is long lineMax)
        {
            AddStatus($"(Lines loaded):{lineMax:N0}");
        }
    }

    private void AddStatus(string text)
    {
        timerStatus.Stop();
        statusText.Text = text;
        timerStatus.Start();
    }

    private void ErrorHandler(KeyValuePair<string, object> state)
    {
        progressBar.Visible = false;
        if (state.Value != null)
        {
            AddStatus(state.Value.ToString() ?? "Error");
        }
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        openFileDialog.RestoreDirectory = true;
        openFileDialog.Filter = @"All files|*.*";
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            cursorText.Visible = false;
            findToolStripMenuItem.Enabled = false;
            hasFile = true;

            Clear();
            textControl.Open(openFileDialog.FileName);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Clear();
        base.OnClosing(e);
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void searchToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!findToolStripMenuItem.Enabled) return;
        textControl.Find();
    }

    private void searchAgainToolStripMenuItem_Click(object sender, EventArgs e)
    {
        textControl.SearchAgain();
    }

    private void gotoToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Goto gt = new Goto(textControl.Rows);
        if (gt.ShowDialog() == DialogResult.OK)
        {
            long line;
            if (long.TryParse(gt.GetLine(), out line))
            {
                textControl.GoToLine(line, true);
            }
        }
    }

    private void HFViewer_Load(object sender, EventArgs e)
    {
        baseTitle = Text;
    }

    void hScrollBar1_GotFocus(object sender, EventArgs e)
    {
        textControl.Select();
    }

    private void vScrollBar1_GotFocus(object sender, EventArgs e)
    {
        textControl.Select();
    }
    private void HFViewer_Leave(object sender, EventArgs e)
    {
    }

    private void HFViewer_Enter(object sender, EventArgs e)
    {
    }

    private void HFViewer_Activated(object sender, EventArgs e)
    {
    }

    private void HFViewer_Deactivate(object sender, EventArgs e)
    {
    }

    private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Preferences pref = new Preferences() { TabSize = textControl.TabSize };

        if (pref.ShowDialog() == DialogResult.OK)
        {
            textControl.TabSize = pref.TabSize;
        }
    }

    private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
    {
        copyToolStripMenuItem.Enabled = (textControl.InSearch);
    }

    private void copyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        textControl.ToClipboard();
    }

    private void findToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
    {
        bool ok = textControl.Rows > 0;

        searchToolStripMenuItem.Enabled = ok;
        gotoToolStripMenuItem.Enabled = ok;
        searchAgainToolStripMenuItem.Enabled = ok;
    }

    private void HFViewer_FormClosing(object sender, FormClosingEventArgs e)
    {
        Clear();
    }

    private void searchCancelToolStripMenuItem_Click(object sender, EventArgs e)
    {
        textControl.StopSearch();
    }

    private void toolStripMenuItemClose_Click(object sender, EventArgs e)
    {
        Clear();
    }

    private void Clear()
    {
        timerStatus.Stop();
        hasFile = false;
        textControl.StopSearch();
        textControl.StopLoad();
        textControl.Clear();
        cursorText.Visible = false;
        cursorText.Text = string.Empty;
        progressBar.Visible = false;
        titleSet = false;
        Text = baseTitle;
        statusText.Text = string.Empty;
        cursorCol = -1;
        cursorRow = -1;

        Task.Run(() =>
        {
            // Allow some time for background threads to finish
            System.Threading.Thread.Sleep(100);
            TextControl.ForceGC();
        });
    }

    private void textControl_MouseUp(object sender, EventArgs e)
    {

    }

    private void timerStatus_Tick(object sender, EventArgs e)
    {
        timerStatus.Stop();
        statusText.Text = string.Empty;
    }
}
