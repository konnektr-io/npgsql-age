using System.Globalization;
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
}
