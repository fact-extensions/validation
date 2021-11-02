
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
    public interface IProcessorProvider<TContext>
        where TContext: IContext
    {
        Processor<TContext> Processor { get; }
    }

    public interface IBinder3Base : IProcessorProvider<Context2>
    {
    }


    /// <summary>
    /// "v3" binder with has-a processor
    /// </summary>
    public interface IFieldBinder : IBinder3Base, IBinder2Base, IBinderBase
    {

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
    

    /// <summary>
    /// "v3" of root binder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FieldBinder<T> : Binder3Base,
        IFieldBinder,
        IBinder2<T>
    {
        // Phasing AbortOnNull out at FieldBinder level
        public bool AbortOnNull
        {
            get => false;
            set => throw new InvalidOperationException("Only Binder2 supports AbortOnNull");
        }

        public IField Field { get; }

        [Obsolete("Use Processor property directly ('v3')")]
        public event ProcessingDelegateAsync ProcessingAsync
        {
            add => Processor.ProcessingAsync += (sender, context) => value(Field, context);
            remove => throw new InvalidOperationException();
        }

        [Obsolete("Use Processor property directly ('v3')")]
        public event ProcessingDelegateAsync ProcessedAsync
        {
            add => Processor.ProcessedAsync += (sender, context) => value(Field, context);
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
            this(new FieldStatus<T>(fieldName), getter, setter)
        {
            
        }
        

        /// <summary>
        /// Creates a new Context2 using this binder's getter() for initial value
        /// and original Field
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
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
        new IFieldBinder Binder { get; }
    }


    public interface IFluentBinder3<out T> : IFluentBinder<T>,
        IFluentBinder3
    {
        
    }


    public class FluentBinder3<T> : FluentBinder2,
        IFluentBinder3<T>
    {
        public new IFieldBinder Binder { get; }

        readonly ShimFieldBase2<T> field;

        public new IField<T> Field => field;

        /*
        public FluentBinder3(IFluentBinder<T> chained) :
            base(chained)
        {
        } */

        /// <summary>
        /// EXPERIMENTAL - for conversion chains only
        /// </summary>
        /// <param name="chained">Binder on which we hang error reporting</param>
        /// <param name="converted">Parameter converter - previous FluentBinder in chain was not of type T</param>
        public FluentBinder3(IFieldBinder chained, Func<T> converter) :
            base(/* DEBT */ (IBinder2)chained, typeof(T))
        {
            Binder = chained;

            field = new ShimFieldBase2<T>(chained.Field.Name, statuses, converter);

            // DEBT: Easy to get wrong
            base.Field = field;

            Initialize();
        }


        /// <summary>
        /// Attach this FluentBinder to an existing Binder
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="initial"></param>
        public FluentBinder3(FieldBinder<T> binder, bool initial) :
            base(binder, typeof(T))
        {
            Binder = binder;

            if (initial)
                // DEBT: Needs refiniement
                field = new ShimFieldBase2<T>(binder.Field.Name, statuses, () => binder.getter());
            else
            {
                // FIX: This seems wrong, getting initial value at FluentBinder setup time
                T initialValue = binder.getter();
                field = new ShimFieldBase2<T>(binder.Field.Name, statuses, () => initialValue);
            }

            // DEBT: Easy to get wrong
            base.Field = field;

            Initialize();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 100% overrides semi-obsolete base
        /// </remarks>
        new void Initialize()
        {
            // This event handler is more or less a re-initializer for subsequent
            // process/validation calls
            Binder.Processor.StartingAsync += (field, context) =>
            {
                statuses.Clear();
                return new ValueTask();
            };

            // DEBT
            var f = (IFieldStatusExternalCollector)Binder.Field;
            f.Add(statuses);
        }


        public FluentBinder3(string name, Func<T> getter) :
            this(new FieldBinder<T>(name, getter), true)
        {
        }
    }


    public class FluentBinder3<T, TTrait> : FluentBinder3<T>
    {
        public FluentBinder3(FieldBinder<T> binder, bool initial) : 
            base(binder, initial)
        {

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