using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    using Experimental;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public static class GroupBinderExtensions
    {

        public static void GroupValidate<T, T1, T2>(this EntityBinder<T> binder, //IAggregatedBinder parent, 
            Expression<Func<T, T1>> field1Lambda,
            Expression<Func<T, T2>> field2Lambda,
            Action<IFieldContext, IField<T1>, IField<T2>> handler)
        {
            /*
            var field1 = binder.CreateShimField(field1Lambda);
            var field2 = binder.CreateShimField(field2Lambda);

            parent.ProcessingAsync += (f, c) =>
            {
                field1.ClearShim();
                field2.ClearShim();

                handler(c, field1, field2);

                return new ValueTask();
            }; */
            binder.Get(field1Lambda).FluentBinder.GroupValidate(
                binder.Get(field2Lambda).FluentBinder,
                (c, f1, f2) =>
                {
                    handler(c, f1, f2);
                    return new ValueTask();
                });
        }
    }
}
