using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.DataBase.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasOne(o => o.Store).WithMany(s => s.Offers).HasForeignKey(o => o.StoreId);
        builder.Property(o => o.Price).HasColumnType("NUMERIC").IsRequired();
        builder.Property(o => o.Url).HasMaxLength(512).IsRequired();
        builder.HasCheckConstraint("CK_Offer_Price", "Price >= 0 AND Price <= 999999");
    }
}
