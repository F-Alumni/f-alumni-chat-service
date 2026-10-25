using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ChatService.Repositories.Context;

namespace ChatService.Tests.Infrastructure;

/// <summary>
/// Manages SQL Server integration test environment and safeguards database isolation.
/// Enforces that tests strictly connect to 'FptAlumni_Guideline_Test_Db' and never touch domain databases.
/// </summary>
public static class SqlServerTestEnvironment
{
    public const string TargetDatabaseName = "FptAlumni_Guideline_Test_Db";
    public const string DefaultLocalConnectionString = "Server=localhost\\SQLEXPRESS;Database=FptAlumni_Guideline_Test_Db;Trusted_Connection=True;TrustServerCertificate=True;";

    private static bool _migrationApplied;
    private static readonly object MigrationLock = new();

    /// <summary>
    /// Resolves the SQL Server connection string for integration testing.
    /// Priority:
    /// 1. TEST_SQLSERVER_CONNECTION_STRING environment variable
    /// 2. SQLSERVER_TEST_CONNECTION_STRING environment variable
    /// 3. ConnectionStrings__TestConnection environment variable
    /// 4. Default local SQL Server Express connection string if RUN_SQLSERVER_TESTS=true
    /// Returns null if not explicitly configured.
    /// </summary>
    public static string? GetConnectionString()
    {
        var connStr = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__TestConnection");

        if (!string.IsNullOrWhiteSpace(connStr))
        {
            ValidateDatabaseIsolation(connStr);
            return connStr;
        }

        var runFlag = Environment.GetEnvironmentVariable("RUN_SQLSERVER_TESTS");
        if (string.Equals(runFlag, "true", StringComparison.OrdinalIgnoreCase) || runFlag == "1")
        {
            ValidateDatabaseIsolation(DefaultLocalConnectionString);
            return DefaultLocalConnectionString;
        }

        return null;
    }

    /// <summary>
    /// Strict safety validation: Ensures the target database is explicitly named 'FptAlumni_Guideline_Test_Db'.
    /// Throws InvalidOperationException if connecting to any other database.
    /// </summary>
    public static void ValidateDatabaseIsolation(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;

        if (!string.Equals(database, TargetDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database isolation safety check FAILED! The integration test suite is strictly restricted " +
                $"to the dedicated disposable database '{TargetDatabaseName}'. Configured target was: '{database}'. " +
                $"Tests will NOT execute to prevent accidental modification of domain databases.");
        }
    }

    /// <summary>
    /// Evaluates whether SQL Server integration testing is eligible to run.
    /// Returns (true, null) if connection string is configured and SQL Server is reachable.
    /// Otherwise returns (false, skipReason).
    /// </summary>
    public static (bool IsEligible, string? SkipReason) EvaluateEligibility()
    {
        string? connectionString;
        try
        {
            connectionString = GetConnectionString();
        }
        catch (Exception ex)
        {
            return (false, $"SQL Server integration tests BLOCKED by safety check: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false,
                "SQL Server integration tests skipped: Test configuration not provided. " +
                "Set environment variable 'TEST_SQLSERVER_CONNECTION_STRING' or 'RUN_SQLSERVER_TESTS=true' to execute real SQL Server tests.");
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false,
                $"SQL Server integration tests skipped: SQL Server instance is unreachable or connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a fresh ChatServiceDbContext configured to use the real SQL Server test database.
    /// NEVER uses InMemory database.
    /// </summary>
    public static ChatServiceDbContext CreateDbContext()
    {
        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Cannot create SQL Server DbContext: connection string is not configured.");
        }

        ValidateDatabaseIsolation(connectionString);

        var options = new DbContextOptionsBuilder<ChatServiceDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ChatServiceDbContext(options);
    }

    /// <summary>
    /// Applies EF Core migrations to the test database if not already applied in this test process.
    /// </summary>
    public static async Task EnsureDatabaseMigratedAsync()
    {
        if (_migrationApplied) return;

        lock (MigrationLock)
        {
            if (_migrationApplied) return;

            using var dbContext = CreateDbContext();
            dbContext.Database.Migrate();
            _migrationApplied = true;
        }

        await Task.CompletedTask;
    }
}
