using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualFileSystem.Toolbox.Extensions
{
    internal static class EnumerableExtensions
    {
        public static bool ContainsLetters(this IEnumerable<char> characters)
        {
            MethodArgumentValidator.ThrowIfNull(characters, "characters");

            foreach (char character in characters)
            {
                if (Char.IsLetter(character))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            MethodArgumentValidator.ThrowIfNull(enumerable, "enumerable");

            return !enumerable.Any();
        }
    }
}