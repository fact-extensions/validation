using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation.Experimental
{
    public static class AggregateBinderExtensions
    {
        /// <summary>
        /// For testing "v3" binder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TBinderProvider"></typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <param name="providerFactory"></param>
        /// <returns></returns>
        public static FluentBinder2<T> AddField3<T, TBinderProvider>(this ICollector<TBinderProvider> binder, string name, Func<T> getter,
            Func<IFluentBinder<T>, TBinderProvider> providerFactory)
            where TBinderProvider : IBinderProvider
        {
            var b = new FieldBinder<T>(name, getter);
            var fb = new FluentBinder2<T>(b);
            binder.Add(providerFactory(fb));
            return fb;
        }


        /// <summary>
        /// For testing "v3" binder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public static FluentBinder2<T> AddField3<T>(this IAggregatedBinderCollector binder, string name, Func<T> getter) =>
            binder.AddField3(name, getter, fb => new BinderManagerBase.ItemBase(fb.Binder, fb));
    }
}
