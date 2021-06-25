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

            //IBinderProvider<T> bp
            var bp = aggregatedBinder.AddField2(control.Name,
                () => tracker.Value,
                _fb => new BinderManagerBase.Item<T>(_fb, control, tracker));

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

                styleManager.ContentChanged(bp);
            };

            control.GotFocus += (s, e) => styleManager.FocusGained(bp);
            control.LostFocus += (s, e) => styleManager.FocusLost(bp);

            // Aggregator-wide init of this particular field so that on any call to
            // aggregatorBinder.Process() current field state style is exactly reflected
            aggregatedBinder.ProcessingAsync += (_, c) =>
            {
                styleManager.Initialize(bp);
                return new ValueTask();
            };

            return bp;
        }

        public static IFluentBinder<string> BindText(this IAggregatedBinder aggregatedBinder, Control control, 
            Func<string> initialGetter = null)
        {
            var bp = Setup<string>(aggregatedBinder, control,
                () => control.Text,
                tracker => control.TextChanged += (s, e) => tracker.Value = control.Text);

            bp.FluentBinder.Setter(v => control.Text = v, initialGetter);

            return bp.FluentBinder;
        }


        public static IFluentBinder<string> BindText(this IAggregatedBinder aggregatedBinder, Control control,
            string initialValue) =>
            aggregatedBinder.BindText(control, () => initialValue);
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

            item.Control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }


        public void FocusLost(BinderManagerBase.Item item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Control.BackColor = hasStatus ?
                colorOptions.UnfocusedStatus :
                colorOptions.ClearedStatus;
        }


        public void FocusGained(BinderManagerBase.Item item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }
    }
}
