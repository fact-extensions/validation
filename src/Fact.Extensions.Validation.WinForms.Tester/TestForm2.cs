using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    using Fact.Extensions.Validation.Experimental;

    public partial class TestForm2 : Form
    {
        readonly BinderManager2 binderManager;

        public TestForm2()
        {
            InitializeComponent();

            binderManager = new BinderManager2(null);

            var fm = binderManager.BindText(txtEntry1);

            fm.Convert<int>().
                GreaterThan(20);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
        }
    }
}
