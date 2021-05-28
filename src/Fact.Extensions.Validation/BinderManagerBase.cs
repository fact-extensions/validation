using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    using Fact.Extensions.Validation.Experimental;
    using System.Linq;

    public class BinderManagerBase<TSource>
    {
        // 1:1 Field binders
        protected List<Item> binders = new List<Item>();

        public class Item
        {
            public IBinder binder;
            public event Action Initialize;
            public readonly TSource control;
            // DEBT: Pretty sure we can deduce this at will based on an initial vs current value
            [Obsolete]
            public bool modified;
            public virtual bool IsModified => false;

            public void DoInitialize() => Initialize?.Invoke();

            public Item(IBinder binder, TSource source)
            {
                this.binder = binder;
                this.control = source;
            }
        }

        public class Item<T> : Item
        {
            public readonly Tracker<T> tracked;

            public override bool IsModified => tracked.IsModified;

            public Item(IBinder2<T> binder, TSource source, T initialValue) :
                this(binder, source, new Tracker<T>(initialValue))
            {
                tracked = new Tracker<T>(initialValue);
            }

            public Item(IBinder2<T> binder, TSource source, Tracker<T> tracker) :
                base(binder, source)
            {
                tracked = tracker;
            }
        }

        /// <summary>
        /// A list of all tracked original/canonical fields
        /// </summary>
        public IEnumerable<IField> Fields =>
            binders.Select(x => x.binder.Field);
    }

}
