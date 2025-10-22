//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
public partial class Goto : Form
{
    private long maxRows = 0;

    public Goto(long rows)
    {
        maxRows = rows;
        SuspendLayout();
        InitializeComponent();
        labelLineNumber.Text = labelLineNumber.Text.Replace("$MaxRows$", maxRows.ToString());
        ResumeLayout(false);
    }

    public string GetLine()
    {
        return tbLine.Text;
    }

    private void tbLine_TextChanged(object sender, EventArgs e)
    {
        long row = Convert.ToInt64(tbLine.Text);
        bOkay.Enabled = row >= 1 && row <= maxRows;
    }
}
