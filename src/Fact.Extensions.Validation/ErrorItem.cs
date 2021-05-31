using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public interface IEntity { }

    public interface IField : 
        IFieldStatusProvider2,
        IFieldStatusCollector2
    {
        string Name { get; }
        object Value { get; }
    }


    public interface IField<T> : IField
    {
        new T Value { get; }
    }


    // TODO: Consider interacting with IDataErrorInfo interface, as per MS standard
    public class FieldStatus : IComparable<FieldStatus>,
        IField
    {
        public FieldStatus() { }
        public FieldStatus(FieldStatus copyFrom) :
            this(copyFrom.name, copyFrom.value, copyFrom.Statuses)
        {
        }

        public FieldStatus(string name, object value, IEnumerable<Status> statuses = null)
        {
            this.name = name;
            this.value = value;
            if(statuses != null)
                this.statuses.AddRange(statuses);
        }

        private object value;
        private string name;

        /// <summary>
        /// Original value presented to engine at beginning of validation/conversion chain
        /// </summary>
        public object Value
        {
            get => value;
            internal set => this.value = value;
        }


        public string Name => name;

        // the time is coming where ErrorItem is going to become StatusItem...
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

        public class Status
        {
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
            ComparisonCode Code { get; }
            readonly object comparedTo;

            // DEBT: Really should come from a factory somewhere
            static string GetDescription(ComparisonCode code)
            {
                switch (code)
                {
                    case ComparisonCode.GreaterThan:
                        return "Must be greater than {0}";
                    
                    case ComparisonCode.LessThan:
                        return "Must be less than {0}";
                    
                    default:
                        throw new IndexOutOfRangeException("Unhandled code");
                }
            }

            public ScalarStatus(Code _code, string description, ComparisonCode code, object comparedTo) :
                base(_code, string.Format(description ?? GetDescription(code), comparedTo))
            {
                Code = code;
                this.comparedTo = comparedTo;
            }
            
            public ScalarStatus(Code code, string description, object comparedTo) : 
                this(code, description, ComparisonCode.Unspecified, comparedTo)
            {
            }
        }

#if DEBUG
        // For unit testing only
        internal ICollection<Status> InternalStatuses => statuses;
#endif

        // DEBT: Make this into an IEnumerable so that aggregator has an easier time of it
        readonly List<Status> statuses = new List<Status>();

        // EXPERIMENTAL - primarily for 'Conflict' registrations
        List<IFieldStatusProvider2> ExternalStatuses = new List<IFieldStatusProvider2>();

        List<IEnumerable<Status>> externalStatuses2 = new List<IEnumerable<Status>>();

        public void AddIfNotPresent(IFieldStatusProvider2 external)
        {
            if(!ExternalStatuses.Contains(external))
                ExternalStatuses.Add(external);
        }


        // EXPERIMENTAL
        public void Add(IEnumerable<Status> external)
        {
            externalStatuses2.Add(external);
        }

        // EXPERIMENTAL - probably not use -- cross field validation better done at a top level
        public IEnumerable<Status> Statuses
        {
            get
            {
                foreach (Status status in ExternalStatuses.SelectMany(x => x.Statuses))
                    yield return status;

                foreach (Status status in statuses)
                    yield return status;

                foreach (Status status in externalStatuses2.SelectMany(x => x))
                    yield return status;
            }
        }

        public void Clear() => statuses.Clear();

        public void Add(Code code, string description) =>
            statuses.Add(new Status(code, description));

        public void Add(Status status) =>
            statuses.Add(status);

        public override int GetHashCode()
        {
            var statusHash = Statuses.Sum(x => x.GetHashCode());

            return (Name ?? "").GetHashCode() ^ statusHash.GetHashCode() ^ (Value ?? "").GetHashCode();
        }

        public override string ToString()
        {
            string s = string.Join("\r", Statuses.Select(x => x.ToString()));
            return $"Name={Name}, Value={Value}, Statuses={s}";
        }

        #region IComparable<FieldStatus> Members

        int IComparable<FieldStatus>.CompareTo(FieldStatus other)
        {
            return other.GetHashCode() - GetHashCode();
        }

        #endregion


        /// <summary>
        /// Used in Category field in ErrorItemCoded
        /// </summary>
        public enum CategoryCode : short
        {
            Arithmetic = 0,
            Comparison,
            Database,
            Memory,
            /// <summary>
            /// Specialized, generic app area.   Avoid using this,
            /// instead make a category code in the range 10000-20000
            /// and consistently use that
            /// </summary>
            AppSpecific = -1,
            /// <summary>
            /// Specialized, generic app area.   Avoid using this,
            /// instead make a lib code in the range 20001-30000
            /// and consistently use that
            /// </summary>
            LibSpecific = -2,
            /// <summary>
            /// Specialized, generic app area.   Avoid using this,
            /// instead make a sys code in the range 30001-32000
            /// and consistently use that
            /// </summary>
            SystemSpecific = -3
        }

        /// <summary>
        /// Used in Code field in ErrorItemCoded when Category == Comparison
        /// </summary>
        public enum ComparisonCode : short
        {
            Unspecified = -1,
            GreaterThan = 0,
            GreaterThanOrEqualTo,
            LessThan,
            LessThanOrEqualTo,
            EqualTo,
            NotEqualTo,
            IsNull,
            IsNotNull
        }
    }


    public class FieldStatus<T> : FieldStatus,
        IField<T>
    {
        public FieldStatus() : base() { }

        public FieldStatus(string name, T value) :
            base(name, value)
        { }

        T IField<T>.Value => (T)base.Value;
    }

    /// <summary>
    /// Error item with machine-discernable category and error code
    /// </summary>
    public class ErrorItemCoded : FieldStatus
    {
        public readonly CategoryCode Category;
        /// <summary>
        /// Denotes what condition Value needs to conform to, but didn't.  
        /// For example ComparisonCode.GreaterThan indicates Value needed
        /// to be greater than something, but wasn't
        /// </summary>
        public readonly short Code;

        public ErrorItemCoded(string parameter, string description, object value, CategoryCode category, short code) :
            base(parameter, value)
        {
            Category = category;
            Code = code;
        }
    }

    public class ExtendedFieldStatus : FieldStatus
    {
        public const int INDEX_NA = -1;
        public const int INDEX_SPREAD = -2;

        public string Prefix { get; set; }

        /// <summary>
        /// Index of array being evaluated, if any
        /// </summary>
        /// <remarks>
        /// -1 means N/A, typical for non array situations
        /// -2 means applies across the whole array ie: "[].Last"
        /// See http://wiki.factmusic.com/Apprentice.error-item.ashx
        /// </remarks>
        public int Index { get; set; }

        public ExtendedFieldStatus() { }
        public ExtendedFieldStatus(FieldStatus copyFrom) : base(copyFrom) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="copyFrom">Copy from.</param>
        /// <param name="prefix"></param>
        /// <param name="parameter">overrides copyFrom's parameter</param>
        /// <param name="index">Index.</param>
        public ExtendedFieldStatus(FieldStatus copyFrom, string prefix, int index) : base(copyFrom)
        {
            Prefix = prefix;
            Index = index;
        }

        public ExtendedFieldStatus(string prefix, string name, string description, object value, Code level = Code.Error) :
            base(name, value)
        {
            Prefix = prefix;
            Add(level, description);
        }

        public ExtendedFieldStatus(string prefix, string parameter, string description, object value, int index, Code level) :
            base(parameter, value)
        {
            Prefix = prefix;
            Index = index;
            Add(level, description);
        }

        public override string ToString()
        {
            if (Prefix != null)
                return Prefix + "." + base.ToString();
            else
                return base.ToString();
        }
    }
}

namespace Fact.Extensions.Validation
{
    public static class ErrorItemUtility
    {
        /// <summary>
        /// Assign a prefix to this ExtendedErrorItem, and if it is not an extended errorItem, promote
        /// it to one so that prefix may be assigned
        /// </summary>
        /// <param name="errorItem"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static void SetPrefix(ref FieldStatus errorItem, string prefix)
        {
            var _errorItem = errorItem as ExtendedFieldStatus;
            if (_errorItem != null)
                _errorItem.Prefix = prefix;
            else
                _errorItem = new ExtendedFieldStatus(errorItem) { Prefix = prefix };
        }


        /// <summary>
        /// Return only fields which have an error status.  Some of the statuses in the field
        /// may NOT be in error.
        /// </summary>
        /// <param name="errors"></param>
        /// <returns>Enumeration of errors matching the criteria</returns>
        public static IEnumerable<FieldStatus> OnlyErrors(this IEnumerable<FieldStatus> errors)
        {
            return errors.Where(x => 
                x.Statuses.Any(y => y.Level == FieldStatus.Code.Error || y.Level == FieldStatus.Code.Exception));
        }


        public static void Error(this IField field, string description) =>
            field.Add(FieldStatus.Code.Error, description);
    }
}
