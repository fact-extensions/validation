using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    public interface IBinderBase
    {
        /// <summary>
        /// Original 'canonical' field with aggregated/total status
        /// </summary>
        IField Field { get; }

        /// <summary>
        /// Discrete source from which to get the value which we will validate against
        /// </summary>
        /// <remarks>
        /// DEBT: Some overlap with IField.Value, disambiguate them
        /// DEBT: lower case naming a no-no here
        /// </remarks>
        Func<object> getter { get; }

        Experimental.Committer Committer { get; }
    }


    /// <summary>
    /// "v3" binder with has-a processor
    /// Dedicated to IField binding
    /// </summary>
    public interface IFieldBinder : Experimental.IBinder3Base, IBinderBase
    {

    }


    /// <summary>
    /// "v3" binder with has-a processor
    /// Dedicated to IField binding
    /// </summary>
    public interface IFieldBinder<T> :
        Experimental.IBinderBase<T>,
        IFieldBinder
    {

    }
}
