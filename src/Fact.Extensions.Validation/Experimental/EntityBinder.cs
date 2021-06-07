using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    // TODO: Make an IEntityBinder so that we can do an IEntityBinder<T>
    public class EntityBinder : Binder2,
        IAggregatedBinder
    {
        List<IBinderProvider> items = new List<IBinderProvider>();

        public abstract class Item : AggregatedBinderBase.ItemBase
        {
            public PropertyInfo Property { get; }

            public abstract void InitValidation();
            
            public Item(IBinder binder, PropertyInfo property) : base(binder)
            {
                Property = property;
            }
        }


        public class Item<T> : Item
        {
            public FluentBinder2<T> FluentBinder { get; }

            public override void InitValidation()
            {
                var shimField = FluentBinder.Field;
                
                FluentBinder.Binder.ProcessingAsync += (f, context) =>
                {
                    // handled automatically by FluentBinder2
                    //statuses.Clear();

                    foreach (var attribute in Property.GetCustomAttributes().OfType<ValidationAttribute>())
                    {
                        attribute.Validate(shimField);
                    }

                    return new ValueTask();
                };

            }

            public Item(FluentBinder2<T> fb, PropertyInfo property) : 
                base(fb.Binder, property)
            {
                FluentBinder = fb;
            }
        }

        public object Value { get; set; }

        // FIX: Need to fuse this and Binder2.Process, if we can
        public async new Task Process(CancellationToken ct = default)
        {
            foreach(var item in items)
            {
                await item.Binder.Process(ct);
            }
        }

        public IEnumerable<IField> Fields => items.Select(x => x.Binder.Field);

        public IEnumerable<IBinder2> Binders => items.Select(x => x.Binder);

        public IEnumerable<IBinderProvider> Items => items;

        public EntityBinder(IField field) : base(field) { }

        public void Add(IBinderProvider item)
        {
            items.Add(item);
        }
    }


    public static class EntityBinderExtensions
    {
        static void ValidationHelper<T>(IBinder2<T> binder, PropertyInfo property)
        {
            var item = CreatePropertyItem2(binder, property);
            var shimField = item.FluentBinder.Field;
            //var fieldBinder = item.FluentBinder.Binder;
            var fieldBinder = binder;
            
            item.InitValidation();
            /*
            fieldBinder.ProcessingAsync += (f, context) =>
            {
                // handled automatically by FluentBinder2
                //statuses.Clear();

                foreach (var attribute in property.GetCustomAttributes().OfType<ValidationAttribute>())
                {
                    attribute.Validate(shimField);
                }

                return new ValueTask();
            };
            */
        }

        static EntityBinder.Item<T> CreatePropertyItem2<T>(IBinder2<T> binder, PropertyInfo property)
        {
            var fb = new FluentBinder2<T>(binder, true);

            // DEBT: A little sloppy having FluentBinder magically do this
            //var statuses = new LinkedList<Status>();
            //field.Add(statuses);
            //var shimField = new ShimFieldBase2<T>(fieldBinder, statuses, getter);
            /*
            var shimField = fb.Field;

            fieldBinder.ProcessingAsync += (f, context) =>
            {
                // handled automatically by FluentBinder2
                //statuses.Clear();

                foreach (var attribute in property.GetCustomAttributes().OfType<ValidationAttribute>())
                {
                    attribute.Validate(shimField);
                }

                return new ValueTask();
            }; */

            var item = new EntityBinder.Item<T>(fb, property);

            return item;
        }

        static EntityBinder.Item<T> CreatePropertyItem<T>(EntityBinder binder, PropertyInfo property)
        {
            var field = new FieldStatus<T>(property.Name, default(T));
            Func<T> getter = () => (T)property.GetValue(binder.Value);
            var fieldBinder = new Binder2<T>(field, getter);

            return CreatePropertyItem2(fieldBinder, property);
        }

        static void InputHelper<T>(EntityBinder binder, PropertyInfo property, bool initValidation)
        {
            var item = CreatePropertyItem<T>(binder, property);

            if(initValidation)
                item.InitValidation();
            
            binder.Add(item);
        }

        public static void BindInput(this EntityBinder binder, Type t, bool initValidation = false)
        {
            //t.GetTypeInfo().GetProperties();
            IEnumerable<PropertyInfo> properties = t.GetRuntimeProperties();

            //var helper = typeof(EntityBinderExtensions).GetRuntimeMethod(nameof(Helper),);
            var t2 = typeof(EntityBinderExtensions);
            var helperMethod = t2.GetRuntimeMethods().Single(x => x.Name == nameof(InputHelper));

            foreach (var property in properties)
            {
                var h = helperMethod.MakeGenericMethod(property.PropertyType);
                h.Invoke(null, new object[] { binder, property, initValidation });
            }
        }


        public static void BindValidation(this EntityBinder binder, Type t)
        {
            IEnumerable<PropertyInfo> properties = t.GetRuntimeProperties();
            IEnumerable<IBinder2> binders = binder.Binders;

            /*
            foreach (var item in binder.Items.OfType<EntityBinder.Item>())
            {
                if(item.Property.PropertyType == t)
                    item.InitValidation();
            } */
            
            var t2 = typeof(EntityBinderExtensions);
            var helperMethod = t2.GetRuntimeMethods().Single(x => x.Name == nameof(ValidationHelper));
            
            foreach (var property in properties)
            {
                IBinder2 match = binders.SingleOrDefault(b => b.Field.Name == property.Name);

                if (match != null)
                {
                    var h = helperMethod.MakeGenericMethod(property.PropertyType);
                    // DEBT: It is assumed binder is IBinder2<T>
                    h.Invoke(null, new object[] { match, property });
                }
            }
        }


        public static Committer BindOutput(this EntityBinder binder, Type t, object instance, 
            Committer committer = null)
        {
            IEnumerable<PropertyInfo> properties = t.GetRuntimeProperties();
            Dictionary<string, IBinder2> binders = binder.Binders.ToDictionary(x => x.Field.Name, y => y);
            
            if (committer == null) committer = new Committer(); 

            foreach(var property in properties)
            {
                if(binders.TryGetValue(property.Name, out IBinder2 b))
                {
                    // DEBT: Assign to DBNull or similar so we can tell if it's uninitialized
                    object staged = null;
                    
                    b.ProcessedAsync += (f, context) =>
                    {
                        staged = context.Value;
                        return new ValueTask();
                    };

                    committer.Committing += () =>
                    {
                        property.SetValue(instance, staged);
                        return new ValueTask();
                    };
                }
            }

            return committer;
        }


        public static Committer BindOutput<T>(this EntityBinder binder, T instance) =>
            binder.BindOutput(typeof(T), instance);


        public static void BindInput2<T>(this EntityBinder binder, T t, bool initValidation = false)
        {
            binder.Value = t;
            binder.BindInput(typeof(T), initValidation);
        }
    }


    public abstract class ValidationAttribute : Attribute
    {
        public abstract void Validate<T>(IField<T> field);
    }

    public class RequiredAttribute : ValidationAttribute
    {
        public override void Validate<T>(IField<T> field)
        {
            if (field.Value == null)
                field.Error(FieldStatus.ComparisonCode.IsNull, null, "");
        }
    }


    public static class FieldAssertExtensions
    {
    }
}
