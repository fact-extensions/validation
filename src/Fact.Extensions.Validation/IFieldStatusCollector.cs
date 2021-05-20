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
        /// Gather an status to be (probably) reviewed at a later time
        /// </summary>
        /// <param name="item"></param>
        void Append(string fieldName, FieldStatus.Status status);
    }


    public interface IFieldStatusCollector2
    {
        void Add(FieldStatus.Status status);
    }


    public static class IFieldStatusProviderExtensions
    {
        public static void Add(this IFieldStatusCollector2 field, FieldStatus.Code code, string description) =>
            field.Add(new FieldStatus.Status(code, description));

        public static void Error(this IFieldStatusCollector2 field, string description) =>
            field.Add(FieldStatus.Code.Error, description);
    }
}
