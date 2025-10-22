//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

namespace HugeFileReader;

partial class SearchCancel
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchCancel));
        label1 = new System.Windows.Forms.Label();
        tbSearchLine = new System.Windows.Forms.TextBox();
        bStop = new System.Windows.Forms.Button();
        timerLine = new System.Windows.Forms.Timer(components);
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new System.Drawing.Point(15, 15);
        label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(57, 15);
        label1.TabIndex = 0;
        label1.Text = "Lines left:";
        // 
        // tbSearchLine
        // 
        tbSearchLine.Location = new System.Drawing.Point(93, 12);
        tbSearchLine.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        tbSearchLine.Name = "tbSearchLine";
        tbSearchLine.ReadOnly = true;
        tbSearchLine.Size = new System.Drawing.Size(116, 23);
        tbSearchLine.TabIndex = 1;
        // 
        // bStop
        // 
        bStop.Location = new System.Drawing.Point(229, 8);
        bStop.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        bStop.Name = "bStop";
        bStop.Size = new System.Drawing.Size(88, 27);
        bStop.TabIndex = 2;
        bStop.Text = "Stop";
        bStop.UseVisualStyleBackColor = true;
        bStop.Click += bStop_Click;
        // 
        // timerLine
        // 
        timerLine.Tick += timerLine_Tick;
        // 
        // SearchCancel
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(332, 48);
        Controls.Add(bStop);
        Controls.Add(tbSearchLine);
        Controls.Add(label1);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
        Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SearchCancel";
        ShowInTaskbar = false;
        StartPosition = System.Windows.Forms.FormStartPosition.Manual;
        Text = "Stop Find";
        TopMost = true;
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox tbSearchLine;
    private System.Windows.Forms.Button bStop;
    private System.Windows.Forms.Timer timerLine;
}
