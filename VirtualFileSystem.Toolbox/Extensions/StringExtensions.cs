using System;
using System.Collections.Generic;
using System.Globalization;

namespace VirtualFileSystem.Toolbox.Extensions
{
    internal static class StringExtensions
    {
        public static string FormatWith(this string formatString, params object[] formattingArguments)
        {
            return String.Format(CultureInfo.CurrentCulture, formatString, formattingArguments);
        }

        /// <summary>
        /// Note: case sensitive
        /// </summary>
        /// <param name="stringToCheck"></param>
        /// <param name="characters"></param>
        /// <returns></returns>
        public static bool ContainsAny(this string stringToCheck, IEnumerable<char> characters)
        {
            Nullable<char> charFound = null;
            
            return ContainsAny(stringToCheck, characters, out charFound);
        }

        /// <summary>
        /// Note: case sensitive
        /// </summary>
        /// <param name="stringToCheck"></param>
        /// <param name="characters"></param>
        /// <param name="characterFound"></param>
        /// <returns></returns>
        public static bool ContainsAny(this string stringToCheck, IEnumerable<char> characters, out Nullable<char> characterFound)
        {
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(stringToCheck, "stringToCheck");
            MethodArgumentValidator.ThrowIfNull(characters, "characters");

            var charactersSeenSoFar = new HashSet<char>();
            var charactersToCheckFor = new HashSet<char>(characters);

            foreach (char character in stringToCheck)
            {
                if (!charactersSeenSoFar.Contains(character) && charactersToCheckFor.Contains(character))
                {
                    characterFound = character;
                    return true;
                }

                charactersSeenSoFar.Add(character);
            }

            characterFound = null;
            return false;
        }
    }
}