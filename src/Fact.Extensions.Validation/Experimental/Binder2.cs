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
        
        public CancellationToken CancellationToken { get; set; }
    }
    
    /// <summary>
    /// Boilerplate for less-typed filter-only style binder
    /// </summary>
    public class Binder2 : BinderBase
    {
        public Binder2(IField field) : base(field)
        {
        }

        public delegate void ProcessingDelegate(IField f, Context2 context);
        // ValueTask guidance here:
        // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
        public delegate ValueTask ProcessingDelegateAsync(IField f, Context2 context);

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
            var context = new Context2();
            context.CancellationToken = ct;
            // NOTE: Odd that following line doesn't compile now.
            // Fortunately our scenario that's OK
            //Processing?.Invoke(field, context);
            var delegates = ProcessingAsync.GetInvocationList();

            var nonsequential = new LinkedList<Task>();

            foreach (ProcessingDelegateAsync d in delegates)
            {
                context.Sequential = true;
                ValueTask task = d(field, context);
                if (context.Sequential)
                    await task;
                else
                    nonsequential.AddLast(task.AsTask());
            }

            Task.WhenAll(nonsequential);
        }
    }


    public static class Binder2Extensions
    {
        public static FluentBinder2<T> As<T>(this Binder2 binder)
        {
            return new FluentBinder2<T>(binder, (T)binder.getter());
        }

        public delegate bool tryConvertDelegate<TFrom, TTo>(TFrom from, out TTo to);

        public static FluentBinder2<T> IsTrue<T>(this FluentBinder2<T> fb, Func<T, bool> predicate, 
            string messageIfFalse, FieldStatus.Code level = FieldStatus.Code.Error)
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField<T> f = fb.Field;
                if(!predicate(f.Value))
                    fb.Field.Add(level, messageIfFalse);

                return new ValueTask();
            };
            return fb;
        }

        public static FluentBinder2<T> IsTrueAsync<T>(this FluentBinder2<T> fb, Func<T, ValueTask<bool>> predicate, 
            string messageIfFalse, FieldStatus.Code level = FieldStatus.Code.Error, bool sequential = true)
        {
            fb.Binder.ProcessingAsync += async (field, context) =>
            {
                IField<T> f = fb.Field;
                context.Sequential = sequential;
                if(!await predicate(f.Value))
                    fb.Field.Add(level, messageIfFalse);
            };
            return fb;
        }

        public static FluentBinder2<T> LessThan<T>(this FluentBinder2<T> fb, T value)
            where T : IComparable
        {
            return fb.IsTrue(v => v.CompareTo(value) < 0,
                $"Must be less than {value}");
        }

        
        public static FluentBinder2<T> GreaterThan<T>(this FluentBinder2<T> fb, T value)
            where T : IComparable
        {
            return fb.IsTrue(v => v.CompareTo(value) > 0,
                $"Must be greater than {value}");
        }

        public static FluentBinder2<TTo> Convert<T, TTo>(this FluentBinder2<T> fb, 
            tryConvertDelegate<IField<T>, TTo> converter)
        {
            var fb2 = new FluentBinder2<TTo>(fb.Binder, default(TTo));
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (converter(fb.Field, out TTo converted))
                {
                    context.Value = converted;
                    fb2.test1 = converted;
                }

                return new ValueTask();
            };
            return fb2;
        }

        public static FluentBinder2<TTo> Convert<TTo>(this IFluentBinder2 fb)
        {
            var fb2 = new FluentBinder2<TTo>(fb.Binder, default(TTo));
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                var converted = (TTo)
                    System.Convert.ChangeType(fb.Field.Value, typeof(TTo));

                context.Value = converted;
                fb2.test1 = converted;

                return new ValueTask();
            };
            return fb2;
        }
    }
    
    public class ShimFieldBase2<T> : ShimFieldBase,
        IField<T>
    {
        readonly Func<T> getter;    // TODO: Maybe make this acquired direct from FluentBinder2
        public override object Value => getter();

        T IField<T>.Value => getter();

        internal ShimFieldBase2(IBinderBase binder, ICollection<FieldStatus.Status> statuses, 
            Func<T> getter) :
            base(binder, statuses)
        {
            this.getter = getter;
        }
    }

    public interface IFluentBinder2
    {
        Binder2 Binder { get; }
        
        IField Field { get; }
    }

    public class FluentBinder2<T> : IFluentBinder2
    {
        internal T test1;
        
        readonly Binder2 binder;

        public Binder2 Binder => binder;
        public ShimFieldBase2<T> Field { get; }

        IField IFluentBinder2.Field => Field;

        readonly List<FieldStatus.Status> statuses = new List<FieldStatus.Status>();

        public FluentBinder2(Binder2 binder, T value)
        {
            test1 = value;
            this.binder = binder;
            Field = new ShimFieldBase2<T>(binder, statuses, () => test1);
            
            // DEBT
            var f = (FieldStatus) binder.Field;
            f.Add(statuses);
        }
    }
}