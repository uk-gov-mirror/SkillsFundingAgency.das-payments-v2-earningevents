using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.Payments.EarningEvents.Model;

namespace SFA.DAS.Payments.EarningEvents.Data.Configuration
{
    internal class GrowthAndSkillsEarningsProcessingModelConfiguration : IEntityTypeConfiguration<GrowthAndSkillsEarningsProcessingModel>
    {
        public void Configure(EntityTypeBuilder<GrowthAndSkillsEarningsProcessingModel> builder)
        {
            builder.ToTable("GrowthAndSkillsEarningsProcessing", "Payments2");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id");
            builder.Property(x => x.GrowthAndSkillsEarningId).HasColumnName("GrowthAndSkillsEarningId").IsRequired();
            builder.Property(x => x.CollectionPeriod).HasColumnName("CollectionPeriod").IsRequired();
            builder.Property(x => x.AcademicYear).HasColumnName("AcademicYear").IsRequired();
            builder.Property(x => x.ProcessedOn).HasColumnName("ProcessedOn");
        }
    }
}
