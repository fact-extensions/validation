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
    /// <summary>
    /// Assertions across a bag of properties
    /// </summary>
    public class VAssert
    {
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
