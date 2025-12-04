using AutoUpdaterDotNET;
using Config.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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
    private Status status = new Status();
    private IConfig config;
    private string jsonConfig;
    public const string UpdateUrl = "https://raw.githubusercontent.com/buzznut/HugeFileReader/master/Installers/HugeFileReader/Output/GigaReaderUpdate.xml";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FilePath { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BufferSize { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SavedFont { get; set; }

    public HFViewer(string file)
    {
        SuspendLayout();
        InitializeComponent();

        baseTitle = Text;

        Clear();

        findToolStripMenuItem.Enabled = false;

        textControl.RegisterStateHandler(ErrorHandler, Lines.LinesErrorKey);
        textControl.RegisterStateHandler(ProgressHandler, Lines.LinesProgressKey);
        textControl.RegisterStateHandler(LinesLoadedHandler, Lines.LinesLoadedKey);
        textControl.RegisterStateHandler(FileChangedHandler, Lines.LinesFileKey);
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

        config = new ConfigurationBuilder<IConfig>()
          .UseJsonFile($"{Application.ProductName}.json")
          .Build();

        jsonConfig = Newtonsoft.Json.JsonConvert.SerializeObject(config);

        if (config.Font != null)
        {
            try
            {
                Font font = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Drawing.Font>(config.Font);
                if (font != null)
                {
                    textControl.Font = font;
                }
            }
            catch { }
        }

        if (config.TabSize > 0)
        {
            textControl.TabSize = config.TabSize;
        }

        AutoUpdater.CheckForUpdateEvent += UpdateCheckEvent;
        
        ResumeLayout();
    }

    private void UpdateCheckEvent(UpdateInfoEventArgs args)
    {
        switch (args.Error)
        {
            case null:
                if (args.IsUpdateAvailable)
                {
                    // Uncomment the following line if you want to show standard update dialog instead.
                    AutoUpdater.ShowUpdateForm(args);
                }
                else
                {
                    MessageBox.Show(
                        "You are running the latest version.", "Updates",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                break;

            case WebException:
                MessageBox.Show(
                            "There is a problem reaching update server. Please check your internet connection and try again later.",
                            "Update Check Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                break;

            default:
                if (Debugger.IsAttached) Debugger.Break();
                MessageBox.Show(args.Error.Message,
                    args.Error.GetType().ToString(), MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                break;
        }
    }

    private void ScreenPosHandler(KeyValuePair<string, object> state)
    {
        if (state.Value is long[] v && v != null && v.Length == 2)
        {
            if (screenRow >= 0 && screenCol >= 0 && screenRow == v[0] && screenCol == v[1]) return;

            screenRow = v[0];
            screenCol = (int)v[1];

            screenPos.Text = string.Format("Screen:{0}/{1},{2}/{3}", (screenRow + 1).ToString("0"), textControl.Rows.ToString("0"), (screenCol + 1).ToString("0"), textControl.Cols.ToString("0"));
        }
    }

    private void SetStatus(KeyValuePair<string, object> state)
    {
        if (state.Value != null && state.Value != null)
        {
            string text = status.Add(state.Value.ToString() ?? "Error", state.Key);
            if (!string.IsNullOrEmpty(text))
            {
                statusText.Text = text;
                timerStatus.Start();
            }
        }
    }

    private void CursorHandler(KeyValuePair<string, object> state)
    {
        if (state.Value is long[] v && v != null && v.Length == 2)
        {
            if (cursorRow >= 0 && cursorCol >= 0 && cursorRow == v[0] && cursorCol == v[1]) return;

            cursorRow = v[0];
            cursorCol = (int)v[1];

            cursorText.Text = string.Format("Cursor:{0},{1}", (cursorRow + 1).ToString("0"), (cursorCol + 1).ToString("0"));
        }
    }

    private void SearchFinishedHandler(KeyValuePair<string, object> state)
    {
        progressBar.Visible = false;
        if (state.Value != null && state.Value != null)
        {
            string text = status.Add(state.Value.ToString() ?? "Error", state.Key);
            if (!string.IsNullOrEmpty(text))
            {
                statusText.Text = text;
                timerStatus.Start();
            }
        }
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
        }
    }

    private void LinesLoadedHandler(KeyValuePair<string, object> kvp)
    {
        bool loaded = kvp.Value is bool f && f;
        cursorText.Visible = loaded;
        screenPos.Visible = loaded;
        progressBar.Visible = !loaded;
        findToolStripMenuItem.Enabled = textControl.Rows > 0;
    }

    private void ProgressHandler(KeyValuePair<string, object> state)
    {
        if ((state.Key == Search.SearchProgressKey || state.Key == Lines.LinesProgressKey) && state.Value is int progress)
        {
            try
            {
                progressBar.Visible = progress < 1000 && progress > 0;
                progressBar.Value = progress;
            }
            catch { }
        }

        if (hasFile && state.Key == Lines.LinesMaxLineKey && state.Value is long lineMax)
        {
            string text = status.Add($"(Lines loaded):{lineMax:N0}", state.Key);
            if (!string.IsNullOrEmpty(text))
            {
                statusText.Text = text;
                timerStatus.Start();
            }
        }
    }

    private void ErrorHandler(KeyValuePair<string, object> state)
    {
        Clear();

        if (state.Value != null)
        {
            if (state.Value is Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        openFileDialog.RestoreDirectory = true;
        openFileDialog.Filter = @"All files|*.*";
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            cursorText.Visible = false;
            screenPos.Visible = false;
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
            config.TabSize = pref.TabSize;
        }
    }

    private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
    {
        copyToolStripMenuItem.Enabled = (textControl.InSearch || textControl.HasSelection);
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
        screenPos.Visible = false;
        screenPos.Text = string.Empty;
        progressBar.Visible = false;
        titleSet = false;
        Text = baseTitle;
        statusText.Text = string.Empty;
        cursorCol = -1;
        cursorRow = -1;
        status.Clear();

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
        string next = status.GetNext();
        if (next == null)
        {
            statusText.Text = string.Empty;
            return;
        }

        statusText.Text = next;
        timerStatus.Start();
    }

    private void fontToolStripMenuItem_Click(object sender, EventArgs e)
    {
        fontDialog.Font = textControl.Font;
        fontDialog.FixedPitchOnly = true;
        DialogResult result = fontDialog.ShowDialog();
        if (result == DialogResult.OK)
        {
            textControl.Font = fontDialog.Font;
            textControl.CalculateScreenDimensions();
            config.Font = Newtonsoft.Json.JsonConvert.SerializeObject(fontDialog.Font);
        }
    }

    private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
    {
        HelpViewer helpViewer = new HelpViewer();
        helpViewer.Initialize(Path.GetDirectoryName(Application.ExecutablePath), "Help", "Help.pdf");
        helpViewer.ShowDialog();
    }

    private void updateToolStripMenuItem_Click(object sender, EventArgs e)
    {
        AutoUpdater.Start(UpdateUrl);
    }

    private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
    {
        HelpViewer helpViewer = new HelpViewer();
        helpViewer.Initialize(Path.GetDirectoryName(Application.ExecutablePath), "About", "About.pdf");
        helpViewer.ShowDialog();
    }
}
