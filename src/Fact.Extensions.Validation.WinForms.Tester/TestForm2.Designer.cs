﻿
namespace Fact.Extensions.Validation.WinForms.Tester
{
    partial class TestForm2
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnOK = new System.Windows.Forms.Button();
            this.txtEntry1 = new System.Windows.Forms.TextBox();
            this.lstStatus = new System.Windows.Forms.ListBox();
            this.txtEntry2 = new System.Windows.Forms.TextBox();
            this.lstEntry1 = new System.Windows.Forms.ListBox();
            this.txtPass1 = new System.Windows.Forms.TextBox();
            this.txtPass2 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(701, 402);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(87, 36);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // txtEntry1
            // 
            this.txtEntry1.Location = new System.Drawing.Point(12, 12);
            this.txtEntry1.Name = "txtEntry1";
            this.txtEntry1.Size = new System.Drawing.Size(100, 23);
            this.txtEntry1.TabIndex = 1;
            // 
            // lstStatus
            // 
            this.lstStatus.FormattingEnabled = true;
            this.lstStatus.ItemHeight = 15;
            this.lstStatus.Location = new System.Drawing.Point(12, 220);
            this.lstStatus.Name = "lstStatus";
            this.lstStatus.Size = new System.Drawing.Size(776, 94);
            this.lstStatus.TabIndex = 2;
            this.lstStatus.TabStop = false;
            // 
            // txtEntry2
            // 
            this.txtEntry2.Location = new System.Drawing.Point(12, 41);
            this.txtEntry2.Name = "txtEntry2";
            this.txtEntry2.Size = new System.Drawing.Size(100, 23);
            this.txtEntry2.TabIndex = 3;
            // 
            // lstEntry1
            // 
            this.lstEntry1.FormattingEnabled = true;
            this.lstEntry1.ItemHeight = 15;
            this.lstEntry1.Items.AddRange(new object[] {
            "Hello World!",
            "Item 1",
            "Item 2"});
            this.lstEntry1.Location = new System.Drawing.Point(118, 12);
            this.lstEntry1.Name = "lstEntry1";
            this.lstEntry1.Size = new System.Drawing.Size(120, 94);
            this.lstEntry1.TabIndex = 4;
            // 
            // txtPass1
            // 
            this.txtPass1.Location = new System.Drawing.Point(244, 12);
            this.txtPass1.Name = "txtPass1";
            this.txtPass1.Size = new System.Drawing.Size(100, 23);
            this.txtPass1.TabIndex = 5;
            // 
            // txtPass2
            // 
            this.txtPass2.Location = new System.Drawing.Point(244, 41);
            this.txtPass2.Name = "txtPass2";
            this.txtPass2.Size = new System.Drawing.Size(100, 23);
            this.txtPass2.TabIndex = 6;
            // 
            // TestForm2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtPass2);
            this.Controls.Add(this.txtPass1);
            this.Controls.Add(this.lstEntry1);
            this.Controls.Add(this.txtEntry2);
            this.Controls.Add(this.lstStatus);
            this.Controls.Add(this.txtEntry1);
            this.Controls.Add(this.btnOK);
            this.Name = "TestForm2";
            this.Text = "TestForm2";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox txtEntry1;
        private System.Windows.Forms.ListBox lstStatus;
        private System.Windows.Forms.TextBox txtEntry2;
        private System.Windows.Forms.ListBox lstEntry1;
        private System.Windows.Forms.TextBox txtPass1;
        private System.Windows.Forms.TextBox txtPass2;
    }
}