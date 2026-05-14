using System.Globalization;
using System.Linq;
using Npgsql.Age.Types;
using Xunit;

namespace Npgsql.AgeTests;

public class AgTypeTests
{
    #region Constructor

    [Fact]
    public void Constructor_ThrowException_When_AgtypeValueIsNull()
    {
        Assert.Throws<NullReferenceException>(() => new Agtype(null!));
    }

    #endregion

    #region GetBoolean()

    [Fact]
    public void GetBoolean_Should_ReturnTrue_For_EquivalentTrueValues()
    {
        var agtype = new Agtype("true");
        var agtype2 = new Agtype("True");
        var agtype3 = new Agtype("TRUE");

        Assert.True(agtype.GetBoolean());
        Assert.True(agtype2.GetBoolean());
        Assert.True(agtype3.GetBoolean());
    }

    [Fact]
    public void GetBoolean_Should_ReturnFalse_For_EquivalentFalseValues()
    {
        var agtype = new Agtype("false");
        var agtype2 = new Agtype("False");
        var agtype3 = new Agtype("FALSE");

        Assert.False(agtype.GetBoolean());
        Assert.False(agtype2.GetBoolean());
        Assert.False(agtype3.GetBoolean());
    }

    [Fact]
    public void GetBoolean_Should_ThrowException_When_AgtypeValueIsInTheWrongFormat()
    {
        var agtype = new Agtype("23");

        Assert.Throws<FormatException>(() => agtype.GetBoolean());
    }

    #endregion

    #region GetFloat()

    [Fact]
    public void GetFloat_Should_ReturnEquivalentDouble_For_Pi()
    {
        var numString = "3.14";
        var agtype = new Agtype(numString);
        var floatEquivalent = float.Parse(numString, CultureInfo.InvariantCulture);

        Assert.Equal(floatEquivalent, agtype.GetFloat());
    }

    #endregion

    #region GetDouble()

    [Fact]
    public void GetDouble_Should_ReturnEquivalentDouble()
    {
        var numString = "1.0023e3";
        var agtype = new Agtype(numString);
        var doubleEquivalent = double.Parse(numString, CultureInfo.InvariantCulture);

        Assert.Equal(doubleEquivalent, agtype.GetDouble());
    }

    [Fact]
    public void GetDouble_Should_ReturnEquivalentDouble_For_Pi()
    {
        var numString = "3.14";
        var agtype = new Agtype(numString);
        var doubleEquivalent = double.Parse(numString, CultureInfo.InvariantCulture);

        Assert.Equal(doubleEquivalent, agtype.GetDouble());
    }

    [Fact]
    public void GetDouble_Should_ReturnDoubleEquivalent_For_NegativeInfinity()
    {
        var agtype = new Agtype("-Infinity");

        Assert.Equal(double.NegativeInfinity, agtype.GetDouble());
    }

    [Fact]
    public void GetDouble_Should_ReturnDoubleEquivalent_For_PositiveInfinity()
    {
        var agtype = new Agtype("Infinity");

        Assert.Equal(double.PositiveInfinity, agtype.GetDouble());
    }

    [Fact]
    public void GetDouble_Should_ReturnDoubleEquivalent_For_NaN()
    {
        var agtype = new Agtype("NaN");

        Assert.Equal(double.NaN, agtype.GetDouble());
    }

    [Fact]
    public void GetDouble_Should_ThrowException_When_AgtypeValueIsInTheWrongFormat()
    {
        var agtype = new Agtype("true");

        Assert.Throws<FormatException>(() => agtype.GetDouble());
    }

    #endregion

    #region GetInteger()

    [Fact]
    public void GetInteger_Should_ReturnEquivalentDouble()
    {
        var numString = "1";
        var agtype = new Agtype(numString);
        var doubleEquivalent = int.Parse(numString);

        Assert.Equal(doubleEquivalent, agtype.GetInt32());
    }

    [Fact]
    public void GetInteger_Should_ThrowException_When_AgtypeValueIsInTheWrongFormat()
    {
        var agtype = new Agtype("true");

        Assert.Throws<FormatException>(() => agtype.GetInt32());
    }

    #endregion

    #region GetLong()

    [Fact]
    public void GetLong_Should_ReturnEquivalentDouble()
    {
        var numString = "1";
        var agtype = new Agtype(numString);
        var doubleEquivalent = long.Parse(numString);

        Assert.Equal(doubleEquivalent, agtype.GetInt64());
    }

    [Fact]
    public void GetLong_Should_ThrowException_When_AgtypeValueIsInTheWrongFormat()
    {
        var agtype = new Agtype("true");

        Assert.Throws<FormatException>(() => agtype.GetInt64());
    }

    #endregion

    #region GetDecimal()

    [Fact]
    public void GetDecimal_Should_ReturnEquivalentDouble()
    {
        var numString = "1";
        var agtype = new Agtype(numString);
        var doubleEquivalent = decimal.Parse(numString);

        Assert.Equal(doubleEquivalent, agtype.GetDecimal());
    }

    [Fact]
    public void GetDecimal_Should_ThrowException_When_AgtypeValueIsInTheWrongFormat()
    {
        var agtype = new Agtype("true");

        Assert.Throws<FormatException>(() => agtype.GetDecimal());
    }

    #endregion

    #region GetList()

    [Fact]
    public void GetList_Should_ReturnEquivalentList()
    {
        var list = new List<object?> { 1, 2, "string", null };
        var agtype = new Agtype("[1, 2, \"string\", null]");

        var agtypeList = agtype.GetList();

        Assert.Equal(list.Count, agtypeList.Count);
        Assert.Equal(list, agtype.GetList());
    }

    [Fact]
    public void GetList_Should_ReturnEquivalentList_When_ItIsANestedList()
    {
        var list = new List<object?>
        {
            1,
            2,
            "string",
            null,
            new List<object?> { 1, 2, "string", null },
        };
        var agtype = new Agtype("[1, 2, \"string\", null, [1, 2, \"string\", null]]");

        var agtypeList = agtype.GetList();

        Assert.Equal(list.Count, agtypeList.Count);
        Assert.Equal(list, agtype.GetList());
    }

    [Fact]
    public void GetList_Should_ReturnNegativeInfinity_When_Supplied_NegativeInfinity()
    {
        var list = new List<object?> { 1, 2, double.NegativeInfinity };
        var agtype = new Agtype("[1, 2, \"-Infinity\"]");

        var agtypeList = agtype.GetList(true);

        Assert.Equal(list.Count, agtypeList.Count);
        Assert.Equal(list, agtypeList);
    }

    #endregion

    #region GetVertex()

    [Fact]
    public void GetVertex_Should_ReturnEquivalentVertex()
    {
        var vertex = new Vertex
        {
            Id = new(2343953235),
            Label = "Person",
            Properties = new() { { "name", "Emmanuel" }, { "age", 22 } },
        };
        var agtype = new Agtype(vertex.ToString());
        var generatedVertex = agtype.GetVertex();

        Assert.Equal(vertex.Id, generatedVertex.Id);
        Assert.Equal(vertex.Label, generatedVertex.Label);
        Assert.Equal(vertex.Properties, generatedVertex.Properties);
    }
    #endregion

    #region GetEdge()

    [Fact]
    public void GetEdge_Should_ReturnEquivalentEdge()
    {
        var edge = new Edge
        {
            Id = new(2),
            StartId = new(0),
            EndId = new(1),
            Label = "Edge_label",
            Properties = new() { { "colour", "red" } },
        };
        var agtype = new Agtype(edge.ToString());
        var generatedEdge = agtype.GetEdge();

        Assert.Equal(edge.Id, generatedEdge.Id);
        Assert.Equal(edge.Label, generatedEdge.Label);
        Assert.Equal(edge.StartId, generatedEdge.StartId);
        Assert.Equal(edge.EndId, generatedEdge.EndId);
        Assert.Equal(edge.Properties, generatedEdge.Properties);
    }
    #endregion

    #region GetPath()

    [Fact]
    public void GetPath_Should_ReturnEquivalentPath()
    {
        Vertex[] vertices =
        [
            new Vertex
            {
                Id = new(0),
                Label = "Label_name_1",
                Properties = new() { { "i", 0 } },
            },
            new Vertex
            {
                Id = new(2),
                Label = "Label_name_1",
                Properties = [],
            },
        ];
        var edge = new Edge
        {
            Id = new(2),
            StartId = vertices[0].Id,
            EndId = vertices[1].Id,
            Label = "Edge_label",
            Properties = [],
        };
        var agtype = new Agtype($"[{vertices[0]}, {edge}, {vertices[1]}]{Age.Types.Path.FOOTER}");
        var path = agtype.GetPath();

        Assert.Equal(1, path.Length);
        Assert.Equal(2, path.Vertices.Length);
        Assert.Single(path.Edges);
        Assert.Equal(vertices, path.Vertices);
        Assert.Equal(vertices[1].Properties, path.Vertices[1].Properties);
        Assert.Equal(edge, path.Edges[0]);
    }

    [Fact]
    public void GetPath_Should_ThrowException_When_AgtypeValueIsInWrongFormat()
    {
        Vertex[] vertices =
        [
            new Vertex
            {
                Id = new(0),
                Label = "Label_name_1",
                Properties = new() { { "i", 0 } },
            },
            new Vertex
            {
                Id = new(2),
                Label = "Label_name_1",
                Properties = [],
            },
        ];
        var edge = new Edge
        {
            Id = new(2),
            StartId = vertices[0].Id,
            EndId = vertices[1].Id,
            Label = "Edge_label",
            Properties = [],
        };
        // Omit the path footer.
        var agtype = new Agtype($"[{vertices[0]}, {edge}, {vertices[1]}]");

        Assert.Throws<FormatException>(() => agtype.GetPath());
    }

    #endregion

    #region IsArray

    [Fact]
    public void IsArray_Should_ReturnTrue_For_ValidArrays()
    {
        Assert.True(new Agtype("[1, 2, 3]").IsArray);
        Assert.True(new Agtype("[]").IsArray);
    }

    [Fact]
    public void IsArray_Should_ReturnTrue_For_VertexArray()
    {
        Assert.True(new Agtype("[{}::vertex]").IsArray);
    }

    [Fact]
    public void IsArray_Should_ReturnFalse_For_NonArrays()
    {
        Assert.False(new Agtype("{}").IsArray);
        Assert.False(new Agtype("{}::vertex").IsArray);
        Assert.False(new Agtype("[1, 2, 3]::path").IsArray);
    }

    #endregion

    #region IsNull

    [Fact]
    public void IsNull_Should_ReturnTrue_For_Null()
    {
        Assert.True(new Agtype("null").IsNull);
    }

    [Fact]
    public void IsNull_Should_ReturnFalse_For_NonNull()
    {
        Assert.False(new Agtype("1").IsNull);
        Assert.False(new Agtype("\"string\"").IsNull);
        Assert.False(new Agtype("true").IsNull);
        Assert.False(new Agtype("[]").IsNull);
        Assert.False(new Agtype("{}").IsNull);
    }

    #endregion

    #region IsMap

    [Fact]
    public void IsMap_Should_ReturnTrue_For_PlainObject()
    {
        Assert.True(new Agtype("{}").IsMap);
        Assert.True(new Agtype("{\"a\": 1}").IsMap);
    }

    [Fact]
    public void IsMap_Should_ReturnFalse_For_VertexEdgeAndNonObjects()
    {
        Assert.False(new Agtype("{}::vertex").IsMap);
        Assert.False(new Agtype("{}::edge").IsMap);
        Assert.False(new Agtype("[1, 2, 3]").IsMap);
        Assert.False(new Agtype("\"string\"").IsMap);
        Assert.False(new Agtype("null").IsMap);
    }

    #endregion

    #region GetArray()

    [Fact]
    public void GetArray_Should_ReturnTypedElements_For_Primitives()
    {
        var agtype = new Agtype("[1, \"hello\", true, null]");
        var elements = agtype.GetArray().ToList();
        Assert.Equal(4, elements.Count);
        Assert.Equal("1", elements[0].GetString());
        Assert.Equal("hello", elements[1].GetString());
        Assert.Equal("true", elements[2].GetString());
        Assert.True(elements[3].IsNull);
    }

    [Fact]
    public void GetArray_Elements_Should_PreserveVertexEdgeFlags()
    {
        var agtype = new Agtype("[{}::vertex, {}::edge]");
        var elements = agtype.GetArray().ToList();
        Assert.True(elements[0].IsVertex);
        Assert.True(elements[1].IsEdge);
    }

    [Fact]
    public void GetArray_OnVertexArray_Should_ReturnVerticesWithCorrectProperties()
    {
        var vertex = new Vertex
        {
            Id = new(2343953235),
            Label = "Person",
            Properties = new() { { "name", "Emmanuel" } },
        };
        var agtype = new Agtype($"[{vertex}]");
        var elements = agtype.GetArray().ToList();
        Assert.Single(elements);
        Assert.True(elements[0].IsVertex);
        var parsed = elements[0].GetVertex();
        Assert.Equal(vertex.Id, parsed.Id);
        Assert.Equal(vertex.Label, parsed.Label);
        Assert.Equal(vertex.Properties, parsed.Properties);
    }

    [Fact]
    public void GetArray_OnNestedArray_Should_SupportNestedGetArray()
    {
        var agtype = new Agtype("[[1, 2], [3, 4]]");
        var outer = agtype.GetArray().ToList();
        Assert.Equal(2, outer.Count);
        var inner = outer[0].GetArray().ToList();
        Assert.Equal(2, inner.Count);
        Assert.Equal("1", inner[0].GetString());
        Assert.Equal("2", inner[1].GetString());
    }

    [Fact]
    public void GetArray_OnEmptyArray_Should_ReturnEmpty()
    {
        var agtype = new Agtype("[]");
        Assert.Empty(agtype.GetArray());
    }

    [Fact]
    public void GetArray_OnNullContainingArray_Should_HaveIsNullTrue()
    {
        var agtype = new Agtype("[null, 1]");
        var elements = agtype.GetArray().ToList();
        Assert.True(elements[0].IsNull);
        Assert.False(elements[1].IsNull);
    }

    [Fact]
    public void GetArray_Should_ThrowFormatException_When_NotArray()
    {
        var agtype = new Agtype("{}");
        Assert.Throws<FormatException>(() => agtype.GetArray());
    }

    #endregion

    #region GetMap()

    [Fact]
    public void GetMap_Should_ReturnCorrectDictionary_For_PlainObject()
    {
        var agtype = new Agtype("{\"a\": 1, \"b\": \"hello\"}");
        var map = agtype.GetMap();
        Assert.Equal(2, map.Count);
        Assert.Equal(1, map["a"]);
        Assert.Equal("hello", map["b"]);
    }

    [Fact]
    public void GetMap_Should_ThrowFormatException_When_NotMap()
    {
        var agtype = new Agtype("[1, 2, 3]");
        Assert.Throws<FormatException>(() => agtype.GetMap());
    }

    #endregion
}
