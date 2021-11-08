using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public class FieldBinder : Experimental.Binder3Base
    {
        public IField Field { get; }

        protected FieldBinder(IField field)
        {
            Field = field;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="t"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        /// <remarks>
        /// DEBT: Doing this in part because GetConstructor isn't available for netstandard13
        /// </remarks>
        public static IFieldBinder Create(Type t, string name, Func<object> getter)
        {
            var ctorHelper = typeof(FieldBinder<>).MakeGenericType(t).
                GetRuntimeMethods().First((x => x.Name == nameof(FieldBinder<object>.Create)));
            var fieldBinder = ctorHelper.Invoke(null, new object[] { name, getter });

            return (IFieldBinder)fieldBinder;
        }
    }
    
    /// <summary>
    /// Core field binder attached to a particular type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FieldBinder<T> : FieldBinder,
        IFieldBinder<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        /// <remarks>
        /// DEBT: Sloppy cast on getter to accomodate FieldBinder.Create
        /// </remarks>
        public static FieldBinder<T> Create(string name, Func<object> getter)
        {
            return new FieldBinder<T>(name, () => (T)getter());
        }

        public Func<T> getter { get; }

        // DEBT: Pretty sure I do not like giving this a 'set;'
        public Action<T> setter { get; set; }

        Func<object> IBinderBase.getter => () => getter();

        public FieldBinder(IField field, Func<T> getter, Action<T> setter = null) : 
            base(field)
        {
            this.getter = getter;
            this.setter = setter;

            // DEBT: Somehow, we need at minimum this ProcessingAsync in here -- don't know why
            Processor.ProcessingAsync += (sender, context) =>
            {
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

        public async Task Process(Experimental.InputContext inputContext = null,
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
}
