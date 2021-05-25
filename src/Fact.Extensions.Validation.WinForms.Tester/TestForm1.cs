using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    using Experimental;

    public partial class TestForm1 : Form
    {
        public TestForm1()
        {
            InitializeComponent();

            var bm = new BinderManager(null);
            var gb = new GroupBinder();

            Binder<string> b = bm.BindText<Control, string>(txtEntry1, "field1");

            gb.Add(b);

            // TODO: Do integer conversion
            b.Assert().
                IsTrue(x => x == "hi", "Must be 'hi'").
                Required();

            b = bm.BindText<Control, string>(txtEntry2, "field2");

            b.Assert().IsTrue(x => x != "hi", "Cannot be 'hi'");

            gb.Add(b);

            gb.Validate += (_, c) =>
            {
                var f1 = gb["field1"];
                var f2 = gb["field2"];
            };

            bm.BindOkButton(btnOK);
            bm.SetupTooltip(this);
            bm.Prep();
        }
    }
}
