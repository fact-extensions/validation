using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Fact.Extensions.Validation.Experimental
{
    public class Context2 : Context
    {
        public object Value { get; set; }    
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
        public delegate Task ProcessingDelegateAsync(IField f, Context2 context);

        public event ProcessingDelegate Processing;
        public event ProcessingDelegateAsync ProcessingAsync;

        public Task Process()
        {
            var context = new Context2();
            Processing?.Invoke(field, context);
            return Task.CompletedTask;
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
            fb.Binder.Processing += (field, context) =>
            {
                IField<T> f = fb.Field;
                if(!predicate(f.Value))
                    fb.Field.Add(level, messageIfFalse);
            };
            return fb;
        }

        public static FluentBinder2<TTo> Convert<T, TTo>(this FluentBinder2<T> fb, 
            tryConvertDelegate<IField<T>, TTo> converter)
        {
            var fb2 = new FluentBinder2<TTo>(fb.Binder, default(TTo));
            fb.Binder.Processing += (field, context) =>
            {
                if (converter(fb.Field, out TTo converted))
                {
                    context.Value = converted;
                    fb2.test1 = converted;
                }
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

    public class FluentBinder2<T>
    {
        internal T test1;
        
        readonly Binder2 binder;

        public Binder2 Binder => binder;
        public ShimFieldBase2<T> Field { get; }

        readonly List<FieldStatus.Status> statuses = new List<FieldStatus.Status>();

        public FluentBinder2(Binder2 binder, T value)
        {
            test1 = value;
            this.binder = binder;
            Field = new ShimFieldBase2<T>(binder, statuses, () => test1);
        }
    }
}