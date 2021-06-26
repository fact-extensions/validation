
namespace Fact.Extensions.Validation.WinForms.Tester
{
    partial class TestForm3
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
            this.lstStatuses = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(566, 382);
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
            // lstStatuses
            // 
            this.lstStatuses.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.lstStatuses.HideSelection = false;
            this.lstStatuses.Location = new System.Drawing.Point(12, 203);
            this.lstStatuses.Name = "lstStatuses";
            this.lstStatuses.Size = new System.Drawing.Size(354, 97);
            this.lstStatuses.TabIndex = 3;
            this.lstStatuses.UseCompatibleStateImageBehavior = false;
            this.lstStatuses.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Field";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Status";
            this.columnHeader2.Width = 250;
            // 
            // TestForm3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.lstStatuses);
            this.Controls.Add(this.txtEntry2);
            this.Controls.Add(this.txtEntry1);
            this.Controls.Add(this.btnOK);
            this.Name = "TestForm3";
            this.Text = "TestForm3";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox txtEntry1;
        private System.Windows.Forms.TextBox txtEntry2;
        private System.Windows.Forms.ListView lstStatuses;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
    }
}