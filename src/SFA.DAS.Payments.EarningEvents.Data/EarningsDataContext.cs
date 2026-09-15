using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.EarningEvents.Data.Configuration;
using SFA.DAS.Payments.EarningEvents.Model;
using SFA.DAS.Payments.Model.Core.Audit;

namespace SFA.DAS.Payments.EarningEvents.Data
{
    public class EarningsDataContext : DbContext, IEarningsDataContext
    {
        private readonly string connectionString;

        public DbSet<GrowthAndSkillsEarningModel> GrowthAndSkillsEarnings { get; set; }
        public DbSet<GrowthAndSkillsEarningPricePeriodModel> GrowthAndSkillsEarningPricePeriods { get; set; }
        public DbSet<GrowthAndSkillsEarningsProcessingModel> GrowthAndSkillsEarningsProcessing { get; set; }


        public EarningsDataContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public EarningsDataContext(DbContextOptions<EarningsDataContext> options)
            : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Payments2");
            modelBuilder.ApplyConfiguration(new GrowthAndSkillsEarningModelConfiguration());
            modelBuilder.ApplyConfiguration(new GrowthAndSkillsEarningPricePeriodModelConfiguration());
            modelBuilder.ApplyConfiguration(new GrowthAndSkillsEarningsProcessingModelConfiguration());
        }

    }
}
