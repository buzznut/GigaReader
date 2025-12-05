//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
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
        try
        {
            long row = Convert.ToInt64(tbLine.Text);
            bOkay.Enabled = row >= 1 && row <= maxRows;
        }
        catch
        {
            bOkay.Enabled = false;
        }
    }
}
