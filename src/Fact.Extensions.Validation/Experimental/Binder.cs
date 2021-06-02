using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fact.Extensions.Validation.Experimental
{
    public class FieldStatusCollection :
        IFieldStatusCollector, IFieldStatusProvider
    {
        List<FieldStatus> fields = new List<FieldStatus>();

        public IEnumerable<FieldStatus> Statuses => fields;

        public bool IsValid => !fields.OnlyErrors().Any();

        public void Append(string name, FieldStatus.Status item)
        {
            FieldStatus field = fields.FirstOrDefault(x => x.Name == name);
            if(field == null)
            {
                field = new FieldStatus(name, null);
                fields.Add(field);
            }
            field.Add(item);
        }
    }


    public class FieldStatusAggregator : IFieldStatusProvider
    {
        List<IFieldStatusProvider> providers = new List<IFieldStatusProvider>();

        public IEnumerable<FieldStatus> Statuses => 
            providers.SelectMany(x => x.Statuses);

        public bool IsValid => providers.All(x => x.IsValid);
    }


    public class InputContext
    {
        public string FieldName { get; set; }
    }


    public abstract class ShimFieldBase : IField
    {
        public string Name { get; }

        public virtual object Value => binder.getter();

        readonly internal ICollection<FieldStatus.Status> statuses;

        public IEnumerable<FieldStatus.Status> Statuses => binder.Field.Statuses;

        public void Add(FieldStatus.Status status) =>
            statuses.Add(status);

        internal readonly IBinderBase binder;

        internal ShimFieldBase(IBinderBase binder, ICollection<FieldStatus.Status> statuses)
        {
            this.statuses = statuses;
            this.binder = binder;
            Name = binder.Field.Name;
        }

    }


    public class ShimFieldBase<T> : 
        ShimFieldBase,
        IField<T>
    {
        public new T Value => (T)base.Value;

        internal ShimFieldBase(IBinderBase binder, ICollection<FieldStatus.Status> statuses) :
            base(binder, statuses)
        { 
        }
    }


    /// <summary>
    /// Like an abstract entity, distinct binders are added to this so that they
    /// all may interact with each other as a group.  Need not be a "flat" structure
    /// like a simple POCO
    /// </summary>
    public class GroupBinder : IFieldStatusCollector
    {
        /// <summary>
        /// Serves as a shim so that error registrations for this field associate to
        /// the EntityBinder too
        /// Also serves as a 1:1 holder for the underlying field binder
        /// </summary>
        internal class _Item : ShimFieldBase
        {
            internal _Item(IBinderBase binder) : 
                base(binder, new List<FieldStatus.Status>())
            {
            }
        }

        readonly Dictionary<string, _Item> fields = new Dictionary<string, _Item>();

        public BinderBase Add(IField field)
        {
            var binder = new BinderBase(field);
            binder.getter = () => field.Value;
            Add(binder);
            return binder;
        }

        public void Add(IBinderBase binder)
        {
            var item = new _Item(binder);
            // DEBT: Can't be doing this cast all the time.  It's safe for the moment
            var field = (FieldStatus)binder.Field;
            field.Add(item.statuses);
            fields.Add(binder.Field.Name, item);
        }

        public void Clear()
        {
            foreach (_Item item in fields.Values)
                item.statuses.Clear();
        }

        /// <summary>
        /// Returns shimmed field
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IField this[string name] => fields[name];

        /// <summary>
        /// List of original non-shimmed fields
        /// </summary>
        public IEnumerable<IField> Fields => fields.Values.Select(x => x.binder.Field);

        /// <summary>
        /// TODO: Rename to Validating
        /// </summary>
        public event Action<GroupBinder, InputContext> Validate;

        public void Evaluate(InputContext context)
        {
            Clear();

            foreach(_Item item in fields.Values)
            {
                /* group is now distinct from one-off fields.  a 3rd party must 
                 * coordinate their validation together
                IBinder binder = item.binder;
                object _uncommitted = binder.getter();
                binder.Field.Value = _uncommitted;
                    binder.Evaluate(_uncommitted); */
            }

            Validate?.Invoke(this, context);
        }

        // Recommended to use shims instead
        public void Append(string fieldName, FieldStatus.Status status)
        {
            fields[fieldName].Add(status);
        }
    }

    public interface IBinderBase
    {
        /// <summary>
        /// Original 'canonical' field with aggregated/total status
        /// </summary>
        IField Field { get; }

        Func<object> getter { get; }

        IField Evaluate();
    }


    public interface IBinderBase<T> : IBinderBase
    {
        new Func<T> getter { get; }
    }


    public interface IBinder : IBinderBase
    {
        void DoFinalize();

        // DEBT: May not want this here
        bool AbortOnNull { get; set; }
    }


    public interface IBinder<T> : IBinder
    {
        event FilterDelegate<T> Filter;
        event Action<IField<T>> Validate;
    }


    public interface IBinder<T, TFinal> : IBinder<T>
    {
        event Action<TFinal> Finalize;
    }

    public class BinderBaseBase
    {
        protected readonly IField field;

        public IField Field => field;

        public BinderBaseBase(IField field)
        {
            this.field = field;
        }

        public virtual IField Evaluate() { throw new NotImplementedException(); }

        public virtual void DoFinalize() { throw new NotImplementedException(); }
    }


    public class BinderBase<T> : BinderBaseBase
    {
        // DEBT:
        public Func<T> getter { get; set; }

        public BinderBase(IField field) : base(field) { }
    }


    public class BinderBase : BinderBase<object>,
        IBinderBase
    {
        public BinderBase(IField field) : base(field)
        {

        }
    }


    public class Context
    {
        /// <summary>
        /// When true, evaluation context proceeds normally (implicitly all the way until the end)
        /// When false, evaluation halts completely (catestrophic failure)
        /// Defaults to true
        /// </summary>
        public bool Abort = false;

        public enum Interaction
        {
            /// <summary>
            /// For automated processes with no interaction at all
            /// </summary>
            None,
            /// <summary>
            /// For human real-time events, happening in the sub-second range.  Things like mouse clicks and
            /// key presses.
            /// </summary>
            High,
            /// <summary>
            /// For things like overall field validation.  On the order of 1-10s expected interaction time
            /// </summary>
            Medium,
            /// <summary>
            /// For things like form submission.  On the order of 11s+ expected interaction time
            /// </summary>
            Low
        }
    }


    public interface IConvertValue<T>
    {
        T Value { set; }
    }

    public class ConvertEventArgs<T> : Context,
        IConvertValue<T>
    {
        T v;
        bool isSet;

        public bool IsSet => isSet;
        
        public T Value
        {
            get => v;
            set
            {
                v = value;
                isSet = true;
            }
        }
    }

    public delegate void FilterDelegate<T>(IField<T> field, Context context);

    /// <summary>
    /// Binder is a very fancy getter and IField status maintainer
    /// </summary>
    /// <typeparam name="T">Initial type, in field.Value</typeparam>
    /// <typeparam name="TFinal">Final type, after conversions and commit</typeparam>
    public class Binder<T, TFinal> : BinderBase,
        IBinder<T, TFinal>,
        IFieldStatusProvider2,
        IFieldStatusCollector2
    {
        /// <summary>
        /// When true, aborts validation processing when a null is seen (more or less treats a value as optional)
        /// When false, ignores null and proceeds forward passing null through onto validation chain
        /// </summary>
        public bool AbortOnNull { get; set; } = true;

        object converted;

        // For exporting status
        List<FieldStatus.Status> Statuses = new List<FieldStatus.Status>();

        /// <summary>
        /// Statuses tracked by this binder 1:1 with this tracked field
        /// </summary>
        List<FieldStatus.Status> InternalStatuses = new List<FieldStatus.Status>();

        IEnumerable<FieldStatus.Status> IFieldStatusProvider2.Statuses => Statuses;

        public void Add(FieldStatus.Status status) =>
            Statuses.Add(status);

        readonly new FieldStatus field;

        public Binder(FieldStatus field, Func<object> getter = null) : base(field)
        {
            this.getter = getter;
            this.field = field;
            Attach();
        }


        void Attach()
        {
            field.Add(InternalStatuses);
            //field.AddIfNotPresent(this);
        }

        // EXPERIMENTAL
        public object Value => field.Value;

        // EXPERIMENTAL
        public object Converted => converted;

        public event Action<TFinal> Finalize;
        /// <summary>
        /// EXPERIMENTAL - may want all Validate to operate this way
        /// </summary>
        public event FilterDelegate<T> Filter;
        /// <summary>
        /// TODO: Rename to Validating
        /// </summary>
        public event Action<IField<T>> Validate;

        /// <summary>
        /// Can only convert from T to TFinal
        /// If multiple conversions are needed, one must chain multiple binders
        /// </summary>
        public delegate void ConvertDelegate(IField<T> field, ConvertEventArgs<TFinal> eventArgs);

        public event ConvertDelegate Convert;

        public override IField Evaluate()
        {
            object value = getter();
            field.Value = value;
            var f = new ShimFieldBase<T>(this, InternalStatuses);

            Statuses.Clear();
            InternalStatuses.Clear();

            if (AbortOnNull)
            {
                if (value == null) return f;

                // DEBT: type specificity not welcome here.  What about 'null' integers, etc?
                if (value is string sValue && string.IsNullOrWhiteSpace(sValue))
                    return f;
            }

            if (Filter != null)
            {
                var context = new Context();
                foreach (var d in Filter.GetInvocationList().OfType<FilterDelegate<T>>())
                {
                    d(f, context);

                    if (context.Abort) return f;
                }
            }

            Validate?.Invoke(f);

            if (Convert != null)
            {
                converted = null;
                var cea = new ConvertEventArgs<TFinal>();
                // Easier to do with a ConvertContext to carry the obj around, or a manual list
                // of delegates which we can abort if things go wrong
                foreach (var d in Convert.GetInvocationList().OfType<ConvertDelegate>())
                {
                    if (cea.IsSet) break;
                    d(f, cea);
                }

                if(cea.IsSet)
                    converted = cea.Value;
                if (typeof(T) == typeof(TFinal))
                    converted = f.Value;
                
                // If convert chain reaches here without converting, it's the chain's responsibility
                // to report errors along the way via f.Error etc
                // Multi-type conversion not supported directly in one binder.  Either:
                // - chain together multiple binders
                // - store output somewhere other than cea.Value (you're on your own, but won't break validator)
            }
            else
                // DEBT: Naming is all wrong here
                this.converted = field.Value;

            return f;
        }

        public override void DoFinalize()
        {
            Finalize?.Invoke((TFinal)converted);
        }
    }

    public class Binder<T> : Binder<T, T>
    {
        public Binder(FieldStatus field, Func<object> getter = null) : 
            base(field, getter)
        {

        }
    }
}


namespace Fact.Extensions.Validation
{
    using Experimental;

    public static class BinderExtensions
    {
        // DEBT: This stateful and non-stateful can't fully coexist and we'll have to choose one
        // or the other eventually
        public static void Evaluate<T>(this BinderBase binder, T value)
        {
            binder.getter = () => value;
            binder.Evaluate();
        }


        public static FluentBinder<T> Assert<T>(this Binder<T> binder)
        {
            return new FluentBinder<T>(binder);
        }

        public static FluentBinder<T> IsTrue<T>(this FluentBinder<T> fluent, 
            Func<T, bool> predicate, string description, bool abortlIfFalse = false)
        {
            fluent.Binder.Filter += (f, c) =>
            {
                if (!predicate(f.Value))
                {
                    f.Error(description);
                    if (abortlIfFalse) c.Abort = true;
                }
            };
            return fluent;
        }


        public static FluentBinder<T> LessThan<T>(this FluentBinder<T> fluent, T lessThanValue,
            string description = "Must be less than {0}")
            where T: IComparable<T>
        {
            fluent.IsTrue(v => v.CompareTo(lessThanValue) < 0, 
                string.Format(description, lessThanValue));
            return fluent;
        }


        public static FluentBinder<T> GreaterThan<T>(this FluentBinder<T> fluent, T greaterThanValue, 
            string description = "Must be greater than {0}")
            where T: IComparable<T>
        {
            fluent.IsTrue(v => v.CompareTo(greaterThanValue) > 0, 
                string.Format(description, greaterThanValue));
            return fluent;
        }


        public static FluentBinder<T> Required<T>(this FluentBinder<T> fluent)
        {
            fluent.Binder.AbortOnNull = false;
            fluent.Binder.Filter += (f, c) =>
            {
                if (f.Value == null) f.Error("Field is required");
            };
            return fluent;
        }


        public static FluentBinder<string> Required(this FluentBinder<string> fluent, bool includeEmptyString = true)
        {
            fluent.Binder.AbortOnNull = false;
            fluent.Binder.Filter += (f, c) =>
            {
                if (includeEmptyString)
                    if (string.IsNullOrEmpty(f.Value)) f.Error("Field is required");
                else
                    if (f.Value == null) f.Error("Field is required");
            };
            return fluent;
        }

        public static void TryConvert<T, TFinal>(this IField<T> field,
            Func<T, IConvertValue<TFinal>, bool> converter, IConvertValue<TFinal> ctx, string err)
        {
            if(!converter(field.Value, ctx))
                field.Error(err);
        }


        public static void TryConvert<T, TFinal>(this Binder<T, TFinal> binder,
            Func<T, TFinal> converter)
        {
            
        }
    }


    public class FluentBinder<T>
    {
        public IBinder<T> Binder { get; }

        public FluentBinder(IBinder<T> binder)
        {
            Binder = binder;
        }
    }
}
