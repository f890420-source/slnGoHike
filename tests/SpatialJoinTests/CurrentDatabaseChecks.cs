using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using prjGoHike.Models;

static partial class CurrentDatabaseChecks
{
    public static string ConnectionString()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "slnGoHike.slnx"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Run current database checks from the repository root.");
        var configuration = new ConfigurationBuilder().SetBasePath(Path.Combine(directory.FullName, "prjGoHike"))
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(GoHikeDataContext).Assembly, optional: true).AddEnvironmentVariables().Build();
        return configuration.GetConnectionString("GoHikeDataContext") ?? throw new InvalidOperationException("Current business database configuration is missing.");
    }

    public static async Task ReadOnlyAsync()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString()) { ConnectTimeout = 10 };
        Console.WriteLine($"Current database target: server={builder.DataSource}, database={builder.InitialCatalog}.");
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await PrintAsync(connection, """
            SELECT DB_NAME() AS DatabaseName, SERVERPROPERTY('ProductVersion') AS SqlServerVersion;
            SELECT 'Trails' AS TableName, COUNT_BIG(*) AS [RowCount] FROM dbo.Trails
            UNION ALL SELECT 'Indicators',COUNT_BIG(*) FROM dbo.Indicators
            UNION ALL SELECT 'TrailSegments',COUNT_BIG(*) FROM dbo.TrailSegments
            UNION ALL SELECT 'IndicatorSegments',COUNT_BIG(*) FROM dbo.IndicatorSegments
            UNION ALL SELECT 'TrailIndicators',COUNT_BIG(*) FROM dbo.TrailIndicators
            UNION ALL SELECT 'BackgroundJobRuns',COUNT_BIG(*) FROM dbo.BackgroundJobRuns;
            SELECT name, TYPE_NAME(user_type_id) AS SqlType, precision, scale, is_nullable
            FROM sys.columns WHERE object_id=OBJECT_ID('dbo.TrailIndicators') AND name='EvaluatedScore';
            SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.BackgroundJobRuns');
            SELECT name,is_disabled FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.BackgroundJobRuns');
            SELECT Status, COUNT_BIG(*) AS Runs FROM dbo.BackgroundJobRuns WHERE JobType='SpatialJoin' GROUP BY Status;
            SELECT 'TrailSegments' AS SourceName, Shape.STSrid AS SRID,COUNT_BIG(*) AS [RowCount] FROM dbo.TrailSegments GROUP BY Shape.STSrid
            UNION ALL SELECT 'IndicatorSegments',Shape.STSrid,COUNT_BIG(*) FROM dbo.IndicatorSegments GROUP BY Shape.STSrid;
            SELECT 'Trails' AS SourceName,COUNT_BIG(*) AS ActiveWithoutSegments FROM dbo.Trails t
              WHERE t.IsPublished=1 AND NOT EXISTS(SELECT 1 FROM dbo.TrailSegments s WHERE s.Trail_Id=t.Trail_Id)
            UNION ALL SELECT 'Indicators',COUNT_BIG(*) FROM dbo.Indicators i
              WHERE i.IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.IndicatorSegments s WHERE s.IndicatorId=i.IndicatorId);
            SELECT SCHEMA_NAME(schema_id) AS SchemaName, name AS TableName FROM sys.tables WHERE SCHEMA_NAME(schema_id)='HangFire';
            """);
    }

    private static async Task PrintAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            if (reader.FieldCount == 0) continue;
            Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)));
            while (await reader.ReadAsync()) Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString())));
        } while (await reader.NextResultAsync());
    }
}
