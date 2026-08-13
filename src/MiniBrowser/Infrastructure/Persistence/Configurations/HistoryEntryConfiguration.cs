using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBrowser.Core.Models;

namespace MiniBrowser.Infrastructure.Persistence.Configurations;

public sealed class HistoryEntryConfiguration : IEntityTypeConfiguration<HistoryEntry>
{
    public void Configure(EntityTypeBuilder<HistoryEntry> builder)
    {
        builder.ToTable("HistoryEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(e => e.Title)
            .HasMaxLength(512);

        builder.Property(e => e.VisitedAt)
            .IsRequired();
    }
}