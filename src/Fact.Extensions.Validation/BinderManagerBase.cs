﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    using Fact.Extensions.Validation.Experimental;
    using System.Linq;


    public interface IAggregatedBinderBase
    {
        IEnumerable<IBinder2> Binders { get; }

        void Add(IBinderProvider binderProvider);
    }

    public interface IAggregatedBinder : IAggregatedBinderBase, IBinder2
    {
    }


    public interface IBinderProvider
    {
        IBinder2 Binder { get; }
    }

    public class AggregatedBinderBase
    {
        public class ItemBase : IBinderProvider
        {
            // DEBT: Due to InternalBindText clumsiness, have to make this non readonly
            public IBinder binder;
            
            // DEBT: Still transitioning to IBinder2, so clumsy here
            public IBinder2 Binder => (IBinder2) binder;

            public ItemBase(IBinder binder)
            {
                this.binder = binder;
            }
        }

    }

    public class BinderManagerBase : AggregatedBinderBase
    {
        public new class ItemBase : AggregatedBinderBase.ItemBase
        {
            public event Action Initialize;
            // DEBT: Pretty sure we can deduce this at will based on an initial vs current value
            [Obsolete]
            public bool modified;
            public virtual bool IsModified => false;

            public void DoInitialize() => Initialize?.Invoke();

            public ItemBase(IBinder binder) : base(binder)
            {
            }
        }
    }

    public class BinderManagerBase<TSource> : BinderManagerBase
    {
        // 1:1 Field binders
        protected List<Item> binders = new List<Item>();

        public class Item : ItemBase
        {
            public readonly TSource control;

            public Item(IBinder binder, TSource source) : base(binder)
            {
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


    public static class IAggregatedBinderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="binder"></param>
        /// <returns></returns>
        /// <remarks>It's possible to have multiple binders associated to one field</remarks>
        public static IEnumerable<IField> Fields(this IAggregatedBinder binder)
        {
            return binder.Binders.Select(x => x.Field).Distinct();
        }
    }
}
