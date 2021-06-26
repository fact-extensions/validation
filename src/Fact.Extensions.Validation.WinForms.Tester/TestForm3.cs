using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Fact.Extensions.Validation.Experimental;
using Fact.Extensions.Validation.WinForms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    public partial class TestForm3 : Form
    {
        readonly IServiceProvider services;

        public TestForm3()
        {
            InitializeComponent();
        }


        public TestForm3(IServiceProvider services) : this()
        {
            this.services = services;

            Initialize();
        }


        void Initialize()
        {
            var ab = new AggregatedBinder3(services);

            //ab.BindText
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
