﻿using Microsoft.Extensions.Configuration;
using Npgsql.Age;

namespace Npgsql.AgeTests;

internal class TestBase
{
    private readonly NpgsqlDataSource _dataSource;

    public TestBase()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Development.json").Build();

        string connectionString = Environment.GetEnvironmentVariable("AGE_CONNECTION_STRING")
            ?? configuration.GetConnectionString("AgeConnectionString")
            ?? throw new ArgumentNullException("AgeConnectionString");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        _dataSource = dataSourceBuilder.UseAge(false).Build();
    }

    public void Dispose()
    {
        _dataSource?.Dispose();
    }

    public NpgsqlDataSource DataSource => _dataSource;

    protected async Task<string> CreateTempGraphAsync()
    {
        var graphName = "temp_graph" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
        await using var command = _dataSource.CreateGraphCommand(graphName);
        await command.ExecuteNonQueryAsync();
        return graphName;
    }

    protected async Task DropTempGraphAsync(string graphName)
    {
        await using var command = _dataSource.DropGraphCommand(graphName);
        await command.ExecuteNonQueryAsync();
    }
}
