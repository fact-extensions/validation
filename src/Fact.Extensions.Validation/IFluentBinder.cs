using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Associated with an IField
    /// </summary>
    public interface IFluentBinder : IBinderProviderBase,
        IFieldProvider<IField>
    {
        /// <summary>
        /// Field associated specifically with this FluentBinder
        /// Errors registered here are localized to this FluentBinder
        /// </summary>
        /// <remarks>
        /// Really underlying IFieldProvider does this job, but keeping here to benefit from
        /// comments specific to this particular interface
        /// </remarks>
        new IField Field { get; }

        /// <summary>
        /// Type which this particular fluent binder is associated with
        /// </summary>
        Type Type { get; }
    }


    /// <summary>
    /// Associated with an IField[T}
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFluentBinder<out T> : IFluentBinder, 
        IFieldProvider<IField<T>>
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

    public interface IFieldProvider<out TField>
        where TField: IFieldBase
    {
        TField Field { get; }
    }
}
