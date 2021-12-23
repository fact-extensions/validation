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


    public class BinderProviderBase<T> : BinderProviderBase, IBinderProvider<T>
    {
        public new FluentBinder<T> FluentBinder { get; }

        IFluentBinder<T> IBinderProvider<T>.FluentBinder => FluentBinder;

        public BinderProviderBase(IFieldBinder binder, FluentBinder<T> fluentBinder) : base(binder, fluentBinder)
        {
            FluentBinder = fluentBinder;
        }
    }

    public interface IModified
    {
        /// <summary>
        /// Indicates whether value has been modified since we started tracking it
        /// </summary>
        bool IsModified { get; }
    }


    /// <summary>
    /// A binder provider which is aware of when its getter() value changes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InteractiveBinderProvider<T> : BinderProviderBase<T>, IModified
    {
        public readonly Tracker<T> tracked;


        /// <summary>
        /// As per tracker, indicates whether core bound value has been changed since we
        /// started tracking its initial value
        /// </summary>
        public bool IsModified => tracked.IsModified;

        public InteractiveBinderProvider(FluentBinder<T> fluentBinder, Tracker<T> tracker) :
                base(fluentBinder.Binder, fluentBinder)
        {
            tracked = tracker;
        }
    }


    /// <summary>
    /// DEBT: Needs name cleanup - an interactive binder provider with associated metadata
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="T"></typeparam>
    public class SourceBinderProvider<TSource, T> : InteractiveBinderProvider<T>, ISourceBinderProvider<TSource>
    {
        public TSource Source { get; }

        public SourceBinderProvider(FluentBinder<T> fluentBinder, TSource source, Tracker<T> tracker) :
            base(fluentBinder, tracker)
        {
            Source = source;
        }
    }


    /// <summary>
    /// DEBT: Needs name cleanup
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    public interface ISourceBinderProvider<TSource> : IBinderProvider, IModified
    {
        TSource Source { get; }
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
