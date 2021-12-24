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


    /// <summary>
    /// 
    /// </summary>
    public abstract class PropertyBinderProvider : BinderProviderBase, IPropertyProvider
    {
        public PropertyInfo Property { get; }

        public static PropertyBinderProvider Create(PropertyInfo property, Func<object> getObj)
        {
            var type = typeof(PropertyBinderProvider<>).MakeGenericType(property.PropertyType);
            var ctor = type.GetRuntimeMethods().First(x => x.Name == nameof(PropertyBinderProvider<object>.Create));
            var pbp = (PropertyBinderProvider)ctor.Invoke(null, new object[] { property, getObj });
            return pbp;
        }


        /// <summary>
        /// Scoop up any ValidationAttribute on 'property' and associate it to fluentbinder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fluentBinder"></param>
        /// <param name="property"></param>
        public static void InitValidation<T>(IFluentBinder<T> fluentBinder, PropertyInfo property)
        {
            var shimField = fluentBinder.Field;
            var attributes = property.GetCustomAttributes().OfType<ValidationAttribute>();

            foreach (var a in attributes)
                a.Configure(fluentBinder);

            fluentBinder.Binder.Processor.ProcessingAsync += (_, context) =>
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
            
        public PropertyBinderProvider(IFieldBinder binder, IFluentBinder fluentBinder,
            PropertyInfo property) : base(binder, fluentBinder)
        {
            Property = property;
        }
    }


    /// <summary>
    /// Associates a specified property with an IFluentBinder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyBinderProvider<T> : 
        PropertyBinderProvider,
        IBinderProvider<T>
    {
        public new IFluentBinder<T> FluentBinder { get; }


        /// <summary>
        /// Creates a property binder where property hangs off instance returned by getObj
        /// </summary>
        /// <param name="property"></param>
        /// <param name="getObj"></param>
        /// <returns></returns>
        public new static PropertyBinderProvider<T> Create(PropertyInfo property, Func<object> getObj)
        {
            var fb = new FluentBinder<T>(property.Name, () => (T)property.GetValue(getObj()));
            return new PropertyBinderProvider<T>(fb, property);
        }

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

    // Keeping v1/v2 old AggregatedBinderBase around just for code reference, because we do some
    // tricky things here
#if UNUSED
    [Obsolete]
    public class AggregatedBinderBase2<TBinderProvider> : Binder2<object>,
        IAggregatedBinderBase<TBinderProvider>
        where TBinderProvider: IBinderProvider
    {
        readonly protected List<TBinderProvider> items = new List<TBinderProvider>();

        public IServiceProvider Services { get; }

        public IEnumerable<IBinderProvider> Providers => items.Cast<IBinderProvider>();

        public AggregatedBinderBase2(IField field, IServiceProvider services = null) :
            // DEBT: A little sloppy to lazy init our getter, however aggregated
            // binder may indeed not need to retrieve any value for context.Value
            // during Process call
            base(field, () => null)
        {
            Services = services;

            // Aggregated binders always have a null value, since they only exist to evaluate child fields
            // This may change in the future, but for now, account for that
            AbortOnNull = false;

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

        /// <summary>
        /// Occurs after interactive/discrete binder processing, whether it generated new status or not
        /// </summary>
        public event BindersProcessedDelegate<TBinderProvider> BindersProcessed;

        protected void FireFieldsProcessed(IEnumerable<TBinderProvider> fields, Context2 context) =>
            BindersProcessed?.Invoke(fields, context);


        public void Add(TBinderProvider item)
        {
            items.Add(item);
            ProcessingAsync += async (field, context) =>
                // Aggregated binder we always have our "Load" style InputContext unless overriden somehow
                await ((IBinder2)item.Binder).Process(context.InputContext, context.CancellationToken);
            ((IBinder2)item.Binder).ProcessedAsync += (field, context) =>
            {
                // Filter out overall load/aggregated Process
                if (context.InputContext?.InitiatingEvent != InitiatingEvents.Load)
                    FireFieldsProcessed(new[] { item }, context);

                return new ValueTask();
            };

            Committer.Committing += item.Binder.Committer.DoCommit;
        }
    }
#endif


    /// <summary>
    /// Most simplistic entity binder you can get.  Maybe underpowered
    /// </summary>
    public class BasicEntityBinder : Binder3Base<IFieldContext>, IBinderBase, IAggregatedBinderProvider
    {
        readonly AggregatedBinderBase<PropertyBinderProvider> aggregatedBinder = 
            new AggregatedBinderBase<PropertyBinderProvider>();
        
        public BasicEntityBinder(Type t, Func<object> getter)
        {
            this.getter = getter;

            var properties = t.GetRuntimeProperties();

            foreach(var property in properties)
            {
                var pbp = PropertyBinderProvider.Create(property, getter);

                pbp.InitValidation();

                aggregatedBinder.Add(pbp);
            }

            // DEBT: Feels a little wrong encapsulating a whole process cycle in this one
            // ProcessingAsync responder
            Processor.ProcessingAsync += async (_, context) =>
            {
                await aggregatedBinder.Processor.ProcessAsync(context);
            };
        }

        public Func<object> getter { get; }

        public IEnumerable<IBinderProvider> Providers => aggregatedBinder.Providers;
    }


    /// <summary>
    /// A combo pass-through aggregation collector and indicator as to a specific type and instance of entity
    /// </summary>
    public class EntityProvider<T> : IAggregatedBinderCollector
    {
        public IAggregatedBinder3 Parent { get; }
        public T Entity { get; }

        public EntityProvider(IAggregatedBinder3 parent, T entity)
        {
            Parent = parent;
            Entity = entity;
        }

        public void Add(IBinderProvider collected) =>
            Parent.Add(collected);
    }


    public static class EntityProviderExtensions
    {
        /// <summary>
        /// Creates a strongly typed shim in front of aggregatedBinder which maps to type and
        /// instance of 'entity'
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="aggregatedBinder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static EntityProvider<T> Entity<T>(this IAggregatedBinder3 aggregatedBinder, T entity)
        {
            return new EntityProvider<T>(aggregatedBinder, entity);
        }


        /// <summary>
        /// UNTESTED
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="entityProvider"></param>
        /// <param name="propertyLambda"></param>
        /// <returns></returns>
        /// <remarks>
        /// Feels like we did this somewhere before, and not BindText either
        /// </remarks>
        public static FluentBinder<TProperty> AddField<TEntity, TProperty>(this EntityProvider<TEntity> entityProvider, 
            Expression<Func<TEntity, TProperty>> propertyLambda)
        {
            var name = propertyLambda.Name;
            var member = (MemberExpression) propertyLambda.Body;
            var property = (PropertyInfo) member.Member;

            FluentBinder<TProperty> fb = entityProvider.AddField(name, () => (TProperty)property.GetValue(entityProvider.Entity));

            PropertyBinderProvider.InitValidation(fb, property);

            fb.Commit(v => property.SetValue(entityProvider.Entity, v));

            return fb;
        }
    }

    /// <summary>
    /// Experimental helper for only-reflection-entity assistance
    /// </summary>
    public class EntityBinder : IAggregatedBinderBase
    {
        public Type EntityType { get; }
        
        protected readonly List<PropertyBinderProvider> providers = new List<PropertyBinderProvider>();

        public IEnumerable<IBinderBase> Binders => providers.Select(x => x.Binder);

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


        /// <summary>
        /// Via reflection/lambda, grab a PropertyBinderProvider
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="propertyLambda"></param>
        /// <returns></returns>
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

        static PropertyBinderProvider<T> CreatePropertyItem2<T>(FieldBinder<T> binder, PropertyInfo property)
        {
            var fb = new FluentBinder<T>(binder, true);

            var item = new PropertyBinderProvider<T>(fb, property);

            return item;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="aggregatedBinder"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        static PropertyBinderProvider<T> CreatePropertyItem<T>(IBinderBase aggregatedBinder, PropertyInfo property)
        {
            var field = new FieldStatus<T>(property.Name, default(T));
            Func<T> getter = () => (T)property.GetValue(aggregatedBinder.getter());
            var fieldBinder = new FieldBinder<T>(field, getter);

            return CreatePropertyItem2(fieldBinder, property);
        }

        /// <summary>
        /// Creates PropertyBinderProvider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder"></param>
        /// <param name="property"></param>
        /// <param name="initValidation"></param>
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
            // FIX: Inappropriate forward cast from IAggregatedBinderBase to IAggregatedBinder happens here, will
            // need attention
            var helperMethod = t2.GetRuntimeMethods().Single(x => x.Name == nameof(InputHelper));

            foreach (var property in properties)
            {
                var h = helperMethod.MakeGenericMethod(property.PropertyType);
                h.Invoke(null, new object[] { binder, property, initValidation });
            }
            
            // DEBT: This one line makes binder and EntityBinder 1:1 -- fix this so that we can
            // extract PropertyBinderProviders directly from prior h.Invoke
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
            Dictionary<string, IFieldBinder> binders = binder.Providers.ToDictionary(x => x.Binder.Field.Name, y => y.Binder);
            
            if (committer == null) committer = new Committer(); 

            foreach(var property in properties)
            {
                if(binders.TryGetValue(property.Name, out IFieldBinder b))
                {
                    // DEBT: Assign to DBNull or similar so we can tell if it's uninitialized
                    object staged = null;

                    b.Processor.ProcessedAsync += (sender, context) =>
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


        public static EntityBinder<T> BindInput2<T>(this IAggregatedBinderBase binder, T t, bool initValidation = false)
        {
            var eb = new EntityBinder<T>();
            binder.BindInput(typeof(T), initValidation, eb);
            return eb;
        }


        /// <summary>
        /// NOT READY YET
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="p"></param>
        /// <param name="getInstance"></param>
        /// <returns></returns>
        public static FluentBinder AddField(this IAggregatedBinderBase binder, PropertyInfo p, Func<object> getInstance)
        {
            return binder.AddField(p.Name, () => p.GetValue(getInstance()));
        }


        public static FluentBinder<TProperty> AddField<T, TProperty>(this IAggregatedBinderBase binder, Expression<Func<T, TProperty>> propertyLambda, Func<T> getInstance)
        {
            var name = propertyLambda.Name;
            var member = propertyLambda.Body as MemberExpression;
            var property = member.Member as PropertyInfo;

            FluentBinder<TProperty> fluentBinder =
                binder.AddField(property.Name, () => (TProperty)property.GetValue(getInstance()));

            PropertyBinderProvider.InitValidation(fluentBinder, property);

            return fluentBinder;
        }






        public static IFluentBinder<T1> IsMatch<T1, T2>(this
            IFluentBinder<T1> fluentBinder1,
            IFluentBinder<T2> fluentBinder2)
        {
            return fluentBinder1.GroupValidate(fluentBinder2, (c, f1, f2) =>
            {
                if(!f1.Value.Equals(f2.Value))
                {
                    f1.Error($"Must match field {f2.Name}");
                    f2.Error($"Must match field {f1.Name}");
                }

                return new ValueTask();
            });
        }


        /*
         * Not quite ready yet- needs more support code cleanup first
        async Task ProcessingHelper(IField processingField, params ShimField3[] fields)
        {
            foreach(var field in fields)
                field.ClearShim();

            foreach(var field in fields)
            {
            }
        }*/


        /// <summary>
        /// Field whose value comes from a getter, statuses from self-maintained source
        /// and associated with a FluentBinder.  Exposed via IBinderProvider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class ShimField3<T> : ShimFieldBase2<T>, IBinderProvider
        {
            public IFieldBinder Binder { get; }

            public IFluentBinder FluentBinder { get; }

            public ShimField3(IFluentBinder binder) :
                base(binder.Field.Name, new List<Status>(), () => (T)binder.Field.Value)
            {
                Binder = binder.Binder;
                FluentBinder = binder;
            }
        }


        public static IFluentBinder<T1> GroupValidate<T1, T2>(this 
            IFluentBinder<T1> fluentBinder1,
            IFluentBinder<T2> fluentBinder2,
            Func<IFieldContext, IField<T1>, IField<T2>, ValueTask> handler)
        {
            var field1 = new ShimField3<T1>(fluentBinder1);
            var field2 = new ShimField3<T2>(fluentBinder2);

            ((IFieldStatusExternalCollector)fluentBinder1.Binder.Field).Add(field1.statuses);
            ((IFieldStatusExternalCollector)fluentBinder2.Binder.Field).Add(field2.statuses);

            // FIX: Want to make these MT safe
            bool isProcessing = false;
            bool isProcessed = false;

            var f1v3binder = fluentBinder1.Binder;
            var f2v3binder = fluentBinder2.Binder;

            var processing = new ProcessingDelegateAsync(async (f, c) =>
            {
                if (!isProcessing)
                {
                    isProcessing = true;

                    // Need to clear out errors here, otherwise below if statements will fail out based on
                    // previous processing errors found in this same group validator
                    field1.ClearShim();
                    field2.ClearShim();

                    // DEBT: Force non-group binder processing to occur first so that conversions
                    // and error discovery can happen.  We only call handler if dependend on fields
                    // themselves have no errors.
                    // NOTE: We get into double-validating here since inevitably an aggregated validate is going
                    // to happen duplicating one of these below.  Consider adding a 'Generation' # on Binder to
                    // know when we've already performed a validation for a particular sweep

                    if (f == fluentBinder1.Binder.Field)
                    {
                        await f2v3binder.Processor.ProcessAsync(c);

                        if (fluentBinder2.Binder.Field.Statuses.Any())
                        {
                            c.Abort = true;
                            return;
                        }
                    }

                    if (f == fluentBinder2.Binder.Field)
                    {
                        await f1v3binder.Processor.ProcessAsync(c);

                        if (fluentBinder1.Binder.Field.Statuses.Any())
                        {
                            c.Abort = true;
                            return;
                        }
                    }

                    await handler(c, field1, field2);
                }
            });

            var processed = new ProcessingDelegateAsync((f, c) =>
            {
                isProcessing = false;
                isProcessed = true;
                return new ValueTask();
            });

            // UNTESTED
            f1v3binder.Processor.ProcessingAsync += (sender, ctx) => processing(ctx.Field, ctx);
            f2v3binder.Processor.ProcessingAsync += (sender, ctx) => processing(ctx.Field, ctx);

            f1v3binder.Processor.ProcessedAsync += (sender, ctx) => processed(ctx.Field, ctx);
            f2v3binder.Processor.ProcessedAsync += (sender, ctx) => processed(ctx.Field, ctx);

            return fluentBinder1;
        }
    }


    public abstract class ValidationAttribute : Attribute
    {
        public virtual void Configure<T>(IFluentBinder<T> fb)
        {

        }

        public abstract void Validate<T>(IField<T> field, IFieldContext context);
    }

    public class RequiredAttribute : ValidationAttribute
    {
        public override void Validate<T>(IField<T> field, IFieldContext context)
        {
            // DEBT: Need a much more robust "required" assessor than merely checking null
            // thing is, we'll likely need an IServiceProvider with a factory to generate
            // checkers - or perhaps lift it out of context somehow
            if (field.Value == null)
            {
                field.Error(FieldStatus.ComparisonCode.IsNull, null, "Must not be null");
                context.Abort = true;
            }
            else if(field.Value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                field.Error(FieldStatus.ComparisonCode.IsNull, stringValue, "Must not be empty");
                context.Abort = true;
            }
        }
    }


    public static class FieldAssertExtensions
    {
    }
}
