using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    public interface IFluentBinder
    {
        IBinderBase Binder { get; }

        /// <summary>
        /// Field associated specifically with this FluentBinder
        /// Errors registered here are localized to this FluentBinder
        /// </summary>
        IField Field { get; }

        /// <summary>
        /// Type which this particular fluent binder is associated with
        /// </summary>
        Type Type { get; }
    }

    public interface IFluentBinder<out T> : IFluentBinder
    {
        /// <summary>
        /// Field associated specifically with this FluentBinder
        /// Errors registered here are localized to this FluentBinder
        /// </summary>
        new IField<T> Field { get; }
    }


    public interface ITrait<out T>
    {
        T Trait { get; }
    }

    public interface IFluentBinder<out T, out TTrait> : IFluentBinder<T>,
        ITrait<TTrait>
    {

    }


    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// DEBT: Fix naming - matches directly to "v3" FluentBinder
    /// </remarks>
    public interface IFluentBinder3 : IFluentBinder
    {
        new IFieldBinder Binder { get; }
    }


    /// <remarks>
    /// DEBT: Fix naming - matches directly to "v3" FluentBinder[T]
    /// </remarks>
    public interface IFluentBinder3<out T> : IFluentBinder<T>,
        IFluentBinder3
    {

    }
}
