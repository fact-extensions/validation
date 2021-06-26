
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// AKA Binder3, mostly same as Binder2 but uses Processor for a has-a vs is-a relationship.  This lets us
// re-use processor for all kinds of things and also opens up easier possibility of AggregateBinder which
// itself has no field
namespace Fact.Extensions.Validation.Experimental
{
    public interface IBinder3Base
    {
        Processor<Context2> Processor { get; }
    }

    public class Binder3Base : IBinder3Base
    {
        public Processor<Context2> Processor { get; } = new Processor<Context2>();

        /*
        public Task ProcessAsync(InputContext inputContext = null, CancellationToken cancellationToken = null)
        {
            // 
        } */
        
        public Committer Committer { get; } = new Committer();
    }
    
    public class FieldBinder<T> : Binder3Base, 
        IBinder2<T>
    {
        public bool AbortOnNull { get; set; } = true;

        public IField Field { get; }
        
        public event ProcessingDelegateAsync ProcessingAsync
        {
            add => Processor.ProcessingAsync += (sender, context) => value(Field, context);
            remove => throw new InvalidOperationException();
        }

        public event ProcessingDelegateAsync ProcessedAsync
        {
            add => Processor.ProcessingAsync += (sender, context) => value(Field, context);
            remove => throw new InvalidOperationException();
        }
        
        
        public event ProcessingDelegateAsync StartingAsync
        {
            add => Processor.StartingAsync += (_, context) => value(Field, context);
            remove => throw new InvalidOperationException();
        }
        
        
        public event Action Aborting
        {
            add => Processor.Aborting += value;
            remove => Processor.Aborting -= value;
        }
        
        static bool DefaultIsNull(T value) =>
            // We don't want this at all, for example int of 0 is valid in all kinds of scenarios
            //Equals(value, default(T));
            value == null;

        readonly Func<T, bool> isNull;

        public Func<T> getter { get; }
        
        // DEBT: Pretty sure I do not like giving this a 'set;'
        public Action<T> setter { get; set; }

        Func<object> IBinderBase.getter => () => getter();
        
        public FieldBinder(IField field, Func<T> getter, Action<T> setter = null)
        {
            Field = field;
            this.getter = getter;
            this.setter = setter;
            this.isNull = isNull ?? DefaultIsNull;

            Processor.ProcessingAsync += (sender, context) =>
            {
                if (AbortOnNull && isNull((T) context.InitialValue))
                    context.Abort = true;

                return new ValueTask();
            };
        }


        public FieldBinder(string fieldName, Func<T> getter, Action<T> setter = null) :
            this(new FieldStatus(fieldName), getter, setter)
        {
            
        }
        
        protected Context2 CreateContext(CancellationToken ct) =>
            new Context2(getter(), Field, ct);

        public async Task Process(InputContext inputContext = null,
            CancellationToken cancellationToken = default)
        {
            Context2 context = CreateContext(cancellationToken);
            context.InputContext = inputContext;

            if (inputContext?.AlreadyRun?.Contains(this) == true)
                return;

            await Processor.ProcessAsync(context, cancellationToken);

            // FIX: Doesn't play nice with AggregatedBinder itself it seems
            inputContext?.AlreadyRun?.Add(this);
        }
    }


    public class AggregatedBinderBase3<TBinderProvider> : Binder3Base,
        IAggregatedBinderBase
        where TBinderProvider: IBinderProvider
    {
        readonly List<TBinderProvider> providers = new List<TBinderProvider>();

        public IEnumerable<IBinderProvider> Providers => providers.Cast<IBinderProvider>();

        // FIX: Naughty cast
        public void Add(IBinderProvider collected) =>
            providers.Add((TBinderProvider)collected);

        public AggregatedBinderBase3()
        {
            Processor.ProcessingAsync += async (sender, context) =>
            {
                foreach(TBinderProvider provider in providers)
                {
                    await provider.Binder.Process(context.InputContext, context.CancellationToken);
                }
            };
        }
    }


    public static class AggregatedBinder3Extensions
    {
        public static async Task Process<TAggregatedBinder>(this TAggregatedBinder aggregatedBinder, 
            CancellationToken cancellationToken = default)
            where TAggregatedBinder: IBinder3Base, IAggregatedBinderBase
        {
            // FIX: We want to pass this in
            // DEBT: AggregatedBinder3 won't have field or initialvalue
            var context = new Context2(null, null, cancellationToken);

            await aggregatedBinder.Processor.ProcessAsync(context, cancellationToken);
        }
    }
}