using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    using Experimental;

    public partial class TestForm1 : Form
    {
        readonly BinderManager binderManager;

        public TestForm1()
        {
            InitializeComponent();

            var bm = new BinderManager(null);
            var gb = new GroupBinder();

            bm.Add(gb);

            Binder<string> b = bm.BindText<Control, string>(txtEntry1, "field1");

            gb.Add(b.Field);

            // TODO: Do integer conversion
            b.Assert().
                IsTrue(x => x == "hi", "Must be 'hi'").
                Required();

            b = bm.BindText<Control, string>(txtEntry2, "field2");

            b.Assert().IsTrue(x => x != "hi2", "Cannot be 'hi2'");

            gb.Add(b.Field);

            gb.Validate += (_, c) =>
            {
                var f1 = gb["field1"];
                var f2 = gb["field2"];

                var f1value = (string)f1.Value;
                var f2value = (string)f2.Value;

                if(f1value.Equals(f2value))
                {
                    // FIX: These are not registering with the Binder<string> fields
                    f1.Error("Cannot equal field2");
                    f2.Error("Cannot equal field1");
                }
            };

            bm.BindOkButton(btnOK);
            bm.SetupTooltip(this);
            bm.Prep();

            bm.Validated += Bm_Validated;

            binderManager = bm;
        }

        private void Bm_Validated()
        {
            lstStatus.Items.Clear();
            var statuses = binderManager.Fields.
                Where(x => x.Statuses.Any()).
                Select(x =>
                {
                    var statuses = string.Join(", ", x.Statuses.Select(y => y.ToString()));
                    return $"{x.Name}: {statuses}";
                }).ToArray();
            lstStatus.Items.AddRange(statuses);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            binderManager.Evaluate();
            if (!binderManager.Fields.SelectMany(f => f.Statuses).Any())
                DialogResult = DialogResult.OK;
            else
            {
                // TODO: Let them know...
            }
        }
    }
}
