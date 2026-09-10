using SFA.DAS.Payments.EarningEvents.Messages;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Model;
using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;
using System.Data.SqlTypes;
using UUIDNext;
using Common = SFA.DAS.Payments.Model.Core;
using EmployerType = SFA.DAS.Payments.EarningEvents.Messages.External.EmployerType;
using LearningType = SFA.DAS.Payments.Model.Core.Entities.LearningType;
using TrainingStatus = SFA.DAS.Payments.EarningEvents.Messages.External.TrainingStatus;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Mapping
{
    public class GSLShortCoursesMapper : GrowthAndSkillsMapper, IGSLShortCoursesMapper
    {
        private const int FundingRules2026AgeThreshold = 25;
        private const decimal DefaultSfaContribution = 0.95m;
        public static readonly DateTime FundingRules2026EligibilityDate = new(2026, 8, 1);

        public IEnumerable<GSLShortCourseEarningsEvent> MapToShortCourseEarningEvents(CalculateGrowthAndSkillsPayments source, IEnumerable<CollectionPeriodModel> openCollectionPeriods)
        {
            var earningEvents = new Dictionary<short, GSLShortCourseEarningsEvent>();
            var collectionPeriods = openCollectionPeriods
                .GroupBy(x => x.AcademicYear)
                .ToDictionary(x => x.Key, x => x.First()); // shouldn't have duplicates


            var earnings = source.Earnings.Where(e => collectionPeriods.ContainsKey(e.AcademicYear)).ToList();

            //Generate blank earning event for each open collection period
            if (!earnings.Any())
            {
                foreach (var collectionPeriod in collectionPeriods)
                {
                    var earningEvent = GenerateShortCourseEarningEvent(source, collectionPeriod.Key, openCollectionPeriods);
                    earningEvents.Add(earningEvent.Key, earningEvent.Value);
                }
                return earningEvents.Values.ToList();
            }

            foreach (var earning in earnings)
            {
                if (!earningEvents.ContainsKey(earning.AcademicYear))
                {
                    var earningEvent = GenerateShortCourseEarningEvent(source, earning.AcademicYear, openCollectionPeriods);
                    earningEvents.Add(earningEvent.Key, earningEvent.Value);
                }
            }

            foreach (var collectionPeriod in openCollectionPeriods)
            {
                if (earningEvents.ContainsKey(collectionPeriod.AcademicYear))
                {
                    earningEvents[collectionPeriod.AcademicYear].Earnings = MapToEarnings(source, collectionPeriod.AcademicYear);
                    earningEvents[collectionPeriod.AcademicYear].PriceEpisodes = MapToEarningEventPriceEpisodes(source, collectionPeriod.AcademicYear);
                }
            }

            return earningEvents.Values.ToList();
        }

        private List<Common.PriceEpisode> MapToEarningEventPriceEpisodes(CalculateGrowthAndSkillsPayments source, short academicYear)
        {
            var priceEpisodes = new List<Common.PriceEpisode>();

            foreach (var earning in source.Earnings.Where(x => x.AcademicYear == academicYear))
            {
                foreach (var pricePeriod in earning.PricePeriods)
                {
                    foreach (var earningPeriod in pricePeriod.Periods)
                    {
                        priceEpisodes.Add(new Common.PriceEpisode
                        {
                            Identifier = BuildPriceEpisodeIdentifier(source.Training, pricePeriod.StartDate),
                            AgreedPrice = pricePeriod.Price,
                            CourseStartDate = source.Training.StartDate,
                            StartDate = source.Training.StartDate,
                            EffectiveTotalNegotiatedPriceStartDate = source.Training.StartDate,
                            PlannedEndDate = source.Training.PlannedEndDate,
                            ActualEndDate = source.Training.ActualEndDate,
                            NumberOfInstalments = pricePeriod.NumberOfInstalments,
                            InstalmentAmount = pricePeriod.InstalmentAmount,
                            CompletionAmount = pricePeriod.CompletionAmount,
                            Completed = (source.Training.TrainingStatus == TrainingStatus.Completed),
                            FundingLineType = BuildFundingLineType(earningPeriod.Employer.EmployerType),
                        });
                    }
                }
            }

            return priceEpisodes;
        }

        private string BuildFundingLineType(EmployerType employerType)
        {
            var employerTypeText = "Levy";
            if (employerType == EmployerType.NonLevy)
            {
                employerTypeText = "Non-Levy";
            }

            return $"GSO Short Courses (Apprenticeship Units) {employerTypeText}";
        }

        private string BuildPriceEpisodeIdentifier(Training training, DateTime startDate)
        {
            return $"{training.CourseCode}-{startDate}";
        }



            private decimal? MapSfaContributionPercentage(EmployerType employerType, Training training)
        {
            
            if (employerType == EmployerType.NonLevy)
            {
                return 1m; // 100%
            }

            // If the earning event is for a levy employer and the start date is before the 2026 eligibility date, it is not eligible for recalculation.
            if (training.StartDate < FundingRules2026EligibilityDate)
            {
                return DefaultSfaContribution;
            }

            if (training.StartDate >= FundingRules2026EligibilityDate &&
                training.AgeAtStartOfTraining < FundingRules2026AgeThreshold)
            {
                return 1m; // 100% for Levy employers under 25 years old
            }
            if (training.StartDate >= FundingRules2026EligibilityDate &&
                training.AgeAtStartOfTraining >= FundingRules2026AgeThreshold)
            {
                return 0.75m; // 75% for Levy employers 25 years old and above
            }
            return DefaultSfaContribution; // 95% for Levy employers
        }

        private KeyValuePair<short, GSLShortCourseEarningsEvent> GenerateShortCourseEarningEvent(
            CalculateGrowthAndSkillsPayments source, short earningYear,
            IEnumerable<CollectionPeriodModel> openCollectionPeriods)
        {
            return new KeyValuePair<short, GSLShortCourseEarningsEvent>
            (
                earningYear, new GSLShortCourseEarningsEvent
                {
                    JobId = 0,
                    EventTime = DateTimeOffset.UtcNow,
                    EventId = Uuid.NewDatabaseFriendly(Database.SqlServer),
                    ExternalEarningsId = source.EarningsId,
                    Ukprn = source.UKPRN,
                    Learner = new Common.Learner
                    {
                        ReferenceNumber = source.Learner.Reference,
                        Uln = source.Learner.ULN
                    },
                    LearningAim = new Common.LearningAim
                    {
                        Reference = source.Training.CourseReference,
                        ProgrammeType = 0,
                        StandardCode = 0,
                        CourseCode = source.Training.CourseCode,
                        FrameworkCode = 0,
                        PathwayCode = 0,
                        FundingLineType = "",
                        SequenceNumber = 0,
                        StartDate = source.Training.StartDate,
                        LearningType = (LearningType)source.Training.LearningType
                    },
                    CollectionPeriod = new Common.CollectionPeriod
                    {
                        AcademicYear = earningYear,
                        Period = openCollectionPeriods.First(x => x.AcademicYear == earningYear).Period
                    },
                    AgeAtStartOfLearning = source.Training.AgeAtStartOfTraining,
                    FundingPlatformType = FundingPlatformType.DigitalApprenticeshipService,
                    IlrSubmissionDateTime = SqlDateTime.MinValue.Value,
                    Earnings = new List<ShortCourseEarning>(),
                    PriceEpisodes = new List<Common.PriceEpisode>()
                });
        }

        private IEnumerable<ShortCourseEarning> MapToEarnings(CalculateGrowthAndSkillsPayments source, short academicYear)
        {
            var shortCourseEarnings = new List<ShortCourseEarning>();

            foreach (var earning in source.Earnings.Where(x => x.AcademicYear == academicYear))
            {
                foreach (var pricePeriod in earning.PricePeriods)
                {
                    foreach (var period in pricePeriod.Periods)
                    {
                        shortCourseEarnings.Add(new ShortCourseEarning
                        {
                            Type = (ShortCourseEarningType)period.EarningType,
                            Periods = new List<Common.EarningPeriod>
                            {
                                    new Common.EarningPeriod
                                    {
                                        AccountId = period.Employer.AccountId,
                                        Amount = period.Amount,
                                        TransferSenderAccountId = MapTransferSenderAccountId(period),
                                        ApprenticeshipEmployerType = (ApprenticeshipEmployerType)period.Employer.EmployerType,
                                        Period = period.DeliveryPeriod,
                                        SfaContributionPercentage = MapSfaContributionPercentage(period.Employer.EmployerType, source.Training),
                                        ApprenticeshipId = period.LearningId,
                                        PriceEpisodeIdentifier = BuildPriceEpisodeIdentifier(source.Training, pricePeriod.StartDate)
                                    }
                                }
                        }
                        );
                    }
                }
            }

            return shortCourseEarnings;
        }
    }
}
