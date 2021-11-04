using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Low level binder inteface bringing in Processor and simplistic object getter
    /// </summary>
    public interface IBinderBase : Experimental.IBinder3Base
    {
        /// <summary>
        /// Discrete source from which to get the value which we will validate/process
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
    public interface IFieldBinder : IBinderBase
    {
        /// <summary>
        /// Original 'canonical' field with aggregated/total status
        /// </summary>
        IField Field { get; }
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
