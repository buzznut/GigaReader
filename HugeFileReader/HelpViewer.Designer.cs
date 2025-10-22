//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Drawing;
using System.Windows.Forms;

namespace HugeFileReader;

partial class HelpViewer
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pictureBoxView.Image?.Dispose();

            if ((components != null))
            {
                components.Dispose();
            }

            foreach (IDisposable disposable in disposables)
            {
                disposable?.Dispose();
            }

            //loadWorker.Dispose();
            //loadWorker = null;

            //doc?.Dispose();
            //doc = null;

            //foreach (Image image in images)
            //{
            //    image?.Dispose();
            //}

            //wait?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpViewer));
        pictureBoxView = new PictureBox();
        textBoxPageInfo = new TextBox();
        buttonRight = new Button();
        buttonEnd = new Button();
        buttonLeft = new Button();
        buttonBegin = new Button();
        splitContainerView = new SplitContainer();
        ((System.ComponentModel.ISupportInitialize)pictureBoxView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitContainerView).BeginInit();
        splitContainerView.Panel1.SuspendLayout();
        splitContainerView.Panel2.SuspendLayout();
        splitContainerView.SuspendLayout();
        SuspendLayout();
        // 
        // pictureBoxView
        // 
        pictureBoxView.Location = new Point(1, 2);
        pictureBoxView.Margin = new Padding(0);
        pictureBoxView.Name = "pictureBoxView";
        pictureBoxView.Size = new Size(208, 100);
        pictureBoxView.SizeMode = PictureBoxSizeMode.AutoSize;
        pictureBoxView.TabIndex = 0;
        pictureBoxView.TabStop = false;
        // 
        // textBoxPageInfo
        // 
        textBoxPageInfo.Anchor = AnchorStyles.Bottom;
        textBoxPageInfo.Location = new Point(299, 6);
        textBoxPageInfo.Name = "textBoxPageInfo";
        textBoxPageInfo.ReadOnly = true;
        textBoxPageInfo.ShortcutsEnabled = false;
        textBoxPageInfo.Size = new Size(120, 23);
        textBoxPageInfo.TabIndex = 2;
        textBoxPageInfo.TabStop = false;
        textBoxPageInfo.TextAlign = HorizontalAlignment.Center;
        // 
        // buttonRight
        // 
        buttonRight.Anchor = AnchorStyles.Bottom;
        buttonRight.Location = new Point(423, 6);
        buttonRight.Name = "buttonRight";
        buttonRight.Size = new Size(33, 23);
        buttonRight.TabIndex = 3;
        buttonRight.Text = ">";
        buttonRight.UseVisualStyleBackColor = true;
        buttonRight.Click += buttonRight_Click;
        // 
        // buttonEnd
        // 
        buttonEnd.Anchor = AnchorStyles.Bottom;
        buttonEnd.Location = new Point(462, 6);
        buttonEnd.Name = "buttonEnd";
        buttonEnd.Size = new Size(33, 23);
        buttonEnd.TabIndex = 4;
        buttonEnd.Text = ">>";
        buttonEnd.UseVisualStyleBackColor = true;
        buttonEnd.Click += buttonEnd_Click;
        // 
        // buttonLeft
        // 
        buttonLeft.Anchor = AnchorStyles.Bottom;
        buttonLeft.Location = new Point(262, 6);
        buttonLeft.Name = "buttonLeft";
        buttonLeft.Size = new Size(33, 23);
        buttonLeft.TabIndex = 5;
        buttonLeft.Text = "<";
        buttonLeft.UseVisualStyleBackColor = true;
        buttonLeft.Click += buttonLeft_Click;
        // 
        // buttonBegin
        // 
        buttonBegin.Anchor = AnchorStyles.Bottom;
        buttonBegin.Location = new Point(225, 6);
        buttonBegin.Name = "buttonBegin";
        buttonBegin.Size = new Size(33, 23);
        buttonBegin.TabIndex = 6;
        buttonBegin.Text = "<<";
        buttonBegin.UseVisualStyleBackColor = true;
        buttonBegin.Click += buttonBegin_Click;
        // 
        // splitContainerView
        // 
        splitContainerView.Dock = DockStyle.Fill;
        splitContainerView.FixedPanel = FixedPanel.Panel2;
        splitContainerView.Location = new Point(0, 0);
        splitContainerView.Name = "splitContainerView";
        splitContainerView.Orientation = Orientation.Horizontal;
        // 
        // splitContainerView.Panel1
        // 
        splitContainerView.Panel1.AutoScroll = true;
        splitContainerView.Panel1.Controls.Add(pictureBoxView);
        // 
        // splitContainerView.Panel2
        // 
        splitContainerView.Panel2.Controls.Add(textBoxPageInfo);
        splitContainerView.Panel2.Controls.Add(buttonBegin);
        splitContainerView.Panel2.Controls.Add(buttonEnd);
        splitContainerView.Panel2.Controls.Add(buttonLeft);
        splitContainerView.Panel2.Controls.Add(buttonRight);
        splitContainerView.Size = new Size(702, 270);
        splitContainerView.SplitterDistance = 241;
        splitContainerView.SplitterWidth = 1;
        splitContainerView.TabIndex = 7;
        // 
        // HelpViewer
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(702, 270);
        Controls.Add(splitContainerView);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "HelpViewer";
        Text = "HelpViewer";
        Load += HelpViewer_Load;
        ((System.ComponentModel.ISupportInitialize)pictureBoxView).EndInit();
        splitContainerView.Panel1.ResumeLayout(false);
        splitContainerView.Panel1.PerformLayout();
        splitContainerView.Panel2.ResumeLayout(false);
        splitContainerView.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerView).EndInit();
        splitContainerView.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private PictureBox pictureBoxView;
    private TextBox textBoxPageInfo;
    private Button buttonRight;
    private Button buttonEnd;
    private Button buttonLeft;
    private Button buttonBegin;
    private SplitContainer splitContainerView;
}
