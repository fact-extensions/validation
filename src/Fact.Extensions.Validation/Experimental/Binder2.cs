using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Fact.Extensions.Validation.Experimental
{

    public delegate void ProcessingDelegate(IField f, Context2 context);
    // ValueTask guidance here:
    // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    public delegate ValueTask ProcessingDelegateAsync(IField f, Context2 context);


#if UNUSED
    /// <summary>
    /// Boilerplate for less-typed filter-only style binder
    /// </summary>
    [Obsolete]
    public class Binder2<T> : BinderBase<T>, IBinder2
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
#endif


    public class Optional<T>
    {
        public T Value { get; set; }
    }


    /// <summary>
    /// Field whose value comes from a getter, and statuses come from external party
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ShimFieldBase2<T> : ShimFieldBaseBase,
        IField<T>
    {
        readonly Func<T> getter;    // TODO: Maybe make this acquired direct from FluentBinder2

        public T Value => getter();

        internal ShimFieldBase2(string name, ICollection<Status> statuses, 
            Func<T> getter) :
            base(name, statuses, () => getter())
        {
            this.getter = getter;
        }
    }





#if UNUSED
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

        IField<T> IFieldProvider<IField<T>>.Field => Field;

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

            ((IBinder2)Binder).ProcessingAsync += (f, c) =>
            {
                InitialValue = (T)chained.Field.Value;
                return new ValueTask();
            };
        }

        public FluentBinder2(IBinderBase binder, bool initial) : 
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


        [Obsolete]
        public FluentBinder2(Binder2<T> binder, bool initial = true) : 
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
    }
#endif
}