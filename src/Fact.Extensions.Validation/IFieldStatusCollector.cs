using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Mechanism for actual gathering of status; has no specific contract for reviewing 
    /// these errors
    /// </summary>
    public interface IFieldStatusCollector : ICollector<Status>
    {
    }


    /// <summary>
    /// Surface for adding whole enumerations of status to this object
    /// Useful when external status changed independently
    /// </summary>
    public interface IFieldStatusExternalCollector
    {
        /// <summary>
        /// Bring in external status tracker
        /// </summary>
        /// <param name="statuses"></param>
        /// <remarks>
        /// Remember, this brings in a reference to the enumerable, meaning changes to
        /// provided enumeration are reflected in this field itself
        /// TODO: Consider adding a 'signal' delegate here
        /// </remarks>
        void Add(IEnumerable<Status> statuses);
    }


    public static class IFieldStatusProviderExtensions
    {
    }


    public static class IFieldStatusCollectorExtensions
    {
        public static void Error(this IFieldStatusCollector field, FieldStatus.ComparisonCode code,
            object value, string description = null) =>
            field.Add(new ScalarStatus(Status.Code.Error, description, code, value));

        public static void Add(this IFieldStatusCollector field, Status.Code code, string description) =>
            field.Add(new Status(code, description));

        public static void Error(this IFieldStatusCollector field, string description) =>
            field.Add(Status.Code.Error, description);
    }
}
