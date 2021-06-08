﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public abstract class PropertyBinderProvider : AggregatedBinderBase.ItemBase
    {
        public PropertyInfo Property { get; }

        public abstract void InitValidation();
            
        public PropertyBinderProvider(IBinder binder, PropertyInfo property) : base(binder)
        {
            Property = property;
        }
    }


    public class PropertyBinderProvider<T> : PropertyBinderProvider
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

        public PropertyBinderProvider(FluentBinder2<T> fb, PropertyInfo property) : 
            base(fb.Binder, property)
        {
            FluentBinder = fb;
        }
    }

    // TODO: Make an IEntityBinder so that we can do an IEntityBinder<T>
    public class AggregatedBinder : Binder2,
        IAggregatedBinder
    {
        List<IBinderProvider> items = new List<IBinderProvider>();


        public IEnumerable<IBinder2> Binders => items.Select(x => x.Binder);

        public IEnumerable<IBinderProvider> Items => items;

        public AggregatedBinder(IField field) : base(field)
        {
            // DEBT: A little sloppy to lazy init our getter, however aggregated
            // binder may indeed not need to retrieve any value for context.Value
            // during Process call
            getter2 = () => null;
        }
        
        public void Add(IBinderProvider item)
        {
            items.Add(item);
            ProcessingAsync += async (field, context) => 
                await item.Binder.Process(context.CancellationToken);
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

        static PropertyBinderProvider<T> CreatePropertyItem2<T>(IBinder2<T> binder, PropertyInfo property)
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

            var item = new PropertyBinderProvider<T>(fb, property);

            return item;
        }

        static PropertyBinderProvider<T> CreatePropertyItem<T>(IBinder2 binder, PropertyInfo property)
        {
            var field = new FieldStatus<T>(property.Name, default(T));
            Func<T> getter = () => (T)property.GetValue(binder.getter());
            var fieldBinder = new Binder2<T>(field, getter);

            return CreatePropertyItem2(fieldBinder, property);
        }

        static void InputHelper<T>(IAggregatedBinder binder, PropertyInfo property, bool initValidation)
        {
            var item = CreatePropertyItem<T>(binder, property);

            if(initValidation)
                item.InitValidation();
            
            binder.Add(item);
        }

        public static void BindInput(this IAggregatedBinder binder, Type t, bool initValidation = false)
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


        // TODO: Add a flag indicating whether to treat non-present fields as either 'null'
        // or to skip validating them entirely - although seems to me almost definitely we
        // want the former
        public static void BindValidation(this IAggregatedBinder binder, Type t)
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
                else
                {
                    // TODO: Optionally ake a new always-null field+binder here
                }
            }
        }


        public static Committer BindOutput(this IAggregatedBinder binder, Type t, object instance, 
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


        public static Committer BindOutput<T>(this AggregatedBinder binder, T instance) =>
            binder.BindOutput(typeof(T), instance);


        public static void BindInput2<T>(this AggregatedBinder binder, T t, bool initValidation = false)
        {
            binder.getter2 = () => t;
            binder.BindInput(typeof(T), initValidation);
        }


        public static FluentBinder2<T> AddField<T>(this IAggregatedBinder binder, string name, Func<T> getter, 
            Func<IBinder2, IBinderProvider> providerFactory)
        {
            var f = new FieldStatus<T>(name, default(T));
            var b = new Binder2<T>(f, getter);
            var fb = new FluentBinder2<T>(b);
            binder.Add(providerFactory(b));
            return fb;
        }

        public static FluentBinder2<T> AddField<T>(this IAggregatedBinder binder, string name, Func<T> getter) =>
            binder.AddField(name, getter, b => new BinderManagerBase.ItemBase(b));
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
