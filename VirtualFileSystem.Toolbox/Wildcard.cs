using System;
using System.Text.RegularExpressions;

namespace VirtualFileSystem.Toolbox
{
    internal class Wildcard
    {
        private readonly Regex _regex;
        private readonly string _pattern;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Wildcard(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentNullException("pattern");
            }

            _regex = new Regex(WildcardToRegex(pattern));
            _pattern = pattern;
        }

        private static string WildcardToRegex(string pattern)
        {
            return Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", "."); // Note: когда-то я делал что-то вроде этого, идею помню, но эту конкретную строку я выгуглил.
        }

        public string Pattern
        {
            get { return _pattern; }
        }

        /// <summary>
        /// Проверяет, соответствует ли заданная строка (<paramref name="stringToMatch"/>) маске.
        /// </summary>
        /// <param name="stringToMatch">Строка, которую надо проверить на соответствие маске.</param>
        /// <returns>True, если <paramref name="stringToMatch"/> удовлетворяет маске. False - в противном случае.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool CheckStringForMatch(string stringToMatch)
        {
            if (stringToMatch == null) throw new ArgumentNullException("stringToMatch");

            Match match = _regex.Match(stringToMatch);

            return match.Value.Equals(stringToMatch);
        }
    }
}