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
            return new FluentBinder2<T>(binder);
        }

        public delegate bool tryConvertDelegate<TFrom, TTo>(TFrom from, out TTo to);

        public static FluentBinder2<TTo> Convert<T, TTo>(this FluentBinder2<T> fb, 
            tryConvertDelegate<IField<T>, TTo> converter)
        {
            fb.Binder.Processing += (field, context) =>
            {
                if (converter(fb.Field, out TTo converted))
                {
                    context.Value = converted;
                }
            };
            var fb2 = new FluentBinder2<TTo>(fb.Binder);
            return fb2;
        }
    }

    public class FluentBinder2<T>
    {
        readonly Binder2 binder;

        public Binder2 Binder => binder;
        public ShimFieldBase<T> Field { get; }

        readonly List<FieldStatus.Status> statuses = new List<FieldStatus.Status>();

        public FluentBinder2(Binder2 binder)
        {
            this.binder = binder;
            Field = new ShimFieldBase<T>(binder, statuses);
        }
    }
}