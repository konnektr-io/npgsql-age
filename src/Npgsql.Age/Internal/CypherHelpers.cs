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
            var match = Regex.Match(cypher, @"RETURN\s+(.*?)(?:\s+LIMIT|\s+SKIP|\s+ORDER|[\[{]|$)", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return "(result agtype)";
            }

            // Split the return values
            var returnValues = match.Groups[1].Value.Split(',');

            // Dictionary to track occurrences of column names
            var columnNames = new Dictionary<string, int>();

            // Generate the 'as (...)' part
            var asPart = string.Join(", ", returnValues.Select((value, index) =>
            {
                var trimmedValue = value.Trim().TrimStart('$');
                // Detect numbers and replace them with 'num'
                if (int.TryParse(trimmedValue, out _) || double.TryParse(trimmedValue, out _))
                {
                    trimmedValue = $"num";
                }
                // Detect function calls (like count(*)) and use the function name as the column name
                if (Regex.IsMatch(trimmedValue, @"\w+\(.*\)"))
                {
                    var exprName = Regex.Match(trimmedValue, @"\w+").Value; // TODO: use index or something when there are multiple of the same $"{exprName}{index} agtype";
                    trimmedValue = exprName;
                }
                // Use the last part for property accessors (with dots)
                if (trimmedValue.Contains('.'))
                {
                    trimmedValue = trimmedValue.Split('.').Last();
                }
                // Trim backticks
                trimmedValue = trimmedValue.Trim('`');
                // Handle aliases
                var aliasPattern = @"\s+AS\s+";
                if (Regex.IsMatch(trimmedValue, aliasPattern, RegexOptions.IgnoreCase))
                {
                    trimmedValue = Regex.Split(trimmedValue, aliasPattern, RegexOptions.IgnoreCase).Last();
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
                // Quote column names if they contain uppercase letters
                if (sanitizedValue.Any(char.IsUpper))
                {
                    sanitizedValue = $"\"{sanitizedValue}\"";
                }

                return $"{sanitizedValue} agtype";
            }));
            return $"({asPart})";
        }
    }
}