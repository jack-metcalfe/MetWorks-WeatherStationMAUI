namespace MetWorks.Common.Logging.Tests;

public class LoggerPostgreSQLTests
{
    [Fact]
    public void ApplyFailFastTimeouts_WhenNoTimeoutsSpecified_AddsDefaults()
    {
        // Arrange
        // Deliberately omit Timeout/CommandTimeout.
        var raw = "Host=127.0.0.1;Username=postgres;Password=postgres;Database=postgres";

        // Act
        var logger = new LoggerPostgreSQL();

        // We can't call the private helper directly; we validate behavior via InitializeAsync's stored ConnectionString.
        // This uses a minimal fake setting repo via the real SettingRepository is too heavy here;
        // instead, assert that NpgsqlConnectionStringBuilder can parse the connection string after init.
        //
        // NOTE: This test focuses on the connection-string transformation behavior being present in the built logger.
        var csb = new Npgsql.NpgsqlConnectionStringBuilder(raw)
        {
            Timeout = 0,
            CommandTimeout = 0
        };

        var applied = InvokeApplyFailFastTimeoutsViaReflection(csb.ConnectionString);
        var appliedCsb = new Npgsql.NpgsqlConnectionStringBuilder(applied);

        // Assert
        Assert.True(appliedCsb.Timeout > 0);
        Assert.True(appliedCsb.CommandTimeout > 0);
    }


    static string InvokeApplyFailFastTimeoutsViaReflection(string connectionString)
    {
        var mi = typeof(LoggerPostgreSQL)
            .GetMethod("ApplyFailFastTimeouts", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(mi);

        var result = mi!.Invoke(null, new object?[] { connectionString });
        Assert.IsType<string>(result);
        return (string)result;
    }
}
