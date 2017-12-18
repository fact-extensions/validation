using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    // TODO: refactor this since ErrorItem isn't a proper description anymore.
    // should be something like FieldStatus
    // TODO: Consider interacting with IDataErrorInfo interface, as per MS standard
    public class FieldStatus : IComparable<FieldStatus>
    {
        public FieldStatus() { }
        public FieldStatus(FieldStatus copyFrom) :
            this(copyFrom.name, copyFrom.Description, copyFrom.value)
        {
            Level = copyFrom.Level;
        }

        public FieldStatus(string name, string description, object value)
        {
            this.name = name;
            this.Description = description;
            this.value = value;
            Level = ErrorLevel.Error;
        }

        private object value;
        private string name;

        public object Value => value;


        public string Name => name;

        // the time is coming where ErrorItem is going to become StatusItem...
        public enum ErrorLevel
        {
            NoChange = -2,      // Specialized error code representing no error state change since last the client issued a request
            Exception = -1,     // Exceptions should be very, very rare - indication of a non-user instigated error
            Error = 0,
            Warning = 1,
            Informational = 2,
            Update = 10,        // Update is a special case status and not an error.  Notifies UI to update given
                                // field with Value

            // these experimental statuses need some refinement.  Statuses like these might get muddy since they may sometimes represent a
            // particular state (adjective) while others represent a state change (verb).  Seems things should be forced into an adjective state, even
            // if it is only for a brief few seconds as the user types something
            Disable = 100,      // experimental: UI specific (but not technology specific) status code for disabling input to a field
            Highlight = 101,    // experimental: UI specific (but not technology specific) status code for highlighting an input field
            Focus = 102         // experimental: UI specific (but not technology specific) status code for focusing input to a field
        }

        public ErrorLevel Level { get; set; }

        public string Description { get; set; }

        public override int GetHashCode()
        {
            return (Name ?? "").GetHashCode() ^ (Description ?? "").GetHashCode() ^ (Value ?? "").GetHashCode();
        }

        public override string ToString()
        {
            return "[" + Level.ToString()[0] + ":" + name + "]: " + Description + " / original value = " + Value;
        }

        #region IComparable<ErrorItem> Members

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
            base(parameter, description, value)
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

        public ExtendedFieldStatus(string prefix, string name, string description, object value, ErrorLevel level = ErrorLevel.Error) :
            base(name, description, value)
        {
            Prefix = prefix;
            Level = level;
        }

        public ExtendedFieldStatus(string prefix, string parameter, string description, object value, int index, ErrorLevel level) :
            base(parameter, description, value)
        {
            Prefix = prefix;
            Index = index;
            Level = level;
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
        /// Return only errors from this enumerable, suppressing Information/Warning messages
        /// </summary>
        /// <param name="errors"></param>
        /// <returns>Enumeration of errors matching the criteria</returns>
        public static IEnumerable<FieldStatus> OnlyErrors(this IEnumerable<FieldStatus> errors)
        {
            return errors.Where(x => x.Level == FieldStatus.ErrorLevel.Error || x.Level == FieldStatus.ErrorLevel.Exception);
        }
    }
}
