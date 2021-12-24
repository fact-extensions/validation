using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Context tracking data for Prcessor
    /// </summary>
    public interface IContext
    {
        /// <summary>
        /// When true, evaluation context proceeds normally (implicitly all the way until the end)
        /// When false, evaluation halts completely (catestrophic failure)
        /// Defaults to true
        /// </summary>
        bool Abort { get; set; }

        /// <summary>
        /// When true, signals Binder.Process that particular processor is completely
        /// awaited before next processor runs.  When false, particular process is
        /// treated fully asynchronously and the next processor begins evaluation in
        /// parallel.  Defaults to true 
        /// </summary>
        bool Sequential { get; set; }

        /// <summary>
        /// EXPERIMENTAL
        /// </summary>
        bool FreeRunning { get; set; }

        /// <summary>
        /// EXPERIMENTAL
        /// </summary>
        /// <remarks>
        /// DEBT: Regular ProcessAsync() call is passed CancellationToken, but with aggregators passing things around
        /// to other processors this may be useful.  If so, undebt and unexperimental this
        /// </remarks>
        CancellationToken CancellationToken { get; }
    }

    public class Context : IContext
    {
        /// <summary>
        /// <see cref="IContext.Abort" />
        /// </summary>
        public bool Abort { get; set; } = false;

        /// <summary>
        /// Indicates synchronous operation requested (vs async)
        /// </summary>
        public bool Sequential { get; set; }

        public bool FreeRunning { get; set; }

        public CancellationToken CancellationToken { get; }

        public Context(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
        }
    }


    /// <summary>
    /// Context tracking data for processor, associated to a particular field
    /// </summary>
    public interface IFieldContext : IContext, IServiceProviderProvider
    {
        object Value { get; set; }
        IField Field { get; }

        Experimental.InputContext InputContext { get; }
    }


    /// <summary>
    /// DEBT: Poor naming - this context has lots of UI/Field goodies where base one does not
    /// </summary>
    public class Context2 : Context, IFieldContext
    {
        /// <summary>
        /// Current value, which starts as populated by the binder's getter but may
        /// be converted as the pipeline is processed
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Tends to be initialized by FieldBinder's getter
        /// </summary>
        public object InitialValue { get; }

        // Still experimental 
        public Experimental.InputContext InputContext { get; set; }

        /// <summary>
        /// Same as IBinder.Field
        /// </summary>
        /// <remarks>
        /// EXPERIMENTAL putting this here 
        /// </remarks>
        public IField Field { get; }

        public IServiceProvider Services { get; }

        public Context2(object initialValue, IField field, CancellationToken cancellationToken) :
            base(cancellationToken)
        {
            Field = field;
            InitialValue = initialValue;
            Value = initialValue;
        }


        public Context2(IServiceProvider services, object initialValue, IField field, CancellationToken cancellationToken)
            : this(initialValue, field, cancellationToken)
        {
            Services = services;
        }
    }

}
