using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        /// <typeparam name="T">Value type associated with tracker</typeparam>
        /// <param name="binder">Binder whose field and processor we're attaching to</param>
        /// <param name="tracker">Tracker whose Updated event we're attaching to</param>
        /// <param name="inputContextFactory"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="continueWith">called right after this binder processor runs</param>
        /// <remarks>
        /// DEBT: Poor naming
        /// continueWith is important because .NET doesn't appear to natively support async event handlers
        /// so the tracker.Updated would likely come back before registering any validation statuses thus
        /// precluding a regular tracker.Update from picking up those results.
        /// DEBT: This can be moved out of the winforms-specific area, once Context2 is more decoupled
        /// </remarks>
        static void ConfigureTracker<T>(IServiceProvider services, IFieldBinder binder, Tracker<T> tracker, 
            Func<InputContext> inputContextFactory,
            CancellationToken cancellationToken, Action continueWith)
        {
            var f = (FieldStatus<T>)binder.Field;   // DEBT: Sloppy cast
            LossyQueue lossyQueue = new LossyQueue();

            // FIX: No win scenario here:
            // - performing this async means that it's predictable that update processing won't finish registering
            //   statuses meaning things like GotFocus/LostFocus may register incorrect status
            // - if one doesn't perform this as async, then long running validations (like DB checks) will freeze up
            //   UI
            // The 'isProcessing' flag experimented with elsewhere is a road to a potential solution
            tracker.Updated += async (v, c) =>
            {
                f.Value = v;

                Func<ValueTask> runner = async () =>
                {
                    // DEBT: Likely we actually need a contextFactory not an inputContextFactory
                    var context = new Context2(services, null, f, cancellationToken);
                    context.InputContext = inputContextFactory();

                    await binder.Processor.ProcessAsync(context, cancellationToken);

                    continueWith();
                };

                if (lossyQueue != null)
                    lossyQueue.Add(runner);
                else
                    await runner();
            };
        }

        /// <summary>
        /// Binds a WinForms control using delegates to set up property and change events
        /// </summary>
        /// <typeparam name="TAggregatedBinder"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="aggregatedBinder"></param>
        /// <param name="control"></param>
        /// <param name="getter">Mechanism to acquire bound value from control</param>
        /// <param name="initEvent">
        /// Initialize tracker which serves as an abstract go-between the Validation system and the bound control Property
        /// </param>
        /// <param name="inputContextFactory"></param>
        /// <param name="name">If not null, override Control.Name naming of created Field with this</param>
        /// <param name="isNull">DEBT: Temporarily not in use</param>
        /// <returns></returns>
        static SourceBinderProvider<Control, T> Setup<TAggregatedBinder, T>(TAggregatedBinder aggregatedBinder, Control control,
            Tracker<T> tracker,
            Func<InputContext> inputContextFactory,
            string name = null,
            Func<T, bool> isNull = null)
            where TAggregatedBinder: IAggregatedBinderBase, IServiceProviderProvider, IProcessorProvider<IFieldContext>
        {
            var services = aggregatedBinder.Services;
            var styleManager = services.GetRequiredService<StyleManager>();
            //var cancellationToken = services.GetService<CancellationToken>(); // Because it's a struct this doesn't work
            // DEBT: Need to feed this some kind of semi-global CancellationToken still.  One which only gets
            // cancelled on shutdown
            var cancellationToken = new CancellationToken();

            SourceBinderProvider<Control, T> bp = aggregatedBinder.AddField(name ?? control.Name,
                () => tracker.Value,
                _fb => new SourceBinderProvider<Control, T>(_fb, control, tracker));

            ConfigureTracker(services, bp.Binder, tracker, inputContextFactory, cancellationToken,
                () => styleManager.ContentChanged(bp));

            bool isProcessing = false;
            bp.Binder.Processor.StartingAsync += (_, context) =>
            {
                isProcessing = true;
                
                // DEBT: Unsure how much of a performance penalty this is, but so far nothing
                // obvious
                // DEBT: Feels like this may be better performed elsewhere in general, though
                // StyleManager itself not a candidate
                var runner = new Func<ValueTask>(async () =>
                {
                    // Deferred ContentChanging UI and only do it when observing a noticeably slow
                    // validation
                    // DEBT: A proper scheduler might be more efficient, though it's likely .NET has a
                    // similar and optimized mechanism to deal with Task.Delay
                    await Task.Delay(150, cancellationToken);
                    if (isProcessing)
                    {
                        // DEBT: Potential race condition, this might run after below call to styleManager.Update
                        await control.BeginInvokeAsync(() => styleManager.ContentChanging(bp));
                    }
                });

                _ = runner();

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
            aggregatedBinder.Processor.ProcessedAsync += (_, c) =>
            {
                // DEBT: Potential race condition, this might run before above 'ContentChanging' BeginInvoke completes
                if (c.InputContext.UiContext != null)
                    c.InputContext.UiContext.Post(delegate { styleManager.Update(bp); }, null);
                else
                    control.BeginInvokeAsync(() => styleManager.Update(bp));

                return new ValueTask();
            };

            return bp;
        }


        /// <summary>
        /// Binds to Text property of control
        /// </summary>
        /// <param name="aggregatedBinder"></param>
        /// <param name="control"></param>
        /// <param name="initialGetter">retrieves value to prepopulate field with</param>
        /// <param name="name">specify name of field.  Default is control.Name - which you probably don't want</param>
        /// <returns></returns>
        public static FluentBinder<string> BindText<TAggregatedBinder>(this TAggregatedBinder aggregatedBinder, Control control, 
            Func<string> initialGetter = null, string name = null)
            where TAggregatedBinder : IAggregatedBinderBase, IServiceProviderProvider, IProcessorProvider<IFieldContext>
        {
            if (initialGetter != null) control.Text = initialGetter();

            var tracker = new Tracker<string>(control.Text);

            control.TextChanged += (s, e) => tracker.Value = control.Text;

            SourceBinderProvider<Control, string> bp = Setup(aggregatedBinder, control,
                tracker,
                () => new InputContext
                {
                    // NOTE: We have to make a new one for each keystroke since we have the
                    // 'AlreadyRun' tracker which otherwise would need a reset
                    InitiatingEvent = InitiatingEvents.Keystroke,
                    InteractionLevel = Interaction.High
                },
                name,
                v => string.IsNullOrWhiteSpace(v));

            // EXPERIMENTAL
            bp.FluentBinder.Setter((string v) => control.Text = v);

            return bp.FluentBinder;
        }


        /// <summary>
        /// Binds the Text property of a control to a particular property of <typeparamref name="TEntity"/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="entityProvider"></param>
        /// <param name="control"></param>
        /// <param name="propertyLambda"></param>
        /// <returns></returns>
        public static FluentBinder<TProperty> BindText<TEntity, TProperty>(this EntityProvider<TEntity> entityProvider, Control control,
            Expression<Func<TEntity, TProperty>> propertyLambda, Func<TProperty, string> toString = null)
        {
            // TODO: Consolidate some of this magic into EntityProvider or an EntityProviderExtensions
            var member = propertyLambda.Body as MemberExpression;
            var property = member.Member as PropertyInfo;
            var name = property.Name;

            if (toString == null)
                toString = v => v?.ToString();

            // DEBT: ToString() alone isn't gonna cut it for real conversions
            FluentBinder<string> fluentBinder = entityProvider.Parent.BindText(
                control, () => toString((TProperty)property.GetValue(entityProvider.Entity)),
                property.Name);
            FluentBinder<TProperty> fluentBinderConverted;

            if (typeof(TProperty) != typeof(string))
            {
                fluentBinderConverted = fluentBinder.Convert<TProperty>();
            }
            else
            {
                // DEBT: Only slightly sloppy, since we're verifying right above that TProperty == string
                fluentBinderConverted = (FluentBinder<TProperty>)(object)fluentBinder;
            }

            PropertyBinderProvider.InitValidation(fluentBinderConverted, property);

            fluentBinderConverted.Commit(v => property.SetValue(entityProvider.Entity, v));

            return fluentBinderConverted;
        }


        public static IFluentBinder BindSelectedItem(this IAggregatedBinder3 aggregatedBinder, ListBox control,
            Func<object> initialGetter = null)
        {
            if(initialGetter != null)
                control.SelectedItem = initialGetter();

            var tracker = new Tracker<object>(control.SelectedItem);

            control.SelectedIndexChanged += (s, e) => tracker.Value = control.SelectedItem;

            IBinderProvider<object> bp = Setup(aggregatedBinder, control,
                tracker,
                () => new InputContext
                {
                    // NOTE: We have to make a new one for each keystroke since we have the
                    // 'AlreadyRun' tracker which otherwise would need a reset
                    InitiatingEvent = InitiatingEvents.Keystroke,
                    InteractionLevel = Interaction.High
                });

            // EXPERIMENTAL
            bp.FluentBinder.Setter((object v) => control.SelectedItem = v);

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

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// DEBT: Change method signatures to take Control and IFieldBinder, if possible
    /// DEBT: Consolidate some of these methods
    /// </remarks>
    public class StyleManager : IStyleManager
    {
        BinderManagerBase.ColorOptions colorOptions = new BinderManagerBase.ColorOptions();

        /// <summary>
        /// Renders control reflecting its current status
        /// </summary>
        /// <param name="item"></param>
        public void Update(ISourceBinderProvider<Control> item)
        {
            Control control = item.Source;
            // If focused, of course render as focused
            // If not modified, FocusGained has an 'Initial' state to render things with
            bool hasStatus = item.Binder.Field.Statuses.Any();

            if (hasStatus)
            {
                if (item.IsModified)
                    control.BackColor = control.Focused ? colorOptions.FocusedStatus : colorOptions.UnfocusedStatus;
                else
                    control.BackColor = colorOptions.InitialStatus;
            }
            else
                control.BackColor = colorOptions.ClearedStatus;
        }


        /// <summary>
        /// Triggered when content has changed and now we are in the midst of processing it
        /// </summary>
        /// <param name="item"></param>
        public void ContentChanging(ISourceBinderProvider<Control> item)
        {
            item.Source.BackColor = Color.LightGray;
        }

        /// <summary>
        /// Called when content change processing is completed
        /// It is assumed field has focus at this time
        /// </summary>
        /// <param name="item"></param>
        public void ContentChanged(ISourceBinderProvider<Control> item)
        {
            Update(item);
            /*
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Source.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus; */
        }


        /// <summary>
        /// Renders control when focus is lost, considering any field statuses
        /// </summary>
        /// <param name="item"></param>
        public void FocusLost(ISourceBinderProvider<Control> item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Source.BackColor = hasStatus ?
                colorOptions.UnfocusedStatus :
                colorOptions.ClearedStatus;
        }


        /// <summary>
        /// Renders control when focus is gained, considering any field statuses
        /// </summary>
        /// <param name="item"></param>
        public void FocusGained(ISourceBinderProvider<Control> item)
        {
            bool hasStatus = item.Binder.Field.Statuses.Any();

            item.Source.BackColor = hasStatus ?
                (item.IsModified ? colorOptions.FocusedStatus : colorOptions.InitialStatus) :
                colorOptions.ClearedStatus;
        }
    }
}
