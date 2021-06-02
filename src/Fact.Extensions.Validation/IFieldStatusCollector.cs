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
        void Append(string fieldName, Status status);
    }


    public interface IFieldStatusCollector2
    {
        void Add(Status status);
    }


    public interface IFieldStatusExternalCollector
    {
        void Add(IEnumerable<Status> statuses);
    }


    public static class IFieldStatusProviderExtensions
    {
    }


    public static class IFieldStatusCollectorExtensions
    {
        public static void Error(this IFieldStatusCollector fsc, string field, string description) =>
            fsc.Append(field, new Status(Status.Code.Error, description));

        public static void Error(this IFieldStatusCollector2 field, FieldStatus.ComparisonCode code,
            object value, string description = null) =>
            field.Add(new ScalarStatus(Status.Code.Error,
                description, FieldStatus.ComparisonCode.Unspecified, value));

        public static void Add(this IFieldStatusCollector2 field, Status.Code code, string description) =>
            field.Add(new Status(code, description));

        public static void Error(this IFieldStatusCollector2 field, string description) =>
            field.Add(Status.Code.Error, description);
    }
}
