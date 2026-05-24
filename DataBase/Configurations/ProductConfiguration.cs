using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.DataBase.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.GlobalArticle).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Brand).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Model).HasMaxLength(128).IsRequired();
        builder.Property(p => p.ImageUrl).HasMaxLength(512);
        builder.Property(p => p.Gender).HasMaxLength(16);
        builder.HasIndex(p => p.GlobalArticle).IsUnique();
    }
}
