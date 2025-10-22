using Microsoft.Extensions.Configuration;
using Npgsql.Age;

namespace Npgsql.AgeTests;

public class TestBase
{
    private readonly NpgsqlDataSource _dataSource;

    public TestBase()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Development.json")
            .Build();

        string connectionString =
            configuration.GetConnectionString("AgeConnectionString")
            ?? throw new ArgumentNullException("AgeConnectionString");

        NpgsqlConnectionStringBuilder connectionStringBuilder =
            new(connectionString) { SearchPath = "ag_catalog, \"$user\", public" };
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            connectionStringBuilder.ConnectionString
        );
        _dataSource = dataSourceBuilder.UseAge().Build();
    }

    public void Dispose()
    {
        _dataSource?.Dispose();
    }

    public NpgsqlDataSource DataSource => _dataSource;

    protected async Task<string> CreateTempGraphAsync()
    {
        var graphName = "temp_graph" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = connection.CreateGraphCommand(graphName);
        await command.ExecuteNonQueryAsync();
        return graphName;
    }

    protected async Task DropTempGraphAsync(string graphName)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = connection.DropGraphCommand(graphName);
        await command.ExecuteNonQueryAsync();
    }
}
