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
        public object Value { get; set; }
        
        /// <summary>
        /// When true, signals Binder.Process that particular processor is completely
        /// awaited before next processor runs.  When false, particular process is
        /// treated fully asynchronously and the next processor begins evaluation in
        /// parallel.  Defaults to true 
        /// </summary>
        public bool Sequential { get; set; }
        
        public CancellationToken CancellationToken { get; }

        public Context2(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }
    }

    public delegate void ProcessingDelegate(IField f, Context2 context);
    // ValueTask guidance here:
    // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
    public delegate ValueTask ProcessingDelegateAsync(IField f, Context2 context);



    public interface IBinder2 : IBinder
    {
        event ProcessingDelegateAsync ProcessingAsync;
    }


    public interface IBinder2<T> : IBinder2,
        IBinderBase<T>
    {

    }

    /// <summary>
    /// Boilerplate for less-typed filter-only style binder
    /// </summary>
    public class Binder2<T> : BinderBaseBase, 
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

        public async Task Process(CancellationToken ct = default)
        {
            var context = new Context2(ct);
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
                if (context.Abort) return;
                if (context.Sequential)
                    await task;
                else
                    nonsequential.AddLast(task.AsTask());
            }

            // guidance from
            // https://stackoverflow.com/questions/27238232/how-can-i-cancel-task-whenall
            var tcs = new TaskCompletionSource<bool>(ct);
            await Task.WhenAny(Task.WhenAll(nonsequential), tcs.Task);
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

        public delegate bool tryConvertDelegate<TFrom, TTo>(TFrom from, out TTo to);

        public static FluentBinder2<T> IsTrue<T>(this FluentBinder2<T> fb, Func<T, bool> predicate, 
            Func<Status> getIsFalseStatus)
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField<T> f = fb.Field;
                if(!predicate(f.Value))
                    f.Add(getIsFalseStatus());

                return new ValueTask();
            };
            return fb;
        }

        public static FluentBinder2<T> IsTrue<T>(this FluentBinder2<T> fb, Func<T, bool> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error) =>
            fb.IsTrue(predicate, () => new Status(level, messageIfFalse));

        public static FluentBinder2<T> IsTrueScalar<T>(this FluentBinder2<T> fb, Func<T, bool> predicate,
            FieldStatus.ComparisonCode code, T compareTo, string messageIfFalse = null,
            Status.Code level = Status.Code.Error) =>
            fb.IsTrue(predicate, () => 
                new ScalarStatus(level, messageIfFalse, code, compareTo));
        
        public static FluentBinder2<T> IsTrueAsync<T>(this FluentBinder2<T> fb, Func<T, ValueTask<bool>> predicate, 
            string messageIfFalse, Status.Code level = Status.Code.Error, bool sequential = true)
        {
            fb.Binder.ProcessingAsync += async (field, context) =>
            {
                IField<T> f = fb.Field;
                context.Sequential = sequential;
                if(!await predicate(f.Value))
                    f.Add(level, messageIfFalse);
            };
            return fb;
        }

        public static FluentBinder2<T> Required<T>(this FluentBinder2<T> fb)
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (fb.Field.Value == null)
                    fb.Field.Error("Value required");
                return new ValueTask();
            };
            return fb;
        }

        public static FluentBinder2<T> LessThan<T>(this FluentBinder2<T> fb, T value,
            string errorDescription = null)
            where T : IComparable
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) < 0,
                FieldStatus.ComparisonCode.LessThan, value, errorDescription);
        }

        
        public static FluentBinder2<T> GreaterThan<T>(this FluentBinder2<T> fb, T value)
            where T : IComparable
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) > 0,
                FieldStatus.ComparisonCode.GreaterThan, value);
        }

        public static FluentBinder2<TTo> Convert<T, TTo>(this FluentBinder2<T> fb, 
            tryConvertDelegate<IField<T>, TTo> converter, string cannotConvert = null, Optional<TTo> defaultValue = null)
        {
            var fb2 = new FluentBinder2<TTo>(fb.Binder, false);
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if(defaultValue != null && context.Value == null)
                {
                    context.Value = defaultValue.Value;
                    fb2.test1 = defaultValue.Value;
                }
                else if (converter(fb.Field, out TTo converted))
                {
                    context.Value = converted;
                    fb2.test1 = converted;
                }
                else
                {
                    context.Abort = true;
                    if(cannotConvert != null)
                        fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, typeof(TTo), cannotConvert);
                }

                return new ValueTask();
            };
            return fb2;
        }

        public static FluentBinder2<TTo> Convert<T, TTo>(this FluentBinder2<T> fb,
            tryConvertDelegate<T, TTo> converter, string cannotConvert, Optional<TTo> defaultValue = null)
        {
            return fb.Convert<T, TTo>((IField<T> field, out TTo converted) =>
                converter(field.Value, out converted), cannotConvert, defaultValue);
        }
        

        public static FluentBinder2<TTo> Convert<TTo>(this IFluentBinder2 fb, Optional<TTo> defaultValue = null)
        {
            var fb2 = new FluentBinder2<TTo>(fb.Binder, false);
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField f = fb.Field;
                Type t = typeof(TTo);

                if(f.Value == null && defaultValue != null)
                {
                    context.Value = defaultValue.Value;
                    fb2.test1 = defaultValue.Value;
                    return new ValueTask();
                }    
                
                try
                {
                    var converted = (TTo)
                        System.Convert.ChangeType(f.Value, t);

                    context.Value = converted;
                    fb2.test1 = converted;
                }
                catch (FormatException)
                {
                    // DEBT: Get a candidate factory to grab descriptions from a comparisoncode + scalar
                    f.Error(FieldStatus.ComparisonCode.Unspecified, t, "Unable to convert to type {0}");
                    context.Abort = true;
                }

                return new ValueTask();
            };
            return fb2;
        }

        static bool FilterStatus(Status s)
            => s.Level != Status.Code.OK;

        public static FluentBinder2<T> Emit<T>(this FluentBinder2<T> fb, Action<T> emitter, 
            Func<Status, bool> whenStatus = null, bool bypassFilter = false)
        {
            if (whenStatus == null) whenStatus = FilterStatus;
            
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField<T> f = fb.Field;
                
                if(bypassFilter || !field.Statuses.Any(whenStatus))
                    emitter(f.Value);

                return new ValueTask();
            };
            return fb;
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

    public interface IFluentBinder2
    {
        IBinder2 Binder { get; }
        
        IField Field { get; }
    }

    public class FluentBinder2<T> : IFluentBinder2
    {
        internal T test1;
        
        readonly IBinder2 binder;

        public IBinder2 Binder => binder;
        public ShimFieldBase2<T> Field { get; }

        IField IFluentBinder2.Field => Field;

        readonly List<Status> statuses = new List<Status>();

        void Initialize()
        {
            // This event handler is more or less a re-initializer for subsequent
            // process/validation calls
            binder.ProcessingAsync += (field, context) =>
            {
                statuses.Clear();
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
}