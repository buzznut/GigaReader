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
        SuspendLayout();
        // 
        // timerSetStatus
        // 
        timerSetStatus.Tick += timerSetStatus_Tick;
        // 
        // timerCursor
        // 
        timerCursor.Enabled = true;
        timerCursor.Interval = 500;
        timerCursor.Tick += timerCursor_Tick;
        // 
        // TextControl
        // 
        AutoScroll = true;
        Name = "TextControl";
        Padding = new System.Windows.Forms.Padding(5);
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
}
