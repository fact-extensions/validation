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
        event BindersProcessedDelegate<TBinderProvider> BindersProcessed;
    }

    public interface IAggregatedBinder : IAggregatedBinderBase,
        IServiceProviderProvider,
        IBinderBase
    {
    }


    public interface IBinderProviderBase<out TBinder>
        where TBinder: IBinderBase
    {
        TBinder Binder { get; }
    }

    /// <summary>
    /// Serves up core IBinderBase
    /// </summary>
    public interface IBinderProviderBase : IBinderProviderBase<IFieldBinder>
    {
    }


    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// DEBT: Rename to IFluentBinderProvider
    /// </remarks>
    public interface IBinderProvider : IBinderProviderBase
    {
        IFluentBinder FluentBinder { get; }
    }


    public interface IBinderProvider<T> : IBinderProvider
    {
        new IFluentBinder<T> FluentBinder { get; }
    }

    public class AggregatedBinderBase
    {
        public class ItemBase : IBinderProvider
        {
            // DEBT: Due to InternalBindText clumsiness, have to make this non readonly
            public IFieldBinder binder;

            public IFluentBinder FluentBinder { get; }
            
            // DEBT: Still transitioning to IBinder2, so clumsy here
            public IFieldBinder Binder => binder;

            public ItemBase(IFieldBinder binder, IFluentBinder fluentBinder)
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

            public ItemBase(IFieldBinder binder, IFluentBinder fluentBinder) : 
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
