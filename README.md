# Konnektr.Npgsql.Age

[![Nuget](https://img.shields.io/nuget/v/Konnektr.Npgsql.Age?color=blue)](https://www.nuget.org/packages/Konnektr.Npgsql.Age/)

## What is Apache AGE?

Apache AGE is an open-source extension for PostgreSQL which provides it with the capabilities of a graph database. This package is a plugin for the Npgsql library which allows you to interact with Apache AGE from C#.

## Quickstart

Here's a simple example to get you started:

```csharp
using Npgsql;
using Npgsql.Age;
using Npgsql.Age.Types;

var connectionString = "Host=server;Port=5432;Username=user;Password=pass;Database=sample1";

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
await using var dataSource = dataSourceBuilder
    .UseAge()
    .Build();

// Create graph
await using (var cmd = dataSource.CreateGraphCommand("graph1"))
{
    await cmd.ExecuteNonQueryAsync();
}

// Add vertices
await using (var cmd = dataSource.CreateCypherCommand("graph1", "CREATE (:Person {age: 23}), (:Person {age: 78})"))
{
    await cmd.ExecuteNonQueryAsync();
}

// Retrieve vertices
await using (var cmd = dataSource.CreateCypherCommand(
    "graph1", "MATCH (n:Person) RETURN n"))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        var agtypeResult = reader.GetValue<Agtype>(0);
        Vertex person = agtypeResult.GetVertex();
        Console.WriteLine(person);
    }
}
```

## Using Cypher Parameters

You can pass parameters to your Cypher queries to avoid SQL injection and improve query reusability. Parameters are referenced in Cypher queries using the `$` prefix (e.g., `$name`, `$age`).

### Using a Dictionary

```csharp
using Npgsql;
using Npgsql.Age;
using Npgsql.Age.Types;
using System.Collections.Generic;

var parameters = new Dictionary<string, object?>
{
    ["name"] = "Alice",
    ["age"] = 30
};

await using (var cmd = dataSource.CreateCypherCommand(
    "graph1", 
    "CREATE (p:Person {name: $name, age: $age}) RETURN p",
    parameters))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        var agtypeResult = reader.GetValue<Agtype>(0);
        Vertex person = agtypeResult.GetVertex();
        Console.WriteLine($"Created: {person}");
    }
}
```

### Using a JSON String

You can also pass parameters as a JSON string:

```csharp
string parametersJson = """{"name": "Bob", "age": 25}""";

await using (var cmd = dataSource.CreateCypherCommand(
    "graph1", 
    "CREATE (p:Person {name: $name, age: $age}) RETURN p",
    parametersJson))
{
    await cmd.ExecuteNonQueryAsync();
}
```

### Complex Parameters

Parameters can include nested objects and arrays:

```csharp
var parameters = new Dictionary<string, object?>
{
    ["person"] = new Dictionary<string, object>
    {
        ["name"] = "Charlie",
        ["age"] = 35,
        ["hobbies"] = new[] { "reading", "cycling" }
    }
};

await using (var cmd = dataSource.CreateCypherCommand(
    "graph1", 
    "CREATE (p:Person {name: $person.name, age: $person.age}) RETURN p",
    parameters))
{
    await cmd.ExecuteNonQueryAsync();
}
```

### Null Values

Null parameter values are supported:

```csharp
var parameters = new Dictionary<string, object?>
{
    ["name"] = "David",
    ["email"] = null  // Optional property
};

await using (var cmd = dataSource.CreateCypherCommand(
    "graph1", 
    "CREATE (p:Person {name: $name}) RETURN p",
    parameters))
{
    await cmd.ExecuteNonQueryAsync();
}
```

## Acknowledgements

* This project is a fork of [Apache AGE](https://github.com/Allison-E/pg-age).
* The project relies heavily on the work of the [Npgsql](https://github.com/npgsql/npgsql) team.
