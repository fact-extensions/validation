using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    // TODO: refactor this since ErrorItem isn't a proper description anymore.
    // should be something like FieldStatus
    // TODO: Consider interacting with IDataErrorInfo interface, as per MS standard
    public class ErrorItem : IComparable<ErrorItem>
    {
        public ErrorItem() { }
        public ErrorItem(ErrorItem copyFrom) :
            this(copyFrom.parameter, copyFrom.Description, copyFrom.value)
        {
            Level = copyFrom.Level;
        }

        public ErrorItem(string parameter, string description, object value)
        {
            this.parameter = parameter;
            this.Description = description;
            this.value = value;
            Level = ErrorLevel.Error;
        }

        private object value;
        private string parameter;

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }


        public string Parameter
        {
            get { return parameter; }
            set { parameter = value; }
        }

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
            return (Parameter ?? "").GetHashCode() ^ (Description ?? "").GetHashCode() ^ (Value ?? "").GetHashCode();
        }

        public override string ToString()
        {
            return "[" + Level.ToString()[0] + ":" + parameter + "]: " + Description + " / original value = " + Value;
        }

        #region IComparable<ErrorItem> Members

        int IComparable<ErrorItem>.CompareTo(ErrorItem other)
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
    public class ErrorItemCoded : ErrorItem
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

    public class ExtendedErrorItem : ErrorItem
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

        public ExtendedErrorItem() { }
        public ExtendedErrorItem(ErrorItem copyFrom) : base(copyFrom) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="copyFrom">Copy from.</param>
        /// <param name="prefix"></param>
        /// <param name="parameter">overrides copyFrom's parameter</param>
        /// <param name="index">Index.</param>
        public ExtendedErrorItem(ErrorItem copyFrom, string prefix, string parameter, int index) : base(copyFrom)
        {
            Parameter = parameter;
            Prefix = prefix;
            Index = index;
        }
        public ExtendedErrorItem(string prefix, string parameter, string description, object value)
        {
            Prefix = prefix;
            Parameter = parameter;
            Description = description;
            Value = value;
            Level = ErrorLevel.Error;
        }

        public ExtendedErrorItem(string prefix, string parameter, string description, object value, ErrorLevel level)
        {
            Prefix = prefix;
            Parameter = parameter;
            Description = description;
            Value = value;
            Level = level;
        }

        public ExtendedErrorItem(string prefix, string parameter, string description, object value, int index, ErrorLevel level)
        {
            Prefix = prefix;
            Parameter = parameter;
            Description = description;
            Value = value;
            Index = index;
            Level = level;
        }

        public ExtendedErrorItem(string prefix, string parameter, string description, object value, int index) :
            this(prefix, parameter, description, value)
        {
            Index = index;
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
        public static void SetPrefix(ref ErrorItem errorItem, string prefix)
        {
            var _errorItem = errorItem as ExtendedErrorItem;
            if (_errorItem != null)
                _errorItem.Prefix = prefix;
            else
                _errorItem = new ExtendedErrorItem(errorItem) { Prefix = prefix };
        }


        /// <summary>
        /// Return only errors from this enumerable, suppressing Information/Warning messages
        /// </summary>
        /// <param name="errors"></param>
        /// <returns>Enumeration of errors matching the criteria</returns>
        public static IEnumerable<ErrorItem> OnlyErrors(this IEnumerable<ErrorItem> errors)
        {
            return errors.Where(x => x.Level == ErrorItem.ErrorLevel.Error || x.Level == ErrorItem.ErrorLevel.Exception);
        }
    }
}
