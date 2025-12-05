//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.ComponentModel;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
public partial class FindText : Form
{
    bool isDown = true;

    public FindText ()
    {
        InitializeComponent ();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SearchText
    {
        get { return tbText.Text; }
        set { tbText.Text = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseCase { get { return cbUseCase.Checked; } set { cbUseCase.Checked = value; } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SearchDown { get { return isDown; } }

    private void bUp_Click (object sender, EventArgs e)
    {
        isDown = false;
        Close ();
    }

    private void bDown_Click (object sender, EventArgs e)
    {
        isDown = true;
        Close ();
    }
}
