
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

    public interface IFluentBinder3 : IFluentBinder
    {
        new IBinder3Base Binder { get; }
    }


    public interface IFluentBinder3<T> : IFluentBinder<T>,
        IFluentBinder3
    {
        
    }


    public class FluentBinder3<T> : FluentBinder2<T>,
        IFluentBinder3<T>
    {
        public new FieldBinder<T> Binder { get; }

        IBinder3Base IFluentBinder3.Binder => Binder;
        
        public FluentBinder3(FieldBinder<T> binder, bool initial) :
            base(binder, initial)
        {
            Binder = binder;
            // DEBT: Eventually I think we're gonna phase this out for FluentBinder-level
            Binder.AbortOnNull = false;
        }


        public FluentBinder3(string name, Func<T> getter) :
            base(name, getter)
        {
            // DEBT: Stop gap while we upgrade to "v3" binder
            Binder = (FieldBinder<T>) base.Binder;
            // DEBT: Eventually I think we're gonna phase this out for FluentBinder-level
            Binder.AbortOnNull = false;
        }
    }


    public class AggregatedBinderBase3<TBinderProvider> : Binder3Base,
        IAggregatedBinderBase<TBinderProvider>
        where TBinderProvider: IBinderProvider
    {
        readonly List<TBinderProvider> providers = new List<TBinderProvider>();

        public IEnumerable<IBinderProvider> Providers => providers.Cast<IBinderProvider>();

        /// <summary>
        /// Occurs after interactive/discrete binder processing, whether it generated new status or not
        /// </summary>
        public event BindersProcessedDelegate<TBinderProvider> BindersProcessed;

        protected void FireFieldsProcessed(IEnumerable<TBinderProvider> fields, Context2 context) =>
            BindersProcessed?.Invoke(fields, context);

        public void Add(TBinderProvider binderProvider)
        {
            providers.Add(binderProvider);
            Committer.Committing += binderProvider.Binder.Committer.DoCommit;

            binderProvider.Binder.ProcessedAsync += (field, context) =>
            {
                // Filter out overall load/aggregated Process
                if (context.InputContext?.InitiatingEvent != InitiatingEvents.Load)
                    FireFieldsProcessed(new[] { binderProvider }, context);

                return new ValueTask();
            };
        }

        public AggregatedBinderBase3()
        {
            Processor.ProcessingAsync += async (sender, context) =>
            {
                foreach(TBinderProvider provider in providers)
                {
                    await provider.Binder.Process(context.InputContext, context.CancellationToken);
                }
            };

            Processor.ProcessedAsync += (_, c) =>
            {
                // Filter out overall load/aggregated Process
                if (c.InputContext?.InitiatingEvent == InitiatingEvents.Load)
                    FireFieldsProcessed(providers, c);

                return new ValueTask();
            };
        }
    }

    /// <summary>
    /// "v3" Aggregated Binder
    /// </summary>
    public class AggregatedBinder3 : AggregatedBinderBase3<IBinderProvider>,
        IAggregatedBinderBase,
        IBinder2ProcessorCore,
        IServiceProviderProvider
    {
        public IServiceProvider Services { get; }

        public AggregatedBinder3(IServiceProvider services = null)
        {
            Services = services;
        }

        // DEBT: Temporary as we phase out v2
        public event ProcessingDelegateAsync ProcessingAsync
        {
            add => Processor.ProcessingAsync += (sender, context) => value(null, context);
            remove => new InvalidOperationException();
        }
    }
}