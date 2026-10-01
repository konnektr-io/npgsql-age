using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Npgsql.Age.Internal
{
    internal static class CypherHelpers
    {
        // Matches the RETURN clause and captures everything up to the next clause keyword
        // (RETURN/LIMIT/SKIP/ORDER) or the end of the query.
        //
        // The terminator must be BOTH preceded by whitespace and a whole word. Without those
        // two constraints a property whose name merely *starts* with a clause keyword truncated
        // the capture: "RETURN t.returnPeriod AS period" stopped at the "return" inside
        // "returnPeriod", leaving the capture as "t." - the column name was then sanitized to an
        // empty string and the generated SQL became "as ( agtype)", which PostgreSQL rejects with
        // 42601 "syntax error at or near )". See pg-age-digitaltwins#115.
        private const string ReturnClausePattern =
            @"RETURN\s+(.+?)(?=\s+(?:RETURN|LIMIT|SKIP|ORDER)\b|\s*$)";

        internal static string GenerateAsPart(string cypher)
        {
            // Pre-process the query to temporarily replace string literals with placeholders
            // This prevents matching RETURN statements inside string literals
            var processedCypher = ReplaceStringLiterals(
                cypher.Replace("\n", " ").Replace("\r", " ")
            );

            // Extract the return part of the Cypher query using the processed version
            MatchCollection matches = Regex.Matches(
                processedCypher,
                ReturnClausePattern,
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

            // Now get the original return part from the unprocessed cypher for actual processing
            var originalCypher = cypher.Replace("\n", " ").Replace("\r", " ");
            var originalMatches = Regex.Matches(
                originalCypher,
                ReturnClausePattern,
                RegexOptions.IgnoreCase
            );

            // Use the same index from the original matches
            var originalMatch = originalMatches[^1];

            // Extract the return values from the original (unprocessed) query
            var returnValues = SplitReturnValues(originalMatch.Groups[1].Value);

            // Dictionary to track occurrences of column names
            var columnNames = new Dictionary<string, int>();

            // Generate the 'as (...)' part
            var asPart = string.Join(
                ", ",
                returnValues.Select(
                    (value, index) =>
                    {
                        var trimmedValue = value.Trim();

                        // Map/array values - a bare literal ("{...}", "[...]") or a projection
                        // applied to an expression ("t { .prop }", "m { .* }") - resolve to the
                        // alias when given, otherwise to a generic "result" column. Without this a
                        // projection fell through to the generic name derivation and produced
                        // meaningless names like "returnPeriod__" from "t { .returnPeriod }".
                        var isProjection = Regex.IsMatch(
                            trimmedValue,
                            @"\w+\s*\{\s*\.",
                            RegexOptions.IgnoreCase
                        );

                        // Handle objects and arrays without aliases
                        if (
                            isProjection
                            || trimmedValue.StartsWith("{")
                            || trimmedValue.StartsWith("[")
                        )
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

                        // Defensive: a blank name would render as "as ( agtype)" / "as ()" -
                        // always invalid SQL (42601). Falling back to a positional name keeps the
                        // query executable instead of failing with a bare syntax error.
                        if (string.IsNullOrWhiteSpace(sanitizedValue))
                        {
                            sanitizedValue = string.IsNullOrWhiteSpace(trimmedValue)
                                ? $"column{index + 1}"
                                : "result";
                        }

                        return $"{sanitizedValue} agtype";
                    }
                )
            );
            return $"({asPart})";
        }

        // Helper method to replace string literals with placeholders to avoid matching content inside strings
        private static string ReplaceStringLiterals(string cypher)
        {
            // Replace single-quoted strings
            cypher = Regex.Replace(cypher, @"'(?:[^'\\]|\\.)*'", "PLACEHOLDER_STRING");
            // Replace double-quoted strings
            cypher = Regex.Replace(cypher, @"""(?:[^""\\]|\\.)*""", "PLACEHOLDER_STRING");
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
