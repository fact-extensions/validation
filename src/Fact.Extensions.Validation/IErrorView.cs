﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
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


    /// <summary>
    /// FIX: Naming - this represents .Status in fieldstatus
    /// </summary>
    public interface IFieldStatusProvider2
    {
        IEnumerable<FieldStatus.Status> Statuses { get; }
    }
}
