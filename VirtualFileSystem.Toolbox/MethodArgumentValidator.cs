using System;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Toolbox
{
    /// <summary>
    /// Provides a shortcut for logic useful for checking whether the values arguments/parameters
    /// of methods are appropriate. (Note: in the case of non-stack-trace-logging application crash due to unhandled argument exception thrown by <see cref="MethodArgumentValidator"/>, will point you right here, which is no good)
    /// </summary>
    internal static class MethodArgumentValidator
    {
        /// <summary>
        /// Checks a method argument for null, throws an ArgumentNullException if the argument is null.
        /// </summary>
        /// <param name="argument">Argument's value to check for null.</param>
        /// <param name="argumentName">Name of the argument that's being checked.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ThrowIfNull(object argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Checks a method argument of type System.String for null or emptiness,
        /// throws an ArgumentNullException if the string argument is null or empty.
        /// </summary>
        /// <param name="argument">Argument's value to check for null and emptiness.</param>
        /// <param name="argumentName">Name of the argument that's being checked.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ThrowIfStringIsNullOrEmpty(string argument, string argumentName)
        {
            if (String.IsNullOrEmpty(argument))
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="argument"></param>
        /// <param name="argumentName"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void ThrowIfIsDefault<T>(T argument, string argumentName)
        {
            if (default(T).Equals(argument))
            {
                throw new ArgumentException("Требуется значение, отличное от значения по умолчанию.", argumentName);
            }
        }

        public static void ThrowIfStringIsTooLong(string argument, int maximumStringLength, string argumentName)
        {
            MethodArgumentValidator.ThrowIfNegative(maximumStringLength, "maximumStringLength");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(argument, argumentName);

            if (argument.Length > maximumStringLength)
            {
                throw new ArgumentException("Принимаются только строки длиной не больше следующего числа символов: {0}".FormatWith(maximumStringLength), argumentName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="argumentName"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ThrowIfNegative(int argument, string argumentName)
        {
            if (argument < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, "Требуется положительное число.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="argumentName"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void ThrowIfDateIsNonUtc(DateTime argument, string argumentName)
        {
            if (argument.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Требуется дата в UTC.", argumentName);
            }
        }
    }
}