using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Fact.Extensions.Validation.Experimental
{
    // DEBT: Stop-gap as we move to v3
    public interface IBinder2ProcessorCore
    {
        [Obsolete("Use Processor property directly ('v3')")]
        event ProcessingDelegateAsync ProcessingAsync;
    }

    public interface IBinder2Base : IBinder2ProcessorCore
    {
        [Obsolete("Use Processor property directly ('v3')")]
        event ProcessingDelegateAsync ProcessedAsync;
        [Obsolete("Use Processor property directly ('v3')")]
        event ProcessingDelegateAsync StartingAsync;

        Task Process(InputContext inputContext = default, CancellationToken ct = default);
    }

    /// <summary>
    /// "v2" with is-a processor
    /// </summary>
    [Obsolete("Do not use - phasing out")]
    public interface IBinder2 : IBinder,
        IBinder2Base
    {
    }


    public interface IFluentBinder
    {

        IBinderBase Binder { get; }

        /// <summary>
        /// Field associated specifically with this FluentBinder
        /// Errors registered here are localized to this FluentBinder
        /// </summary>
        IField Field { get; }
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

}