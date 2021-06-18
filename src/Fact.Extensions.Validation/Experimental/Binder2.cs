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


    public interface IBinder2Base
    {
        event ProcessingDelegateAsync ProcessingAsync;
        event ProcessingDelegateAsync ProcessedAsync;

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

        public delegate bool tryConvertDelegate<TFrom, TTo>(TFrom from, out TTo to);

        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate, 
            Func<Status> getIsFalseStatus)
            where TFluentBinder: IFluentBinder<T>
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

        /*
        public static IFluentBinder<T> IsTrue<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            Func<Status> getIsFalseStatus) =>
            IsTrue<IFluentBinder<T>, T>(fb, predicate, getIsFalseStatus); */

        public static IFluentBinder<T> IsTrue<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error) =>
            fb.IsTrue(predicate, () => new Status(level, messageIfFalse));

        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error)
            where TFluentBinder: IFluentBinder<T> =>
            fb.IsTrue(predicate, () => new Status(level, messageIfFalse));

        public static IFluentBinder<T> IsTrueScalar<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            FieldStatus.ComparisonCode code, T compareTo, string messageIfFalse = null,
            Status.Code level = Status.Code.Error) =>
            fb.IsTrue(predicate, () => 
                new ScalarStatus(level, messageIfFalse, code, compareTo));

        public static TFluentBinder IsTrueScalar<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            FieldStatus.ComparisonCode code, T compareTo, string messageIfFalse = null,
            Status.Code level = Status.Code.Error)
            where TFluentBinder: IFluentBinder<T>
            =>
            fb.IsTrue(predicate, () => new ScalarStatus(level, messageIfFalse, code, compareTo));

        public static IFluentBinder<T> IsTrueAsync<T>(this IFluentBinder<T> fb, Func<T, ValueTask<bool>> predicate, 
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


        public static TFluentBinder StartsWith<TFluentBinder>(this TFluentBinder fb, string mustStartWith)
            where TFluentBinder: IFluentBinder<string>
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (!((string)fb.Field.Value).StartsWith(mustStartWith))
                    fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, mustStartWith,
                        $"Must start with: {mustStartWith}");
                return new ValueTask();
            };
            return fb;
        }


        public static TFluentBinder Contains<TFluentBinder>(this TFluentBinder fb, string mustContain)
            where TFluentBinder: IFluentBinder<string>
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (!((string)fb.Field.Value).Contains(mustContain))
                    fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, mustContain,
                        $"Must start with: {mustContain}");
                return new ValueTask();
            };
            return fb;
        }

        public static TFluentBinder Required<TFluentBinder>(this TFluentBinder fb)
            where TFluentBinder: IFluentBinder
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (fb.Field.Value == null)
                    fb.Field.Error("Value required");
                return new ValueTask();
            };
            return fb;
        }

        public static TFluentBinder LessThan<TFluentBinder, T>(this TFluentBinder fb, T value,
            string errorDescription = null)
            where T : IComparable
            where TFluentBinder : IFluentBinder<T>
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) < 0,
                FieldStatus.ComparisonCode.LessThan, value, errorDescription);
        }

        
        public static TFluentBinder GreaterThan<TFluentBinder, T>(this TFluentBinder fb, T value)
            where T : IComparable
            where TFluentBinder : IFluentBinder<T>
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) > 0,
                FieldStatus.ComparisonCode.GreaterThan, value);
        }

        public static TFluentBinder GreaterThanOrEqualTo<TFluentBinder, T>(this TFluentBinder fb, T value)
            where T : IComparable
            where TFluentBinder : IFluentBinder<T>
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) > 0,
                FieldStatus.ComparisonCode.GreaterThan, value);
        }


        public static FluentBinder2<TTo> Convert<T, TTo>(this IFluentBinder<T> fb, 
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

        public static FluentBinder2<TTo> Convert<T, TTo>(this IFluentBinder<T> fb,
            tryConvertDelegate<T, TTo> converter, string cannotConvert, Optional<TTo> defaultValue = null)
        {
            return fb.Convert<T, TTo>((IField<T> field, out TTo converted) =>
                converter(field.Value, out converted), cannotConvert, defaultValue);
        }
        

        public static FluentBinder2<TTo> Convert<TTo>(this IFluentBinder fb, Optional<TTo> defaultValue = null)
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


        static FluentBinder2<DateTimeOffset> FromEpochToDateTimeOffset<T>(this IFluentBinder<T> fb)
        {
            var fbConverted = fb.Convert((IField<T> f, out DateTimeOffset dt) =>
            {
                // NOTE: We know we can safely do this because only the <int> and <long> overloads
                // are permitted to call this method
                var value = System.Convert.ToInt64(f.Value);
                try
                {
                    dt = DateTimeOffset.FromUnixTimeSeconds(value);
                    return true;
                }
                catch (ArgumentOutOfRangeException aoore)
                {
                    f.Error(FieldStatus.ComparisonCode.Unspecified, f.Value, aoore.Message);
                    return false;
                }
            });
            return fbConverted;
        }

        public static FluentBinder2<DateTimeOffset> FromEpochToDateTimeOffset(this IFluentBinder<int> fb) =>
            FromEpochToDateTimeOffset<int>(fb);

        public static FluentBinder2<DateTimeOffset> FromEpochToDateTimeOffset(this IFluentBinder<long> fb) =>
            FromEpochToDateTimeOffset<long>(fb);

        public static FluentBinder2<DateTimeOffset> ToDateTimeOffset(this IFluentBinder<int, EpochTrait> fb) =>
            FromEpochToDateTimeOffset<int>(fb);

        public static FluentBinder2<DateTimeOffset> ToDateTimeOffset(this IFluentBinder<long, EpochTrait> fb) =>
            FromEpochToDateTimeOffset<long>(fb);

        static bool FilterStatus(Status s)
            => s.Level != Status.Code.OK;

        public static IFluentBinder<T> Emit<T>(this IFluentBinder<T> fb, Action<T> emitter, 
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


        public struct EpochTrait
        {

        }


        public static FluentBinder2<T, TTrait> WithTrait<T, TTrait>(this IFluentBinder<T> fb,
            TTrait trait = default(TTrait))
        {
            return new FluentBinder2<T, TTrait>(fb.Binder);
        }


        /// <summary>
        /// Tags the fluent binder as a UNIX Epoch
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fb"></param>
        /// <returns></returns>
        public static FluentBinder2<int, EpochTrait> AsEpoch(this IFluentBinder<int> fb) =>
            new FluentBinder2<int, EpochTrait>(fb.Binder, false);
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