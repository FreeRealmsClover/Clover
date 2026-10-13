using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sanctuary.Database.Tests;

[TestClass]
public class SqliteTest : DatabaseTestBase
{
    public required TestContext TestContext { get; set; }

    protected override void Configure(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:ConnectionString"] = "Data Source=:memory:"
        });
    }

    [TestMethod]
    public async Task IsValidAsync()
    {
        await using var dbContext = await CreateDbContextAsync(TestContext.CancellationToken);

        await dbContext.Database.MigrateAsync(TestContext.CancellationToken);

        Assert.IsTrue(await dbContext.Database.CanConnectAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task PetCustomizationMigrationMatchesCurrentModelAsync()
    {
        await using var dbContext = await CreateDbContextAsync(TestContext.CancellationToken);
        await dbContext.Database.OpenConnectionAsync(TestContext.CancellationToken);
        await dbContext.Database.MigrateAsync(TestContext.CancellationToken);

        Assert.IsFalse(dbContext.Database.HasPendingModelChanges());
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Items') WHERE name IN ('PetName', 'PetClothing')";
        Assert.AreEqual(2L, await command.ExecuteScalarAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task PetCustomizationUpgradePreservesExistingUsersAsync()
    {
        await using var dbContext = await CreateDbContextAsync(TestContext.CancellationToken);
        await dbContext.Database.OpenConnectionAsync(TestContext.CancellationToken);
        await dbContext.GetService<IMigrator>().MigrateAsync("20260927000415_AddPendingModActions", TestContext.CancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Users (Username, Password, MaxCharacters, IsMember, IsAdmin, IsMod, Created) " +
            "VALUES ('PetUpgradeProbe', 'test-only', 10, 0, 0, 0, '2026-10-13 00:00:00+00:00')", TestContext.CancellationToken);

        await dbContext.Database.MigrateAsync(TestContext.CancellationToken);
        var user = await dbContext.Users.SingleAsync(user => user.Username == "PetUpgradeProbe", TestContext.CancellationToken);
        Assert.IsFalse(user.IsLead);
        Assert.AreEqual("test-only", user.Password);
        Assert.IsFalse(dbContext.Database.HasPendingModelChanges());
    }
}
