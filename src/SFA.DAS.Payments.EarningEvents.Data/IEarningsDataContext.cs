
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.EarningEvents.Model;

namespace SFA.DAS.Payments.EarningEvents.Data
{
    public interface IEarningsDataContext
    {
        DbSet<GrowthAndSkillsEarningModel> GrowthAndSkillsEarnings { get; set; }
        DbSet<GrowthAndSkillsEarningPricePeriodModel> GrowthAndSkillsEarningPricePeriods { get; set; }
        DbSet<GrowthAndSkillsEarningsProcessingModel> GrowthAndSkillsEarningsProcessing { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        int SaveChanges();
    }
}
