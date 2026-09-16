using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.Payments.EarningEvents.Model;
using SFA.DAS.Payments.Model.Core.Audit;

namespace SFA.DAS.Payments.EarningEvents.Data.Configuration
{
    public class GrowthAndSkillsEarningPricePeriodModelConfiguration : IEntityTypeConfiguration<GrowthAndSkillsEarningPricePeriodModel>
    {
        public void Configure(EntityTypeBuilder<GrowthAndSkillsEarningPricePeriodModel> builder)
        {
            builder.ToTable("GrowthAndSkillsEarningPricePeriod", "Payments2");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id");
            builder.Property(x => x.GrowthAndSkillsEarningsId).HasColumnName("GrowthAndSkillsEarningsId").IsRequired();
            builder.Property(x => x.Price).HasColumnName("Price").IsRequired().HasColumnType("decimal(15,5)");
            builder.Property(x => x.StartDate).HasColumnName("StartDate").IsRequired();
            builder.Property(x => x.EndDate).HasColumnName("EndDate");
            builder.Property(x => x.DeliveryPeriod).HasColumnName("DeliveryPeriod").IsRequired();
            builder.Property(x => x.AcademicYear).HasColumnName("AcademicYear").IsRequired();
            builder.Property(x => x.EarningType).HasColumnName("EarningType").IsRequired();
            builder.Property(x => x.Amount).HasColumnName("Amount").IsRequired().HasColumnType("decimal(15,5)");
            builder.Property(x => x.EmployerAccountId).HasColumnName("EmployerAccountId").IsRequired();
            builder.Property(x => x.EmployerType).HasColumnName("EmployerType").IsRequired();
            builder.Property(x => x.FundingAccountId).HasColumnName("FundingAccountId").IsRequired();
            builder.Property(x => x.ProcessedOn).HasColumnName("ProcessedOn");
            builder.Property(x => x.ApprenticeshipId).HasColumnName("ApprenticeshipId");
            builder.HasOne(x => x.GrowthAndSkillsEarning)
                .WithMany(x => x.PricePeriods)
                .HasForeignKey(x => x.GrowthAndSkillsEarningsId)
                .HasPrincipalKey(x => x.EarningsId);
        }
    }
}
