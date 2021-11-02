//using Fact.Extensions.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// TODO: Put this out into Fact.Extensions.Validation
// refinement of existing VAssert classes
// they are pretty awesome, but also a bit wily so redoing them
namespace Fact.Extensions.Validation
{
    using Experimental;


    public class ValidationException : Exception
    {
        public IEnumerable<IField> Fields { get; }

        public ValidationException(string message, IEnumerable<IField> fields) : 
            base(message)
        {
            Fields = fields;
        }

        public ValidationException() { }
    }


    public class AssertException : ValidationException
    {
        public AssertException(string message, IEnumerable<IField> fields) : 
            base(message, fields)
        {

        }


        public AssertException() { }
    }

    /// <summary>
    /// Assertions across a bag of properties
    /// </summary>
    public class VAssert
    {
        protected readonly IAggregatedBinder3 aggregatedBinder;

        public IAggregatedBinder3 AggregatedBinder => aggregatedBinder;

        public VAssert(IAggregatedBinder3 aggregatedBinder)
        {
            this.aggregatedBinder = aggregatedBinder;
        }

        public async Task AssertAsync()
        {
            await aggregatedBinder.Process();
            var statuses = aggregatedBinder.Fields().SelectMany(f => f.Statuses);
            if(statuses.Any())
            {
                var fields = aggregatedBinder.Fields().Where(f => f.Statuses.Any());
                throw new AssertException("", fields);
            }
            //aggregatedBinder.Bi
        }
    }


    public class VAssert<T> : VAssert
    {
        public Experimental.IEntityBinder<T> Binder { get; }

        public VAssert(T entity) :
            base(new AggregatedBinder3())
        {
            Binder = aggregatedBinder.BindInput2(entity);
        }
    }


    /// <summary>
    /// Assertions for one parameter only
    /// </summary>
    public class VAssertSingle
    {
        readonly string parameterName;
        readonly object parameterValue;

        // TODO: bring in proper ErrorItem directly from Apprentice, and call event using
        // that as a parameter
        // format for now is: parameter, description, offending value
        public event Action<string, string, object> ErrorFound;

        public VAssertSingle(string parameterName, object parameterValue)
        {
            this.parameterName = parameterName;
            this.parameterValue = parameterValue;
        }

        public string Name => parameterName;

        public object Value => parameterValue;

        internal void Error(string description)
        {
            ErrorFound?.Invoke(parameterName, description, parameterValue);
        }

        internal bool IsTrue(bool condition, string failureDescription)
        {
            if (!condition)
            {
                Error(failureDescription);
                return false;
            }
            return true;
        }
    }


    public class VAssertSingle<T> : VAssertSingle
    {
        new public T Value => (T)base.Value;

        /*
        public static explicit operator T(VAssertSingle<T> assert)
        {
            return assert.Value;
        }*/

        public static implicit operator T(VAssertSingle<T> assert)
        {
            return assert.Value;
        }

        public VAssertSingle(string parameterName, T parameterValue)
            : base(parameterName, parameterValue)
        {

        }
    }
}
