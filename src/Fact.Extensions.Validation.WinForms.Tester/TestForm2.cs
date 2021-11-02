using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    using Fact.Extensions.Validation.Experimental;
    using System.Linq;

    public partial class TestForm2 : Form
    {
        //readonly
            AggregatedBinder3 binderManager;

        public IServiceProvider Services { get; set; }

        public TestForm2()
        {
            InitializeComponent();

            AggregatedBinderExtensions.v3mode = false;

            // So that 'services' can initialize
            Load += TestForm2_Load;
        }

        int? result1, result2;


        private async void TestForm2_Load(object sender, EventArgs e)
        {
            var regLocation = @"Software\Fact\Extensions\Validation\Diagnostic\Test";
            var reg = new RegistryBinder(Microsoft.Win32.RegistryHive.CurrentUser, regLocation, false);

            var summaryField = new FieldStatus("summary");

            var regValue1 = reg.Add("Value1").Required((object v) => v == null);
            var regValue2 = reg.Add("Value2").Required((object v) => v == null).Convert<int>();
            var regVersion = reg.Add("Version").
                Convert<int>().GroupValidate(regValue2, (c, version, v2) =>
                {
                    if (version.Value > 2 && v2.Value > 10)
                        version.Error("Arbitrary error");

                    return new ValueTask();
                });

            // DEBT: Always have to add this after other Adds, but would rather not have to
            reg.AddSummaryProcessor(summaryField);

            // regVersion.Convert<int>() flipping out
            var context = new Context2(null, null, default);
            await reg.Process(context);

            var statuses = summaryField.Statuses.ToArray();

            var field = new FieldStatus("test");
            binderManager = new AggregatedBinder3(Services);

            var fm = binderManager.BindText(txtEntry1);

            fm.Convert<int>().
                GreaterThan(20).
                Commit(v => result1 = v);

            var fb = binderManager.BindText(txtEntry2, 0);

            fb.
                LessThan(5).
                Commit(v => result2 = v);

            binderManager.BindersProcessed += BinderManager_Validated;
            binderManager.BindersProcessed += BinderManager_Validated1;

            var fb2 = binderManager.BindSelectedItem(lstEntry1);
            var fb2str = fb2.Convert<string>().
                Required().
                StartsWith("Item");

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

        private void BinderManager_Validated(IEnumerable<IBinderProvider> binders, Context2 context)
        {
            lstStatus.Items.Clear();
            var fields2 = binderManager.Fields();
            var statuses = fields2.
                Where(x => x.Statuses.Any()).
                Select(x =>
                {
                    var statuses = string.Join(", ", x.Statuses.Select(y => y.ToString()));
                    return $"{x.Name}: {statuses}";
                }).ToArray();
            lstStatus.Items.AddRange(statuses);
        }

        private async void btnOK_Click(object sender, EventArgs e)
        {
            await binderManager.Committer.DoCommit();

            DialogResult = DialogResult.OK;
        }
    }
}
