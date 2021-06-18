using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Fact.Extensions.Validation.Experimental
{
    public class Context2 : Context
    {
        /// <summary>
        /// Current value, which starts as populated by the binder's getter but may
        /// be converted as the pipeline is processed
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// When true, signals Binder.Process that particular processor is completely
        /// awaited before next processor runs.  When false, particular process is
        /// treated fully asynchronously and the next processor begins evaluation in
        /// parallel.  Defaults to true 
        /// </summary>
        public bool Sequential { get; set; }
        
        public CancellationToken CancellationToken { get; }

        // Still experimental 
        public InputContext InputContext { get; set; }

        public Context2(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }
    }

    public delegate void ProcessingDelegate(IField f, Context2 context);
    // ValueTask guidance here:
    // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    public delegate ValueTask ProcessingDelegateAsync(IField f, Context2 context);




    public class Binder2Base : BinderBaseBase
    {
        public Binder2Base(IField field) : base(field)
        {
        }
    }

    /// <summary>
    /// Boilerplate for less-typed filter-only style binder
    /// </summary>
    public class Binder2<T> : Binder2Base, 
        IBinder2<T>
    {
        public bool AbortOnNull { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Func<T> getter2;

        Func<T> IBinderBase<T>.getter => getter2;

        public Func<object> getter => () => getter2();

        public Binder2(IField field) : base(field)
        {
        }

        public Binder2(IField<T> field, Func<T> getter = null) : base(field)
        {
            if (getter == null)
                getter = () => field.Value;
            this.getter2 = getter;
        }

        public event ProcessingDelegate Processing
        {
            remove
            {
                throw new NotSupportedException("Must use ProcessingAsync propery");
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
        public event ProcessingDelegateAsync ProcessingAsync;
        public event ProcessingDelegateAsync ProcessedAsync;

        protected Context2 CreateContext(CancellationToken ct) =>
            new Context2(ct);

        public async Task Process(InputContext inputContext = default, CancellationToken ct = default)
        {
            Context2 context = CreateContext(ct);
            context.InputContext = inputContext;
            context.Value = getter();

            // NOTE: Odd that following line doesn't compile now.
            // Fortunately our scenario that's OK
            //Processing?.Invoke(field, context);
            var delegates = ProcessingAsync.GetInvocationList();

            var nonsequential = new LinkedList<Task>();

            foreach (ProcessingDelegateAsync d in delegates)
            {
                context.Sequential = true;
                ValueTask task = d(field, context);
                if (context.Abort) break;
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
        }
    }


    public class Binder2 : Binder2<object>
    {
        public Binder2(IField field) : base(field)
        {

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
    
    public class ShimFieldBase2<T> : ShimFieldBase,
        IField<T>
    {
        readonly Func<T> getter;    // TODO: Maybe make this acquired direct from FluentBinder2
        public override object Value => getter();

        T IField<T>.Value => getter();

        internal ShimFieldBase2(IBinderBase binder, ICollection<Status> statuses, 
            Func<T> getter) :
            base(binder, statuses)
        {
            this.getter = getter;
        }
    }



    public class FluentBinder2<T> : IFluentBinder<T>
    {
        internal T test1;
        
        readonly IBinder2 binder;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Experimentally putting this here instead of inside binder itself.  That way, conversion chains can have
        /// independent logic for whether to treat null as an exception or a silent pass
        /// NOT FUNCTIONAL
        /// </remarks>
        public bool AbortOnNull { get; set; } = true;

        public IBinder2 Binder => binder;
        public ShimFieldBase2<T> Field { get; }

        IField IFluentBinder.Field => Field;

        readonly List<Status> statuses = new List<Status>();

        void Initialize()
        {
            // This event handler is more or less a re-initializer for subsequent
            // process/validation calls
            binder.ProcessingAsync += (field, context) =>
            {
                statuses.Clear();
                // Doesn't quite work because some scenarios have parallel FluentBinders
                //if (AbortOnNull && Field.Value == null)
                    //context.Abort = true;
                return new ValueTask();
            };

            // DEBT
            var f = (IFieldStatusExternalCollector)binder.Field;
            f.Add(statuses);
        }

        public FluentBinder2(IBinder2 binder, bool initial)
        {
            this.binder = binder;

            if (initial)
                // DEBT: Needs refiniement
                Field = new ShimFieldBase2<T>(binder, statuses, () => (T)binder.getter());
            else
                Field = new ShimFieldBase2<T>(binder, statuses, () => test1);

            Initialize();
        }


        public FluentBinder2(IBinder2<T> binder, bool initial = true)
        {
            this.binder = binder;

            if (initial)
                // DEBT: Needs refiniement
                Field = new ShimFieldBase2<T>(binder, statuses, binder.getter);
            else
                Field = new ShimFieldBase2<T>(binder, statuses, () => test1);

            Initialize();
        }
    }


    public class FluentBinder2<T, TTrait> : FluentBinder2<T>,
        IFluentBinder<T, TTrait>
    {
        public TTrait Trait { get; }

        public FluentBinder2(IBinder2 binder, bool initial = true, 
            TTrait trait = default(TTrait)) : 
            base(binder, initial)
        {
            Trait = trait;
        }
    }
}