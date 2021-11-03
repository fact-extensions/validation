using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Fact.Extensions.Validation
{
    public interface IContext
    {
        bool Abort { get; }

        /// <summary>
        /// When true, signals Binder.Process that particular processor is completely
        /// awaited before next processor runs.  When false, particular process is
        /// treated fully asynchronously and the next processor begins evaluation in
        /// parallel.  Defaults to true 
        /// </summary>
        bool Sequential { get; set; }
    }

    public class Context
    {
        /// <summary>
        /// When true, evaluation context proceeds normally (implicitly all the way until the end)
        /// When false, evaluation halts completely (catestrophic failure)
        /// Defaults to true
        /// </summary>
        public bool Abort { get; set; } = false;
    }



    public class Context2 : Context, IContext
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

        /// <summary>
        /// Indicates synchronous operation requested (vs async)
        /// </summary>
        public bool Sequential { get; set; }

        public CancellationToken CancellationToken { get; }

        // Still experimental 
        public Experimental.InputContext InputContext { get; set; }

        // EXPERIMENTAL putting this here -- same as IBinder.Field
        public IField Field { get; }

        public Context2(object initialValue, IField field, CancellationToken cancellationToken)
        {
            Field = field;
            InitialValue = initialValue;
            Value = initialValue;
            CancellationToken = cancellationToken;
        }
    }

}
