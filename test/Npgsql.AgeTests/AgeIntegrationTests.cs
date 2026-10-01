using Npgsql.Age;
using Npgsql.Age.Types;

namespace Npgsql.AgeTests;

public class AgeIntegrationTests : TestBase
{
    [Fact]
    public async Task OpenConnectionAsync_ExtensionExists()
    {
        // Check if the extension exists in the database.
        var command = DataSource.CreateCommand(
            "SELECT extname FROM pg_extension WHERE extname = 'age';"
        );
        var result = await command.ExecuteScalarAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GraphExistsAsync_Should_ReturnTrueIfGraphExists()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var graphExistsCommand = connection.GraphExistsCommand(graphName);
        var graphExists = await graphExistsCommand.ExecuteScalarAsync();
        Assert.True((bool)graphExists!);
    }

    [Fact]
    public async Task GraphExistsAsync_Should_ReturnFalseIfGraphNotExists()
    {
        var graphName = "sidjfa23knlsd9a8dfndfhjbnzxeunjakssdf3sdmvns_asdjfk";
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var graphExistsCommand = connection.GraphExistsCommand(graphName);
        var graphExists = await graphExistsCommand.ExecuteScalarAsync();
        Assert.False((bool)graphExists!);
    }

    [Fact]
    public async Task Value_Should_BeNull_When_AGEOutputsNull()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
    RETURN NULL
$$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.Null(agResult);

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task GetDouble_Should_ReturnPositiveInfinity_When_AGEOutputsInfinity()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
        RETURN 'Infinity'::float
    $$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.Equal(double.PositiveInfinity, agResult?.GetDouble());

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task GetDouble_Should_ReturnNaN_When_AGEOutputsNaN()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
            RETURN 'NaN'::float
        $$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.Equal(double.NaN, agResult?.GetDouble());

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task GetVertex_Should_ReturnCorrectVertex()
    {
        var graphname = await CreateTempGraphAsync();
        ulong id = 234323;
        var label = "Person";
        var i = 3;

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
            WITH {{id: {id}, label: ""{label}"", properties: {{i: {i}}}}}::vertex as v
            RETURN v
        $$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var vertex = agResult?.GetVertex();

        Assert.NotNull(vertex);
        Assert.Equal(id, vertex?.Id.Value);
        Assert.Equal(label, vertex?.Label);
        Assert.Equal(i, vertex?.Properties["i"]);

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task GetList_Should_CorrectlyParseNullValues()
    {
        var graphname = await CreateTempGraphAsync();
        var list = new List<object?> { 1, 2, 3, 2, null };

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
            WITH [1, 2, 3, 2, NULL] AS list
            RETURN list
        $$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.Equal(list, agResult?.GetList());

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_With_NoParameters_Should_ReturnDataReader()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = connection.CreateCypherCommand(graphName, "RETURN 1");
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_ReturnsExpectedResults()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = connection.CreateCypherCommand(graphName, "RETURN 1");
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);

        var schema = await dataReader.GetColumnSchemaAsync();

        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        Assert.NotNull(agResult);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteInvalidCypherQueryAsync_Should_ThrowException()
    {
        var graphName = await CreateTempGraphAsync();
        await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var connection = await DataSource.OpenConnectionAsync();
            await using var command = connection.CreateCypherCommand(graphName, "INVALID QUERY");
            await command.ExecuteReaderAsync();
        });
        await DropTempGraphAsync(graphName);
    }

    // It shouldn't work ... but it does because of a quirk in how AGE 1.5.0 parses string literals to agtype,
    // which was changed in later versions to be more strict and require the explicit 'cstring' cast for this to work.
    // The right way to do this is to use '{\"bignumber\":5e24}'::cstring::agtype
    [Fact(Skip = "Does not work with AGE 1.6.0 and above")]
    public async Task ExecuteCypherQueryAsync_WithStringToAgtypeMap_Should_Work_Age_1_5()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using var command = connection.CreateCypherCommand(
            graphName,
            "WITH '{\"bignumber\":5e24}'::agtype as obj RETURN obj.bignumber"
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        Assert.Equal(5e24, agResult?.GetDouble());

        await DropTempGraphAsync(graphName);
    }

    [Fact(Skip = "Does not work with AGE 1.5.0")]
    public async Task ExecuteCypherQueryAsync_WithStringToAgtypeMap_Should_Work_Age_1_6()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using var command = connection.CreateCypherCommand(
            graphName,
            "WITH '{\"bignumber\":5e24}'::cstring::agtype as obj RETURN obj.bignumber"
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        Assert.Equal(5e24, agResult?.GetDouble());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithEscapedStringReturn_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using var command = connection.CreateCypherCommand(
            graphName,
            @"WITH 'This\u00A0is a and\/or string\r\n\tw\\some \'special\' ""characters"" and \\""escaped quotes\\"".' as p
	RETURN p"
        );
        var str = (string)(Agtype)(await command.ExecuteScalarAsync())!;

        Assert.Equal("This\u00A0is a and/or string\r\n\tw\\some 'special' \"characters\" and \\\"escaped quotes\\\".", str);
        await DropTempGraphAsync(graphName);
    }

    // This test uses ::jsonb::agtype chained cast which is only available in AGE 1.6.0+
    [Fact]
    public async Task ExecuteCypherQueryAsync_WithEscapedJsonStringReturn_Should_Work()
    {
        if (!await AgeVersionSupportsTypeCasts())
            return;
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using var command = connection.CreateCypherCommand(
            graphName,
            @"WITH '""This\\u00A0is a and\\/or string\\r\\n\\tw\\\\some \'special\' \\\""characters\\\"" and \\\\\\\""escaped quotes\\\\\\\"".""'::jsonb::agtype as p
RETURN p"
        );
        var str = (string)(Agtype)(await command.ExecuteScalarAsync())!;

        if (await AgeVersionKeepsChainedCastsAsText())
        {
            // AGE 1.8.0+ evaluates the cast chain differently: '...'::jsonb::agtype inside a
            // Cypher expression no longer parses the JSON, so the result is an agtype string
            // holding the raw JSON source, with escapes left literal (\u00A0, \/ and \").
            Assert.Equal(
                "\"This\\u00A0is a and\\/or string\\r\\n\\tw\\\\some 'special' \\\"characters\\\" and \\\\\\\"escaped quotes\\\\\\\".\"",
                str
            );
        }
        else
        {
            Assert.Equal(
                "This\u00A0is a and/or string\r\n\tw\\some 'special' \"characters\" and \\\"escaped quotes\\\".",
                str
            );
        }
        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithObjectReturn_Should_Work()
    {
        if (!await AgeVersionSupportsTypeCasts())
            return;
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using var command = connection.CreateCypherCommand(
            graphName,
            @"WITH [{id: 0, label: ""label_name_1"", properties: {
	n: [null, 5, 3.1, 3.2, 3.3::money, 3.14::int, 12345::int8, 'nan'::float, 'infinity'::float, '-infinity'::float, '3.1E-11'::float, 5.34::numeric],
	b: [true, false, 0::boolean],
	t: ['2026-02-19T13:14:00'::date, '2026-02-19T13:14:00'::timestamp],
	s: 'This is a string\r\n\tw\\some \'special\' ""characters"" and \\""escaped quotes\\"".',
	ip: 'Anything'::bytea
	}}::vertex,
    {id: 2, start_id: 0, end_id: 1, label: ""edge_label"", properties: {
	m: [{`Key ``is`` ""special""`: 'value'}, '{""This is a string\\r\\n\\tw\\\\some \'special\' \\\""characters\\\"" and \\\\\\\""escaped quotes\\\\\\\""."": ""value""}'::jsonb::agtype]
	}}::edge,
    {id: 1, label: ""label_name_2"", properties: {}}::vertex
]::path as p
RETURN p"
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var path = Assert.IsType<Age.Types.Path>(agResult?.Get<object>());

        var vertex1 = Assert.IsType<Vertex>(path.Segments[0]);
        Assert.Equal(new GraphId(0), vertex1.Id);
        Assert.Equal("label_name_1", vertex1.Label);
        Assert.True(vertex1.Properties.TryGetValue("n", out var propN));
        Assert.Equal(new object?[] { null, 5, 3.1m, 3.2m, "$3.30", 3, 12345, double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0.000000000031m, 5.34m }, Assert.IsType<List<object>>(propN));
        Assert.True(vertex1.Properties.TryGetValue("b", out var propB));
        Assert.Equal(new object?[] { true, false, false }, Assert.IsType<List<object>>(propB));
        Assert.True(vertex1.Properties.TryGetValue("t", out var propT));
        Assert.Equal(new object?[] { "2026-02-19", "2026-02-19T13:14:00" }, Assert.IsType<List<object>>(propT));
        Assert.True(vertex1.Properties.TryGetValue("s", out var propS));
        Assert.Equal("This is a string\r\n\tw\\some 'special' \"characters\" and \\\"escaped quotes\\\".", Assert.IsType<string>(propS));
        Assert.True(vertex1.Properties.TryGetValue("ip", out var propIp));
        Assert.Equal(@"\x416e797468696e67", Assert.IsType<string>(propIp));

        var edge = Assert.IsType<Edge>(path.Segments[1]);
        Assert.Equal(new GraphId(2), edge.Id);
        Assert.Equal(new GraphId(0), edge.StartId);
        Assert.Equal(new GraphId(1), edge.EndId);
        Assert.Equal("edge_label", edge.Label);
        Assert.True(edge.Properties.TryGetValue("m", out var propM));
        if (await AgeVersionKeepsChainedCastsAsText())
        {
            // AGE 1.8.0+ evaluates the cast chain differently: the ::jsonb::agtype element
            // is no longer parsed into a map, so it arrives as the raw JSON object text.
            Assert.Equal(
                new object[]
                {
                    new Dictionary<string, object>()
                    {
                        { @"Key `is` ""special""", "value"},
                    },
                    @"{""This is a string\r\n\tw\\some 'special' \""characters\"" and \\\""escaped quotes\\\""."": ""value""}",
                },
                Assert.IsType<List<object>>(propM)
            );
        }
        else
        {
            Assert.Equal(new object[]
            {
                new Dictionary<string, object>() {
                    { @"Key `is` ""special""", "value"},
                },
                new Dictionary<string, object>() {
                    { "This is a string\r\n\tw\\some 'special' \"characters\" and \\\"escaped quotes\\\".", "value" }
                }
            }, Assert.IsType<List<object>>(propM));
        }

        var vertex2 = Assert.IsType<Vertex>(path.Segments[2]);
        Assert.Equal(new GraphId(1), vertex2.Id);
        Assert.Equal("label_name_2", vertex2.Label);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithDictionaryParameters_Should_ReturnCorrectResults()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        // Create a vertex first
        await using var createCommand = connection.CreateCypherCommand(
            graphName,
            "CREATE (p:Person {name: 'Alice', age: 30}) RETURN p"
        );
        await createCommand.ExecuteNonQueryAsync();

        // Query with parameters using dictionary
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice", ["minAge"] = 25 };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "MATCH (p:Person) WHERE p.name = $name AND p.age > $minAge RETURN p.name, p.age",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var nameResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var ageResult = await dataReader.GetFieldValueAsync<Agtype?>(1);

        Assert.Equal("Alice", nameResult?.GetString().Trim('"'));
        Assert.Equal(30, ageResult?.GetInt32());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithDictionaryParameters_WithExplicitPrepare_Should_ReturnCorrectResults()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        // Create a vertex first
        await using var createCommand = connection.CreateCypherCommand(
            graphName,
            "CREATE (p:Person {name: 'Alice', age: 30}) RETURN p"
        );
        await createCommand.ExecuteNonQueryAsync();

        // Query with parameters using dictionary
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice", ["minAge"] = 25 };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "MATCH (p:Person) WHERE p.name = $name AND p.age > $minAge RETURN p.name, p.age",
            parameters
        );
        await command.PrepareAsync();
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var nameResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var ageResult = await dataReader.GetFieldValueAsync<Agtype?>(1);

        Assert.Equal("Alice", nameResult?.GetString().Trim('"'));
        Assert.Equal(30, ageResult?.GetInt32());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithJsonStringParameters_Should_ReturnCorrectResults()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        // Create vertices first
        await using var createCommand = connection.CreateCypherCommand(
            graphName,
            "CREATE (p1:Person {name: 'Bob', age: 25}), (p2:Person {name: 'Charlie', age: 35}) RETURN p1, p2"
        );
        await createCommand.ExecuteNonQueryAsync();

        // Query with parameters using JSON string
        var parametersJson = "{\"targetAge\": 25, \"personName\": \"Bob\"}";

        await using var command = connection.CreateCypherCommand(
            graphName,
            "MATCH (p:Person) WHERE p.age = $targetAge AND p.name = $personName RETURN p",
            parametersJson
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var result = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var vertex = result?.GetVertex();

        Assert.NotNull(vertex);
        Assert.Equal("Bob", vertex?.Properties["name"]);
        Assert.Equal(25, vertex?.Properties["age"]);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithComplexParameters_Should_HandleDifferentTypes()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["stringParam"] = "test",
            ["intParam"] = 42,
            ["doubleParam"] = 3.14,
            ["boolParam"] = true,
            ["listParam"] = new[] { 1, 2, 3 },
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            @"RETURN $stringParam, $intParam, $doubleParam, $boolParam, $listParam",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var stringResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var intResult = await dataReader.GetFieldValueAsync<Agtype?>(1);
        var doubleResult = await dataReader.GetFieldValueAsync<Agtype?>(2);
        var boolResult = await dataReader.GetFieldValueAsync<Agtype?>(3);
        var listResult = await dataReader.GetFieldValueAsync<Agtype?>(4);

        Assert.Equal("test", stringResult?.GetString());
        Assert.Equal(42, intResult?.GetInt32());
        Assert.Equal(3.14, doubleResult?.GetDouble());
        Assert.True(boolResult?.GetBoolean());
        Assert.Equal(new object[] { 1, 2, 3 }, listResult?.GetList());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithNullParameters_Should_HandleNull()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["nullParam"] = null,
            ["validParam"] = "notNull",
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "RETURN $nullParam, $validParam",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var nullResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var validResult = await dataReader.GetFieldValueAsync<Agtype?>(1);

        Assert.Null(nullResult);
        Assert.Equal("notNull", validResult?.GetString());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithEmptyParameters_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>();

        await using var command = connection.CreateCypherCommand(
            graphName,
            "RETURN 'no parameters used'",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var result = await dataReader.GetFieldValueAsync<Agtype?>(0);
        Assert.Equal("no parameters used", result?.GetString());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithNestedObject_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["person"] = new Dictionary<string, object>
            {
                ["name"] = "Alice",
                ["age"] = 30,
                ["address"] = new Dictionary<string, object>
                {
                    ["city"] = "Seattle",
                    ["zipCode"] = "98101",
                },
            },
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "CREATE (p:Person {name: $person.name, age: $person.age, city: $person.address.city, zipCode: $person.address.zipCode}) RETURN p",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var result = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var vertex = result?.GetVertex();

        Assert.NotNull(vertex);
        Assert.Equal("Alice", vertex?.Properties["name"]);
        Assert.Equal(30, vertex?.Properties["age"]);
        Assert.Equal("Seattle", vertex?.Properties["city"]);
        Assert.Equal("98101", vertex?.Properties["zipCode"]);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithArrayParameter_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["hobbies"] = new[] { "reading", "cycling", "photography" },
            ["scores"] = new[] { 85, 92, 78, 95 },
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "RETURN $hobbies, $scores",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var hobbiesResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var scoresResult = await dataReader.GetFieldValueAsync<Agtype?>(1);

        Assert.Equal(
            new object[] { "reading", "cycling", "photography" },
            hobbiesResult?.GetList()
        );
        Assert.Equal(new object[] { 85, 92, 78, 95 }, scoresResult?.GetList());

        await DropTempGraphAsync(graphName);
    }

    // This test uses ::jsonb::agtype chained cast which is only available in AGE 1.6.0+
    [Fact]
    public async Task ExecuteCypherQueryAsync_WithQuotedStringValues_Should_Work()
    {
        if (!await AgeVersionSupportsTypeCasts())
            return;
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["data"] = new Dictionary<string, object>
            {
                ["escapes"] = "\"characters\" and \\\"escaped quotes\\\" and \\\\\"escaped quotes\\\\\"."
            },
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            @"UNWIND [
  '""characters"" and \\""escaped quotes\\"" and \\\\""escaped quotes\\\\"".' ,
  '""\\\""characters\\\"" and \\\\\\\""escaped quotes\\\\\\\"" and \\\\\\\\\\\""escaped quotes\\\\\\\\\\\"".""'::jsonb::agtype,
  $data.escapes
] as v
RETURN v + ' -> ' + replace(v, '""', '$') as p",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);

        var actual = new List<string>();
        while(await dataReader.ReadAsync())
        {
            var mainResult = await dataReader.GetFieldValueAsync<Agtype>(0);
            actual.Add((string)mainResult);
        }
        if (await AgeVersionKeepsChainedCastsAsText())
        {
            // AGE 1.8.0+ evaluates the cast chain differently: the ::jsonb::agtype
            // element is no longer parsed into a Cypher string, so every row keeps
            // the raw JSON quoting (and the jsonb element its escapes) undecoded.
            var expected180 = new List<string>
            {
                @"""characters"" and \""escaped quotes\"" and \\""escaped quotes\\"". -> $characters$ and \$escaped quotes\$ and \\$escaped quotes\\$.",
                @"""\""characters\"" and \\\""escaped quotes\\\"" and \\\\\""escaped quotes\\\\\""."" -> $\$characters\$ and \\\$escaped quotes\\\$ and \\\\\$escaped quotes\\\\\$.$",
                @"""characters"" and \""escaped quotes\"" and \\""escaped quotes\\"". -> $characters$ and \$escaped quotes\$ and \\$escaped quotes\\$.",
            };
            Assert.Equal(expected180, actual);
        }
        else
        {
            Assert.Equal(Enumerable.Repeat(@"""characters"" and \""escaped quotes\"" and \\""escaped quotes\\"". -> $characters$ and \$escaped quotes\$ and \\$escaped quotes\\$.", 3), actual);
        }

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithSpecialValues_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["data"] = new Dictionary<string, object>
            {
                ["integer"] = 3,
                ["inf"] = double.PositiveInfinity,
                ["decimal"] = 7.893m,
                ["escapes"] = "This\u00A0is a and/or string\r\n\tw\\some 'special' \"characters\" and \\\"escaped quotes\\\"."
            },
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "RETURN $data",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var mainResult = await dataReader.GetFieldValueAsync<Agtype>(0);
        var actual = mainResult.Get<Dictionary<string, object>>();

        Assert.Equivalent(parameters["data"], actual);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithNestedArraysAndObjects_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["data"] = new Dictionary<string, object>
            {
                ["tags"] = new[] { "developer", "architect" },
                ["metadata"] = new Dictionary<string, object>
                {
                    ["level"] = "senior",
                    ["years"] = 10,
                },
                ["projects"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "Project A",
                        ["status"] = "completed",
                    },
                    new Dictionary<string, object>
                    {
                        ["name"] = "Project B",
                        ["status"] = "active",
                    },
                },
            },
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "RETURN $data.tags, $data.metadata.level, $data.metadata.years, $data.projects",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var tagsResult = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var levelResult = await dataReader.GetFieldValueAsync<Agtype?>(1);
        var yearsResult = await dataReader.GetFieldValueAsync<Agtype?>(2);
        var projectsResult = await dataReader.GetFieldValueAsync<Agtype?>(3);

        Assert.Equal(new object[] { "developer", "architect" }, tagsResult?.GetList());
        Assert.Equal("senior", levelResult?.GetString());
        Assert.Equal(10, yearsResult?.GetInt32());

        var projects = projectsResult?.GetList();
        Assert.NotNull(projects);
        Assert.Equal(2, projects.Count);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_WithArrayInVertexCreation_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["name"] = "Bob",
            ["skills"] = new[] { "C#", "Python", "SQL" },
            ["certifications"] = new[] { "Azure", "AWS" },
        };

        await using var command = connection.CreateCypherCommand(
            graphName,
            "CREATE (p:Developer {name: $name, skills: $skills, certifications: $certifications}) RETURN p",
            parameters
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(dataReader.HasRows);
        Assert.True(await dataReader.ReadAsync());

        var result = await dataReader.GetFieldValueAsync<Agtype?>(0);
        var vertex = result?.GetVertex();

        Assert.NotNull(vertex);
        Assert.Equal("Bob", vertex?.Properties["name"]);

        var skills = vertex?.Properties["skills"] as System.Collections.IList;
        Assert.NotNull(skills);
        Assert.Equal(3, skills.Count);
        Assert.Contains("C#", skills.Cast<object>());
        Assert.Contains("Python", skills.Cast<object>());
        Assert.Contains("SQL", skills.Cast<object>());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task GetList_Should_ReturnTypedElements()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
    RETURN [1, 'hello', true, NULL]
$$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.NotNull(agResult);
        Assert.True(agResult.Value.IsArray);
        var elements = agResult.Value.GetList();
        Assert.Equal(4, elements.Count);
        Assert.Equal(1, elements[0]);
        Assert.Equal("hello", elements[1]);
        Assert.Equal(true, elements[2]);
        Assert.Null(elements[3]);

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task GetList_OnVertexArray_Should_PreserveVertexType()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
    CREATE (a:Person {{name: 'Alice', age: 30}})
    CREATE (b:Person {{name: 'Bob', age: 25}})
    WITH [a, b] AS vertices
    RETURN vertices
$$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.NotNull(agResult);
        Assert.True(agResult.Value.IsArray);
        var elements = agResult.Value.GetList();
        Assert.Equal(2, elements.Count);
        var v0 = Assert.IsType<Vertex<Dictionary<string, object?>>>(elements[0]);
        var v1 = Assert.IsType<Vertex<Dictionary<string, object?>>>(elements[1]);
        Assert.Equal("Alice", v0.Properties["name"]);
        Assert.Equal("Bob", v1.Properties["name"]);

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task GetList_OnNestedArray_Should_ReturnNestedLists()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
    RETURN [[1, 2], [3, 4]]
$$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.NotNull(agResult);
        Assert.True(agResult.Value.IsArray);
        var outer = agResult.Value.GetList();
        Assert.Equal(2, outer.Count);
        var inner = Assert.IsType<List<object?>>(outer[0]);
        Assert.Equal(2, inner.Count);
        Assert.Equal(1, inner[0]);
        Assert.Equal(2, inner[1]);

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task GetMap_Should_ReturnCorrectDictionary()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
    RETURN {{key: 'value', num: 42}}
$$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.NotNull(agResult);
        Assert.True(agResult.Value.IsMap);
        Assert.False(agResult.Value.IsArray);
        var map = agResult.Value.GetMap();
        Assert.Equal("value", map["key"]);
        Assert.Equal(42, map["num"]);

        await DropTempGraphAsync(graphname);
    }

    [Fact]
    public async Task IsNull_Should_ReturnTrue_ForNullReturnedByAge()
    {
        var graphname = await CreateTempGraphAsync();

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $@"SELECT * FROM ag_catalog.cypher('{graphname}', $$
    RETURN NULL
$$) as (value agtype);",
            connection
        );
        await using var dataReader = await command.ExecuteReaderAsync();
        Assert.True(await dataReader.ReadAsync());
        var agResult = await dataReader.GetFieldValueAsync<Agtype?>(0);

        Assert.Null(agResult);

        await DropTempGraphAsync(graphname);
    }

    // Regression tests for pg-age-digitaltwins#115: GenerateAsPart emitted an empty
    // column-definition list for scalar and partial map-projection return items, producing
    // "as ( agtype)" which PostgreSQL rejects with 42601 "syntax error at or near )".
    // These execute the generated SQL end-to-end against a real AGE graph.

    [Fact]
    public async Task ExecuteCypherQueryAsync_ScalarReturnWithAlias_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using (
            var setup = connection.CreateCypherCommand(
                graphName,
                "CREATE (:Twin { `$dtId`: 't1', name: 'room1', returnPeriod: 'P1D' })"
            )
        )
        {
            await setup.ExecuteNonQueryAsync();
        }

        await using var command = connection.CreateCypherCommand(
            graphName,
            "MATCH (t:Twin) RETURN t.returnPeriod AS period"
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var value = await dataReader.GetFieldValueAsync<Agtype?>(0);
        Assert.Equal("P1D", value?.GetString());

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_PartialProjectionWithAlias_Should_Work()
    {
        // Map projection ("x { .prop }") is a Cypher-language feature that AGE only gained in
        // 1.6.0 - on 1.5.0 the query itself is a syntax error, which is not what this fix is
        // about. The column-list generation is still asserted on every version by the unit
        // tests in Repro115Tests; this test proves PostgreSQL accepts the generated SQL.
        if (!await AgeVersionSupportsTypeCasts())
            return;

        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using (
            var setup = connection.CreateCypherCommand(
                graphName,
                "CREATE (:Twin { `$dtId`: 't1', name: 'room1', returnPeriod: 'P1D' })"
            )
        )
        {
            await setup.ExecuteNonQueryAsync();
        }

        await using var command = connection.CreateCypherCommand(
            graphName,
            "MATCH (t:Twin) RETURN t { .returnPeriod } AS twin"
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var value = await dataReader.GetFieldValueAsync<Agtype?>(0);
        Assert.Equal("P1D", (string?)value?.GetMap()["returnPeriod"]);

        await DropTempGraphAsync(graphName);
    }

    [Fact]
    public async Task ExecuteCypherQueryAsync_ScalarReturnWithClausePrefixedProperty_Should_Work()
    {
        // "returnPeriod" starts with the RETURN keyword and previously truncated the derived
        // column name to an empty string, producing 42601. The "limit" property is a Cypher
        // reserved word, so only "returnPeriod" is queried here.
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        await using (
            var setup = connection.CreateCypherCommand(
                graphName,
                "CREATE (:Twin { `$dtId`: 't1', returnPeriod: 'P1D' })"
            )
        )
        {
            await setup.ExecuteNonQueryAsync();
        }

        await using var command = connection.CreateCypherCommand(
            graphName,
            "MATCH (t:Twin) RETURN t.returnPeriod AS period"
        );
        await using var dataReader = await command.ExecuteReaderAsync();

        Assert.NotNull(dataReader);
        Assert.True(await dataReader.ReadAsync());
        var periodValue = await dataReader.GetFieldValueAsync<Agtype?>(0);
        Assert.Equal("P1D", periodValue?.GetString());

        await DropTempGraphAsync(graphName);
    }
}
