using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Npgsql.Age.Internal
{
    internal static class CypherHelpers
    {
        internal static string GenerateAsPart(string cypher)
        {
            // Extract the return part of the Cypher query
            MatchCollection matches = Regex.Matches(
                cypher.Replace("\n", " ").Replace("\r", " "),
                @"RETURN\s+(.+?)(?=\s*(?:RETURN|LIMIT|SKIP|ORDER|$))",
                RegexOptions.IgnoreCase
            );

            if (matches.Count == 0)
            {
                return "(result agtype)";
            }

            // Take the last match that is at the end of the query
            var match = matches[^1];

            // There is no return statement, and the query has the 'return' word somewhere else
            if (
                Regex.IsMatch(
                    match.Groups[1].Value,
                    @"\b(CREATE|MATCH|SET|WITH|REMOVE|DELETE)\b",
                    RegexOptions.IgnoreCase
                )
            )
            {
                return "(result agtype)";
            }

            // Extract the return values while avoiding splitting inside {} or []
            var returnValues = SplitReturnValues(match.Groups[1].Value);

            // Dictionary to track occurrences of column names
            var columnNames = new Dictionary<string, int>();

            // Generate the 'as (...)' part
            var asPart = string.Join(
                ", ",
                returnValues.Select(
                    (value, index) =>
                    {
                        var trimmedValue = value.Trim();

                        // Handle objects and arrays without aliases
                        if (trimmedValue.StartsWith("{") || trimmedValue.StartsWith("["))
                        {
                            // Check for alias
                            var aliasMatch = Regex.Match(
                                trimmedValue,
                                @"AS\s+(\w+)",
                                RegexOptions.IgnoreCase
                            );
                            if (aliasMatch.Success)
                            {
                                return $"{aliasMatch.Groups[1].Value} agtype";
                            }
                            return "result agtype";
                        }

                        // Detect numbers and replace them with 'num'
                        if (
                            int.TryParse(trimmedValue, out _)
                            || double.TryParse(trimmedValue, out _)
                        )
                        {
                            trimmedValue = $"num";
                        }

                        // Handle aliases first (before function detection to preserve alias information)
                        var aliasPattern = @"\s+AS\s+";
                        if (Regex.IsMatch(trimmedValue, aliasPattern, RegexOptions.IgnoreCase))
                        {
                            trimmedValue = Regex
                                .Split(trimmedValue, aliasPattern, RegexOptions.IgnoreCase)
                                .Last();
                        }
                        else
                        {
                            // Detect function calls (like count(*)) and use the function name as the column name
                            if (Regex.IsMatch(trimmedValue, @"\w+\(.*\)"))
                            {
                                var exprName = Regex.Match(trimmedValue, @"\w+").Value;
                                trimmedValue = exprName;
                            }
                            // Use the last part for property accessors (with dots)
                            else if (trimmedValue.Contains('.'))
                            {
                                trimmedValue = trimmedValue.Split('.').Last();
                            }
                            // Handle square bracket property accessors (like n[0] or n['name'])
                            else if (trimmedValue.Contains('['))
                            {
                                var match = Regex.Match(trimmedValue, @"\['(.*?)'\]");
                                if (match.Success)
                                {
                                    trimmedValue = match.Groups[1].Value;
                                }
                            }

                            // Trim backticks
                            trimmedValue = trimmedValue.Trim('`');
                        }

                        // Replace special characters with underscores
                        var sanitizedValue = Regex.Replace(trimmedValue, @"[^\w]", "_");

                        // Handle duplicate column names
                        if (columnNames.ContainsKey(sanitizedValue))
                        {
                            columnNames[sanitizedValue]++;
                            sanitizedValue += columnNames[sanitizedValue].ToString();
                        }
                        else
                        {
                            columnNames[sanitizedValue] = 0;
                        }

                        // Quote column names if they contain uppercase letters, special characters, or start with a dollar sign
                        if (value.Contains('['))
                        {
                            sanitizedValue = $"\"{trimmedValue}\"";
                        }
                        else if (sanitizedValue.Any(char.IsUpper) || sanitizedValue.StartsWith("$"))
                        {
                            sanitizedValue = $"\"{sanitizedValue}\"";
                        }

                        return $"{sanitizedValue} agtype";
                    }
                )
            );
            return $"({asPart})";
        }

        internal static string EscapeCypher(string cypher)
        {
            // Escape backslashes
            cypher = Regex.Replace(cypher, @"\\(?!')", "\\\\");

            return cypher;
        }

        // Helper method to split return values while avoiding splitting inside {} or [] or ()
        private static List<string> SplitReturnValues(string returnPart)
        {
            var result = new List<string>();
            var current = new List<char>();
            int braceCount = 0,
                bracketCount = 0,
                parenthesesCount = 0;

            foreach (var c in returnPart)
            {
                if (c == ',' && braceCount == 0 && bracketCount == 0 && parenthesesCount == 0)
                {
                    result.Add(new string(current.ToArray()).Trim());
                    current.Clear();
                }
                else
                {
                    if (c == '{')
                        braceCount++;
                    if (c == '}')
                        braceCount--;
                    if (c == '[')
                        bracketCount++;
                    if (c == ']')
                        bracketCount--;
                    if (c == '(')
                        parenthesesCount++;
                    if (c == ')')
                        parenthesesCount--;
                    current.Add(c);
                }
            }

            if (current.Count > 0)
            {
                result.Add(new string(current.ToArray()).Trim());
            }

            return result;
        }
    }
}
