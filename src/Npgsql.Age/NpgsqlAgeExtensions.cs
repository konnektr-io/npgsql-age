﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql.Age.Internal;
using Npgsql;
using Npgsql.TypeMapping;

namespace Npgsql.Age
{
    public static class NpgsqlAgeExtensions
    {
        /// <summary>
        /// Use Apache AGE types and connection initializer
        /// </summary>
        /// <param name="builder">Npgsql data source builder.</param>
        /// <param name="superUser">Whether to use super user privileges.</param>
        /// <returns></returns>
        public static NpgsqlDataSourceBuilder UseAge(this NpgsqlDataSourceBuilder builder, bool superUser = true)
        {
            builder.AddTypeInfoResolverFactory(new AgtypeResolverFactory());
            builder.UsePhysicalConnectionInitializer(
                connection => ConnectionInitializer.UsePhysicalConnectionInitializer(connection, superUser),
                connection => ConnectionInitializer.UsePhysicalConnectionInitializerAsync(connection, superUser));
            return builder;
        }
    }

    public static class NpgsqlDataSourceAgeExtensions
    {
        public static NpgsqlCommand CreateGraphCommand(this NpgsqlDataSource dataSource, string graphName)
        {
            NpgsqlCommand command = dataSource.CreateCommand($"SELECT * FROM ag_catalog.create_graph($1);");
            command.Parameters.AddWithValue(graphName);
            return command;
        }

        public static NpgsqlCommand DropGraphCommand(this NpgsqlDataSource dataSource, string graphName)
        {
            NpgsqlCommand command = dataSource.CreateCommand($"SELECT * FROM ag_catalog.drop_graph($1, true);");
            command.Parameters.AddWithValue(graphName);
            return command;
        }

        public static NpgsqlCommand GraphExistsCommand(this NpgsqlDataSource dataSource, string graphName)
        {
            NpgsqlCommand command = dataSource.CreateCommand($"SELECT 1 FROM ag_catalog.ag_graph WHERE name = $1;");
            command.Parameters.AddWithValue(graphName);
            return command;
        }

        public static async NpgsqlCommand CreateCypherCommand(this NpgsqlDataSource dataSource, string graphName, string cypher)
        {
            string asPart = CypherHelpers.GenerateAsPart(cypher);
            // LOAD '$libdir/plugins/age';SET search_path = ag_catalog, \"$user\", public;
            string query = $"SELECT * FROM cypher('{graphName}', $$ {cypher} $$) as {asPart};";
            var connection = await dataSource.OpenConnectionAsync();
            connection.StateChange += async (sender, args) =>
            {
                if (args.CurrentState == System.Data.ConnectionState.Open)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "LOAD '$libdir/plugins/age';SET search_path = ag_catalog, \"$user\", public;";
                    await command.ExecuteNonQueryAsync();
                }
            };
            NpgsqlCommand command = dataSource.CreateCommand(query);
            return command;
        }
    }

    public static class NpgsqlConnectionAgeExtensions
    {
        public static NpgsqlCommand CreateGraphCommand(this NpgsqlConnection connection, string graphName)
        {
            NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM ag_catalog.create_graph($1);";
            command.Parameters.AddWithValue("name", graphName);
            return command;
        }

        public static NpgsqlCommand DropGraphCommand(this NpgsqlConnection connection, string graphName)
        {
            NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM ag_catalog.drop_graph($1);";
            command.Parameters.AddWithValue("name", graphName);
            return command;
        }

        public static NpgsqlCommand GraphExistsCommand(this NpgsqlConnection connection, string graphName)
        {
            NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM ag_catalog.ag_graph WHERE name = $1;";
            command.Parameters.AddWithValue("name", graphName);
            return command;
        }

        public static NpgsqlCommand CreateCypherCommand(this NpgsqlConnection connection, string graph, string cypher)
        {
            string asPart = CypherHelpers.GenerateAsPart(cypher);
            string query = $"SELECT * FROM cypher('{graph}', $$ {cypher} $$) as {asPart};";
            NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = query;
            return command;
        }
    }

    internal static class CypherHelpers
    {
        internal static string GenerateAsPart(string cypher)
        {
            // Extract the return part of the Cypher query
            var match = Regex.Match(cypher, @"RETURN\s+(.*)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return "(result agtype)";
            }

            // Split the return values
            var returnValues = match.Groups[1].Value.Split(',');

            // Generate the 'as (...)' part
            var asPart = string.Join(", ", returnValues.Select((value, index) =>
            {
                var trimmedValue = value.Trim();
                if (int.TryParse(trimmedValue, out _) || double.TryParse(trimmedValue, out _))
                {
                    return $"num{index} agtype";
                }
                return $"{trimmedValue} agtype";
            }));
            return $"({asPart})";
        }
    }
}
