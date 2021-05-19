using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Mechanism for actual gathering of status; has no specific contract for reviewing 
    /// these errors
    /// </summary>
    public interface IFieldStatusCollector
    {
        /// <summary>
        /// Gather an error to be (probably) reviewed at a later time
        /// </summary>
        /// <param name="item"></param>
        void Append(FieldStatus item);
    }
}
