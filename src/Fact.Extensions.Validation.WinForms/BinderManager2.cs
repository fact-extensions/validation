using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Fact.Extensions.Validation.Experimental;

namespace Fact.Extensions.Validation.WinForms
{
    public class BinderManager2 : BinderManagerBase
    {
        public BinderManager2(IServiceProvider services) : base(services)
        {

        }


        public FluentBinder2<T> Add<TControl, T>(Binder2 binder, TControl control, Func<TControl, T> getter, out Item item)
            where TControl: Control
        {
            binder.getter = () => getter(control);
            item = new Item()
            {
                binder = binder,
                control = control
            };
            // FIX: This is not even close to a good way to set up "initial value"
            var fb = new FluentBinder2<T>(binder, getter(control));
            return fb;
        }


        public FluentBinder2<string> AddText(Binder2 binder, Control control)
        {
            var fb = Add(binder, control, c => c.Text, out Item item);
            bool modified = false;
            bool touched = false;
            string initialText = control.Text;
            Color initialAlertColor = options.Color.InitialStatus;
            Color inputAlertColor = options.Color.FocusedStatus;
            Color clearColor = options.Color.ClearedStatus;

            control.TextChanged += (s, e) =>
            {
                binder.Process().Wait();

                bool hasStatus = binder.Field.Statuses.Any();

                modified = !initialText.Equals(control.Text);
                item.modified = modified;
                touched = true;

                //OnEvaluate(item, hasStatus);

                control.BackColor = hasStatus ?
                    (modified ? inputAlertColor : initialAlertColor) :
                    clearColor;
            };
            return fb;
        }


        public FluentBinder2<string> BindText(Control control)
        {
            var f = new FieldStatus(control.Name, null);
            return AddText(new Binder2(f), control);
        }
    }
}
