//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

namespace HugeFileReader;

partial class Preferences
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Preferences));
        label1 = new System.Windows.Forms.Label();
        tbTabSize = new System.Windows.Forms.TextBox();
        bCancel = new System.Windows.Forms.Button();
        bOkay = new System.Windows.Forms.Button();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new System.Drawing.Point(15, 15);
        label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(51, 15);
        label1.TabIndex = 0;
        label1.Text = "Tab size:";
        // 
        // tbTabSize
        // 
        tbTabSize.Location = new System.Drawing.Point(82, 8);
        tbTabSize.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        tbTabSize.Name = "tbTabSize";
        tbTabSize.Size = new System.Drawing.Size(116, 23);
        tbTabSize.TabIndex = 1;
        // 
        // bCancel
        // 
        bCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        bCancel.Location = new System.Drawing.Point(178, 59);
        bCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        bCancel.Name = "bCancel";
        bCancel.Size = new System.Drawing.Size(88, 27);
        bCancel.TabIndex = 3;
        bCancel.Text = "Cancel";
        bCancel.UseVisualStyleBackColor = true;
        // 
        // bOkay
        // 
        bOkay.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        bOkay.DialogResult = System.Windows.Forms.DialogResult.OK;
        bOkay.Location = new System.Drawing.Point(83, 59);
        bOkay.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        bOkay.Name = "bOkay";
        bOkay.Size = new System.Drawing.Size(88, 27);
        bOkay.TabIndex = 2;
        bOkay.Text = "OK";
        bOkay.UseVisualStyleBackColor = true;
        bOkay.Click += bOkay_Click;
        // 
        // Preferences
        // 
        AcceptButton = bOkay;
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        CancelButton = bCancel;
        ClientSize = new System.Drawing.Size(280, 99);
        Controls.Add(bCancel);
        Controls.Add(bOkay);
        Controls.Add(tbTabSize);
        Controls.Add(label1);
        Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
        Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "Preferences";
        ShowInTaskbar = false;
        Text = "Preferences";
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox tbTabSize;
    private System.Windows.Forms.Button bCancel;
    private System.Windows.Forms.Button bOkay;
}
