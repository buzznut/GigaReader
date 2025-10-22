//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
public partial class SearchCancel : Form
{
    private long _line;
    private bool _stop;
    private readonly DateTime _start;
    readonly Point offset;

    public SearchCancel (Form parent)
    {
        offset = parent.Location;
        InitializeComponent ();
        _start = DateTime.Now;
        Visible = false;
        Point pt = offset;
        pt.X += 50;
        pt.Y += 50;

        Screen scr = Screen.FromPoint (offset);
        if (pt.X < scr.Bounds.Left)
        {
            pt.X = scr.Bounds.Left;
        }
        else if (pt.X > scr.Bounds.Right)
        {
            pt.X = scr.Bounds.Right - 50;
        }

        if (pt.Y < scr.Bounds.Top)
        {
            pt.Y = scr.Bounds.Top;
        }
        else if (pt.Y > scr.Bounds.Bottom)
        {
            pt.Y = scr.Bounds.Bottom - 50;
        }

        Location = pt;
        SetDesktopLocation (pt.X, pt.Y);
        StartPosition = FormStartPosition.Manual;

        timerLine.Start ();
    }

    private void bStop_Click (object sender, EventArgs e)
    {
        _stop = true;
    }

    public void CurrentLine (long line)
    {
        _line = line;
        if (_line < 0)
        {
            timerLine.Stop ();
            Close ();
        }
    }

    public bool KeepSearching ()
    {
        return !_stop;
    }

    private void timerLine_Tick (object sender, EventArgs e)
    {
        if ((DateTime.Now - _start) > TimeSpan.FromSeconds (2) && !Visible)
        {
            Show ();
        }

        tbSearchLine.Text = Convert.ToString (_line);
    }
}
