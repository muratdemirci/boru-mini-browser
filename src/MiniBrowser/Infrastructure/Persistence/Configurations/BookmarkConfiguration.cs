using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBrowser.Core.Models;

namespace MiniBrowser.Infrastructure.Persistence.Configurations;

public sealed class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("Bookmarks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(b => b.Title)
            .HasMaxLength(512);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.HasIndex(b => b.Url)
            .IsUnique();
    }
}