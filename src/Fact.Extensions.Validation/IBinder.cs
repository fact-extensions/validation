using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Low level binder inteface which couples a Processor and simplistic object getter
    /// </summary>
    /// <remarks>
    /// DEBT: Decouple from Context2 since Context2 has a 1:1 field relationship, but Binder3Base'd stuff
    /// isn't always
    /// </remarks>
    public interface IBinderBase : IProcessorProvider<Context2>
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
    /// Binder with has-a processor ala IBinderBase
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
