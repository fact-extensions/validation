using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    public partial class TestForm1 : Form
    {
        public TestForm1()
        {
            InitializeComponent();

            var f = new FieldStatus("field1", null);
            var b = new Experimental.Binder<string>(f);

            // TODO: Do integer conversion
            b.Assert().IsTrue(x => x == "hi", "Must be 'hi'");

            b.Bind(txtEntry1);
        }
    }
}
