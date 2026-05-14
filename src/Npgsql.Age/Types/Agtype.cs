using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Npgsql.Age.Internal.JsonConverters;

namespace Npgsql.Age.Types
{
    /// <summary>
    /// Represent the <c>ag_catalog.agtype</c> PostgreSQL type.
    /// </summary>
    public readonly struct Agtype
    {
        private readonly string _value;

        /// <summary>
        /// Initialises a new instance of <see cref="Agtype"/>.
        /// </summary>
        /// <param name="value"></param>
        public Agtype(string value)
        {
            _value = value.Trim('\u0001');
        }

        #region Public methods
        /// <summary>
        /// Return the agtype value as a string.
        /// </summary>
        /// <returns>
        /// String value.
        /// </returns>
        public string GetString() => _value.Trim('"');

        /// <summary>
        /// Return the agtype value as a boolean.
        /// </summary>
        /// <returns>
        /// Boolean value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public bool GetBoolean() => bool.Parse(_value);

        /// <summary>
        /// Return the agtype value as a float.
        /// </summary>
        /// <returns>
        /// Float value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public float GetFloat()
        {
            if (_value.Equals("-Infinity", StringComparison.OrdinalIgnoreCase))
                return float.NegativeInfinity;
            if (_value.Equals("Infinity", StringComparison.OrdinalIgnoreCase))
                return float.PositiveInfinity;
            if (_value.Equals("NaN", StringComparison.OrdinalIgnoreCase))
                return float.NaN;

            return float.Parse(_value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Return the agtype value as a double.
        /// </summary>
        /// <returns>
        /// Double value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public double GetDouble()
        {
            if (_value.Equals("-Infinity", StringComparison.OrdinalIgnoreCase))
                return double.NegativeInfinity;
            if (_value.Equals("Infinity", StringComparison.OrdinalIgnoreCase))
                return double.PositiveInfinity;
            if (_value.Equals("NaN", StringComparison.OrdinalIgnoreCase))
                return double.NaN;

            return double.Parse(_value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Return the agtype value as a byte.
        /// </summary>
        /// <returns>
        /// Byte value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public byte GetByte() => byte.Parse(_value);

        /// <summary>
        /// Return the agtype value as an sbyte.
        /// </summary>
        /// <returns>
        /// SByte value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public sbyte GetSByte() => sbyte.Parse(_value);

        /// <summary>
        /// Return the agtype value as a short.
        /// </summary>
        /// <returns>
        /// Short value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public short GetInt16() => short.Parse(_value);

        /// <summary>
        /// Return the agtype value as a ushort.
        /// </summary>
        /// <returns>
        /// UShort value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public ushort GetUInt16() => ushort.Parse(_value);

        /// <summary>
        /// Return the agtype value as an integer.
        /// </summary>
        /// <returns>
        /// Integer value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public int GetInt32() => int.Parse(_value);

        /// <summary>
        /// Return the agtype value as a uint.
        /// </summary>
        /// <returns>
        /// UInt value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public uint GetUInt32() => uint.Parse(_value);

        /// <summary>
        /// Return the agtype value as a long.
        /// </summary>
        /// <returns>
        /// Long value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public long GetInt64() => long.Parse(_value);

        /// <summary>
        /// Return the agtype value as a ulong.
        /// </summary>
        /// <returns>
        /// ULong value.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the value of the agtype cannot be correctly parsed.
        /// </exception>
        public ulong GetUInt64() => ulong.Parse(_value);

        /// <summary>
        /// Return the agtype value as a decimal.
        /// </summary>
        /// <returns>
        /// Decimal value.
        /// </returns>
        public decimal GetDecimal() => decimal.Parse(_value);

        /// <summary>
        /// Return the agtype value as a list.
        /// </summary>
        ///
        /// <remarks>
        /// The list may contain mixed data types.
        /// Example: [1, 2, "string", null].
        /// </remarks>
        ///
        /// <param name="readFloatingPointLiterals">
        /// Indicates if the reserved floating values "-Infinity", "Infinity",
        /// and "NaN" should be parsed to <see cref="double.NegativeInfinity"/>,
        /// <see cref="double.PositiveInfinity"/>, and <see cref="double.NaN"/>
        /// respectively.
        /// <para>
        /// If <see langword="false"/>, the reserved floating values are parsed as
        /// strings.
        /// </para>
        /// </param>
        ///
        /// <returns>
        /// List of objects.
        /// </returns>
        public List<object?> GetList(bool readFloatingPointLiterals = true)
        {
            var result = JsonSerializer.Deserialize<List<object?>>(
                _value,
                SerializerOptions.Default
            );

            return result!;
        }

        /// <summary>
        /// Return true if the agtype is a vertex.
        /// </summary>
        /// <returns>
        /// Boolean value.
        /// </returns>
        public bool IsVertex => _value.EndsWith(Vertex.FOOTER);

        /// <summary>
        /// Return the agtype value as a <see cref="Vertex"/>.
        /// </summary>
        /// <returns>
        /// Vertex.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the agtype cannot be converted to a vertex.
        /// </exception>
        public Vertex GetVertex()
        {
            bool isValidVertex = _value.EndsWith(Vertex.FOOTER);
            if (!isValidVertex)
                throw new FormatException(
                    "Cannot convert agtype to vertex. Agtype is not a valid vertex."
                );

            var json = _value.Replace(Vertex.FOOTER, "");
            var vertex = JsonSerializer.Deserialize<Vertex>(json, SerializerOptions.Default);

            return vertex!;
        }

        /// <summary>
        /// Return true if the agtype is an edge.
        /// </summary>
        /// <returns>
        /// Boolean value.
        /// </returns>
        public bool IsEdge => _value.EndsWith(Edge.FOOTER);

        /// <summary>
        /// Return the agtype value as a <see cref="Edge"/>.
        /// </summary>
        /// <returns>
        /// Edge.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the agtype cannot be converted to an edge.
        /// </exception>
        public Edge GetEdge()
        {
            bool isValidEdge = _value.EndsWith(Edge.FOOTER);
            if (!isValidEdge)
                throw new FormatException(
                    "Cannot convert agtype to edge. Agtype is not a valid edge."
                );

            var json = _value.Replace(Edge.FOOTER, "");
            var edge = JsonSerializer.Deserialize<Edge>(json, SerializerOptions.Default);

            return edge!;
        }

        /// <summary>
        /// Return true if the agtype is an edge.
        /// </summary>
        /// <returns>
        /// Boolean value.
        /// </returns>
        public bool IsPath => _value.EndsWith(Path.FOOTER);

        /// <summary>
        /// Return the agtype value as a path containing vertices and edges.
        /// </summary>
        /// <returns>
        /// A <see cref="Path"/>.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the agtype cannot be converted to a path.
        /// </exception>
        public Path GetPath()
        {
            bool isValidEdge = _value.EndsWith(Path.FOOTER);
            if (!isValidEdge)
                throw new FormatException(
                    "Cannot convert agtype to path. Agtype is not a valid path."
                );

            try
            {
                var json = _value
                    .Replace(Vertex.FOOTER, "")
                    .Replace(Path.FOOTER, "")
                    .Replace(Edge.FOOTER, "");
                var path = JsonSerializer.Deserialize<object[]>(
                    json,
                    SerializerOptions.PathSerializer
                );

                return path is null ? throw new Exception("Path cannot be null.") : new Path(path);
            }
            catch (JsonException e)
            {
                throw new FormatException(
                    "Path may be in the wrong format and cannot be parsed correctly.",
                    e
                );
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the agtype represents a null value.
        /// </summary>
        public bool IsNull => _value == "null";

        /// <summary>
        /// Returns <see langword="true"/> if the agtype is an array.
        /// </summary>
        /// <remarks>
        /// Paths (which also start with <c>[</c>) are not considered arrays because their
        /// string representation ends with the <c>::path</c> footer rather than <c>]</c>.
        /// </remarks>
        public bool IsArray => _value.StartsWith('[') && _value.EndsWith(']');

        /// <summary>
        /// Returns <see langword="true"/> if the agtype is a plain JSON object (map).
        /// </summary>
        public bool IsMap => _value.StartsWith('{') && _value.EndsWith('}') && !IsVertex && !IsEdge;

        /// <summary>
        /// Returns the elements of the agtype array as individual <see cref="Agtype"/> values,
        /// preserving type annotations so that <see cref="IsVertex"/>, <see cref="IsEdge"/>,
        /// and other type-check properties work correctly on each element.
        /// </summary>
        /// <exception cref="FormatException">
        /// Thrown when the agtype is not an array.
        /// </exception>
        public IEnumerable<Agtype> GetArray()
        {
            if (!IsArray)
                throw new FormatException(
                    "Cannot convert agtype to array. Agtype is not a valid array."
                );

            // Walk the raw string tracking JSON nesting depth and string literals,
            // splitting on top-level commas. This preserves ::vertex / ::edge suffixes
            // on each element so the returned Agtype values keep their type information.
            int depth = 0;
            bool inString = false;
            int start = 1; // skip opening '['
            int end = _value.Length - 1; // position of closing ']'

            for (int i = start; i < end; i++)
            {
                char c = _value[i];
                if (inString)
                {
                    if (c == '\\')
                        i++; // skip escaped character
                    else if (c == '"')
                        inString = false;
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                            inString = true;
                            break;
                        case '{':
                        case '[':
                            depth++;
                            break;
                        case '}':
                        case ']':
                            depth--;
                            break;
                        case ',' when depth == 0:
                            var item = _value.Substring(start, i - start).Trim();
                            if (item.Length > 0)
                                yield return new Agtype(item);
                            start = i + 1;
                            break;
                    }
                }
            }

            // Yield the last (or only) item
            if (end > start)
            {
                var lastItem = _value.Substring(start, end - start).Trim();
                if (lastItem.Length > 0)
                    yield return new Agtype(lastItem);
            }
        }

        /// <summary>
        /// Returns the agtype map as a <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <exception cref="FormatException">
        /// Thrown when the agtype is not a map.
        /// </exception>
        public Dictionary<string, object?> GetMap()
        {
            if (!IsMap)
                throw new FormatException(
                    "Cannot convert agtype to map. Agtype is not a valid map."
                );

            return JsonSerializer.Deserialize<Dictionary<string, object?>>(
                    _value,
                    SerializerOptions.Default
                ) ?? throw new FormatException("Cannot convert agtype to map.");
        }
        #endregion

        #region Explicit operators
        public static explicit operator byte(Agtype agtype) => agtype.GetByte();

        public static explicit operator sbyte(Agtype agtype) => agtype.GetSByte();

        public static explicit operator short(Agtype agtype) => agtype.GetInt16();

        public static explicit operator ushort(Agtype agtype) => agtype.GetUInt16();

        public static explicit operator int(Agtype agtype) => agtype.GetInt32();

        public static explicit operator uint(Agtype agtype) => agtype.GetUInt32();

        public static explicit operator long(Agtype agtype) => agtype.GetInt64();

        public static explicit operator ulong(Agtype agtype) => agtype.GetUInt64();

        public static explicit operator decimal(Agtype agtype) => agtype.GetDecimal();

        public static explicit operator float(Agtype agtype) => agtype.GetFloat();

        public static explicit operator double(Agtype agtype) => agtype.GetDouble();

        public static explicit operator string(Agtype agtype) => agtype.GetString();

        public static explicit operator List<object?>(Agtype agtype) => agtype.GetList();

        public static explicit operator Vertex(Agtype agtype) => agtype.GetVertex();

        public static explicit operator Edge(Agtype agtype) => agtype.GetEdge();
        #endregion
    }
}
