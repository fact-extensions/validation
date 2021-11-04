using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public class FluentBinder : IFluentBinder
    {
        public IFieldBinder Binder { get; }


        /// <summary>
        /// Field local to this FluentBinder
        /// </summary>
        public IField Field { get; protected set; }

        public Type Type { get; }

        // DEBT: Field MUST be initialized by calling class
        protected FluentBinder(IFieldBinder binder, Type type)
        {
            Binder = binder;
            Type = type;
        }


        /// <summary>
        /// Statuses associated with just this binder
        /// Attaches to Field as an external status
        /// </summary>
        protected readonly List<Status> statuses = new List<Status>();


        /// <summary>
        /// Set up:
        /// - clearing of local statuses to this FluentBinder
        /// - awareness of this FluentBinder's local statuses to overall Binder
        /// </summary>
        protected void Initialize()
        {
            Binder.Processor.StartingAsync += (_, context) =>
            {
                statuses.Clear();
                return new ValueTask();
            };

            // DEBT
            var f = (IFieldStatusExternalCollector)Binder.Field;
            f.Add(statuses);
        }
    }


    public class FluentBinder<T> : FluentBinder, IFluentBinder<T>
    {
        readonly Experimental.ShimFieldBase2<T> field;

        public new IField<T> Field => field;

        /// <summary>
        /// </summary>
        /// <param name="chained">Binder on which we hang error reporting</param>
        /// <param name="getter">
        /// Getter for shim field - can assist in parameter conversion when previous
        /// FluentBinder in chain is not of type T
        /// </param>
        public FluentBinder(IFieldBinder chained, Func<T> getter = null) :
            base(chained, typeof(T))
        {
            field = new Experimental.ShimFieldBase2<T>(chained.Field.Name, statuses,
                getter ?? (() => (T)chained.getter()));

            // DEBT: Easy to get wrong
            base.Field = field;

            Initialize();
        }


        /// <summary>
        /// Attach this FluentBinder to an existing Binder
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="initial"></param>
        public FluentBinder(IFieldBinder<T> binder, bool initial) :
            base(binder, typeof(T))
        {
            if (initial)
                // DEBT: Needs refiniement
                field = new Experimental.ShimFieldBase2<T>(binder.Field.Name, statuses, () => binder.getter());
            else
            {
                // FIX: This seems wrong, getting initial value at FluentBinder setup time
                T initialValue = binder.getter();
                field = new Experimental.ShimFieldBase2<T>(binder.Field.Name, statuses, () => initialValue);
            }

            // DEBT: Easy to get wrong
            base.Field = field;

            Initialize();
        }



        /// <summary>
        /// Creates an in-place FieldStatus and FieldBinder
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        public FluentBinder(string name, Func<T> getter) :
            this(new FieldBinder<T>(name, getter), true)
        {
        }
    }


    public class FluentBinder<T, TTrait> : FluentBinder<T>,
        IFluentBinder<T, TTrait>
    {
        public FluentBinder(IFieldBinder<T> binder, bool initial) :
            base(binder, initial)
        {

        }

        public TTrait Trait => throw new NotImplementedException();
    }
}
