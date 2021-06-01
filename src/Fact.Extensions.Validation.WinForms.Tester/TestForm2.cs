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
    using System.Linq;

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

            binderManager.Validated += BinderManager_Validated;
        }

        private void BinderManager_Validated()
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
        }
    }
}
