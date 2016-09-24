using System;
using System.Text.RegularExpressions;

namespace RestGen
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentNullException(nameof(str));
            return char.ToLower(str[0]) + str.Substring(1);
        }

        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentNullException(nameof(str));
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static string ToIdentifier(this string str)
        {
            return IdentifierPattern.Replace(str ?? string.Empty,
                m => m.Value[m.Length - 1].ToString().ToUpper());
        }

        private static readonly Regex IdentifierPattern = new Regex(@"[^\w]+\w");
    }
}