//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System.Runtime.Versioning;
using System.Windows.Forms;
using System.ComponentModel;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
public partial class Preferences : Form
{
    public Preferences()
    {
        InitializeComponent();

        tbTabSize.Text = TabSize.ToString();
    }

    public int TabSize = 4;

    private void bOkay_Click(object sender, System.EventArgs e)
    {
        TabSize = int.TryParse(tbTabSize.Text, out int tabSize) ? tabSize : 4;
    }
}
