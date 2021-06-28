using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Fact.Extensions.Validation.Experimental
{
    public interface IBinder2Base
    {
        event ProcessingDelegateAsync ProcessingAsync;
        event ProcessingDelegateAsync ProcessedAsync;
        event Action Resetting;

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
        // EXPERIMENTAL
        bool AbortOnNull { get; set; }

        IBinder2 Binder { get; }

        IField Field { get; }
    }

    public interface IFluentBinder<T> : IFluentBinder
    {
        // DEBT: IField<T> would be better here if we can
        new ShimFieldBase2<T> Field { get; }
    }


    public interface ITrait<T>
    {
        T Trait { get; }
    }

    public interface IFluentBinder<T, TTrait> : IFluentBinder<T>,
        ITrait<TTrait>
    {

    }

}