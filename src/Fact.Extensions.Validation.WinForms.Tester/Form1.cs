using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    public partial class Form1 : Form
    {
        public IServiceProvider Services { get; set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void btnTestForm1_Click(object sender, EventArgs e)
        {
            // NOTE: Phasing out
        }

        private void btnTestForm2_Click(object sender, EventArgs e)
        {
            var form = new TestForm2(Services);
            form.ShowDialog();
        }
    }
}
