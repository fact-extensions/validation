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


    public class ShimFieldBase : IField
    {
        public string Name { get; }

        public object Value => binder.getter();

        readonly internal ICollection<FieldStatus.Status> statuses;

        public IEnumerable<FieldStatus.Status> Statuses => binder.Field.Statuses;

        public void Add(FieldStatus.Status status) =>
            statuses.Add(status);

        internal readonly IBinder binder;

        internal ShimFieldBase(IBinder binder, ICollection<FieldStatus.Status> statuses)
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

        internal ShimFieldBase(IBinder binder, ICollection<FieldStatus.Status> statuses) :
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
            internal _Item(IBinder binder) : 
                base(binder, new List<FieldStatus.Status>())
            {
            }
        }

        readonly Dictionary<string, _Item> fields = new Dictionary<string, _Item>();

        public IBinder Add(FieldStatus field)
        {
            var binder = new BinderBase(field);
            Add(binder);
            return binder;
        }

        public void Add(IBinder binder)
        {
            var item = new _Item(binder);
            fields.Add(binder.Field.Name, item);
        }

        public void Clear()
        {
            foreach (_Item item in fields.Values)
                item.statuses.Clear();
        }

        public IField this[string name] => fields[name];

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

    public interface IBinder
    {
        FieldStatus Field { get; }

        Func<object> getter { get; }

        IField Evaluate();
    }

    public class BinderBase : IBinder
    {
        // DEBT:
        public Func<object> getter { get; set; }

        protected readonly FieldStatus field;

        public FieldStatus Field => field;

        public BinderBase(FieldStatus field)
        {
            this.field = field;
        }

        public virtual IField Evaluate() { throw new NotImplementedException(); }
    }


    public class Context
    {
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

    /// <summary>
    /// Binder is a very fancy getter and IField status maintainer
    /// </summary>
    public class Binder<T> : BinderBase,
        IBinder,
        IFieldStatusProvider2,
        IFieldStatusCollector2
    {
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

        public Binder(FieldStatus field, Func<object> getter = null) : base(field)
        {
            this.getter = getter;
            Attach();
        }


        void Attach()
        {
            field.Add(InternalStatuses);
            //field.AddIfNotPresent(this);
        }

        /// <summary>
        /// When true, aborts validation processing when a null is seen (more or less treats a value as optional)
        /// When false, ignores null and proceeds forward passing null through onto validation chain
        /// </summary>
        public bool AbortOnNull = true;

        // EXPERIMENTAL
        public object Value => field.Value;

        // EXPERIMENTAL
        public object Converted => converted;

        public event Action<object> Finalize;
        /// <summary>
        /// EXPERIMENTAL - may want all Validate to operate this way
        /// </summary>
        public event FilterDelegate Filter;
        public event Action<IField<T>> Validate;

        public delegate void FilterDelegate(IField<T> field, Context context);
        public delegate object ConvertDelegate(IField<T> field, object from);

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
                foreach (var d in Filter.GetInvocationList().OfType<FilterDelegate>())
                {
                    d(f, context);

                    if (context.Abort) return f;
                }
            }

            Validate?.Invoke(f);

            if (Convert != null)
            {
                this.converted = null;
                object converted = field.Value;
                // Easier to do with a ConvertContext to carry the obj around, or a manual list
                // of delegates which we can abort if things go wrong
                foreach (var d in Convert.GetInvocationList().OfType<ConvertDelegate>())
                    converted = d(f, converted);

                this.converted = converted;
            }

            return f;
        }

        public void DoFinalize()
        {
            Finalize?.Invoke(converted);
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
        public static void Evaluate<T>(this Experimental.Binder<T> binder, T value)
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
    }


    public class FluentBinder<T>
    {
        readonly Binder<T> binder;

        public Binder<T> Binder => binder;

        public FluentBinder(Binder<T> binder)
        {
            this.binder = binder;
        }
    }
}
