﻿using System;
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

                IFieldStatusCollector2 field = fbConverted.Field;

                if (success)
                    // DEBT: Kinda redundant, assigning value here -and- getter itself pointing to value
                    context.Value = converted;
                else if(cannotConvert != null)
                    field.Error( FieldStatus.ComparisonCode.Unspecified, typeof(TFrom),
                        cannotConvert ?? "Conversion from type {0} failed");

                return new ValueTask();
            };
            return fbConverted;
        }




        static bool FilterStatus(Status s)
            => s.Level != Status.Code.OK;

        public static IFluentBinder<T> Emit<T>(this IFluentBinder<T> fb, Action<T> emitter,
            Func<Status, bool> whenStatus = null, bool bypassFilter = false)
        {
            if (whenStatus == null) whenStatus = FilterStatus;

            fb.Binder.Processor.ProcessingAsync += (_, context) =>
            {
                IField field = context.Field;
                IField<T> f = fb.Field;

                if (bypassFilter || !field.Statuses.Any(whenStatus))
                    emitter(f.Value);

                return new ValueTask();
            };
            return fb;
        }


        public static FluentBinder<T, TTrait> WithTrait<T, TTrait>(this IFluentBinder<T> fb,
            TTrait trait = default(TTrait)) =>
            // DEBT: Smooth out this cast
            new FluentBinder<T, TTrait>((IFieldBinder<T>)fb.Binder, false);


        /// <summary>
        /// Tags the fluent binder as a UNIX Epoch
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public static FluentBinder<int, Traits.Epoch> AsEpoch(this IFluentBinder<int> fb) =>
            fb.WithTrait<int, Traits.Epoch>();


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

        
        public static TFluentBinder Required<TFluentBinder, T>(this TFluentBinder fluentBinder, Func<T, bool> isEmpty)
            where TFluentBinder : IFieldProvider<IField<T>>, IBinderProviderBase<IFieldBinder>
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
            where TFluentBinder : IFluentBinder<object>
            =>
            fluentBinder.Required<TFluentBinder, object>(v => v == null);

        public static TFluentBinder Required<TFluentBinder>(this TFluentBinder fluentBinder)
            where TFluentBinder : IFluentBinder<string> =>
            fluentBinder.Required<TFluentBinder, string>(string.IsNullOrWhiteSpace);

        public static TFluentBinder Optional<TFluentBinder, T>(this TFluentBinder fluentBinder, Func<T, bool> isEmpty)
            where TFluentBinder : IFluentBinder<T>
        {
            fluentBinder.Binder.Processor.StartingAsync += (_, context) =>
            {
                if (isEmpty(fluentBinder.Field.Value)) context.Abort = true;
                return new ValueTask();
            };
            return fluentBinder;
        }
        
        public static TFluentBinder Optional<TFluentBinder>(this TFluentBinder fluentBinder)
            where TFluentBinder : IFluentBinder<string> =>
            fluentBinder.Optional<TFluentBinder, string>(string.IsNullOrWhiteSpace);
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
