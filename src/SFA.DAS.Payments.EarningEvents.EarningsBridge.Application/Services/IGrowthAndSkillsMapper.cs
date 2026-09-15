using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Model;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;

public interface IGrowthAndSkillsMapper
{
    GrowthAndSkillsEarningModel MapToGrowthAndSkillsEarningModel(CalculateGrowthAndSkillsPayments source);    
    IEnumerable<GSLShortCourseEarningsEvent> MapToShortCourseEarningEvents(CalculateGrowthAndSkillsPayments source, IEnumerable<CollectionPeriodModel> openCollectionPeriods);

    IEnumerable<CollectionPeriodModel> MapCollectionYearToCollectionPeriodModels(CollectionYear collectionYear);

    IEnumerable<DasEarningsReceivedEvent> MapToDasEarningsReceivedEvents(CalculateGrowthAndSkillsPayments source, IEnumerable<CollectionPeriodModel> openCollectionPeriods);

    CalculateGrowthAndSkillsPayments MapToCalculateGrowthAndSkillsPayments(GrowthAndSkillsEarningModel earning);
}
