using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    public class Status
    {
        public enum Code
        {
            NoChange = -2,      // Specialized error code representing no error state change since last the client issued a request
            Exception = -1,     // Exceptions should be very, very rare - indication of a non-user instigated error
            OK = 0,             // Generally unused, since we assume
            Error = 1,
            Warning = 2,
            Informational = 3,
            Conflict = 4,       // This field is in conflict with another
            Update = 10,        // Update is a special case status and not an error.  Notifies UI to update given
                                // field with Value

            // these experimental statuses need some refinement.  Statuses like these might get muddy since they may sometimes represent a
            // particular state (adjective) while others represent a state change (verb).  Seems things should be forced into an adjective state, even
            // if it is only for a brief few seconds as the user types something
            Disable = 100,      // experimental: UI specific (but not technology specific) status code for disabling input to a field
            Highlight = 101,    // experimental: UI specific (but not technology specific) status code for highlighting an input field
            Focus = 102         // experimental: UI specific (but not technology specific) status code for focusing input to a field
        }

        public Code Level { get; }

        public string Description { get; }

        public Status() { }

        public Status(Code level, string description = null)
        {
            Level = level;
            Description = description;
        }

        public override int GetHashCode() =>
            (Description ?? "").GetHashCode();

        public override string ToString() =>
            $"[{Level}: {Description}]";

        public string ToString(object value) =>
            Description + " / original value = " + value;

        public string ToString(string name, object value)
        {
            return "[" + Level.ToString()[0] + ":" + name + "]: " + ToString(value);
        }
    }


    public class ConflictStatus : Status
    {
        readonly IField conflictingWith;

        public ConflictStatus(IField conflictingWith) : base(Code.Conflict)
        {
            this.conflictingWith = conflictingWith;
        }
    }


    public class ScalarStatus : Status
    {
        FieldStatus.ComparisonCode Code { get; }
        readonly object comparedTo;

        // DEBT: Really should come from a factory somewhere
        static string GetDescription(FieldStatus.ComparisonCode code)
        {
            switch (code)
            {
                case FieldStatus.ComparisonCode.GreaterThan:
                    return "Must be greater than {0}";

                case FieldStatus.ComparisonCode.LessThan:
                    return "Must be less than {0}";

                default:
                    throw new IndexOutOfRangeException("Unhandled code");
            }
        }

        public ScalarStatus(Code _code, string description, FieldStatus.ComparisonCode code, object scalar) :
            base(_code, string.Format(description ?? GetDescription(code), scalar))
        {
            Code = code;
            this.comparedTo = scalar;
        }

        public ScalarStatus(Code code, string description, object scalar) :
            this(code, description, FieldStatus.ComparisonCode.Unspecified, scalar)
        {
        }
    }
}
