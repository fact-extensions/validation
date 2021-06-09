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
    public class BinderManager2 : AggregatedBinder
        //: BinderManagerBase
    {
        /// <summary>
        /// Occurs after interactive validation, whether it generated new status or not
        /// </summary>
        public event Action Validated;

        readonly StyleManager styleManager = new StyleManager();

        public BinderManager2(IServiceProvider services) //: base(services)
            : base(new FieldStatus("test", null), services)
        {
        }


        public FluentBinder2<T> Add<TControl, T>(IBinder2<T> binder, TControl control, 
            //Func<TControl, T> getter, 
            Tracker<T> tracker,
            out BinderManagerBase.Item<T> item)
            where TControl: Control
        {
            //binder.getter = () => getter(control);
            //binder.getter2 = () => getter(control);
            // DEBT: "initial value" needs more work, but coming along
            var fb = new FluentBinder2<T>(binder, true);

            item = new BinderManagerBase.Item<T>(fb, control, tracker);

            Add(item);

            BinderManagerBase.Item<T> item2 = item;

            control.GotFocus += (s, e) => styleManager.FocusGained(item2);
            control.LostFocus += (s, e) => styleManager.FocusLost(item2);


            return fb;
        }


        public FluentBinder2<string> AddText(IBinder2<string> binder, Control control, Tracker<string> tracker)
        {
            //var fb = Add(binder, control, c => c.Text, out Item<string> item);
            var fb = Add(binder, control, tracker, out BinderManagerBase.Item<string> item);

            control.TextChanged += async (s, e) =>
            {
                item.tracked.Value = control.Text;
                await binder.Process();

                Validated?.Invoke();

                styleManager.ContentChanged(item);

                //OnEvaluate(item, hasStatus);
            };
            return fb;
        }


        public FluentBinder2<string> BindText(Control control)
        {
            var f = new FieldStatus<string>(control.Name, null);
            var tracker = new Tracker<string>(control.Text);
            var binder = new Binder2<string>(f, () => tracker.Value);
            tracker.Updated += (v, c) => f.Value = v;

            return AddText(binder, control, tracker);
        }
    }


    public interface IStyleManager
    {

    }

    public class StyleManager : IStyleManager
    {
        BinderManagerBase.ColorOptions colorOptions = new BinderManagerBase.ColorOptions();

        public void ContentChanged(BinderManagerBase.Item item)
        {
            bool hasStatus = item.binder.Field.Statuses.Any();

            item.control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }


        public void FocusLost(BinderManagerBase.Item item)
        {
            bool hasStatus = item.binder.Field.Statuses.Any();

            item.control.BackColor = hasStatus ?
                colorOptions.UnfocusedStatus :
                colorOptions.ClearedStatus;
        }


        public void FocusGained(BinderManagerBase.Item item)
        {
            bool hasStatus = item.binder.Field.Statuses.Any();

            item.control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }
    }
}
