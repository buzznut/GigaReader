//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

namespace HugeFileReader
{
    partial class Goto
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Goto));
            labelLineNumber = new System.Windows.Forms.Label();
            tbLine = new System.Windows.Forms.TextBox();
            bOkay = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // labelLineNumber
            // 
            labelLineNumber.AutoSize = true;
            labelLineNumber.Location = new System.Drawing.Point(15, 15);
            labelLineNumber.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelLineNumber.Name = "labelLineNumber";
            labelLineNumber.Size = new System.Drawing.Size(161, 15);
            labelLineNumber.TabIndex = 0;
            labelLineNumber.Text = "Line number (1-$MaxRows$):";
            // 
            // tbLine
            // 
            tbLine.Location = new System.Drawing.Point(15, 33);
            tbLine.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tbLine.Name = "tbLine";
            tbLine.Size = new System.Drawing.Size(222, 23);
            tbLine.TabIndex = 1;
            tbLine.TextChanged += tbLine_TextChanged;
            // 
            // bOkay
            // 
            bOkay.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bOkay.DialogResult = System.Windows.Forms.DialogResult.OK;
            bOkay.Location = new System.Drawing.Point(197, 63);
            bOkay.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bOkay.Name = "bOkay";
            bOkay.Size = new System.Drawing.Size(40, 23);
            bOkay.TabIndex = 2;
            bOkay.Text = "Go";
            bOkay.UseVisualStyleBackColor = true;
            // 
            // Goto
            // 
            AcceptButton = bOkay;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(244, 94);
            Controls.Add(bOkay);
            Controls.Add(tbLine);
            Controls.Add(labelLineNumber);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(260, 133);
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(260, 133);
            Name = "Goto";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Goto Line";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelLineNumber;
        private System.Windows.Forms.TextBox tbLine;
        private System.Windows.Forms.Button bOkay;
    }
}
