﻿using System;
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
        /// <summary>
        /// Configures binder provider to rewrite back to its Field.Value when tracker changes
        /// Also kicks off a validation chain at that time semi-asynchronously
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bp"></param>
        /// <param name="tracker"></param>
        /// <param name="inputContextFactory"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="continueWith">called right after this binder processor runs</param>
        /// <remarks>
        /// DEBT: Poor naming
        /// continueWith is important because .NET doesn't appear to natively support async event handlers
        /// so the tracker.Updated would likely come back before registering any validation statuses thus
        /// precluding a regular tracker.Update from picking up those results.
        /// DEBT: This can be moved out of the winforms-specific area
        /// </remarks>
        static void ConfigureTracker<T>(IFieldBinder binder, Tracker<T> tracker, 
            Func<InputContext> inputContextFactory,
            CancellationToken cancellationToken, Action continueWith)
        {
            var f = (FieldStatus<T>)binder.Field;   // DEBT: Sloppy cast

            // FIX: No win scenario here:
            // - performing this async means that it's predictable that update processing won't finish registering
            //   statuses meaning things like GotFocus/LostFocus may register incorrect status
            // - if one doesn't perform this as async, then long running validations (like DB checks) will freeze up
            //   UI
            // The 'isProcessing' flag experimented with elsewhere is a road to a potential solution
            tracker.Updated += async (v, c) =>
            {
                f.Value = v;

                // DEBT: Likely we actually need a contextFactory not an inputContextFactory
                var context = new Context2(null, f, cancellationToken);
                context.InputContext = inputContextFactory();

                await binder.Processor.ProcessAsync(context, cancellationToken);

                continueWith();
            };
        }

        static SourceBinderProvider<Control, T> Setup<TAggregatedBinder, T>(TAggregatedBinder aggregatedBinder, Control control,
            Func<T> getter, 
            Action<Tracker<T>> initEvent,
            Func<InputContext> inputContextFactory,
            Func<T, bool> isNull = null)
            where TAggregatedBinder: IAggregatedBinderBase, IServiceProviderProvider, IProcessorProvider<Context2>
        {
            var services = aggregatedBinder.Services;
            var styleManager = services.GetRequiredService<StyleManager>();
            //var cancellationToken = services.GetService<CancellationToken>(); // Because it's a struct this doesn't work
            // DEBT: Need to feed this cancellationtoken still
            var cancellationToken = new CancellationToken();
            var tracker = new Tracker<T>(getter());
            initEvent(tracker);

            SourceBinderProvider<Control, T> bp = aggregatedBinder.AddField(control.Name,
                () => tracker.Value,
                _fb => new SourceBinderProvider<Control, T>(_fb, control, tracker));

            ConfigureTracker(bp.Binder, tracker, inputContextFactory, cancellationToken,
                () => styleManager.ContentChanged(bp));

            // DEBT: Heed the isProcessing awareness so as to reflect async status
            // properly in styleManager
            bool isProcessing = false;
            bp.Binder.Processor.StartingAsync += (_, context) =>
            {
                isProcessing = true;
                return new ValueTask();
            };
            bp.Binder.Processor.ProcessedAsync += (_, context) =>
            {
                isProcessing = false;
                return new ValueTask();
            };

            control.GotFocus += (s, e) => styleManager.FocusGained(bp);
            control.LostFocus += (s, e) => styleManager.FocusLost(bp);

            // Aggregator-wide init of this particular field so that on any call to
            // aggregatorBinder.Process() current field state style is exactly reflected
            aggregatedBinder.Processor.ProcessingAsync += (_, c) =>
            {
                styleManager.Initialize(bp);
                return new ValueTask();
            };

            return bp;
        }


        /// <summary>
        /// Binds to Text property of control
        /// </summary>
        /// <param name="aggregatedBinder"></param>
        /// <param name="control"></param>
        /// <param name="initialGetter"></param>
        /// <returns></returns>
        public static IFluentBinder<string> BindText<TAggregatedBinder>(this TAggregatedBinder aggregatedBinder, Control control, 
            Func<string> initialGetter = null)
            where TAggregatedBinder : IAggregatedBinderBase, IServiceProviderProvider, IProcessorProvider<Context2>
        {
            IBinderProvider<string> bp = Setup(aggregatedBinder, control,
                () => control.Text,
                tracker => control.TextChanged += (s, e) => tracker.Value = control.Text,
                () => new InputContext
                {
                    // NOTE: We have to make a new one for each keystroke since we have the
                    // 'AlreadyRun' tracker which otherwise would need a reset
                    InitiatingEvent = InitiatingEvents.Keystroke,
                    InteractionLevel = Interaction.High
                },
                v => string.IsNullOrWhiteSpace(v));

            bp.FluentBinder.Setter(v => control.Text = v, initialGetter);

            return bp.FluentBinder;
        }


        public static IFluentBinder BindSelectedItem(this IAggregatedBinder3 aggregatedBinder, ListBox control,
            Func<object> initialGetter = null)
        {
            IBinderProvider<object> bp = Setup(aggregatedBinder, control,
                () => control.SelectedItem,
                tracker => control.SelectedIndexChanged += (s, e) => tracker.Value = control.SelectedItem,
                () => new InputContext
                {
                    // NOTE: We have to make a new one for each keystroke since we have the
                    // 'AlreadyRun' tracker which otherwise would need a reset
                    InitiatingEvent = InitiatingEvents.Keystroke,
                    InteractionLevel = Interaction.High
                });

            bp.FluentBinder.Setter(v => control.SelectedItem = v, initialGetter);

            return bp.FluentBinder;
        }


        /// <summary>
        /// Binds to Text property of control
        /// </summary>
        /// <param name="aggregatedBinder"></param>
        /// <param name="control"></param>
        /// <param name="initialValue"></param>
        /// <returns></returns>
        public static IFluentBinder<string> BindText(this IAggregatedBinder3 aggregatedBinder, Control control,
            string initialValue) =>
            aggregatedBinder.BindText(control, () => initialValue);


        /// <summary>
        /// Auto-converting BindText, since native one is always string
        /// </summary>
        public static FluentBinder<T> BindText<T>(this IAggregatedBinder3 aggregatedBinder, Control control) =>
            aggregatedBinder.BindText(control).Convert<T>();

        /// <summary>
        /// Auto-converting BindText, since native one is always string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="aggregatedBinder"></param>
        /// <param name="control"></param>
        /// <param name="initialValue"></param>
        /// <returns></returns>
        public static FluentBinder<T> BindText<T>(this IAggregatedBinder3 aggregatedBinder, Control control,
            T initialValue) =>
            aggregatedBinder.BindText(control, initialValue.ToString()).Convert<T>();
    }


    public interface IStyleManager
    {

    }

    public class StyleManager : IStyleManager
    {
        BinderManagerBase.ColorOptions colorOptions = new BinderManagerBase.ColorOptions();

        public void Initialize(ISourceBinderProvider<Control> item) => ContentChanged(item);

        public void ContentChanged(ISourceBinderProvider<Control> item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }


        public void FocusLost(ISourceBinderProvider<Control> item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Control.BackColor = hasStatus ?
                colorOptions.UnfocusedStatus :
                colorOptions.ClearedStatus;
        }


        public void FocusGained(ISourceBinderProvider<Control> item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Control.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }
    }
}
