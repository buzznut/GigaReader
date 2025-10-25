using System;

namespace HugeFileReader;

partial class HFViewer
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose (bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose ();
        }
        base.Dispose (disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HFViewer));
        menuStrip = new System.Windows.Forms.MenuStrip();
        fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        toolStripMenuItemClose = new System.Windows.Forms.ToolStripMenuItem();
        toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        findToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        searchAgainToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        searchCancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
        gotoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        statusStrip = new System.Windows.Forms.StatusStrip();
        progressBar = new System.Windows.Forms.ToolStripProgressBar();
        statusText = new System.Windows.Forms.ToolStripStatusLabel();
        cursorText = new System.Windows.Forms.ToolStripStatusLabel();
        openFileDialog = new System.Windows.Forms.OpenFileDialog();
        textControl = new TextControl();
        timerStatus = new System.Windows.Forms.Timer(components);
        menuStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip
        // 
        menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, findToolStripMenuItem, optionsToolStripMenuItem });
        menuStrip.Location = new System.Drawing.Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
        menuStrip.Size = new System.Drawing.Size(666, 24);
        menuStrip.TabIndex = 0;
        menuStrip.Text = "menuStrip1";
        // 
        // fileToolStripMenuItem
        // 
        fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openToolStripMenuItem, toolStripMenuItemClose, toolStripSeparator1, exitToolStripMenuItem });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
        fileToolStripMenuItem.Text = "File";
        // 
        // openToolStripMenuItem
        // 
        openToolStripMenuItem.Name = "openToolStripMenuItem";
        openToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        openToolStripMenuItem.Text = "Open...";
        openToolStripMenuItem.Click += openToolStripMenuItem_Click;
        // 
        // toolStripMenuItemClose
        // 
        toolStripMenuItemClose.Name = "toolStripMenuItemClose";
        toolStripMenuItemClose.Size = new System.Drawing.Size(180, 22);
        toolStripMenuItemClose.Text = "Close";
        toolStripMenuItemClose.Click += toolStripMenuItemClose_Click;
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4;
        exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        exitToolStripMenuItem.Text = "Exit";
        exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
        // 
        // editToolStripMenuItem
        // 
        editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { copyToolStripMenuItem });
        editToolStripMenuItem.Name = "editToolStripMenuItem";
        editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
        editToolStripMenuItem.Text = "Edit";
        editToolStripMenuItem.DropDownOpening += editToolStripMenuItem_DropDownOpening;
        // 
        // copyToolStripMenuItem
        // 
        copyToolStripMenuItem.Name = "copyToolStripMenuItem";
        copyToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C;
        copyToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        copyToolStripMenuItem.Text = "Copy";
        copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
        // 
        // findToolStripMenuItem
        // 
        findToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { searchToolStripMenuItem, searchAgainToolStripMenuItem, searchCancelToolStripMenuItem, toolStripSeparator2, gotoToolStripMenuItem });
        findToolStripMenuItem.Enabled = false;
        findToolStripMenuItem.Name = "findToolStripMenuItem";
        findToolStripMenuItem.Size = new System.Drawing.Size(42, 20);
        findToolStripMenuItem.Text = "Find";
        findToolStripMenuItem.DropDownOpening += findToolStripMenuItem_DropDownOpening;
        // 
        // searchToolStripMenuItem
        // 
        searchToolStripMenuItem.Name = "searchToolStripMenuItem";
        searchToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F;
        searchToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
        searchToolStripMenuItem.Text = "Search...";
        searchToolStripMenuItem.Click += searchToolStripMenuItem_Click;
        // 
        // searchAgainToolStripMenuItem
        // 
        searchAgainToolStripMenuItem.Name = "searchAgainToolStripMenuItem";
        searchAgainToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
        searchAgainToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
        searchAgainToolStripMenuItem.Text = "Search again";
        searchAgainToolStripMenuItem.Click += searchAgainToolStripMenuItem_Click;
        // 
        // searchCancelToolStripMenuItem
        // 
        searchCancelToolStripMenuItem.Name = "searchCancelToolStripMenuItem";
        searchCancelToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Q;
        searchCancelToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
        searchCancelToolStripMenuItem.Text = "Cancel search";
        searchCancelToolStripMenuItem.Click += searchCancelToolStripMenuItem_Click;
        // 
        // toolStripSeparator2
        // 
        toolStripSeparator2.Name = "toolStripSeparator2";
        toolStripSeparator2.Size = new System.Drawing.Size(187, 6);
        // 
        // gotoToolStripMenuItem
        // 
        gotoToolStripMenuItem.Name = "gotoToolStripMenuItem";
        gotoToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G;
        gotoToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
        gotoToolStripMenuItem.Text = "Goto...";
        gotoToolStripMenuItem.Click += gotoToolStripMenuItem_Click;
        // 
        // optionsToolStripMenuItem
        // 
        optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { preferencesToolStripMenuItem });
        optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
        optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
        optionsToolStripMenuItem.Text = "Options";
        // 
        // preferencesToolStripMenuItem
        // 
        preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
        preferencesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
        preferencesToolStripMenuItem.Text = "Preferences...";
        preferencesToolStripMenuItem.Click += preferencesToolStripMenuItem_Click;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { progressBar, statusText, cursorText });
        statusStrip.Location = new System.Drawing.Point(0, 391);
        statusStrip.Name = "statusStrip";
        statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
        statusStrip.Size = new System.Drawing.Size(666, 22);
        statusStrip.TabIndex = 3;
        statusStrip.Text = "statusStrip1";
        // 
        // progressBar
        // 
        progressBar.Maximum = 1000;
        progressBar.Name = "progressBar";
        progressBar.Size = new System.Drawing.Size(117, 18);
        progressBar.Visible = false;
        // 
        // statusText
        // 
        statusText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        statusText.Name = "statusText";
        statusText.Size = new System.Drawing.Size(609, 17);
        statusText.Spring = true;
        statusText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // cursorText
        // 
        cursorText.Name = "cursorText";
        cursorText.Size = new System.Drawing.Size(40, 17);
        cursorText.Text = "cursor";
        // 
        // textControl
        // 
        textControl.AutoScroll = true;
        textControl.BackColor = System.Drawing.SystemColors.Control;
        textControl.Dock = System.Windows.Forms.DockStyle.Fill;
        textControl.Font = new System.Drawing.Font("Consolas", 10F);
        textControl.ForeColor = System.Drawing.SystemColors.ControlText;
        textControl.Location = new System.Drawing.Point(0, 24);
        textControl.Name = "textControl";
        textControl.Padding = new System.Windows.Forms.Padding(5);
        textControl.Size = new System.Drawing.Size(666, 367);
        textControl.TabIndex = 4;
        textControl.TabSize = 4;
        // 
        // timerStatus
        // 
        timerStatus.Interval = 5000;
        timerStatus.Tick += timerStatus_Tick;
        // 
        // HFViewer
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(666, 413);
        Controls.Add(textControl);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
        KeyPreview = true;
        MainMenuStrip = menuStrip;
        Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        MinimumSize = new System.Drawing.Size(350, 300);
        Name = "HFViewer";
        Text = "HugeViewer";
        Activated += HFViewer_Activated;
        Deactivate += HFViewer_Deactivate;
        FormClosing += HFViewer_FormClosing;
        Load += HFViewer_Load;
        Enter += HFViewer_Enter;
        Leave += HFViewer_Leave;
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private System.Windows.Forms.MenuStrip menuStrip;
    private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem findToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem searchAgainToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    private System.Windows.Forms.ToolStripMenuItem gotoToolStripMenuItem;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripProgressBar progressBar;
    private System.Windows.Forms.ToolStripStatusLabel statusText;
    private System.Windows.Forms.ToolStripStatusLabel cursorText;
    private System.Windows.Forms.OpenFileDialog openFileDialog;
    private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem searchCancelToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemClose;
    private TextControl textControl;
    private System.Windows.Forms.Timer timerStatus;
}

