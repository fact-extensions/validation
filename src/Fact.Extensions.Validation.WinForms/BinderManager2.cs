using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Fact.Extensions.Validation.Experimental;

using Microsoft.Extensions.DependencyInjection;

namespace Fact.Extensions.Validation.WinForms
{
    public class BinderManager2 : AggregatedBinder
        //: BinderManagerBase
    {
        readonly StyleManager styleManager = new StyleManager();

        public BinderManager2(IServiceProvider services, string name) //: base(services)
            : base(new FieldStatus(name, null), services)
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

                // FIX: Passing null context is a no-no
                FireFieldsProcessed(new[] { item }, null);

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


    public static class AggregatedBinderExtensions
    {
        static IBinderProvider<T> Setup<T>(IAggregatedBinder aggregatedBinder, Control control, Func<T> getter, 
            Action<Tracker<T>> initEvent)
        {
            var services = aggregatedBinder.Services;
            var styleManager = services.GetRequiredService<StyleManager>();
            //var cancellationToken = services.GetService<CancellationToken>(); // Because it's a struct this doesn't work
            // DEBT: Need to feed this cancellationtoken still
            var cancellationToken = new CancellationToken();
            var tracker = new Tracker<T>(getter());
            initEvent(tracker);
            
            IBinderProvider<T> bp = aggregatedBinder.AddField(control.Name, 
                () => tracker.Value, 
                _fb => new BinderManagerBase.Item<T>(_fb, control, tracker));

            var _item = (BinderManagerBase.Item)bp;
            var f = (FieldStatus<T>)bp.Binder.Field;   // DEBT: Sloppy cast
            // DEBT: Move InputContext creation elsewhere since we aren't sure it's a Keystroke etc here
            var inputContext = new InputContext
            {
                InitiatingEvent = InitiatingEvents.Keystroke,
                InteractionLevel = Interaction.High
            };
            tracker.Updated += async (v, c) =>
            {
                f.Value = v;

                await bp.Binder.Process(inputContext, cancellationToken);

                styleManager.ContentChanged(_item);
            };

            control.GotFocus += (s, e) => styleManager.FocusGained(_item);
            control.LostFocus += (s, e) => styleManager.FocusLost(_item);

            // Aggregator-wide init of this particular field so that on any call to
            // aggregatorBinder.Process() current field state style is exactly reflected
            aggregatedBinder.ProcessingAsync += (_, c) =>
            {
                styleManager.Initialize(_item);
                return new ValueTask();
            };

            return bp;
        }

        // UNFINISHED
        public static IFluentBinder<string> BindText2(this IAggregatedBinder aggregatedBinder, Control control)
        {
            var bp = Setup<string>(aggregatedBinder, control,
                () => control.Text,
                tracker => control.TextChanged += (s, e) => tracker.Value = control.Text);

            return bp.FluentBinder;
        }
    }


    public interface IStyleManager
    {

    }

    public class StyleManager : IStyleManager
    {
        BinderManagerBase.ColorOptions colorOptions = new BinderManagerBase.ColorOptions();

        public void Initialize(BinderManagerBase.Item item) => ContentChanged(item);

        public void ContentChanged(BinderManagerBase.Item item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }


        public void FocusLost(BinderManagerBase.Item item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.control.BackColor = hasStatus ?
                colorOptions.UnfocusedStatus :
                colorOptions.ClearedStatus;
        }


        public void FocusGained(BinderManagerBase.Item item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }
    }
}
