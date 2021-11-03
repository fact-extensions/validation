using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    // DEBT: Experimental only because naming of IFieldBinder still in flux and FluentBinder3 is not final name
    using Experimental;

    public static class FluentConvertExtensions
    {
        public delegate bool tryConvertDelegate<TFrom, TTo>(TFrom from, out TTo to);

        public static FluentBinder3<TTo> Convert<T, TTo>(this IFluentBinder<T> fb,
            tryConvertDelegate<IField<T>, TTo> converter, string cannotConvert = null, Optional<TTo> defaultValue = null)
        {
            TTo converted = default(TTo);
            var fb2 = new FluentBinder3<TTo>((IFieldBinder)fb.Binder, () => converted);
            var v3binder = (IFieldBinder)fb.Binder;
            v3binder.Processor.ProcessingAsync += (_, context) =>
            {
                if (defaultValue != null && context.Value == null)
                {
                    context.Value = defaultValue.Value;
                    //fb2.InitialValue = defaultValue.Value;
                }
                else if (converter(fb.Field, out converted))
                {
                    context.Value = converted;
                    //fb2.InitialValue = converted;
                }
                else
                {
                    context.Abort = true;
                    // DEBT: Seems like we want some kind of generic cannot convert error if no explicit one is specified
                    if (cannotConvert != null)
                        fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, typeof(TTo), cannotConvert);
                }

                return new ValueTask();
            };
            return fb2;
        }

        public static FluentBinder3<TTo> Convert<T, TTo>(this IFluentBinder<T> fb,
            tryConvertDelegate<T, TTo> converter, string cannotConvert, Optional<TTo> defaultValue = null)
        {
            return fb.Convert<T, TTo>((IField<T> field, out TTo converted) =>
                converter(field.Value, out converted), cannotConvert, defaultValue);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="fb"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static FluentBinder3<TTo> Convert<TTo>(this IFluentBinder fb, Optional<TTo> defaultValue = null)
        {
            TTo converted = default(TTo);
            var fb2 = new FluentBinder3<TTo>((IFieldBinder)fb.Binder, () => converted);
            var v3binder = (IFieldBinder)fb.Binder;
            v3binder.Processor.ProcessingAsync += (_, context) =>
            {
                IField f = fb.Field;
                Type t = typeof(TTo);

                if (f.Value == null && defaultValue != null)
                {
                    context.Value = defaultValue.Value;
                    //fb2.InitialValue = defaultValue.Value;
                    return new ValueTask();
                }

                try
                {
                    converted = (TTo)
                        System.Convert.ChangeType(f.Value, t);

                    context.Value = converted;
                    //fb2.InitialValue = converted;
                }
                catch (FormatException)
                {
                    // DEBT: Get a candidate factory to grab descriptions from a comparisoncode + scalar
                    f.Error(FieldStatus.ComparisonCode.Unspecified, t, "Unable to convert to type {0}");
                    context.Abort = true;
                }
                catch (InvalidCastException)
                {
                    // DEBT: Would be far better to check for null before issuing conversion.  Not doing so
                    // because some types can be null and the logic for determining that is a tiny bit involved
                    // DEBT: Not a foregone conclusion that InvalidCastException is because we can't convert a value
                    // type to null -- but probably that's why we get the exception
                    f.Error(FieldStatus.ComparisonCode.IsNull, null, "Null not allowed here");
                    context.Abort = true;
                }
                catch (OverflowException)
                {
                    f.Error(FieldStatus.ComparisonCode.GreaterThan, t, "Out of bounds for type {0}");
                    context.Abort = true;
                }

                return new ValueTask();
            };
            return fb2;
        }

    }
}
