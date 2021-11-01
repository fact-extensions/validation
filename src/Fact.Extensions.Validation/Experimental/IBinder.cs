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
        event ProcessingDelegateAsync ProcessedAsync;
        event ProcessingDelegateAsync StartingAsync;

        Task Process(InputContext inputContext = default, CancellationToken ct = default);
    }

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

        IField Field { get; }
    }

    public interface IFluentBinder<out T> : IFluentBinder
    {
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