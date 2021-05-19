using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Mechanism for actual gathering of errors; has no specific contract for reviewing 
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


    /// <summary>
    /// Mechanism to inspect present state of errors; has no contract to append/modify
    /// said errors
    /// </summary>
    public interface IFieldStatusProvider
    {
        /// <summary>
        /// Acquire list of accumulated errors
        /// </summary>
        IEnumerable<FieldStatus> Statuses { get; }

        bool IsValid { get; }
    }
}
