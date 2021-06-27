using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public interface IEntity { }

    // TODO: Consider interacting with IDataErrorInfo interface, as per MS standard
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Fields take on two forms:
    /// 1.  One that is always associated with a binder
    /// 2.  One this is static for serialization purposes
    /// </remarks>
    public class FieldStatus : 
        FieldBase,
        IComparable<FieldStatus>,
        IField,
        IFieldStatusExternalCollector
    {
        //public FieldStatus() { }
        public FieldStatus(FieldStatus copyFrom) :
            this(copyFrom.Name, copyFrom.value)
        {
            if (copyFrom.statuses != null)
                statuses.AddRange(copyFrom.statuses);
        }

        public FieldStatus(string name, object value = null) :
            base(name)
        {
            this.value = value;
        }

        object value;

        /// <summary>
        /// Original value presented to engine at beginning of processing/validation/conversion chain
        /// </summary>
        public object Value
        {
            get => value;
            internal set => this.value = value;
        }




#if DEBUG
        // For unit testing only
        internal ICollection<Status> InternalStatuses => statuses;
#endif

        // DEBT: Make this into an IEnumerable so that aggregator has an easier time of it
        readonly List<Status> statuses = new List<Status>();

        List<IEnumerable<Status>> externalStatuses2 = new List<IEnumerable<Status>>();

        public void Add(IEnumerable<Status> external)
        {
            externalStatuses2.Add(external);
        }

        public IEnumerable<Status> Statuses
        {
            get
            {
                foreach (Status status in statuses)
                    yield return status;

                foreach (Status status in externalStatuses2.SelectMany(x => x))
                    yield return status;
            }
        }

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
        //public FieldStatus() : base() { }

        public FieldStatus(string name, T value) :
            base(name, value)
        { }

        public new T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
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

        //public ExtendedFieldStatus() { }
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

        public ExtendedFieldStatus(string prefix, string name, string description, object value, Status.Code level = Status.Code.Error) :
            base(name, value)
        {
            Prefix = prefix;
            this.Add(level, description);
        }

        public ExtendedFieldStatus(string prefix, string parameter, string description, object value, int index, Status.Code level) :
            base(parameter, value)
        {
            Prefix = prefix;
            Index = index;
            this.Add(level, description);
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
