using HugeFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HugeFileReader;

partial class TextControl
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
            // Dispose managed resources
            brushNormalBackground?.Dispose();
            brushNormalText?.Dispose();
            brushSelectionBackground?.Dispose();
            brushSelectionText?.Dispose();

            lines?.Dispose();
            lines = null;
        }

        // Dispose unmanaged resources if any here
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        timerSetStatus = new System.Windows.Forms.Timer(components);
        timerCursor = new System.Windows.Forms.Timer(components);
        timerStateHandler = new System.Windows.Forms.Timer(components);
        vScrollBar = new System.Windows.Forms.VScrollBar();
        hScrollBar = new System.Windows.Forms.HScrollBar();
        SuspendLayout();
        // 
        // timerSetStatus
        // 
        timerSetStatus.Interval = 50;
        timerSetStatus.Tick += timerSetStatus_Tick;
        // 
        // timerCursor
        // 
        timerCursor.Enabled = true;
        timerCursor.Interval = 500;
        timerCursor.Tick += timerCursor_Tick;
        // 
        // timerStateHandler
        // 
        timerStateHandler.Enabled = true;
        timerStateHandler.Tick += stateHandler_Tick;
        // 
        // vScrollBar
        // 
        vScrollBar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        vScrollBar.Location = new System.Drawing.Point(850, -5);
        vScrollBar.Name = "vScrollBar";
        vScrollBar.Size = new System.Drawing.Size(13, 448);
        vScrollBar.TabIndex = 1;
        vScrollBar.Scroll += ScrollEvent;
        vScrollBar.Enter += vScrollBar_Enter;
        vScrollBar.KeyDown += vScrollBar_KeyDown;
        vScrollBar.KeyPress += vScrollBar_KeyPress;
        vScrollBar.KeyUp += vScrollBar_KeyUp;
        vScrollBar.PreviewKeyDown += vScrollBar_PreviewKeyDown;
        // 
        // hScrollBar
        // 
        hScrollBar.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        hScrollBar.Location = new System.Drawing.Point(-5, 444);
        hScrollBar.Name = "hScrollBar";
        hScrollBar.Size = new System.Drawing.Size(854, 13);
        hScrollBar.TabIndex = 2;
        hScrollBar.Scroll += ScrollEvent;
        hScrollBar.KeyDown += hScrollBar_KeyDown;
        hScrollBar.KeyPress += hScrollBar_KeyPress;
        hScrollBar.KeyUp += hScrollBar_KeyUp;
        hScrollBar.PreviewKeyDown += hScrollBar_PreviewKeyDown;
        // 
        // TextControl
        // 
        Controls.Add(hScrollBar);
        Controls.Add(vScrollBar);
        Name = "TextControl";
        Size = new System.Drawing.Size(858, 452);
        Scroll += ScrollEvent;
        SizeChanged += SizeChangedEvent;
        Paint += PaintEvent;
        KeyDown += KeyDownEvent;
        KeyUp += KeyUpEvent;
        MouseClick += MouseClickEvent;
        MouseDoubleClick += MouseDoubleClickEvent;
        MouseDown += MouseDownEvent;
        MouseEnter += MouseEnterEvent;
        MouseLeave += MouseLeaveEvent;
        MouseMove += MouseMoveEvent;
        MouseUp += MouseUpEvent;
        MouseWheel += MouseWheelEvent;
        PreviewKeyDown += PreviewKeyDownEvent;
        Resize += SizeChangedEvent;
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Timer timerSetStatus;
    private System.Windows.Forms.Timer timerCursor;
    private System.Windows.Forms.Timer timerStateHandler;
    private System.Windows.Forms.VScrollBar vScrollBar;
    private System.Windows.Forms.HScrollBar hScrollBar;
}
