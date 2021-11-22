using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
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
}
