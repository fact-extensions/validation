using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    using IFieldBinder = Experimental.IFieldBinder;
    using FluentBinderBase = Experimental.FluentBinder2;

    public class FluentBinder<T> : FluentBinderBase,
        IFluentBinder3<T>
    {
        public new IFieldBinder Binder { get; }

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
            Binder = chained;

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
        public FluentBinder(Experimental.IFieldBinder<T> binder, bool initial) :
            base(binder, typeof(T))
        {
            Binder = binder;

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
        /// 
        /// </summary>
        /// <remarks>
        /// 100% overrides semi-obsolete base
        /// </remarks>
        new void Initialize()
        {
            // This event handler is more or less a re-initializer for subsequent
            // process/validation calls
            Binder.Processor.StartingAsync += (field, context) =>
            {
                statuses.Clear();
                return new ValueTask();
            };

            // DEBT
            var f = (IFieldStatusExternalCollector)Binder.Field;
            f.Add(statuses);
        }


        /// <summary>
        /// Creates an in-place FieldStatus and FieldBinder
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        public FluentBinder(string name, Func<T> getter) :
            this(new Experimental.FieldBinder<T>(name, getter), true)
        {
        }
    }


    public class FluentBinder<T, TTrait> : FluentBinder<T>,
        IFluentBinder<T, TTrait>
    {
        public FluentBinder(Experimental.IFieldBinder<T> binder, bool initial) :
            base(binder, initial)
        {

        }

        public TTrait Trait => throw new NotImplementedException();
    }
}
