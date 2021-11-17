using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public static class FluentNullExtensions
    {
        static void OnRequiredError(IFieldStatus field, Context context)
        {
            field.Error(FieldStatus.ComparisonCode.IsNull, null, "Field is required");
            context.Abort = true;
        }



        public static TFluentBinder Required<TFluentBinder>(this TFluentBinder fluentBinder,
            Func<object, bool> isEmpty, Action<IFieldStatus, Context> onError = null)
            where TFluentBinder : IFieldProvider<IField>, IBinderProviderBase<IFieldBinder>
        {
            return fluentBinder.IsTrue(v => !isEmpty(v), onError ?? OnRequiredError);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TFluentBinder"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="fluentBinder"></param>
        /// <param name="isEmpty"></param>
        /// <returns></returns>
        /// <remarks>DEBT: Consolidate with above 'Required'</remarks>
        public static TFluentBinder Required<TFluentBinder, T>(this TFluentBinder fluentBinder, Func<T, bool> isEmpty,
            Action<IFieldStatus, Context> onError = null)
            where TFluentBinder : IFieldProvider<IField<T>>, IBinderProviderBase<IFieldBinder>
        {
            return fluentBinder.IsTrue((T v) => !isEmpty(v), onError ?? OnRequiredError);
        }


        /// <summary>
        /// Asserts bound value is not null
        /// </summary>
        /// <param name="fluentBinder"></param>
        /// <typeparam name="TFluentBinder"></typeparam>
        /// <returns></returns>
        public static TFluentBinder IsNotNull<TFluentBinder>(this TFluentBinder fluentBinder)
            where TFluentBinder : IFluentBinder
            =>
            fluentBinder.Required(v => v == null,
                (field, context) =>
                {
                    field.Error(FieldStatus.ComparisonCode.IsNull, null);
                    context.Abort = true;
                });

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
}
