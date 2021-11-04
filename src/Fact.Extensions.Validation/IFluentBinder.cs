using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Associated with an IField
    /// </summary>
    /// <remarks>
    /// TODO: Consolidate with IFluentBinder3 -- namely, upgrade 'Binder' to use IFieldBinder rather than IBinderBase
    /// Not doing yet because of last lingering "v2" class code which doesn't use IFieldBinder
    /// </remarks>
    public interface IFluentBinder : IBinderProviderBase
    {
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


    /// <summary>
    /// Associated with an IField
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


    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// DEBT: Fix naming - matches directly to "v3" FluentBinder
    /// </remarks>
    public interface IFluentBinder3 : IFluentBinder,
        IBinderProviderBase<IFieldBinder>
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


    public interface IFieldProvider<out TField>
        where TField: IField
    {
        TField Field { get; }
    }
}
