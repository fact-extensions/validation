using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public static class FluentExtensions
    {
        /// <summary>
        /// From a field create a FluentBinder with a strong type association
        /// </summary>
        /// <param name="field"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        /// <remarks>
        /// DEBT: Do some kind of runtime check here to help assert that indeed
        /// presented IField is of type T
        /// </remarks>
        public static FluentBinder<T> Bind<T>(this IField field, Func<T> getter)
        {
            var b = new FieldBinder<T>(field, getter);
            var fb = new FluentBinder<T>(b, true);
            return fb;
        }


        /// <summary>
        /// From a field create a FluentBinder without a strong type association
        /// </summary>
        /// <param name="field"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public static FluentBinder<object> BindNonTyped(this IField field, Func<object> getter)
        {
            var b = new FieldBinder<object>(field, getter);
            var fb = new FluentBinder<object>(b, true);
            return fb;
        }


        /// <summary>
        /// Forcefully typecasts fluent binder or creates a new chained one of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fb"></param>
        /// <returns></returns>
        /// <remarks>
        /// Different from a conversion which assists in changing one type to another, this
        /// method presumes the underlying binder IS the specified type
        /// </remarks>
        public static FluentBinder<T> As<T>(this IFluentBinder fb)
        {
            if (fb is FluentBinder<T> fbTyped) return fbTyped;

            // TODO: Check fb.Type and make sure this is a valid cast

            fbTyped = new FluentBinder<T>(fb.Binder);
            return fbTyped;
        }



        /*
        public static IFluentBinder<T> IsTrue<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            Func<Status> getIsFalseStatus) =>
            IsTrue<IFluentBinder<T>, T>(fb, predicate, getIsFalseStatus); */




        public static IFluentBinder<T> IsTrueAsync<T>(this IFluentBinder<T> fb, Func<T, ValueTask<bool>> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error, bool sequential = true)
        {
            ((IFieldBinder)fb.Binder).Processor.ProcessingAsync += async (_, context) =>
            {
                IField<T> f = fb.Field;
                context.Sequential = sequential;
                if (!await predicate(f.Value))
                    f.Add(level, messageIfFalse);
            };
            return fb;
        }




#if UNUSED
        public static TFluentBinder Required_Legacy<TFluentBinder>(this TFluentBinder fb)
            where TFluentBinder : IFluentBinder
        {
            var v2binder = (IBinder2)fb.Binder;

            v2binder.AbortOnNull = false;
            v2binder.ProcessingAsync += (field, context) =>
            {
                if (fb.Field.Value == null)
                {
                    fb.Field.Error("Value required");
                    context.Abort = true;
                }
                return new ValueTask();
            };
            return fb;
        }
#endif



        /// <summary>
        /// Only lightly tested "v3" version of convert
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="fb"></param>
        /// <param name="converter"></param>
        /// <param name="cannotConvert">If not null, a false from 'converter' adds this message as an error.
        /// Defaults to null, expecting that 'converter' itself registers errors on the field</param>
        /// <returns></returns>
        [Obsolete("Regular Convert is now upgraded to v3 - use that one")]
        public static FluentBinder<TTo> Convert3<TFrom, TTo>(this IFluentBinder<TFrom> fb,
            FluentConvertExtensions.tryConvertDelegate<IField<TFrom>, TTo> converter, string cannotConvert = null)
        {
            TTo converted = default(TTo);
            var fbConverted = new FluentBinder<TTo>(fb.Binder, () => converted);
            // DEBT: Experimentally processing conversion as START of converted binder rather than
            // end of unconverted binder -- seems to make more sense, but other converters don't do
            // it this way
            fbConverted.Binder.Processor.StartingAsync += (sender, context) =>
            {
                bool success = converter(fb.Field, out converted);

                // FIX: We should be using this one, but it's yet another one and seemingly uncoupled from fbConverted.Field
                //IFieldStatusCollector2 field = context.Field;

                IFieldStatusCollector field = fbConverted.Field;

                if (success)
                    // DEBT: Kinda redundant, assigning value here -and- getter itself pointing to value
                    context.Value = converted;
                else if (cannotConvert != null)
                    field.Error(FieldStatus.ComparisonCode.Unspecified, typeof(TFrom),
                        cannotConvert ?? "Conversion from type {0} failed");

                return new ValueTask();
            };
            return fbConverted;
        }




        public static FluentBinder<T, TTrait> WithTrait<T, TTrait>(this IFluentBinder<T> fb,
            TTrait trait = default(TTrait)) =>
            // DEBT: Smooth out this cast
            new FluentBinder<T, TTrait>((IFieldBinder<T>)fb.Binder, false);


        // EXPERIMENTAL
        // Convert and assign on initialization only
        public static FluentBinder<TTo> Chain<T, TTo>(this IFluentBinder<T> fluentBinder, IFieldBinder binder,
            FluentConvertExtensions.tryConvertDelegate<IField<T>, TTo> convert, Action<TTo> setter)
        {
            // TODO: Rather than check a flag each time, remove the delegate from the ProcessedAsync chain
            bool initialized = false;
            var fbChained = new FluentBinder<TTo>(binder);
            var v3binder = (IFieldBinder)fluentBinder.Binder;
            v3binder.Processor.ProcessedAsync += (_, c) =>
            {
                if (initialized) return new ValueTask();

                // Do one time conversion + setter initialization of chained FluentBinder
                if (convert(fluentBinder.Field, out TTo initialValue))
                {
                    //fbChained.InitialValue = initialValue;
                    setter(initialValue);
                }
                else
                {
                    // DEBT: Register error on fb<TTo> for conversion problem
                }

                return new ValueTask();
            };
            /*
            binder.ProcessingAsync += (f, c) =>
            {
                if(!initialized)
                {
                    convert()
                }
                return new ValueTask();
            }; */
            //fluentBinder.Convert(convert)
            return fbChained;
        }


        public static FluentBinder<T> Chain<T>(this IFluentBinder<T> fluentBinder, IFieldBinder binder, Action<T> setter)
        {
            return fluentBinder.Chain(binder,
            (IField<T> f, out T v) =>
            {
                v = f.Value;
                return true;
            }, setter);
        }
    }


    namespace Traits
    {
        /// <summary>
        /// Tags a FluentBinder as a UNIX epoch
        /// </summary>
        public struct Epoch
        {

        }
    }
}
