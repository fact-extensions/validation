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
        //readonly
            BinderManager2 binderManager;

        public IServiceProvider Services { get; set; }

        public TestForm2()
        {
            InitializeComponent();

            // So that services can initialize
            Load += TestForm2_Load;
        }

        private void TestForm2_Load(object sender, EventArgs e)
        {
            binderManager = new BinderManager2(Services);

            var fm = binderManager.BindText(txtEntry1);

            fm.Convert<int>().
                GreaterThan(20);

            var fb = binderManager.BindText2(txtEntry2);

            fb.Convert<int>()
                .LessThan(5);

            binderManager.Validated += BinderManager_Validated;
            binderManager.Validated += BinderManager_Validated1;
        }

        public TestForm2(IServiceProvider services) : this()
        {
            Services = services;
        }

        private void BinderManager_Validated1()
        {
            var hasStatus = binderManager.Fields().SelectMany(x => x.Statuses).Any();

            btnOK.Enabled = !hasStatus;
        }

        private void BinderManager_Validated()
        {
            lstStatus.Items.Clear();
            var statuses = binderManager.Fields().
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
            DialogResult = DialogResult.OK;
        }
    }
}
