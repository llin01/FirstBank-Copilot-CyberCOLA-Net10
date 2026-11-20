namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Exception;

    #endregion

    /// <summary>
    /// Provides utility methods for object validation, null checking, and comparison operations.
    /// </summary>
    public static class ObjectUtils
    {
        /// <summary>
        ///     Throw <code>NullReferenceException</code> when <code>obj</code> is null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T CheckNotNull<T>(T obj)
        {
            if (obj == null)
            {
                throw new NullReferenceException();
            }

            return obj;
        }

        /// <summary>
        ///     return <code>defaultValue</code> if <code>obj</code> is null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T CheckNotNull<T>(T obj, T defaultValue)
        {
            return obj == null ? defaultValue : obj;
        }

        /// <summary>
        /// Validates that a byte array or string has the specified length.
        /// </summary>
        /// <param name="value">The value to check (byte[] or string).</param>
        /// <param name="length">The expected length.</param>
        /// <param name="exceptMessage">Optional exception message if validation fails.</param>
        /// <exception cref="InvalidValueException">Thrown when the value is null or length does not match.</exception>
        public static void CheckLength(object value, int length, string exceptMessage = "")
        {
            if (value == null)
            {
                throw new InvalidValueException("value is null");
            }

            if (value is byte[])
            {
                CheckEqual(((byte[])value).Length, length, exceptMessage);
            }
            else if (value is string)
            {
                CheckEqual(((string)value).Length, length, exceptMessage);
            }
        }

        /// <summary>
        /// Validates that the expected value equals the actual value.
        /// </summary>
        /// <param name="expect">The expected value.</param>
        /// <param name="value">The actual value.</param>
        /// <param name="exceptMessage">Exception message if validation fails.</param>
        /// <exception cref="InvalidValueException">Thrown when values are not equal.</exception>
        public static void CheckEqual(object expect, object value, string exceptMessage)
        {
            if (!expect.Equals(value))
            {
                throw new InvalidValueException(exceptMessage);
            }
        }

        /// <summary>
        /// Checks if a value is null.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is null; otherwise, false.</returns>
        public static bool IsNull(object value)
        {
            return value == null;
        }

        /// <summary>
        ///     If <code>self</code> is null, return <code>defaultValue</code>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Default<T>(this object self, T defaultValue)
        {
            return (T)(self ?? defaultValue);
        }

        /// <summary>
        /// Compares two byte arrays for equality.
        /// </summary>
        /// <param name="self">The first byte array.</param>
        /// <param name="value">The second byte array.</param>
        /// <returns>True if both arrays have the same length and content; otherwise, false.</returns>
        public static bool Equal(this byte[] self, byte[] value)
        {
            if (!self.Length.Equals(value.Length))
            {
                return false;
            }

            return !self.Where((t, i) => !t.Equals(value[i])).Any();
        }
    }
}
