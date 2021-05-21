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


    public class EntityBinder
    {
        object uncommitted;

        readonly Dictionary<string, Binder> fields = new Dictionary<string, Binder>();

        public Binder Add(string name, object initialValue = null)
        {
            var binder = new Binder(name, initialValue);
            fields.Add(name, binder);
            return binder;
        }

        public Binder this[string name] => fields[name];

        public event Action<EntityBinder, InputContext> Validate;

        public void Evaluate(object uncommitted, InputContext context)
        {
            this.uncommitted = uncommitted;

            foreach(Binder binder in fields.Values)
            {
                object _uncommitted = binder.getter();
                binder.Evaluate(_uncommitted);
            }

            Validate?.Invoke(this, context);
        }
    }


    public class Binder : 
        IFieldStatusProvider2,
        IFieldStatusCollector2
    {
        public Func<object> getter;

        object converted;
        readonly FieldStatus field;
        FieldStatus uncommitted;

        public FieldStatus Field => uncommitted;

        // For exporting status
        List<FieldStatus.Status> Statuses = new List<FieldStatus.Status>();

        IEnumerable<FieldStatus.Status> IFieldStatusProvider2.Statuses => Statuses;

        public void Add(FieldStatus.Status status) =>
            Statuses.Add(status);

        public Binder(string name, object initialValue = null)
        {
            field = new FieldStatus(name, initialValue);
        }

        // EXPERIMENTAL
        public object Value => uncommitted?.Value;

        // EXPERIMENTAL
        public object Converted => converted;

        public event Action<object> Finalize;
        public event Action<FieldStatus> Validate;
        public event Func<FieldStatus, object, object> Convert;

        public FieldStatus Evaluate<T>(T value)
        {
            // Copying to do pre-commit validation
            uncommitted = new FieldStatus(field.Name, value);
            //var f = new FieldStatus(field.Name, value);
            var f = uncommitted;

            f.Clear();
            Statuses.Clear();

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
