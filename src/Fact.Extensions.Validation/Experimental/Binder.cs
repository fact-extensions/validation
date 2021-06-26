using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    // EXPERIMENTAL
    public enum InitiatingEvents
    {
        Click,
        Keystroke,
        /// <summary>
        /// Form load or reload
        /// </summary>
        Load,
        /// <summary>
        /// When we can't tell exactly why a control updated, but it did, use this
        /// </summary>
        ControlUpdate
    }


    public class InputContext
    {
        public string FieldName { get; set; }

        /// <summary>
        /// What occurred to spur this processing chain in the first place
        /// </summary>
        /// <remarks>
        /// EXPERIMENTAL
        /// </remarks>
        public InitiatingEvents InitiatingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// EXPERIMENTAL
        /// </remarks>
        public Interaction? InteractionLevel { get; set; }

        // Tracks which binders have been run for this processing sweep, to avoid
        // recursion.
        // TODO: Do a binder cache mechanism which knows to look at getter's cached response
        // and evaluate whether to use cached Statuses or re-process
        public HashSet<IBinder2> AlreadyRun { get; } = new HashSet<IBinder2>();
    }


    public abstract class ShimFieldBase : IField
    {
        public string Name { get; }

        public virtual object Value => binder.getter();

        readonly internal ICollection<Status> statuses;

        public IEnumerable<Status> Statuses => binder.Field.Statuses;

        public void Add(Status status) =>
            statuses.Add(status);

        protected readonly IBinderBase binder;

        // Clear only the locally shimmed in statuses - leave original field alone
        public void ClearShim() => statuses.Clear();

        protected ShimFieldBase(IBinderBase binder, ICollection<Status> statuses)
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

        internal ShimFieldBase(IBinderBase binder, ICollection<Status> statuses) :
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
        internal class _Item : ShimFieldBase
        {
            internal _Item(IBinderBase binder) :
                base(binder, new List<Status>())
            {
            }
        }

        readonly Dictionary<string, ShimFieldBase> fields = new Dictionary<string, ShimFieldBase>();

        [Obsolete("Use typed T version instead")]
        public BinderBase Add(IField field)
        {
            var binder = new BinderBase(field);
            binder.getter = () => field.Value;
            Add(binder);
            return binder;
        }

        [Obsolete("Use typed T version instead")]
        public void Add(IBinderBase binder)
        {
            var item = new _Item(binder);
            // DEBT: Can't be doing this cast all the time.  It's safe for the moment
            var field = (IFieldStatusExternalCollector)binder.Field;
            field.Add(item.statuses);
            fields.Add(binder.Field.Name, item);
        }

        public Binder2<T> Add<T>(IField<T> field)
        {
            // DEBT: Pretty sure we don't need a full powered Binder2 here
            var binder = new Binder2<T>(field, () => field.Value);
            Add2(binder);
            return binder;
        }

        public void Add2<T>(IBinder2<T> binder)
        {
            var item = new ShimFieldBase<T>(binder, new List<Status>());
            // DEBT: Can't be doing this cast all the time.  It's safe for the moment
            var field = (IFieldStatusExternalCollector)binder.Field;
            field.Add(item.statuses);
            fields.Add(binder.Field.Name, item);
        }

        public void Clear()
        {
            foreach (ShimFieldBase item in fields.Values)
                item.statuses.Clear();
        }

        /// <summary>
        /// Returns shimmed field
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IField this[string name] => fields[name];

        /// <summary>
        /// TODO: Rename to Validating
        /// </summary>
        public event Func<GroupBinder, Context2, ValueTask> Validate;

        public Task Evaluate(InputContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            var ctx = new Context2(cancellationToken);
            ctx.InputContext = context;

            Clear();

            foreach(ShimFieldBase item in fields.Values)
            {
                /* group is now distinct from one-off fields.  a 3rd party must 
                 * coordinate their validation together
                IBinder binder = item.binder;
                object _uncommitted = binder.getter();
                binder.Field.Value = _uncommitted;
                    binder.Evaluate(_uncommitted); */
            }

            // DEBT: Decompose and run them one at a time just like Binder2 so that async is respected
            // Do this by making a common base class or at least a common helper method
            Validate?.Invoke(this, ctx);

            return Task.CompletedTask;
        }

        // Recommended to use shims instead
        public void Append(string fieldName, Status status)
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

        Committer Committer { get; }
    }


    public interface IBinderBase<T> : IBinderBase
    {
        new Func<T> getter { get; }

        /// <summary>
        /// Optional
        /// </summary>
        Action<T> setter { get; set; }
    }


    public interface IBinder : IBinderBase
    {
        // DEBT: May not want this here
        bool AbortOnNull { get; set; }
    }


    public class BinderBaseBase
    {
        protected readonly IField field;

        public IField Field => field;

        public BinderBaseBase(IField field)
        {
            this.field = field;
        }

        // Bringing back binder-level commit ability
        public Committer Committer { get; } = new Committer();
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
    }


    public enum Interaction
    {
        /// <summary>
        /// For automated processes with no interaction at all
        /// </summary>
        None,
        /// <summary>
        /// For manually activated batch processes.  Similar to 'None' except there is the most
        /// minimal level of user interaction
        /// </summary>
        Manual,
        /// <summary>
        /// For things like form submission, final button presses.  On the order of 11s+ expected interaction time
        /// </summary>
        Low,
        /// <summary>
        /// For things like overall field validation, focus gain/loss.
        /// On the order of 1-10s expected interaction time
        /// </summary>
        Medium,
        /// <summary>
        /// For human real-time events, happening in the sub-second range.  Things like mouse clicks and
        /// key presses.
        /// </summary>
        High
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
}


namespace Fact.Extensions.Validation
{
    using Experimental;
    using System.Linq.Expressions;

    public static class GroupBinderExtensions
    {
        public static void DoValidate<T1, T2>(this GroupBinder binder, string fieldName1, string fieldName2,
            Action<Context2, IField<T1>, IField<T2>> handler)
        {
            var field1 = (IField<T1>)binder[fieldName1];
            var field2 = (IField<T2>)binder[fieldName2];

            binder.Validate += (gb, ctx) =>
            {
                handler(ctx, field1, field2);
                return new ValueTask();
            };
        }


        /// <summary>
        /// Inspects lambda expression, retrieves PropertyBinderProvider associated with it
        /// and creates a new ShimFieldBase<typeparamref name="T"/> associated to its binder
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder"></param>
        /// <param name="fieldLambda"></param>
        /// <returns></returns>
        static ShimFieldBase<T> CreateShimField<TEntity, T>(this EntityBinder<TEntity> binder, 
            Expression<Func<TEntity, T>> fieldLambda)
        {
            PropertyBinderProvider<T> field1 = binder.Get(fieldLambda);
            var statuses = new List<Status>();
            var field1shim = new ShimFieldBase<T>(field1.Binder, statuses);
            // The following are causing the debugger to crash -- smells like a recursive situation
            // DEBT: Naughty cast
            ((IFieldStatusExternalCollector)field1.Binder.Field).Add(statuses);
            return field1shim;
        }

        public static void GroupValidate<T, T1>(this EntityBinder<T> binder, IAggregatedBinder parent,
            Expression<Func<T, T1>> field1Lambda,
            Action<Context2, IField<T1>> handler)
        {
            // DEBT: Doesn't handle conversions, for that we'll need to pass in FluentBinders and likely
            // that particular one won't want to be EntityBinder based
            PropertyBinderProvider<T1> field1 = binder.Get(field1Lambda);
            var field1shim = new ShimFieldBase<T1>(field1.Binder, new List<Status>());

            parent.ProcessingAsync += (f, c) =>
            {
                // DEBT: Likely will want a GroupBinder (GroupBinder2?) so that we can do multiple ProcessingAsync
                // with only one field1shim and one field1shim clear
                field1shim.statuses.Clear();

                handler(c, field1shim);

                return new ValueTask();
            };
        }

        public static void GroupValidate<T, T1, T2>(this EntityBinder<T> binder, //IAggregatedBinder parent, 
            Expression<Func<T, T1>> field1Lambda,
            Expression<Func<T, T2>> field2Lambda,
            Action<Context2, IField<T1>, IField<T2>> handler)
        {
            /*
            var field1 = binder.CreateShimField(field1Lambda);
            var field2 = binder.CreateShimField(field2Lambda);

            parent.ProcessingAsync += (f, c) =>
            {
                field1.ClearShim();
                field2.ClearShim();

                handler(c, field1, field2);

                return new ValueTask();
            }; */
            binder.Get(field1Lambda).FluentBinder.GroupValidate(
                binder.Get(field2Lambda).FluentBinder,
                (c, f1, f2) =>
                {
                    handler(c, f1, f2);
                    return new ValueTask();
                });
        }

        public static void Test2<T, T1>(this IEntityBinder<T> binder, Expression<Func<T1>> fieldLambda,
            Action<Context2, IField<T1>> handler)
        {

        }
    }
}
