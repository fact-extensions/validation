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
    using Synthetic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class TestForm4 : Form, 
        IServiceProviderProvider
    {
        public IServiceProvider Services { get; }
        readonly AggregatedBinder aggregatedBinder;

        UsAddress address = new UsAddress();
        User user = new User();

        public TestForm4(IServiceProvider services) : this()
        {
            Services = services;
            aggregatedBinder = new AggregatedBinder(Services);
            _ = InitValidation();
        }

        public TestForm4()
        {
            InitializeComponent();
        }


        async Task InitValidation()
        {
            // TODO: Do this with 3 aggregators (one parent, two children) instead of one unified one

            address.City = "Los Angeles";

            var addressBinder = aggregatedBinder.Entity(address);

            addressBinder.BindText(txtAddress1, x => x.Street1);
            addressBinder.BindText(txtCity, x => x.City);

            var profileBinder = aggregatedBinder.Entity(user);

            var fbPassword1 = profileBinder.BindText(txtPassword1, x => x.Password1);
            var fbPassword2 = profileBinder.BindText(txtPassword2, x => x.Password2);

            // FIX: This doesn't instantly turn the other field red/pink
            fbPassword1.IsMatch(fbPassword2);

            aggregatedBinder.BindersProcessed += AggregatedBinder_BindersProcessed;

            try
            {
                var inputContext = new InputContext
                {
                    InitiatingEvent = InitiatingEvents.Load,
                    InteractionLevel = Interaction.Low,
                    // This magic line lets underlying system do gaurunteed UI-thread operations
                    // before window handle is created
                    UiContext = System.Threading.SynchronizationContext.Current
                };

                await aggregatedBinder.Process(inputContext);
            }
            catch(Exception e)
            {

            }
        }

        // DEBT: This event is so far always generated on the UI thread, but that may change
        private void AggregatedBinder_BindersProcessed(IEnumerable<IBinderProvider> binders, IFieldContext context)
        {
            var fieldsWithStatuses = aggregatedBinder.Fields().
                Where(x => x.Statuses.Any());
            btnOK.Enabled = !fieldsWithStatuses.Any();
        }

        private async void btnOK_Click(object sender, EventArgs e)
        {
            await aggregatedBinder.Committer.DoCommit();

            DialogResult = DialogResult.OK;
        }
    }
}
