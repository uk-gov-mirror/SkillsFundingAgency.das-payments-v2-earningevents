using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Model.Core.Incentives;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Mapping
{
    public interface IGSLFunctionalSkillMapper: IGrowthAndSkillsMapper
    {
        void Map(CalculateGrowthAndSkillsPayments source, CollectionPeriodModel collectionPeriod, GSLFunctionalSkillEarningsEvent destination);
    }

    public class GSLFunctionalSkillMapper : GrowthAndSkillsMapper, IGSLFunctionalSkillMapper
    {
        public void Map(CalculateGrowthAndSkillsPayments source, CollectionPeriodModel collectionPeriod, GSLFunctionalSkillEarningsEvent destination)
        {
            destination.JobId = 0;
            destination.ExternalEarningsId = source.EarningsId;
            destination.FundingPlatformType = FundingPlatformType.DigitalApprenticeshipService;
            destination.Ukprn = source.UKPRN;
            destination.CollectionYear = collectionPeriod.AcademicYear;
            destination.ContractType = ContractType.Act1;
            destination.PriceEpisodes = new List<Payments.Model.Core.PriceEpisode>();
            destination.CollectionPeriod = new Payments.Model.Core.CollectionPeriod { AcademicYear = collectionPeriod.AcademicYear, Period = collectionPeriod.Period };
            destination.StartDate = source.Training.StartDate;
            destination.AgeAtStartOfLearning= source.Training.AgeAtStartOfTraining;            
            destination.Learner = new Payments.Model.Core.Learner
            {
                Uln = source.Learner.ULN,
                ReferenceNumber = source.Learner.Reference                
            };
            destination.LearningAim = new Payments.Model.Core.LearningAim
            {
                Reference = source.Training.CourseReference,
                StandardCode = int.Parse(source.Training.CourseCode),
                ProgrammeType = 25,
                FrameworkCode = 0,
                PathwayCode = 0,
                StartDate = source.Training.StartDate,
                CourseCode = source.Training.CourseCode,
                FundingLineType = GetFundingLineType(source.Training.AgeAtStartOfTraining),                
                LearningType = Payments.Model.Core.Entities.LearningType.MathsAndEnglish,
            };
            var earnings = source.Earnings.Where(e => e.AcademicYear == collectionPeriod.AcademicYear)
                .SelectMany(e => e.PricePeriods)
                .SelectMany(pp => pp.Periods)
                .GroupBy(period => period.EarningType)
                .Select(group => new FunctionalSkillEarning { 
                    Type = Convert(group.Key),
                    Periods = group.Select(period => new Payments.Model.Core.EarningPeriod
                    {
                        AccountId = period.Employer.AccountId,
                        TransferSenderAccountId = period.Employer.FundingAccountId,
                        ApprenticeshipEmployerType = Convert(period.Employer.EmployerType),
                        ApprenticeshipId = period.LearningId,
                        //AgreedOnDate = 
                        Period = period.DeliveryPeriod,
                        Amount = period.Amount,
                        SfaContributionPercentage = 1,
                    }).ToList().AsReadOnly()
                }).ToList();
            destination.Earnings = earnings.AsReadOnly();
        }

        private FunctionalSkillType Convert(EarningType earningType)
        {
            return earningType switch
            {
                EarningType.OnProgrammeMathsAndEnglish => FunctionalSkillType.OnProgrammeMathsAndEnglish,
                EarningType.BalancingMathsAndEnglish => FunctionalSkillType.BalancingMathsAndEnglish,
                EarningType.LearningSupport => FunctionalSkillType.LearningSupport,
                _ => throw new ArgumentOutOfRangeException(nameof(earningType), $"Unsupported functional skill earning type: {earningType}"),
            };
        }

        private ApprenticeshipEmployerType Convert(EmployerType employerType)
        {
            return employerType switch
            {
                EmployerType.Levy => ApprenticeshipEmployerType.Levy,
                EmployerType.NonLevy => ApprenticeshipEmployerType.NonLevy,
                _ => throw new ArgumentOutOfRangeException(nameof(employerType), $"Unsupported employer type: {employerType}"),
            };
        }

        private string GetFundingLineType(byte ageAtStartOfLearning)
        {
            return ageAtStartOfLearning switch
            {
                (15 or 16 or 17 or 18) => "16-18 Apprenticeship (Employer on App Service)",
                _ => "19+ Apprenticeship (Employer on App Service)"
            };
        }
    }
}