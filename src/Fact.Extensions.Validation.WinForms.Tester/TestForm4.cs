using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    public partial class TestForm4 : Form, 
        IServiceProviderProvider
    {
        public IServiceProvider Services { get; }

        public TestForm4(IServiceProvider services)
        {
            Services = services;
        }

        public TestForm4()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {

        }
    }
}
