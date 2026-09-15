using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Model;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;

public interface IEarningsRepository
{
    Task SaveEarnings(GrowthAndSkillsEarningModel growthAndSkillsEarningModel);
    Task <List<GrowthAndSkillsEarningModel>> GetGrowthAndSkillsEarnings(long ukPrn, long uln, string courseCode);
    Task<List<GrowthAndSkillsEarningModel>> GetUnprocessedEarningsForCollectionPeriod(short academicYear, byte collectionPeriod);
    Task MarkEarningProcessed(Guid earningsId, short academicYear, byte collectionPeriod, DateTime processedOn);
}