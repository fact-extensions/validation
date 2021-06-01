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
        /// <summary>
        /// Occurs after interactive validation, whether it generated new status or not
        /// </summary>
        public event Action Validated;


        public BinderManager2(IServiceProvider services) : base(services)
        {

        }


        public FluentBinder2<T> Add<TControl, T>(Binder2<T> binder, TControl control, Func<TControl, T> getter, out Item<T> item)
            where TControl: Control
        {
            binder.getter = () => getter(control);
            binder.getter2 = () => getter(control);
            item = new Item<T>()
            {
                binder = binder,
                control = control,
                initialValue = getter(control)
            };

            binders.Add(item);

            Item<T> item2 = item;

            control.GotFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ?
                    (item2.modified ? options.Color.FocusedStatus : options.Color.InitialStatus) :
                    options.Color.ClearedStatus;
            };


            control.LostFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ?
                    options.Color.UnfocusedStatus :
                    options.Color.ClearedStatus;
            };


            // DEBT: "initial value" needs more work, but coming along
            var fb = new FluentBinder2<T>(binder, true);
            return fb;
        }


        public FluentBinder2<string> AddText(Binder2<string> binder, Control control)
        {
            var fb = Add(binder, control, c => c.Text, out Item<string> item);
            bool touched = false;
            Color initialAlertColor = options.Color.InitialStatus;
            Color inputAlertColor = options.Color.FocusedStatus;
            Color clearColor = options.Color.ClearedStatus;

            control.TextChanged += async (s, e) =>
            {
                await binder.Process();

                Validated?.Invoke();

                bool hasStatus = binder.Field.Statuses.Any();

                item.modified = !item.initialValue.Equals(control.Text);
                touched = true;

                //OnEvaluate(item, hasStatus);

                control.BackColor = hasStatus ?
                    (item.modified ? inputAlertColor : initialAlertColor) :
                    clearColor;
            };
            return fb;
        }


        public FluentBinder2<string> BindText(Control control)
        {
            var f = new FieldStatus<string>(control.Name, null);
            return AddText(new Binder2<string>(f), control);
        }
    }
}
