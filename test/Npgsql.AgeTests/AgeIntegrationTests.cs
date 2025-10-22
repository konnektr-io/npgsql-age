using System.Text.Json;
using Npgsql.Age;
using Npgsql.Age.Internal;
using Npgsql.Age.Types;
using NpgsqlTypes;

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
        Assert.Equal(true, boolResult?.GetBoolean());
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
    public async Task ExecuteCypherQueryAsync_WithRootParameterAccess_Should_Work()
    {
        var graphName = await CreateTempGraphAsync();
        await using var connection = await DataSource.OpenConnectionAsync();

        var parameters = new Dictionary<string, object?>
        {
            ["$dtId"] = "device-001",
            ["name"] = "Temperature Sensor",
            ["location"] = "Building A",
            ["status"] = "active",
        };

        // First create the vertex with a specific property from the root
        await using var createCommand = connection.CreateCypherCommand(
            graphName,
            "CREATE (t:Twin {dtId: $['$dtId']}) RETURN t",
            parameters
        );
        await using var createReader = await createCommand.ExecuteReaderAsync();

        Assert.NotNull(createReader);
        Assert.True(createReader.HasRows);
        Assert.True(await createReader.ReadAsync());

        var createResult = await createReader.GetFieldValueAsync<Agtype?>(0);
        var createdVertex = createResult?.GetVertex();

        Assert.NotNull(createdVertex);
        Assert.Equal("device-001", createdVertex?.Properties["dtId"]);

        // Now test MERGE with SET using root parameter
        await using var mergeCommand = connection.CreateCypherCommand(
            graphName,
            @"MERGE (t:Twin {dtId: $['$dtId']})
              SET t = $
              RETURN t",
            parameters
        );
        await using var mergeReader = await mergeCommand.ExecuteReaderAsync();

        Assert.NotNull(mergeReader);
        Assert.True(mergeReader.HasRows);
        Assert.True(await mergeReader.ReadAsync());

        var mergeResult = await mergeReader.GetFieldValueAsync<Agtype?>(0);
        var mergedVertex = mergeResult?.GetVertex();

        Assert.NotNull(mergedVertex);
        Assert.Equal("device-001", mergedVertex?.Properties["$dtId"]);
        Assert.Equal("Temperature Sensor", mergedVertex?.Properties["name"]);
        Assert.Equal("Building A", mergedVertex?.Properties["location"]);
        Assert.Equal("active", mergedVertex?.Properties["status"]);

        await DropTempGraphAsync(graphName);
    }
}
