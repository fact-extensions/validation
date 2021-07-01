﻿using System;
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


    public class ShimFieldBaseBase
    {
        public string Name { get; }

        readonly internal ICollection<Status> statuses;

        public ShimFieldBaseBase(string name, ICollection<Status> statuses)
        {
            Name = name;
            this.statuses = statuses;
        }

        // Clear only the locally shimmed in statuses - leave original field alone
        public void ClearShim() => statuses.Clear();

        public void Add(Status status) => statuses.Add(status);
    }


    public abstract class ShimFieldBase : ShimFieldBaseBase,
        IField
    {
        public virtual object Value => binder.getter();

        public IEnumerable<Status> Statuses => binder.Field.Statuses;

        protected readonly IBinderBase binder;

        protected ShimFieldBase(IBinderBase binder, ICollection<Status> statuses) :
            base(binder.Field.Name, statuses)
        {
            this.binder = binder;
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
        /// <summary>
        /// When true, bound value is basically optional and processing aborts immediately
        /// when a null is seen.  When false, null is passed right on through the processing
        /// chain.  Defaults to true
        /// </summary>
        /// <remarks>
        /// DEBT: May not want this here
        /// </remarks>
        bool AbortOnNull { get; set; }
    }


    public class BinderBase
    {
        protected readonly IField field;

        public IField Field => field;

        public BinderBase(IField field)
        {
            this.field = field;
        }

        // Bringing back binder-level commit ability
        public Committer Committer { get; } = new Committer();
    }


    public class BinderBase<T> : BinderBase, IBinderBase<T>
        //: IBinderBase<T>
    {
        /// <summary>
        /// Strongly typed getter
        /// NOTE: May be 'object' in circumstances where init time we don't commit to a type
        /// </summary>
        /// <remarks>
        /// TODO: Phase this into private/readonly
        /// </remarks>
        public Func<T> getter2;

        public Func<T> getter => getter2;

        Func<object> IBinderBase.getter => () => getter2();

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// TODO: Phase this into readonly/private
        /// </remarks>
        public Action<T> setter { get; set; }
        
        public BinderBase(IField field, Func<T> getter, Action<T> setter = null) : base(field)
        {
            getter2 = getter;
            this.setter = setter;
        }
    }


    public class Context
    {
        /// <summary>
        /// When true, evaluation context proceeds normally (implicitly all the way until the end)
        /// When false, evaluation halts completely (catestrophic failure)
        /// Defaults to true
        /// </summary>
        public bool Abort { get; set; } = false;
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
    }
}
