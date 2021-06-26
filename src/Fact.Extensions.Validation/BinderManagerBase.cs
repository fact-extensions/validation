using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    using Fact.Extensions.Validation.Experimental;
    using System.Linq;


    public interface IAggregatedBinderProvider
    {
        IEnumerable<IBinderProvider> Providers { get; }
    }


    public interface IServiceProviderProvider
    {
        IServiceProvider Services { get; }
    }


    public interface ICollector<T>
    {
        void Add(T collected);
    }

    public interface IAggregatedBinderCollector : ICollector<IBinderProvider>
    {
    }

    public interface IAggregatedBinderBase : 
        IAggregatedBinderProvider,
        IAggregatedBinderCollector
    {
    }

    public interface IAggregatedBinderBase<TBinderProvider> :
        IAggregatedBinderProvider,
        ICollector<TBinderProvider>
    {
    }

    public interface IAggregatedBinder : IAggregatedBinderBase,
        IServiceProviderProvider,
        IBinder2
    {
    }


    public interface IBinderProvider
    {
        IFluentBinder FluentBinder { get; }

        IBinder2 Binder { get; }
    }


    public interface IBinderProvider<T> : IBinderProvider
    {
        new IFluentBinder<T> FluentBinder { get; }

        new IBinder2<T> Binder { get; }
    }

    public class AggregatedBinderBase
    {
        public class ItemBase : IBinderProvider
        {
            // DEBT: Due to InternalBindText clumsiness, have to make this non readonly
            public IBinder2 binder;

            public IFluentBinder FluentBinder { get; }
            
            // DEBT: Still transitioning to IBinder2, so clumsy here
            public IBinder2 Binder => binder;

            public ItemBase(IBinder2 binder, IFluentBinder fluentBinder)
            {
                this.binder = binder;
                FluentBinder = fluentBinder;
            }
        }

    }

    public interface IModified
    {
        bool IsModified { get; }
    }

    public class BinderManagerBase : AggregatedBinderBase
    {
        public new class ItemBase : AggregatedBinderBase.ItemBase, IModified
        {
            public event Action Initialize;
            public virtual bool IsModified => false;

            public void DoInitialize() => Initialize?.Invoke();

            public ItemBase(IBinder2 binder, IFluentBinder fluentBinder) : 
                base(binder, fluentBinder)
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
            public TSource Control;



            public Item(IFluentBinder fluentBinder, TSource source) : 
                base(fluentBinder.Binder, fluentBinder)
            {
                Control = source;
            }
        }

        public class Item<T> : Item, IBinderProvider<T>
        {
            public readonly Tracker<T> tracked;

            public override bool IsModified => tracked.IsModified;

            IFluentBinder<T> IBinderProvider<T>.FluentBinder => (IFluentBinder<T>)base.FluentBinder;

            IBinder2<T> IBinderProvider<T>.Binder => (IBinder2<T>)base.Binder;

            public Item(IFluentBinder<T> fluentBinder, TSource source, Tracker<T> tracker) :
                base(fluentBinder, source)
            {
                tracked = tracker;
            }
        }
    }


    public static class IAggregatedBinderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="binder"></param>
        /// <returns></returns>
        /// <remarks>It's possible to have multiple binders associated to one field</remarks>
        public static IEnumerable<IField> Fields(this IAggregatedBinderProvider binder)
        {
            return binder.Providers.Select(x => x.Binder.Field).Distinct();
        }
    }
}
