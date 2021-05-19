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
            field.Statuses.Add(item);
        }
    }


    public class FieldStatusAggregator : IFieldStatusProvider
    {
        List<IFieldStatusProvider> providers = new List<IFieldStatusProvider>();

        public IEnumerable<FieldStatus> Statuses => 
            providers.SelectMany(x => x.Statuses);

        public bool IsValid => providers.All(x => x.IsValid);
    }


    public class Binder
    {
        object converted;
        readonly FieldStatus field;

        public Binder(string name)
        {
            field = new FieldStatus(name, null);
        }

        public event Action<object> Finalize;
        public event Action<FieldStatus> Validate;
        public event Func<FieldStatus, object, object> Convert;

        public FieldStatus Evaluate(object value)
        {
            var f = new FieldStatus(field.Name, value);

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
