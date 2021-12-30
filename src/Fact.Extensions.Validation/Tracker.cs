using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    using LossyQueue = Experimental.LossyQueue;
    using InputContext = Experimental.InputContext;

    // DEBT: Consolidate with Fact.Extensions.Collection.State
    public class TrackerBase
    {
        public object DefaultContext = null;

        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset TouchedAt { get; protected set; }
        // DEBT: Updating event fires every time (really is a Touching/Touched event)
        // But UpdatedAt only is set when a value not equal to old one is specified.
        // Consider changing this to 'ChangedAt' (would use 'ModifiedAt' but modified has a
        // special meaning for us)
        public DateTimeOffset UpdatedAt { get; protected set; }

        protected TrackerBase(DateTimeOffset now)
        {
            if (now == default)
                now = DateTimeOffset.Now;

            CreatedAt = now;
            TouchedAt = now;
            UpdatedAt = now;
        }
    }


    public class Tracker<T> : TrackerBase, IModified
    {
        static bool DefaultEquals(T t1, T t2)
        {
            return object.Equals(t1, t2);
        }

        readonly Func<T, T, bool> equals;
        readonly int historyDepth;

        public Tracker(T initialValue = default, Func<T, T, bool> equals = null, 
            int historyDepth = 0,
            DateTimeOffset now = default) : base(now)
        {
            InitialValue = value = initialValue;
            this.equals = equals ?? DefaultEquals;
            if(historyDepth > 0)
            {
                this.historyDepth = historyDepth;
                history = new LinkedList<Item>();
            }
        }

        T value;

        public event Action<T, T, object> Updating;
        public event Action<T, object> Updated;

        internal class Item
        {
            internal T Value { get; set; }
            internal DateTimeOffset UpdatedAt { get; set; }
            internal object Context { get; set; }
        }

        LinkedList<Item> history;

        public T InitialValue { get; }

        public T Value
        {
            get => value;
            set
            {
                Update(value, DefaultContext);
            }
        }

        void AddToHistory(T value, object context, DateTimeOffset now)
        {
            var item = new Item { Context = context, UpdatedAt = now, Value = value };
            history.AddLast(item);
            if (history.Count > historyDepth)
                history.RemoveFirst();
        }

        public void Update(T value, object context = null, DateTimeOffset now = default)
        {
            if (now == default)
                now = DateTimeOffset.Now;

            Updating?.Invoke(this.value, value, context);
            if (!equals(this.value, value))
                UpdatedAt = now;
            this.value = value;
            TouchedAt = now;
            if (history != null)
                AddToHistory(value, context, now);
            Updated?.Invoke(value, context);
        }


        /// <summary>
        /// Indicates whether value has been modified since we started tracking it
        /// </summary>
        public bool IsModified => !equals(InitialValue, value);


        /// <summary>
        /// Indicates whether any update has occurred on this value, even if it matches
        /// the original value
        /// </summary>
        public bool IsTouched => TouchedAt > CreatedAt;
    }
    
    public static class TrackerUtility
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
        /// </remarks>
        public static void ConfigureTracker<T>(IServiceProvider services, IFieldBinder binder, Tracker<T> tracker, 
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
    }
}
