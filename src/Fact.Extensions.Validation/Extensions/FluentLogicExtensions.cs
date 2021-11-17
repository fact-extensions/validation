using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public static class FluentLogicExtensions
    {
        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            Func<Status> getIsFalseStatus)
            where TFluentBinder : IFieldProvider<IField<T>>, IBinderProviderBase<IFieldBinder>
        {
            return fb.IsTrue(predicate, (field, _) => field.Add(getIsFalseStatus()));
        }


        public static TFluentBinder IsTrue<TFluentBinder>(this TFluentBinder fb, Func<object, bool> predicate,
            Func<Status> getIsFalseStatus)
            where TFluentBinder : IFieldProvider<IField>, IBinderProviderBase<IFieldBinder>
        {
            return fb.IsTrue(predicate, (field, _) => field.Add(getIsFalseStatus()));
        }


        public static TFluentBinder IsTrue<TFluentBinder>(this TFluentBinder fb, Func<object, bool> predicate,
            Action<IFieldStatus, Context> onFalse)
            where TFluentBinder : IFieldProvider<IField>, IBinderProviderBase<IFieldBinder>
        {
            fb.Binder.Processor.ProcessingAsync += (_, context) =>
            {
                IField f = fb.Field;
                if (!predicate(f.Value))
                    onFalse(f, context);

                return new ValueTask();
            };
            return fb;
        }


        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            Action<IFieldStatus, Context> onFalse)
            where TFluentBinder : IFieldProvider<IField<T>>, IBinderProviderBase<IFieldBinder>
        {
            fb.Binder.Processor.ProcessingAsync += (_, context) =>
            {
                IField<T> f = fb.Field;
                if (!predicate(f.Value))
                    onFalse(f, context);

                return new ValueTask();
            };
            return fb;
        }


        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, ValueTask<bool>> predicate,
            Action<IFieldStatus, Context> onFalse)
            where TFluentBinder : IFieldProvider<IField<T>>, IBinderProviderBase<IFieldBinder>
        {
            fb.Binder.Processor.ProcessingAsync += async (_, context) =>
            {
                IField<T> f = fb.Field;
                if (!await predicate(f.Value))
                    onFalse(f, context);
            };
            return fb;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fb"></param>
        /// <param name="predicate"></param>
        /// <param name="messageIfFalse"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <remarks>
        /// DEBT: Is needed because otherwise C# flows to the above 'object' only one
        /// </remarks>
        public static IFluentBinder<T> IsTrue<T>(this IFluentBinder<T> fb, Func<T, bool> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error) =>
            fb.IsTrue(predicate, (f, _) => f.Add(level, messageIfFalse));

        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            string messageIfFalse, Status.Code level = Status.Code.Error)
            where TFluentBinder : IFluentBinder<T> =>
            fb.IsTrue(predicate, (f, _) => f.Add(level, messageIfFalse));


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
        public static TFluentBinder IsEqualTo<TFluentBinder>(this TFluentBinder fb, object compareTo)
            where TFluentBinder : IFluentBinder =>
            fb.IsTrueScalar(v => v.Equals(compareTo), FieldStatus.ComparisonCode.EqualTo,
                $"Must be equal to: {compareTo}");

        public static TFluentBinder IsNotEqualTo<TFluentBinder>(this TFluentBinder fb, object compareTo)
            where TFluentBinder : IFluentBinder =>
            fb.IsTrueScalar(v => !v.Equals(compareTo), FieldStatus.ComparisonCode.NotEqualTo,
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
                FieldStatus.ComparisonCode.GreaterThanOrEqualTo, value);
        }
    }
}
