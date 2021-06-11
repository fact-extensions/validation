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
            AggregatedBinder binderManager;

        public IServiceProvider Services { get; set; }

        public TestForm2()
        {
            InitializeComponent();

            // So that 'services' can initialize
            Load += TestForm2_Load;
        }

        private async void TestForm2_Load(object sender, EventArgs e)
        {
            var field = new FieldStatus("test", null);
            binderManager = new AggregatedBinder(field, Services);

            var fm = binderManager.BindText(txtEntry1);

            fm.Convert<int>().
                GreaterThan(20);

            var fb = binderManager.BindText(txtEntry2);

            fb.Convert<int>()
                .LessThan(5);

            binderManager.BindersProcessed += BinderManager_Validated;
            binderManager.BindersProcessed += BinderManager_Validated1;

            await binderManager.Process();
        }

        public TestForm2(IServiceProvider services) : this()
        {
            Services = services;
        }

        private void BinderManager_Validated1(IEnumerable<IBinderProvider> fields, Context2 context)
        {
            var hasStatus = binderManager.Fields().SelectMany(x => x.Statuses).Any();

            btnOK.Enabled = !hasStatus;
        }

        private void BinderManager_Validated(IEnumerable<IBinderProvider> fields, Context2 context)
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
