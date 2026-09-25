using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Sanctuary.Database.MySql;

public sealed class MySqlDatabaseFactory : IDbContextFactory<DatabaseContext>, IDesignTimeDbContextFactory<MySqlDatabaseContext>
{
    private readonly DbContextOptions<DatabaseContext> _options = null!;

    public MySqlDatabaseFactory()
    {
    }

    public MySqlDatabaseFactory(DbContextOptions<DatabaseContext> options)
    {
        _options = options;
    }

    public DatabaseContext CreateDbContext()
    {
        return new MySqlDatabaseContext(_options);
    }

    public MySqlDatabaseContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
                .AddUserSecrets<MySqlDatabaseContext>()
                .Build();

        var databaseOptions = configuration.GetSection(DatabaseOptions.Section).Get<DatabaseOptions>();

        // Falls back to Database__Provider / Database__ConnectionString /
        // Database__VersionString env vars (the same names the app itself
        // reads at runtime) when User Secrets don't provide these - lets
        // `dotnet ef` commands run from a one-off container (pointed at the
        // real DB via env vars) without needing secrets configured there too.
        databaseOptions ??= new DatabaseOptions
        {
            Provider = Enum.Parse<DatabaseProvider>(Environment.GetEnvironmentVariable("Database__Provider") ?? "0"),
            ConnectionString = Environment.GetEnvironmentVariable("Database__ConnectionString")!,
            VersionString = Environment.GetEnvironmentVariable("Database__VersionString"),
        };

        ArgumentNullException.ThrowIfNull(databaseOptions);
        ArgumentException.ThrowIfNullOrEmpty(databaseOptions.ConnectionString);

        var builder = new DbContextOptionsBuilder();

        CreateInstance(builder, databaseOptions);

        return new MySqlDatabaseContext(builder.Options);
    }

    public static DbContextOptionsBuilder CreateInstance(DbContextOptionsBuilder builder, DatabaseOptions databaseOptions)
    {
        return builder.UseMySql(databaseOptions.ConnectionString,
            ServerVersion.Parse(databaseOptions.VersionString),
            options =>
            {
                options.EnableRetryOnFailure();
            });
    }
}