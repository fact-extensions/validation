using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Fact.Extensions.Validation.Experimental
{
    public interface IContext
    {
        bool Abort { get; }
        
        /// <summary>
        /// When true, signals Binder.Process that particular processor is completely
        /// awaited before next processor runs.  When false, particular process is
        /// treated fully asynchronously and the next processor begins evaluation in
        /// parallel.  Defaults to true 
        /// </summary>
        bool Sequential { get; set; }
    }
    
    public class Context2 : Context, IContext
    {
        /// <summary>
        /// Current value, which starts as populated by the binder's getter but may
        /// be converted as the pipeline is processed
        /// </summary>
        public object Value { get; set; }

        public object InitialValue { get; }
        
        public bool Sequential { get; set; }
        
        public CancellationToken CancellationToken { get; }

        // Still experimental 
        public InputContext InputContext { get; set; }

        // EXPERIMENTAL putting this here -- same as IBinder.Field
        public IField Field { get; }

        public Context2(object initialValue, IField field, CancellationToken cancellationToken)
        {
            Field = field;
            InitialValue = initialValue;
            Value = initialValue;
            CancellationToken = cancellationToken;
        }
    }

        
    public delegate void ProcessingDelegate(IField f, Context2 context);
    // ValueTask guidance here:
    // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    public delegate ValueTask ProcessingDelegateAsync(IField f, Context2 context);


    /// <summary>
    /// Boilerplate for less-typed filter-only style binder
    /// </summary>
    public class Binder2<T> : BinderBase<T>, 
        IBinder2<T>
    {
        // DEBT: Sometimes a text entry of "" means null, an int of 0, etc.
        // we need a mechanism to account for that
        public bool AbortOnNull { get; set; } = true;

        readonly Func<T, bool> isNull;

        static bool DefaultIsNull(T value) =>
            // We don't want this at all, for example int of 0 is valid in all kinds of scenarios
            //Equals(value, default(T));
            value == null;

        public Binder2(IField field, Func<T> getter, Func<T, bool> isNull = null) : 
            base(field, getter)
        {
            this.isNull = isNull ?? DefaultIsNull;
        }


        public event ProcessingDelegate Processing
        {
            remove
            {
                throw new NotSupportedException("Must use ProcessingAsync property");
            }
            add
            {
                ProcessingAsync +=
                    (field1, context) =>
                    {
                        value(field1, context);
                        return new ValueTask();
                    };
            }
        }

        public event ProcessingDelegateAsync StartingAsync;
        public event ProcessingDelegateAsync ProcessingAsync;
        public event ProcessingDelegateAsync ProcessedAsync;
        public event Action Aborting;

        protected Context2 CreateContext(object initialValue, CancellationToken ct) =>
            new Context2(initialValue, field, ct);

        public async Task Process(InputContext inputContext = default, CancellationToken ct = default)
        {
            T initialValue = getter2();
            Context2 context = CreateContext(initialValue, ct);
            context.InputContext = inputContext;

            if (inputContext?.AlreadyRun?.Contains(this) == true)
                return;

            if (StartingAsync != null)
                await StartingAsync(field, context);

            // NOTE: This had a serious issue where we abort before clearing out potentially
            // previous statuses/errors.  Just added 'Resetting' event hopefully we can augment
            // anyone listening with that event so that we can clear things out in this case.
            // Feels clumsy though.  Might be better as a "StartProcessing" type event which
            // would flow into the FluentBinder's initializer event pretty smoothly.  And
            // in fact that would work better overall, otherwise aborts mid chain would
            // never fire some later fluentbinders or similar and those statuses would
            // continue to linger
            if (context.Abort || (AbortOnNull && isNull(initialValue)))
            {
                Aborting?.Invoke();
                if(ProcessedAsync != null)
                    await ProcessedAsync.Invoke(field, context);
                return;
            }

            // NOTE: Odd that following line doesn't compile now.
            // Fortunately our scenario that's OK
            //Processing?.Invoke(field, context);
            var delegates = ProcessingAsync?.GetInvocationList() ?? Enumerable.Empty<object>();

            var nonsequential = new LinkedList<Task>();

            foreach (ProcessingDelegateAsync d in delegates)
            {
                context.Sequential = true;
                ValueTask task = d(field, context);
                if (context.Abort)
                {
                    Aborting?.Invoke();
                    break;
                }
                if (context.Sequential)
                    await task;
                else
                    nonsequential.AddLast(task.AsTask());
            }

            // guidance from
            // https://stackoverflow.com/questions/27238232/how-can-i-cancel-task-whenall
            var tcs = new TaskCompletionSource<bool>(ct);
            await Task.WhenAny(Task.WhenAll(nonsequential), tcs.Task);

            if(ProcessedAsync != null)
                await ProcessedAsync.Invoke(field, context);

            // FIX: Doesn't play nice with AggregatedBinder itself it seems
            inputContext?.AlreadyRun?.Add(this);
        }
    }



    public class Optional<T>
    {
        public T Value { get; set; }
    }


    public static class Binder2Extensions
    {
        public static FluentBinder2<T> As<T>(this IBinder2 binder)
        {
            return new FluentBinder2<T>(binder, true);
        }

        public static FluentBinder2<T> As<T>(this IBinder2<T> binder)
        {
            return new FluentBinder2<T>(binder, true);
        }


    }


    /// <summary>
    /// Field whose value comes from a getter, and statuses come from external party
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ShimFieldBase2<T> : ShimFieldBaseBase,
        IField<T>
    {
        readonly Func<T> getter;    // TODO: Maybe make this acquired direct from FluentBinder2
        object IField.Value => getter();

        public T Value => getter();

        internal ShimFieldBase2(string name, ICollection<Status> statuses, 
            Func<T> getter) :
            base(name, statuses)
        {
            this.getter = getter;
        }
    }


    public class FluentBinder2 : IFluentBinder
    {
        public IBinder2 Binder { get; }


        /// <summary>
        /// Field local to this FluentBinder
        /// </summary>
        public IField Field { get; protected set; }

        public Type Type { get; }

        // DEBT: Field MUST be initialized by calling class
        protected FluentBinder2(IBinder2 binder, Type type = null)
        {
            Binder = binder;
            Type = type;
        }


        /// <summary>
        /// Statuses associated with just this binder
        /// Attaches to Field as an external status
        /// </summary>
        protected readonly List<Status> statuses = new List<Status>();


        /// <summary>
        /// Set up:
        /// - clearing of local statuses to this FluentBinder
        /// - awareness of this FluentBinder's local statuses to overall Binder
        /// </summary>
        protected void Initialize()
        {
            // This event handler is more or less a re-initializer for subsequent
            // process/validation calls
            Binder.StartingAsync += (field, context) =>
            {
                statuses.Clear();
                return new ValueTask();
            };

            // DEBT
            var f = (IFieldStatusExternalCollector)Binder.Field;
            f.Add(statuses);
        }
    }



    public class FluentBinder2<T> : FluentBinder2,
        IFluentBinder<T>
    {
        /// <summary>
        /// Rolling value loosely analoguous to Context.Value
        /// This is the "runtime init" value preceded potentially by other FluentBinders
        /// vs the "system init" value which we generally are uninterested in
        /// </summary>
        internal T InitialValue;
        
        /// <summary>
        /// Field whose statuses are only associated with this particular FluentBinder
        /// </summary>
        public new  ShimFieldBase2<T> Field { get; }

        IField<T> IFluentBinder<T>.Field => Field;

        new void Initialize()
        {
            base.Initialize();

            // DEBT: Easy to get wrong
            base.Field = Field;
        }

        public FluentBinder2(IFluentBinder chained) :
            this(chained.Binder, false)
        {
            // DEBT: Do runtime check immediately to verify we're dealing with a getter whose
            // type is compatible with T.  Be warned though that this will be a too-early call to
            // getter, so we'll want to make the runtime check skippable

            Binder.ProcessingAsync += (f, c) =>
            {
                InitialValue = (T)chained.Field.Value;
                return new ValueTask();
            };
        }

        public FluentBinder2(IBinder2 binder, bool initial) : 
            base(binder, typeof(T))
        {
            string name = binder.Field.Name;

            if (initial)
                // DEBT: Needs refiniement
                Field = new ShimFieldBase2<T>(name, statuses, () => (T)binder.getter());
            else
                Field = new ShimFieldBase2<T>(name, statuses, () => InitialValue);

            Initialize();
        }


        public FluentBinder2(IBinder2<T> binder, bool initial = true) : 
            base(binder, typeof(T))
        {
            string name = binder.Field.Name;

            if (initial)
                // DEBT: Needs refiniement
                Field = new ShimFieldBase2<T>(name, statuses, binder.getter);
            else
                Field = new ShimFieldBase2<T>(name, statuses, () => InitialValue);

            Initialize();
        }


        /// <summary>
        /// EXPERIMENTAL
        /// Hard wired to create a new FieldBinder ("v3" Binder)
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        /// <remarks>
        /// I think in real life we won't be directly calling this constructor from user code
        /// And also although FieldBinder is a fine choice, it may not be the only game in town
        /// </remarks>
        public FluentBinder2(string fieldName, Func<T> getter, Action<T> setter = null) :
            this(new FieldBinder<T>(fieldName, getter, setter), true)
        {
            
        }
    }


    public class FluentBinder2<T, TTrait> : FluentBinder2<T>,
        IFluentBinder<T, TTrait>
    {
        public TTrait Trait { get; }

        public FluentBinder2(IFluentBinder<T> chained) : 
            base(chained)
        {
        }

        public FluentBinder2(IBinder2 binder, bool initial = true, 
            TTrait trait = default(TTrait)) : 
            base(binder, initial)
        {
            Trait = trait;
        }
    }
}