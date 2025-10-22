using System.Collections.Generic;
using System.Text.Json;
using Npgsql.Age.Internal;
using Npgsql.Age.Types;

namespace Npgsql.Age
{
    public static class NpgsqlAgeExtensions
    {
        /// <summary>
        /// Use Apache AGE types and connection initializer
        /// </summary>
        /// <param name="builder">Npgsql data source builder.</param>
        /// <param name="loadFromPlugins">Whether to use super user privileges.</param>
        /// <returns>The same builder instance so that multiple calls can be chained</returns>
        public static NpgsqlDataSourceBuilder UseAge(
            this NpgsqlDataSourceBuilder builder,
            bool loadFromPlugins = false
        )
        {
            builder.AddTypeInfoResolverFactory(new AgtypeResolverFactory());
            builder.UsePhysicalConnectionInitializer(
                connection =>
                    ConnectionInitializer.UsePhysicalConnectionInitializer(
                        connection,
                        loadFromPlugins
                    ),
                connection =>
                    ConnectionInitializer.UsePhysicalConnectionInitializerAsync(
                        connection,
                        loadFromPlugins
                    )
            );

            return builder;
        }
    }

    public static class NpgsqlConnectionAgeExtensions
    {
        public static NpgsqlCommand CreateGraphCommand(
            this NpgsqlConnection connection,
            string graphName
        )
        {
            return new NpgsqlCommand($"SELECT * FROM ag_catalog.create_graph($1);", connection)
            {
                Parameters = { new NpgsqlParameter { Value = graphName } },
            };
        }

        public static NpgsqlCommand DropGraphCommand(
            this NpgsqlConnection connection,
            string graphName
        )
        {
            return new NpgsqlCommand($"SELECT * FROM ag_catalog.drop_graph($1, true);", connection)
            {
                Parameters = { new NpgsqlParameter { Value = graphName } },
            };
        }

        public static NpgsqlCommand GraphExistsCommand(
            this NpgsqlConnection connection,
            string graphName
        )
        {
            return new NpgsqlCommand(
                $"SELECT EXISTS (SELECT 1 FROM ag_catalog.ag_graph WHERE name = $1);",
                connection
            )
            {
                Parameters = { new NpgsqlParameter { Value = graphName } },
            };
        }

        public static NpgsqlCommand CreateCypherCommand(
            this NpgsqlConnection connection,
            string graphName,
            string cypher
        )
        {
            string query =
                $"SELECT * FROM ag_catalog.cypher('{graphName}', $$ {CypherHelpers.EscapeCypher(cypher)} $$) as {CypherHelpers.GenerateAsPart(cypher)};";
            return new NpgsqlCommand(query, connection);
        }

        /// <summary>
        /// Creates a Cypher command with parameters passed as a dictionary
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="graphName">The name of the graph</param>
        /// <param name="cypher">The Cypher query with parameter placeholders (e.g., $name)</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <returns>An NpgsqlCommand ready for execution</returns>
        public static NpgsqlCommand CreateCypherCommand(
            this NpgsqlConnection connection,
            string graphName,
            string cypher,
            Dictionary<string, object?> parameters
        )
        {
            string parametersJson = JsonSerializer.Serialize(parameters);
            return CreateCypherCommand(connection, graphName, cypher, parametersJson);
        }

        /// <summary>
        /// Creates a Cypher command with parameters passed as a JSON string
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="graphName">The name of the graph</param>
        /// <param name="cypher">The Cypher query with parameter placeholders (e.g., $name)</param>
        /// <param name="parametersJson">JSON string containing parameter names and values</param>
        /// <returns>An NpgsqlCommand ready for execution</returns>
        public static NpgsqlCommand CreateCypherCommand(
            this NpgsqlConnection connection,
            string graphName,
            string cypher,
            string parametersJson
        )
        {
            string query =
                $"SELECT * FROM ag_catalog.cypher('{graphName}', $$ {CypherHelpers.EscapeCypher(cypher)} $$, $1) as {CypherHelpers.GenerateAsPart(cypher)};";
            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue(new Agtype(parametersJson));
            command.Parameters[0].DataTypeName = "ag_catalog.agtype";
            return command;
        }
    }
}
