//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

namespace HugeFileReader;

partial class FindText
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindText));
        bCancel = new System.Windows.Forms.Button();
        bDown = new System.Windows.Forms.Button();
        tbText = new System.Windows.Forms.TextBox();
        label1 = new System.Windows.Forms.Label();
        cbUseCase = new System.Windows.Forms.CheckBox();
        bUp = new System.Windows.Forms.Button();
        SuspendLayout();
        // 
        // bCancel
        // 
        bCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        bCancel.Location = new System.Drawing.Point(372, 54);
        bCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        bCancel.Name = "bCancel";
        bCancel.Size = new System.Drawing.Size(88, 27);
        bCancel.TabIndex = 5;
        bCancel.Text = "Cancel";
        bCancel.UseVisualStyleBackColor = true;
        // 
        // bDown
        // 
        bDown.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        bDown.DialogResult = System.Windows.Forms.DialogResult.OK;
        bDown.Location = new System.Drawing.Point(278, 54);
        bDown.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        bDown.Name = "bDown";
        bDown.Size = new System.Drawing.Size(88, 27);
        bDown.TabIndex = 4;
        bDown.Text = "Down";
        bDown.UseVisualStyleBackColor = true;
        bDown.Click += bDown_Click;
        // 
        // tbText
        // 
        tbText.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        tbText.Location = new System.Drawing.Point(57, 14);
        tbText.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        tbText.Name = "tbText";
        tbText.Size = new System.Drawing.Size(402, 23);
        tbText.TabIndex = 1;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new System.Drawing.Point(14, 20);
        label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(31, 15);
        label1.TabIndex = 0;
        label1.Text = "Text:";
        // 
        // cbUseCase
        // 
        cbUseCase.AutoSize = true;
        cbUseCase.Location = new System.Drawing.Point(57, 59);
        cbUseCase.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        cbUseCase.Name = "cbUseCase";
        cbUseCase.Size = new System.Drawing.Size(104, 19);
        cbUseCase.TabIndex = 2;
        cbUseCase.Text = "Case sensitive?";
        cbUseCase.UseVisualStyleBackColor = true;
        // 
        // bUp
        // 
        bUp.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        bUp.DialogResult = System.Windows.Forms.DialogResult.OK;
        bUp.Location = new System.Drawing.Point(183, 54);
        bUp.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        bUp.Name = "bUp";
        bUp.Size = new System.Drawing.Size(88, 27);
        bUp.TabIndex = 3;
        bUp.Text = "Up";
        bUp.UseVisualStyleBackColor = true;
        bUp.Click += bUp_Click;
        // 
        // SearchText
        // 
        AcceptButton = bDown;
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        CancelButton = bCancel;
        ClientSize = new System.Drawing.Size(474, 89);
        Controls.Add(bUp);
        Controls.Add(cbUseCase);
        Controls.Add(bCancel);
        Controls.Add(bDown);
        Controls.Add(tbText);
        Controls.Add(label1);
        Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
        Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new System.Drawing.Size(67, 63);
        Name = "SearchText";
        ShowInTaskbar = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Find";
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button bCancel;
    private System.Windows.Forms.Button bDown;
    private System.Windows.Forms.TextBox tbText;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.CheckBox cbUseCase;
    private System.Windows.Forms.Button bUp;
}
