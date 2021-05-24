
namespace Fact.Extensions.Validation.WinForms.Tester
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnTestForm1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnTestForm1
            // 
            this.btnTestForm1.Location = new System.Drawing.Point(12, 12);
            this.btnTestForm1.Name = "btnTestForm1";
            this.btnTestForm1.Size = new System.Drawing.Size(75, 23);
            this.btnTestForm1.TabIndex = 0;
            this.btnTestForm1.Text = "TestForm1";
            this.btnTestForm1.UseVisualStyleBackColor = true;
            this.btnTestForm1.Click += new System.EventHandler(this.btnTestForm1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnTestForm1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnTestForm1;
    }
}

