
namespace Fact.Extensions.Validation.WinForms.Tester
{
    partial class TestForm1
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
            this.txtEntry2 = new System.Windows.Forms.TextBox();
            this.lstStatus = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(384, 257);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
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
            // txtEntry2
            // 
            this.txtEntry2.Location = new System.Drawing.Point(12, 41);
            this.txtEntry2.Name = "txtEntry2";
            this.txtEntry2.Size = new System.Drawing.Size(100, 23);
            this.txtEntry2.TabIndex = 2;
            // 
            // lstStatus
            // 
            this.lstStatus.FormattingEnabled = true;
            this.lstStatus.ItemHeight = 15;
            this.lstStatus.Location = new System.Drawing.Point(12, 79);
            this.lstStatus.Name = "lstStatus";
            this.lstStatus.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.lstStatus.Size = new System.Drawing.Size(447, 94);
            this.lstStatus.TabIndex = 3;
            // 
            // TestForm1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(471, 292);
            this.Controls.Add(this.lstStatus);
            this.Controls.Add(this.txtEntry2);
            this.Controls.Add(this.txtEntry1);
            this.Controls.Add(this.btnOK);
            this.Name = "TestForm1";
            this.Text = "TestForm1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox txtEntry1;
        private System.Windows.Forms.TextBox txtEntry2;
        private System.Windows.Forms.ListBox lstStatus;
    }
}