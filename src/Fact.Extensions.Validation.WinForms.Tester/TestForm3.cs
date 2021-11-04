using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Fact.Extensions.Validation.Experimental;
using Fact.Extensions.Validation.WinForms;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    public partial class TestForm3 : Form
    {
        readonly IServiceProvider services;
        readonly AggregatedBinder aggregatedBinder;

        public TestForm3()
        {
            InitializeComponent();
        }


        public TestForm3(IServiceProvider services) : this()
        {
            this.services = services;

            aggregatedBinder = new AggregatedBinder(services);

            Initialize().Wait();
        }

        async Task Initialize()
        {
            // FIX: Abort processing not quite right, so Required() not functioning properly

            aggregatedBinder.BindText(txtEntry1).
                Required().
                Convert<int>().GreaterThan(10);

            aggregatedBinder.BindText(txtEntry2).
                Required().
                Convert<int>().LessThan(5);

            aggregatedBinder.BindersProcessed += AggregatedBinder_BindersProcessed;

            await aggregatedBinder.Process();
        }

        private void AggregatedBinder_BindersProcessed(IEnumerable<IBinderProvider> binders, Context2 context)
        {
            // Although we are handled only the binders which are affected, we process them all
            // to see what overall status is each time.  Could be optimized.

            var fields = aggregatedBinder.Fields();

            lstStatuses.Items.Clear();
            var statuses = fields.
                Where(x => x.Statuses.Any()).
                Select(x =>
                {
                    var statuses = string.Join(", ", x.Statuses.Select(y => y.ToString()));
                    return new ListViewItem(new[] { x.Name, statuses });
                }).ToArray();
            lstStatuses.Items.AddRange(statuses);
            btnOK.Enabled = statuses.Length == 0;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
