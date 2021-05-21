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

        public object Value => binder.Value;

        readonly internal ICollection<FieldStatus.Status> statuses;

        public IEnumerable<FieldStatus.Status> Statuses => binder.Field.Statuses;

        public void Add(FieldStatus.Status status) =>
            statuses.Add(status);

        internal readonly Binder binder;

        internal ShimFieldBase(Binder binder, ICollection<FieldStatus.Status> statuses)
        {
            this.statuses = statuses;
            this.binder = binder;
            Name = binder.Field.Name;
        }

    }


    public class GroupBinder : IFieldStatusCollector
    {
        object uncommitted;

        /// <summary>
        /// Serves as a shim so that error registrations for this field associate to
        /// the EntityBinder too
        /// Also serves as a 1:1 holder for the underlying field binder
        /// </summary>
        class _Item : ShimFieldBase
        {
            internal _Item(Binder binder) : 
                base(binder, new List<FieldStatus.Status>())
            {
            }
        }

        readonly Dictionary<string, _Item> fields = new Dictionary<string, _Item>();

        public Binder Add(FieldStatus field)
        {
            var binder = new Binder(field);
            var item = new _Item(binder);
            fields.Add(field.Name, item);
            // Underlying field will now report error statuses tracked by our shim shield too
            binder.Field.Add(item.Statuses);
            return binder;
        }

        public void Clear()
        {
            foreach (_Item item in fields.Values)
                item.statuses.Clear();
        }

        public IField this[string name] => fields[name];

        public event Action<GroupBinder, InputContext> Validate;

        public void Evaluate(object uncommitted, InputContext context)
        {
            this.uncommitted = uncommitted;

            foreach(_Item item in fields.Values)
            {
                Binder binder = item.binder;
                object _uncommitted = binder.getter();
                binder.Evaluate(_uncommitted);
            }

            Validate?.Invoke(this, context);
        }

        public void Append(string fieldName, FieldStatus.Status status)
        {
            fields[fieldName].Add(status);
        }
    }


    public class Binder : 
        IFieldStatusProvider2,
        IFieldStatusCollector2
    {
        public Func<object> getter;

        object converted;
        readonly FieldStatus field;

        public FieldStatus Field => field;

        // For exporting status
        List<FieldStatus.Status> Statuses = new List<FieldStatus.Status>();

        /// <summary>
        /// Statuses tracked by this binder 1:1 with this tracked field
        /// </summary>
        List<FieldStatus.Status> InternalStatuses = new List<FieldStatus.Status>();

        IEnumerable<FieldStatus.Status> IFieldStatusProvider2.Statuses => Statuses;

        public void Add(FieldStatus.Status status) =>
            Statuses.Add(status);

        public Binder(FieldStatus field)
        {
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

        public event Action<object> Finalize;
        public event Action<IField> Validate;
        public event Func<FieldStatus, object, object> Convert;

        public IField Evaluate<T>(T value)
        {
            field.Value = value;
            var f = new ShimFieldBase(this, InternalStatuses);

            //f.Clear();
            Statuses.Clear();
            InternalStatuses.Clear();

            Validate?.Invoke(f);
            // Easier to do with a ConvertContext to carry the obj around, or a manual list
            // of delegates which we can abort if things go wrong
            //Convert?.Invoke(f);
            converted = value;

            return f;
        }

        public void DoFinalize()
        {
            Finalize?.Invoke(converted);
        }
    }
}
