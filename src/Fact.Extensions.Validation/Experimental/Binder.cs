﻿using System;
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

        //void Evaluate(object o);
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

        // EXPERIMENTAL
        public object Value => field.Value;

        // EXPERIMENTAL
        public object Converted => converted;

        public event Action<object> Finalize;
        public event Action<IField<T>> Validate;

        public delegate object ConvertDelegate(IField<T> field, object from);

        public event ConvertDelegate Convert;

        public IField Evaluate()
        {
            field.Value = getter();
            var f = new ShimFieldBase<T>(this, InternalStatuses);

            //f.Clear();
            Statuses.Clear();
            InternalStatuses.Clear();

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
    public static class BinderExtensions
    {
        // DEBT: This stateful and non-stateful can't fully coexist and we'll have to choose one
        // or the other eventually
        public static void Evaluate<T>(this Experimental.Binder<T> binder, T value)
        {
            binder.getter = () => value;
            binder.Evaluate();
        }
    }
}
