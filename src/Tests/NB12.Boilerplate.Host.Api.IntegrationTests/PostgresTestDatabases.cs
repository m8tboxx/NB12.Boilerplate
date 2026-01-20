using Npgsql;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public sealed class PostgresTestDatabases : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        public string AuthConnectionString { get; }
        public string AuditConnectionString { get; }
        public string AuthDbName { get; }
        public string AuditDbName { get; }

        public PostgresTestDatabases(string adminConnectionString, string baseConnectionString)
        {
            _adminConnectionString = adminConnectionString;

            // neue DBs pro Testlauf
            AuthDbName = $"nb12_auth_test_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            AuditDbName = $"nb12_audit_test_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

            AuthConnectionString = $"{baseConnectionString};Database={AuthDbName}";
            AuditConnectionString = $"{baseConnectionString};Database={AuditDbName}";
        }

        public async Task CreateAsync()
        {
            await CreateDatabaseAsync(AuthDbName);
            await CreateDatabaseAsync(AuditDbName);
        }

        private async Task CreateDatabaseAsync(string dbName)
        {
            await using var conn = new NpgsqlConnection(_adminConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"CREATE DATABASE ""{dbName}""";
            await cmd.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            // Drop DBs am Ende (inkl. Terminate active connections)
            await DropDatabaseAsync(AuthDbName);
            await DropDatabaseAsync(AuditDbName);
        }

        private async Task DropDatabaseAsync(string dbName)
        {
            await using var conn = new NpgsqlConnection(_adminConnectionString);
            await conn.OpenAsync();

            // aktive Sessions killen, sonst DROP DATABASE blockiert
            await using (var kill = conn.CreateCommand())
            {
                kill.CommandText = @"
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = @db AND pid <> pg_backend_pid();";
                kill.Parameters.AddWithValue("db", dbName);
                await kill.ExecuteNonQueryAsync();
            }

            await using (var drop = conn.CreateCommand())
            {
                drop.CommandText = $@"DROP DATABASE IF EXISTS ""{dbName}""";
                await drop.ExecuteNonQueryAsync();
            }
        }
    }
}
