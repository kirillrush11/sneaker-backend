using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.DataBase.Configurations;

public class SizeAvailabilityConfiguration : IEntityTypeConfiguration<SizeAvailability>
{
    public void Configure(EntityTypeBuilder<SizeAvailability> builder)
    {
        builder.HasOne(s => s.Offer)
            .WithMany(o => o.Sizes)
            .HasForeignKey(s => s.OfferId);
    }
}
