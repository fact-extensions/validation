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
        public static FluentBinder2<T> Bind<T>(this IField field, Func<T> getter)
        {
            var b = new FieldBinder<T>(field, getter);
            var fb = new FluentBinder2<T>(b, true);
            return fb;
        }


        /// <summary>
        /// From a field create a FluentBinder without a strong type association
        /// </summary>
        /// <param name="field"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public static FluentBinder3<object> BindNonTyped(this IField field, Func<object> getter)
        {
            var b = new FieldBinder<object>(field, getter);
            var fb = new FluentBinder3<object>(b, true);
            return fb;
        }

        public static FluentBinder2<T> As<T>(this FluentBinder2 fb)
        {
            if (fb is FluentBinder2<T> fbTyped) return fbTyped;

            // TODO: Check fb.Type and make sure this is a valid cast

            fbTyped = new FluentBinder2<T>(fb);
            return fbTyped;
        }

        public delegate bool tryConvertDelegate<TFrom, TTo>(TFrom from, out TTo to);

        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            Func<Status> getIsFalseStatus)
            where TFluentBinder : IFluentBinder<T>
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField<T> f = fb.Field;
                if (!predicate(f.Value))
                    f.Add(getIsFalseStatus());

                return new ValueTask();
            };
            return fb;
        }


        public static TFluentBinder IsTrue<TFluentBinder>(this TFluentBinder fb, Func<object, bool> predicate,
            Func<Status> getIsFalseStatus)
            where TFluentBinder : IFluentBinder
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField f = fb.Field;
                if (!predicate(f.Value))
                    f.Add(getIsFalseStatus());

                return new ValueTask();
            };
            return fb;
        }

        /*
        public static IFluentBinder<T> IsTrue<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            Func<Status> getIsFalseStatus) =>
            IsTrue<IFluentBinder<T>, T>(fb, predicate, getIsFalseStatus); */

        public static IFluentBinder<T> IsTrue<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error) =>
            fb.IsTrue(predicate, () => new Status(level, messageIfFalse));

        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error)
            where TFluentBinder : IFluentBinder<T> =>
            fb.IsTrue(predicate, () => new Status(level, messageIfFalse));

        public static IFluentBinder<T> IsTrueScalar<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            FieldStatus.ComparisonCode code, T compareTo, string messageIfFalse = null,
            Status.Code level = Status.Code.Error) =>
            fb.IsTrue(predicate, () =>
                new ScalarStatus(level, messageIfFalse, code, compareTo));

        public static TFluentBinder IsTrueScalar<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            FieldStatus.ComparisonCode code, T compareTo, string messageIfFalse = null,
            Status.Code level = Status.Code.Error)
            where TFluentBinder : IFluentBinder<T>
            =>
            fb.IsTrue(predicate, () => new ScalarStatus(level, messageIfFalse, code, compareTo));

        public static TFluentBinder IsTrueScalar<TFluentBinder>(this TFluentBinder fb, Func<object, bool> predicate,
            FieldStatus.ComparisonCode code, object compareTo, string messageIfFalse = null,
            Status.Code level = Status.Code.Error)
            where TFluentBinder : IFluentBinder
            =>
            fb.IsTrue(predicate, () => new ScalarStatus(level, messageIfFalse, code, compareTo));

        public static IFluentBinder<T> IsTrueAsync<T>(this IFluentBinder<T> fb, Func<T, ValueTask<bool>> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error, bool sequential = true)
        {
            fb.Binder.ProcessingAsync += async (field, context) =>
            {
                IField<T> f = fb.Field;
                context.Sequential = sequential;
                if (!await predicate(f.Value))
                    f.Add(level, messageIfFalse);
            };
            return fb;
        }


        public static TFluentBinder StartsWith<TFluentBinder>(this TFluentBinder fb, string mustStartWith)
            where TFluentBinder : IFluentBinder<string>
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (!fb.Field.Value.StartsWith(mustStartWith))
                    fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, mustStartWith,
                        $"Must start with: {mustStartWith}");
                return new ValueTask();
            };
            return fb;
        }


        public static TFluentBinder Contains<TFluentBinder>(this TFluentBinder fb, string mustContain)
            where TFluentBinder : IFluentBinder<string>
        {
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (!fb.Field.Value.Contains(mustContain))
                    fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, mustContain,
                        $"Must start with: {mustContain}");
                return new ValueTask();
            };
            return fb;
        }

        public static TFluentBinder Required<TFluentBinder>(this TFluentBinder fb)
            where TFluentBinder : IFluentBinder
        {
            fb.Binder.AbortOnNull = false;
            fb.Binder.ProcessingAsync += (field, context) =>
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

        public static TFluentBinder IsEqualTo<TFluentBinder>(this TFluentBinder fb, object compareTo)
            where TFluentBinder : IFluentBinder =>
            fb.IsTrueScalar(v => v.Equals(compareTo), FieldStatus.ComparisonCode.EqualTo,
                $"Must be equal to: {compareTo}");

        public static TFluentBinder IsNotEqualTo<TFluentBinder>(this TFluentBinder fb, object compareTo)
            where TFluentBinder : IFluentBinder =>
            fb.IsTrueScalar(v => !v.Equals(compareTo), FieldStatus.ComparisonCode.EqualTo,
                $"Must not be equal to: {compareTo}");
        
        public static TFluentBinder LessThan<TFluentBinder, T>(this TFluentBinder fb, T value,
            string errorDescription = null)
            where T : IComparable<T>
            where TFluentBinder : IFluentBinder<T>
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) < 0,
                FieldStatus.ComparisonCode.LessThan, value, errorDescription);
        }


        public static TFluentBinder GreaterThan<TFluentBinder, T>(this TFluentBinder fb, T value)
            where T : IComparable<T>
            where TFluentBinder : IFluentBinder<T>
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) > 0,
                FieldStatus.ComparisonCode.GreaterThan, value);
        }

        public static TFluentBinder GreaterThanOrEqualTo<TFluentBinder, T>(this TFluentBinder fb, T value)
            where T : IComparable<T>
            where TFluentBinder : IFluentBinder<T>
        {
            return fb.IsTrueScalar(v => v.CompareTo(value) > 0,
                FieldStatus.ComparisonCode.GreaterThan, value);
        }


        /// <summary>
        /// Only lightly tested "v3" version of convert
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="fb"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static FluentBinder3<TTo> Convert3<TFrom, TTo>(this IFluentBinder3<TFrom> fb, 
            tryConvertDelegate<IField<TFrom>, TTo> converter, string cannotConvert = null)
        {
            TTo converted = default(TTo);
            var fbConverted = new FluentBinder3<TTo>(fb.Field.Name, () => converted);
            // DEBT: Experimentally processing conversion as START of converted binder rather than
            // end of unconverted binder -- seems to make more sense, but other converters don't do
            // it this way
            fbConverted.Binder.Processor.ProcessingAsync += (sender, context) =>
            {
                bool success = converter(fb.Field, out converted);

                // FIX: We should be using this one, but it's yet another one and seemingly uncoupled from fbConverted.Field
                //IFieldStatusCollector2 field = context.Field;

                IFieldStatusCollector2 field = fbConverted.Field;

                if (success)
                    // DEBT: Kinda redundant, assigning value here -and- getter itself pointing to value
                    context.Value = converted;
                else
                    field.Error( FieldStatus.ComparisonCode.Unspecified, typeof(TFrom),
                        cannotConvert ?? "Conversion from type {0} failed");

                return new ValueTask();
            };
            return fbConverted;
        }


        public static FluentBinder2<TTo> Convert<T, TTo>(this IFluentBinder<T> fb,
            tryConvertDelegate<IField<T>, TTo> converter, string cannotConvert = null, Optional<TTo> defaultValue = null)
        {
            var fb2 = new FluentBinder2<TTo>(fb.Binder, false);
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                if (defaultValue != null && context.Value == null)
                {
                    context.Value = defaultValue.Value;
                    fb2.InitialValue = defaultValue.Value;
                }
                else if (converter(fb.Field, out TTo converted))
                {
                    context.Value = converted;
                    fb2.InitialValue = converted;
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

        public static FluentBinder2<TTo> Convert<T, TTo>(this IFluentBinder<T> fb,
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
        public static FluentBinder2<TTo> Convert<TTo>(this IFluentBinder fb, Optional<TTo> defaultValue = null)
        {
            var fb2 = new FluentBinder2<TTo>(fb.Binder, false);
            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField f = fb.Field;
                Type t = typeof(TTo);

                if (f.Value == null && defaultValue != null)
                {
                    context.Value = defaultValue.Value;
                    fb2.InitialValue = defaultValue.Value;
                    return new ValueTask();
                }

                try
                {
                    var converted = (TTo)
                        System.Convert.ChangeType(f.Value, t);

                    context.Value = converted;
                    fb2.InitialValue = converted;
                }
                catch (FormatException)
                {
                    // DEBT: Get a candidate factory to grab descriptions from a comparisoncode + scalar
                    f.Error(FieldStatus.ComparisonCode.Unspecified, t, "Unable to convert to type {0}");
                    context.Abort = true;
                }
                catch(InvalidCastException)
                {
                    // DEBT: Would be far better to check for null before issuing conversion.  Not doing so
                    // because some types can be null and the logic for determining that is a tiny bit involved
                    // DEBT: Not a foregone conclusion that InvalidCastException is because we can't convert a value
                    // type to null -- but probably that's why we get the exception
                    f.Error(FieldStatus.ComparisonCode.IsNull, null, "Null not allowed here");
                    context.Abort = true;
                }
                catch(OverflowException)
                {
                    f.Error(FieldStatus.ComparisonCode.GreaterThan, t, "Out of bounds for type {0}");
                    context.Abort = true;
                }

                return new ValueTask();
            };
            return fb2;
        }


        static FluentBinder2<DateTimeOffset> FromEpochToDateTimeOffset<T>(this IFluentBinder<T> fb)
        {
            var fbConverted = fb.Convert((IField<T> f, out DateTimeOffset dt) =>
            {
                // NOTE: We know we can safely do this because only the <int> and <long> overloads
                // are permitted to call this method
                var value = System.Convert.ToInt64(f.Value);

                try
                {
                    dt = DateTimeOffset.FromUnixTimeSeconds(value);
                    return true;
                }
                catch (ArgumentOutOfRangeException aoore)
                {
                    f.Error(FieldStatus.ComparisonCode.Unspecified, f.Value, aoore.Message);
                    return false;
                }
            });
            return fbConverted;
        }

        public static FluentBinder2<DateTimeOffset> FromEpochToDateTimeOffset(this IFluentBinder<int> fb) =>
            FromEpochToDateTimeOffset<int>(fb);

        public static FluentBinder2<DateTimeOffset> FromEpochToDateTimeOffset(this IFluentBinder<long> fb) =>
            FromEpochToDateTimeOffset<long>(fb);

        public static FluentBinder2<DateTimeOffset> ToDateTimeOffset(this IFluentBinder<int, Traits.Epoch> fb) =>
            FromEpochToDateTimeOffset<int>(fb);

        public static FluentBinder2<DateTimeOffset> ToDateTimeOffset(this IFluentBinder<long, Traits.Epoch> fb) =>
            FromEpochToDateTimeOffset<long>(fb);

        static bool FilterStatus(Status s)
            => s.Level != Status.Code.OK;

        public static IFluentBinder<T> Emit<T>(this IFluentBinder<T> fb, Action<T> emitter,
            Func<Status, bool> whenStatus = null, bool bypassFilter = false)
        {
            if (whenStatus == null) whenStatus = FilterStatus;

            fb.Binder.ProcessingAsync += (field, context) =>
            {
                IField<T> f = fb.Field;

                if (bypassFilter || !field.Statuses.Any(whenStatus))
                    emitter(f.Value);

                return new ValueTask();
            };
            return fb;
        }


        public static FluentBinder2<T, TTrait> WithTrait<T, TTrait>(this IFluentBinder<T> fb,
            TTrait trait = default(TTrait)) =>
            new FluentBinder2<T, TTrait>(fb);


        /// <summary>
        /// Tags the fluent binder as a UNIX Epoch
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public static FluentBinder2<int, Traits.Epoch> AsEpoch(this IFluentBinder<int> fb) =>
            fb.WithTrait<int, Traits.Epoch>();


        // EXPERIMENTAL
        // Convert and assign on initialization only
        public static FluentBinder2<TTo> Chain<T, TTo>(this IFluentBinder<T> fluentBinder, IBinder2 binder, tryConvertDelegate<IField<T>, TTo> convert,
            Action<TTo> setter)
        {
            // TODO: Rather than check a flag each time, remove the delegate from the ProcessedAsync chain
            bool initialized = false;
            var fbChained = new FluentBinder2<TTo>(binder, true);
            fluentBinder.Binder.ProcessedAsync += (f, c) =>
            {
                if (initialized) return new ValueTask();

                // Do one time conversion + setter initialization of chained FluentBinder
                if (convert(fluentBinder.Field, out TTo initialValue))
                {
                    fbChained.InitialValue = initialValue;
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


        public static FluentBinder2<T> Chain<T>(this IFluentBinder<T> fluentBinder, IBinder2 binder, Action<T> setter)
        {
            return fluentBinder.Chain(binder,
            (IField<T> f, out T v) =>
            {
                v = f.Value;
                return true;
            }, setter);
        }


        /// <summary>
        /// Configures optional setter to write back to validating source
        /// </summary>
        /// <typeparam name="TFluentBinder"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="fluentBinder"></param>
        /// <param name="setter"></param>
        /// <param name="initialGetter"></param>
        /// <returns></returns>
        /// <remarks>
        /// Be careful - no compile time enforcement of T
        /// Mainly so that we can experiment with Win32 Registry setters
        /// Otherwise this could be IFluentBinder<typeparamref name="T"/> constrained
        /// </remarks>
        public static TFluentBinder Setter<TFluentBinder, T>(this TFluentBinder fluentBinder, Action<T> setter,
            Func<T> initialGetter = null)
            where TFluentBinder: IFluentBinder
        {
            var binder = (IBinderBase<T>)fluentBinder.Binder;
            binder.setter = setter;
            if(initialGetter != null)
                setter(initialGetter());
            return fluentBinder;
        }


        public static IFluentBinder<T> Commit<T>(this IFluentBinder<T> fluentBinder, Committer committer, Action<T> commit)
        {
            committer.Committing += () =>
            {
                //commit(fb.InitialValue);
                // DEBT: We may prefer to use InitialValue, though fb.Field.Value is smart enough to get us the
                // right thing
                IField<T> f = fluentBinder.Field;
                commit(f.Value);
                return new ValueTask();
            };
            return fluentBinder;
        }


        public static IFluentBinder<T> Commit<T>(this IFluentBinder<T> fluentBinder, Action<T> commit) =>
            fluentBinder.Commit(fluentBinder.Binder.Committer, commit);

        // Temporarily named Required3 until we phase out "v2"
        public static TFluentBinder Required3<TFluentBinder, T>(this TFluentBinder fluentBinder, Func<T, bool> isEmpty)
            where TFluentBinder : IFluentBinder3<T>
        {
            fluentBinder.Binder.Processor.ProcessingAsync += (_, context) =>
            {
                if (isEmpty(fluentBinder.Field.Value))
                {
                    // DEBT: IsNull is wrong code here, since v may actually be empty string or similar
                    fluentBinder.Field.Error(FieldStatus.ComparisonCode.IsNull, null, "Field is required");
                    context.Abort = true;
                }

                return new ValueTask();
            };
            return fluentBinder;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fluentBinder"></param>
        /// <typeparam name="TFluentBinder"></typeparam>
        /// <returns></returns>
        /// <remarks>
        /// TODO: If we can somehow get IFluentBinder3 to be covariant, then this IsNotNull might be usable
        /// more often
        /// </remarks>
        public static TFluentBinder IsNotNull<TFluentBinder>(this TFluentBinder fluentBinder)
            where TFluentBinder : IFluentBinder3<object>
            =>
            fluentBinder.Required3<TFluentBinder, object>(v => v == null);

        public static TFluentBinder Required3<TFluentBinder>(this TFluentBinder fluentBinder)
            where TFluentBinder : IFluentBinder3<string> =>
            fluentBinder.Required3<TFluentBinder, string>(string.IsNullOrWhiteSpace);

        public static TFluentBinder Optional<TFluentBinder, T>(this TFluentBinder fluentBinder, Func<T, bool> isEmpty)
            where TFluentBinder : IFluentBinder3<T>
        {
            fluentBinder.Binder.Processor.StartingAsync += (_, context) =>
            {
                if (isEmpty(fluentBinder.Field.Value)) context.Abort = true;
                return new ValueTask();
            };
            return fluentBinder;
        }
        
        public static TFluentBinder Optional<TFluentBinder>(this TFluentBinder fluentBinder)
            where TFluentBinder : IFluentBinder3<string> =>
            fluentBinder.Optional<TFluentBinder, string>(string.IsNullOrWhiteSpace);
    }


    namespace Traits
    {
        public struct Epoch
        {

        }
    }
}
