using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation.Experimental
{
    public class VAssert<T> : VAssert
    {
        public Experimental.IEntityBinder<T> Binder { get; }

        public VAssert(T entity) :
            base(new AggregatedBinder())
        {
            Binder = aggregatedBinder.BindInput2(entity);
        }
    }
}
