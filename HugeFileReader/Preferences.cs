//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
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
