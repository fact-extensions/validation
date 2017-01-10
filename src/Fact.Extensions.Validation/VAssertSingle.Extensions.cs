using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public static class VAssertSingle_Extensions
    {
        public static void IsTrue(this VAssertSingle<bool> assert)
        {
            if (!assert.Value) assert.Error("Value expected to be true");
        }

        public static void IsFalse(this VAssertSingle<bool> assert)
        {
            if (assert.Value) assert.Error("Value expected to be false");
        }


        public static bool IsEqual(this VAssertSingle<string> assert, string expected)
        {
            return assert.IsTrue(assert.Value.Equals(expected), "Strings must be equal");
        }


        public static VAssertSingle<int> Length(this VAssertSingle<string> assert)
        {
            return new VAssertSingle<int>(assert.Name + ".Length", assert.Value.Length);
        }


        public static bool IsUnderLength(this VAssertSingle<string> assert, int maximumLength)
        {
            return assert.IsTrue(assert.Value.Length < maximumLength, "String length cannot exceed " + maximumLength);
        }


        public static bool IsLessThan<T>(this VAssertSingle<T> assert, int maximum)
            where T : IComparable
        {
            // TODO: Haven't actually tested this yet
            return assert.IsTrue(assert.Value.CompareTo(maximum) < 0, "Must be less than " + maximum);
        }


        public static bool IsGreaterThan<T>(this VAssertSingle<T> assert, int maximum)
            where T : IComparable
        {
            // TODO: Haven't actually tested this yet
            return assert.IsTrue(assert.Value.CompareTo(maximum) > 0, "Must be greater than " + maximum);
        }


        public static bool IsNull<T>(this VAssertSingle<T> assert)
            where T : class
        {
            var result = assert.Value == null;

            if (!result) assert.Error("Cannot be null");

            return result;
        }

        public static bool IsNotNull<T>(this VAssertSingle<T> assert)
            where T : class
        {
            var result = assert.Value != null;

            if (!result) assert.Error("Cannot be null");

            return result;
        }
    }
}
