using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    using System.Linq;


    public interface IAggregatedBinderProvider<out TBinderProvider>
    {
        IEnumerable<TBinderProvider> Providers { get; }
    }


    public interface IAggregatedBinderProvider : IAggregatedBinderProvider<IBinderProvider>
    {
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

    [Obsolete]
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
    /// Serves up core IFieldBinder
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


    /// <summary>
    /// Technically a fluent field binder provider
    /// </summary>
    /// <typeparam name="T">Type associated with binder itself</typeparam>
    public interface IBinderProvider<out T> : IBinderProvider
    {
        new IFluentBinder<T> FluentBinder { get; }
    }

    /// <summary>
    /// Simplistic/reference IBinderProvider implementation
    /// </summary>
    public class BinderProviderBase : IBinderProvider
    {
        public IFluentBinder FluentBinder { get; }

        public IFieldBinder Binder { get; }

        public BinderProviderBase(IFieldBinder binder, IFluentBinder fluentBinder)
        {
            Binder = binder;
            FluentBinder = fluentBinder;
        }
    }


    public class BinderProviderBase<T> : BinderProviderBase
    {
        public new FluentBinder<T> FluentBinder { get; }

        public BinderProviderBase(IFieldBinder binder, FluentBinder<T> fluentBinder) : base(binder, fluentBinder)
        {
            FluentBinder = fluentBinder;
        }
    }

    public interface IModified
    {
        bool IsModified { get; }
    }

    public class BinderManagerBase
    {
        /// <summary>
        /// Has smarts to track whether we're modified
        /// </summary>
        public class ItemBase : BinderProviderBase, IModified
        {
            public event Action Initialize;
            public virtual bool IsModified => false;

            public void DoInitialize() => Initialize?.Invoke();

            public ItemBase(IFluentBinder fluentBinder) : 
                base(fluentBinder.Binder, fluentBinder)
            {
            }
        }

        public class ItemBase<T> : ItemBase
        {
            public new FluentBinder<T> FluentBinder { get; }

            public ItemBase(FluentBinder<T> fluentBinder) : base(fluentBinder)
            {
                FluentBinder = fluentBinder;
            }
        }
    }


    /// <summary>
    /// Semi-specifically for GUI controls, though really a small refactor could make this into TContext or TMeta
    /// to make it fully inspecific
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    public class BinderManagerBase<TSource> : BinderManagerBase
    {
        public class Item : ItemBase
        {
            public TSource Control;



            public Item(IFluentBinder fluentBinder, TSource source) : 
                base(fluentBinder)
            {
                Control = source;
            }
        }

        public class Item<T> : Item, IBinderProvider<T>
        {
            public readonly Tracker<T> tracked;


            /// <summary>
            /// As per tracker, indicates whether core bound value has been changed since we
            /// started tracking its initial value
            /// </summary>
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
