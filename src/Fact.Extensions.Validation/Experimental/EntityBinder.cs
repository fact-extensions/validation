using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public interface IPropertyProvider
    {
        PropertyInfo Property { get; }
    }

    public abstract class PropertyBinderProvider : AggregatedBinderBase.ItemBase, IPropertyProvider
    {
        public PropertyInfo Property { get; }

        public static void InitValidation<T>(IFluentBinder<T> fluentBinder, PropertyInfo property)
        {
            var shimField = fluentBinder.Field;
            var attributes = property.GetCustomAttributes().OfType<ValidationAttribute>();

            foreach (var a in attributes)
                a.Configure(fluentBinder);

            fluentBinder.Binder.ProcessingAsync += (f, context) =>
            {
                // handled automatically by FluentBinder2
                //statuses.Clear();

                foreach (var attribute in attributes)
                {
                    attribute.Validate(shimField, context);
                }

                return new ValueTask();
            };
        }


        public abstract void InitValidation();
            
        public PropertyBinderProvider(IBinder binder, IFluentBinder fluentBinder,
            PropertyInfo property) : base(binder, fluentBinder)
        {
            Property = property;
        }
    }


    public class PropertyBinderProvider<T> : 
        PropertyBinderProvider,
        IBinderProvider<T>
    {
        public new IFluentBinder<T> FluentBinder { get; }

        public new IBinder2<T> Binder => (IBinder2<T>)base.Binder;

        public override void InitValidation()
        {
            InitValidation(FluentBinder, Property);
        }

        public PropertyBinderProvider(IFluentBinder<T> fb, PropertyInfo property) : 
            base(fb.Binder, fb, property)
        {
            FluentBinder = fb;
        }
    }

    public delegate void BindersProcessedDelegate(IEnumerable<IBinderProvider> binders, Context2 context);

    // TODO: Make an IEntityBinder so that we can do an IEntityBinder<T>
    public class AggregatedBinder : Binder2,
        IAggregatedBinder
    {
        /// <summary>
        /// Occurs after interactive/discrete binder processing, whether it generated new status or not
        /// </summary>
        public event BindersProcessedDelegate BindersProcessed;

        protected void FireFieldsProcessed(IEnumerable<IBinderProvider> fields, Context2 context) =>
            BindersProcessed?.Invoke(fields, context);

        List<IBinderProvider> items = new List<IBinderProvider>();

        public IServiceProvider Services { get; }

        public IEnumerable<IBinderProvider> Providers => items;

        internal class Context : Context2
        {
            internal Context(CancellationToken ct) : base(ct)
            {

            }
        }

        public AggregatedBinder(IField field, IServiceProvider services = null) : base(field)
        {
            // DEBT: A little sloppy to lazy init our getter, however aggregated
            // binder may indeed not need to retrieve any value for context.Value
            // during Process call
            getter2 = () => null;
            Services = services;

            // DEBT: Sloppy assigning a new InputContext during processing chain.  We do this so that the
            // default input context is set even when one initiates aggregatedBinder.Process() with no arguments
            ProcessingAsync += (f, c) =>
            {
                if (c.InputContext == null)
                    c.InputContext = new InputContext { InitiatingEvent = InitiatingEvents.Load };
                return new ValueTask();
            };

            // TODO: Suppress FieldsProcessed-per-field firing when doing an overall process event.  Probably
            // best way to do that is via a specialized Context which also tracks processing source or similar

            ProcessedAsync += (f, c) =>
            {
                // Filter out overall load/aggregated Process
                if (c.InputContext?.InitiatingEvent == InitiatingEvents.Load)
                    FireFieldsProcessed(items, c);

                return new ValueTask();
            };
        }

        public void Add(IBinderProvider item)
        {
            // DEBT: event is kind of more of a refresh, but Load will do for now as a blunt instrument
            var inputContext = new InputContext { InitiatingEvent = InitiatingEvents.Load };

            items.Add(item);
            ProcessingAsync += async (field, context) => 
                await item.Binder.Process(inputContext, context.CancellationToken);
            item.Binder.ProcessedAsync += (field, context) =>
            {
                // Filter out overall load/aggregated Process
                if(context.InputContext?.InitiatingEvent != InitiatingEvents.Load)
                    FireFieldsProcessed(new[] { item }, context);

                return new ValueTask();
            };
        }
    }


    /// <summary>
    /// Experimental helper for only-reflection-entity assistance
    /// </summary>
    public class EntityBinder : IAggregatedBinderBase
    {
        public Type EntityType { get; }
        
        protected readonly List<PropertyBinderProvider> providers = new List<PropertyBinderProvider>();

        public IEnumerable<IBinder2> Binders => providers.Select(x => x.Binder);

        public IEnumerable<IBinderProvider> Providers => providers;

        // DEBT: A bit sloppy here we might want a more discrete IAggregatedBinderBase instead
        public void Add(IBinderProvider binderProvider) =>
            Add((PropertyBinderProvider) binderProvider);
        
        public void Add(PropertyBinderProvider binderProvider)
        {
            providers.Add(binderProvider);
        }

        public EntityBinder(Type entityType)
        {
            EntityType = entityType;
        }

        void BindValidation()
        {
            this.BindValidation(EntityType);
        }


        Committer BindOutput(object instance, Committer committer = null)
        {
            return this.BindOutput(EntityType, instance, committer);
        }
    }

    public interface IEntityBinder : IAggregatedBinderBase
    {
        IEnumerable<PropertyBinderProvider> Binders { get; }
    }

    public interface IEntityBinder<T> : IEntityBinder
    {
    }


    public class EntityBinder<T> : EntityBinder, IEntityBinder<T>
    {
        new public IEnumerable<PropertyBinderProvider> Binders => providers;

        public EntityBinder() : base(typeof(T))
        {

        }

        public PropertyBinderProvider<TProperty> Get<TProperty>(Expression<Func<T, TProperty>> propertyLambda)
        {
            var name = propertyLambda.Name;
            var member = propertyLambda.Body as MemberExpression;
            var properInfo = member.Member as PropertyInfo;

            var p = providers.Single(x => x.Property == properInfo);
            return (PropertyBinderProvider<TProperty>)p;
        }
    }


    public static class EntityBinderExtensions
    {
        //static void ValidationHelper<T>(IBinder2<T> binder, PropertyInfo property)
        static void ValidationHelper<T>(IBinderProvider binderProvider, PropertyInfo property)
        {
            //var binder = binderProvider.Binder;
            // NOTE: Binder sometimes isn't typed, but IFluentBinder<T> always has a T
            var fluentBinder = (IFluentBinder<T>)binderProvider.FluentBinder;
            //var item = CreatePropertyItem2<T>(binder, property);
            //var shimField = item.FluentBinder.Field;
            //var fieldBinder = item.FluentBinder.Binder;
            //var fieldBinder = binder;

            //item.InitValidation();

            PropertyBinderProvider.InitValidation(fluentBinder, property);
        }

        static PropertyBinderProvider<T> CreatePropertyItem2<T>(IBinder2 binder, PropertyInfo property)
        {
            FluentBinder2<T> fb;

            // DEBT: Want, but maybe can't have, IBinder2<T> through and through
            if (binder is IBinder2<T> typedBinder)
                fb = new FluentBinder2<T>(typedBinder, true);
            else
                fb = new FluentBinder2<T>(binder, true);

            var item = new PropertyBinderProvider<T>(fb, property);

            return item;
        }

        static PropertyBinderProvider<T> CreatePropertyItem<T>(IBinder2 aggregatedBinder, PropertyInfo property)
        {
            var field = new FieldStatus<T>(property.Name, default(T));
            Func<T> getter = () => (T)property.GetValue(aggregatedBinder.getter());
            var fieldBinder = new Binder2<T>(field, getter);

            return CreatePropertyItem2<T>(fieldBinder, property);
        }

        static void InputHelper<T>(IAggregatedBinder binder, PropertyInfo property, bool initValidation)
        {
            var item = CreatePropertyItem<T>(binder, property);

            if(initValidation)
                item.InitValidation();
            
            binder.Add(item);
        }

        public static EntityBinder BindInput(this IAggregatedBinderBase binder, Type t, bool initValidation = false, 
            EntityBinder eb = null)
        {
            //t.GetTypeInfo().GetProperties();
            IEnumerable<PropertyInfo> properties = t.GetRuntimeProperties();
            
            if(eb == null)
                eb = new EntityBinder(t);

            //var helper = typeof(EntityBinderExtensions).GetRuntimeMethod(nameof(Helper),);
            var t2 = typeof(EntityBinderExtensions);
            var helperMethod = t2.GetRuntimeMethods().Single(x => x.Name == nameof(InputHelper));

            foreach (var property in properties)
            {
                var h = helperMethod.MakeGenericMethod(property.PropertyType);
                h.Invoke(null, new object[] { binder, property, initValidation });
            }
            
            foreach (var binderProvider in binder.Providers.OfType<PropertyBinderProvider>())
                eb.Add(binderProvider);

            return eb;
        }


        // TODO: Add a flag indicating whether to treat non-present fields as either 'null'
        // or to skip validating them entirely - although seems to me almost definitely we
        // want the former
        public static void BindValidation(this IAggregatedBinderBase binder, Type t)
        {
            IEnumerable<PropertyInfo> properties = t.GetRuntimeProperties();
            //IEnumerable<IBinder2> binders = binder.Binders;
            IEnumerable<IBinderProvider> binderProviders = binder.Providers;

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
                //IBinder2 match = binders.SingleOrDefault(b => b.Field.Name == property.Name);
                IBinderProvider match = binderProviders.SingleOrDefault(p => p.Binder.Field.Name == property.Name);

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


        public static Committer BindOutput(this IAggregatedBinderProvider binder, Type t, object instance, 
            Committer committer = null)
        {
            IEnumerable<PropertyInfo> properties = t.GetRuntimeProperties();
            Dictionary<string, IBinder2> binders = binder.Providers.ToDictionary(x => x.Binder.Field.Name, y => y.Binder);
            
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


        public static Committer BindOutput<T>(this IAggregatedBinderProvider binder, T instance) =>
            binder.BindOutput(typeof(T), instance);


        public static EntityBinder<T> BindInput2<T>(this AggregatedBinder binder, T t, bool initValidation = false)
        {
            var eb = new EntityBinder<T>();
            // DEBT: Still sloppy assigning getter like this
            binder.getter2 = () => t;
            binder.BindInput(typeof(T), initValidation, eb);
            return eb;
        }


        public static FluentBinder2<T> AddField<T>(this IAggregatedBinderCollector binder, string name, Func<T> getter, 
            Func<IFluentBinder<T>, IBinderProvider> providerFactory)
        {
            // default(T) because early init is not the same as runtime init
            // early init is when system is setting up the rules
            // runtime init is at the start of when pipeline processing actually occurs
            var f = new FieldStatus<T>(name, default(T));
            var b = new Binder2<T>(f, getter);
            var fb = new FluentBinder2<T>(b);
            binder.Add(providerFactory(fb));
            return fb;
        }

        public static IBinderProvider<T> AddField<T>(this IAggregatedBinderCollector binder, string name, Func<T> getter,
            Func<IFluentBinder<T>, IBinderProvider<T>> providerFactory)
        {
            var f = new FieldStatus<T>(name, default(T));
            var b = new Binder2<T>(f, getter);
            var fb = new FluentBinder2<T>(b);
            var bp = providerFactory(fb);
            binder.Add(bp);
            return bp;
        }


        public static FluentBinder2<T> AddField<T>(this IAggregatedBinderCollector binder, string name, Func<T> getter) =>
            binder.AddField(name, getter, fb => new BinderManagerBase.ItemBase(fb.Binder, fb));


        public class SummaryProcessor
        {
            internal Dictionary<IBinderProvider, Item> items = new Dictionary<IBinderProvider, Item>();

            public class Item
            {
                public int Warnings { get; set; }
                public int Errors { get; set; }

                public void Update(IEnumerable<Status> statuses)
                {
                    // TODO: Fire off events when these change so that Statuses down below has to do less work

                    Warnings = statuses.Count(x => x.Level == Status.Code.Warning);
                    Errors = statuses.Count(x => x.Level == Status.Code.Error);
                }
            }

            public IEnumerable<Status> Statuses
            {
                get
                {
                    int errors = items.Values.Sum(x => x.Errors);
                    int warnings = items.Values.Sum(x => x.Warnings);

                    if (errors > 0) yield return new Status(Status.Code.Error, $"Encountered {errors} errors");
                    if (warnings > 0) yield return new Status(Status.Code.Error, $"Encountered {warnings} warnings");
                }
            }
        }

        public static void AddSummaryProcessor(this AggregatedBinder aggregatedBinder)
        {
            var sp = new SummaryProcessor();
            // DEBT: Do away with this cast
            var f = (FieldStatus)aggregatedBinder.Field;
            f.Add(sp.Statuses);

            foreach(var provider in aggregatedBinder.Providers)
            {
                var item = new SummaryProcessor.Item();
                item.Update(provider.Binder.Field.Statuses);
                sp.items.Add(provider, item);
            }

            aggregatedBinder.BindersProcessed += (providers, context) =>
            {
                foreach(var provider in providers)
                {
                    sp.items[provider].Update(provider.Binder.Field.Statuses);
                }
            };
        }
    }


    public abstract class ValidationAttribute : Attribute
    {
        public virtual void Configure<T>(IFluentBinder<T> fb)
        {

        }

        public abstract void Validate<T>(IField<T> field, Context2 context);
    }

    public class RequiredAttribute : ValidationAttribute
    {
        public override void Configure<T>(IFluentBinder<T> fb)
        {
            // We're gonna handle aborting on null ourselves
            // DEBT: There could be conditions where other validators come first and want to see
            // if there is a null.  However, arguably, they would be doing the job 'Required' is trying to do here
            // TODO: Look into evaluation order also as that may become a factor -- i.e. RequiredAttribute needs
            // to run first
            fb.AbortOnNull = false;
        }
        public override void Validate<T>(IField<T> field, Context2 context)
        {
            if (field.Value == null)
            {
                field.Error(FieldStatus.ComparisonCode.IsNull, null, "Must not be null");
                context.Abort = true;
            }
        }
    }


    public static class FieldAssertExtensions
    {
    }
}
