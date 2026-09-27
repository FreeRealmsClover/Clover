using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Sanctuary.Database.Entities;

namespace Sanctuary.Database.Sqlite.Configuration;

public sealed class DbPendingModActionConfiguration : IEntityTypeConfiguration<DbPendingModAction>
{
    public void Configure(EntityTypeBuilder<DbPendingModAction> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).IsRequired().ValueGeneratedOnAdd();

        builder.HasIndex(a => a.Processed);

        builder.Property(a => a.ActionType).IsRequired().HasMaxLength(16);
        builder.Property(a => a.TargetUserId).IsRequired();
        builder.Property(a => a.Until).IsRequired(false);
        builder.Property(a => a.IssuedBy).IsRequired().HasMaxLength(64);

        builder.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("DATE()");
        builder.Property(a => a.Processed).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.ProcessedAt).IsRequired(false);
    }
}
