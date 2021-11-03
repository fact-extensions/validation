using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    using IFieldBinder = Experimental.IFieldBinder;

    public static class FluentLogicExtensions
    {
        public static TFluentBinder IsTrue<TFluentBinder, T>(this TFluentBinder fb, Func<T, bool> predicate,
            Func<Status> getIsFalseStatus)
            where TFluentBinder : IFluentBinder<T>
        {
            ((IFieldBinder)fb.Binder).Processor.ProcessingAsync += (_, context) =>
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
            ((IFieldBinder)fb.Binder).Processor.ProcessingAsync += (_, context) =>
            {
                IField f = fb.Field;
                if (!predicate(f.Value))
                    f.Add(getIsFalseStatus());

                return new ValueTask();
            };
            return fb;
        }
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
    }
}
