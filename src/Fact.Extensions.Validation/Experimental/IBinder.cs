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
    public interface IBinder2 : IBinder,
        IBinder2Base
    {
    }


    public interface IBinder2<T> : IBinder2,
        IBinderBase<T>
    {

    }

    public interface IFluentBinder
    {

        IBinder2 Binder { get; }

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


    public interface ITrait<T>
    {
        T Trait { get; }
    }

    public interface IFluentBinder<out T, TTrait> : IFluentBinder<T>,
        ITrait<TTrait>
    {

    }

}