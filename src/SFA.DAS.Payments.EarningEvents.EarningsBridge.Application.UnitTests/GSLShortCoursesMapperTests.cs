using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Mapping;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.Model.Core.Entities;
using Common = SFA.DAS.Payments.Model.Core;
using CourseType = SFA.DAS.Payments.EarningEvents.Messages.External.CourseType;
using EarningType = SFA.DAS.Payments.EarningEvents.Messages.External.EarningType;
using EmployerType = SFA.DAS.Payments.EarningEvents.Messages.External.EmployerType;
using LearningType = SFA.DAS.Payments.EarningEvents.Messages.External.LearningType;
using TrainingStatus = SFA.DAS.Payments.EarningEvents.Messages.External.TrainingStatus;
using UUIDNext;
using UUIDNext.Tools;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.UnitTests
{

    [TestFixture]
    public class GSLShortCoursesMapperTests
    {
        private CalculateGrowthAndSkillsPayments _message;
        private GSLShortCoursesMapper _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new GSLShortCoursesMapper();

            _message = new CalculateGrowthAndSkillsPayments
            {
                EarningsId = Guid.NewGuid(),
                EmployerContribution = 1000m,
                UKPRN = 10002233,
                Training = new Training
                {
                    CourseCode = "123456",
                    CourseType = CourseType.ShortCourse,
                    CourseReference = "ZSC00123",
                    LearningType = LearningType.ApprenticeshipUnit,
                    StartDate = new DateTime(2026, 1, 1),
                    TrainingStatus = TrainingStatus.Continuing,
                    AgeAtStartOfTraining = 25,
                    PlannedEndDate = new DateTime(2026, 1, 15),
                    ActualEndDate = new DateTime(2026, 1, 31),
                    LearningKey = Guid.NewGuid()
                },
                Learner = new Learner
                {
                    ULN = 12345678,
                    Reference = "LEARNREF001",
                    LearnerKey = Guid.NewGuid()
                },
                Earnings = new List<Earnings>
                {
                    new Earnings
                    {
                        AcademicYear = 2526,
                        PricePeriods = new List<PricePeriod>
                        {
                            new PricePeriod
                            {
                                StartDate = new DateTime(2026, 1, 1),
                                Price = 5000m,
                                EndDate = new DateTime(2026, 1, 31),
                                CompletionAmount = 1000m,
                                InstalmentAmount = 2000m,
                                NumberOfInstalments = 2,
                                Periods = new List<EarningPeriod>
                                {
                                    new EarningPeriod
                                    {
                                        Employer = new Employer
                                        {
                                            EmployerType = EmployerType.Levy,
                                            AccountId = 10000,
                                            FundingAccountId = 10000
                                        },
                                        Amount = 2000m,
                                        DeliveryPeriod = 1,
                                        EarningType = EarningType.Milestone1,
                                        LearningId = 123456
                                    }
                                }
                            },
                            new PricePeriod
                            {
                                StartDate = new DateTime(2026, 2, 1),
                                Price = 4500m,
                                EndDate = new DateTime(2026, 2, 28),
                                CompletionAmount = 1500m,
                                InstalmentAmount = 1500m,
                                NumberOfInstalments = 3,
                                Periods = new List<EarningPeriod>
                                {
                                    new EarningPeriod
                                    {
                                        Employer = new Employer
                                        {
                                            EmployerType = EmployerType.Levy,
                                            AccountId = 10001,
                                            FundingAccountId = 10001
                                        },
                                        Amount = 1500m,
                                        DeliveryPeriod = 2,
                                        EarningType = EarningType.Completion,
                                        LearningId = 123456
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        [Test]
        public void Properties_are_mapped_from_inbound_message_to_short_course_earning_events()
        {
            // Arrange
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };
            
            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);
            
            // Assert
            var earningEvent = earningEvents.Single();
            var expectedFundingLineType = "GSO Short Courses (Apprenticeship Units) Levy";
            VerifyEarningsAndPricePeriods(earningEvent, expectedFundingLineType, collectionPeriods[0].Period, 2526);
        }

        [Test]
        public void Properties_are_mapped_from_inbound_message_to_short_course_earning_events_over_multiple_academic_years()
        {
            // Arrange
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 14,
                    Status = CollectionPeriodStatus.Open
                },
                new CollectionPeriodModel
                {
                    AcademicYear = 2627,
                    Period = 2,
                    Status = CollectionPeriodStatus.Open
                }
            };

            _message.Earnings = new List<Earnings>
            {
                new Earnings
                {
                    AcademicYear = 2526,
                    PricePeriods = new List<PricePeriod>
                    {
                        new PricePeriod
                        {
                            StartDate = new DateTime(2026, 1, 1),
                            Price = 5000m,
                            EndDate = new DateTime(2026, 1, 31),
                            CompletionAmount = 1000m,
                            InstalmentAmount = 2000m,
                            NumberOfInstalments = 2,
                            Periods = new List<EarningPeriod>
                            {
                                new EarningPeriod
                                {
                                    Employer = new Employer
                                    {
                                        EmployerType = EmployerType.Levy,
                                        AccountId = 10000,
                                        FundingAccountId = 10000
                                    },
                                    Amount = 2000m,
                                    DeliveryPeriod = 1,
                                    EarningType = EarningType.Milestone1,
                                    LearningId = 123456
                                }
                            }
                        }
                    }
                },
                new Earnings
                {
                    AcademicYear = 2627,
                    PricePeriods = new List<PricePeriod>
                    {
                        new PricePeriod
                        {
                            StartDate = new DateTime(2027, 1, 1),
                            Price = 4000m,
                            EndDate = new DateTime(2027, 1, 31),
                            CompletionAmount = 1500m,
                            InstalmentAmount = 1000m,
                            NumberOfInstalments = 2,
                            Periods = new List<EarningPeriod>
                            {
                                new EarningPeriod
                                {
                                    Employer = new Employer
                                    {
                                        EmployerType = EmployerType.Levy,
                                        AccountId = 10000,
                                        FundingAccountId = 10000
                                    },
                                    Amount = 2000m,
                                    DeliveryPeriod = 1,
                                    EarningType = EarningType.Completion,
                                    LearningId = 123456
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            earningEvents.Should().HaveCount(2);
            var firstEarningEvent = earningEvents.Single(x => x.CollectionPeriod.AcademicYear == 2526);
            var secondEarningEvent = earningEvents.Single(x => x.CollectionPeriod.AcademicYear == 2627);

            var expectedFundingLineType = "GSO Short Courses (Apprenticeship Units) Levy";
            VerifyEarningsAndPricePeriods(firstEarningEvent, expectedFundingLineType, collectionPeriods[0].Period, 2526);
            VerifyEarningsAndPricePeriods(secondEarningEvent, expectedFundingLineType, collectionPeriods[1].Period, 2627);
        }

        [Test]
        public void Multiple_earning_periods_within_the_same_price_period_are_mapped()
        {
            // Arrange
            _message.Earnings = new List<Earnings>
            {
                new Earnings
                {
                    AcademicYear = 2526,
                    PricePeriods = new List<PricePeriod>
                    {
                        new PricePeriod
                        {
                            StartDate = new DateTime(2026, 1, 1),
                            Price = 5000m,
                            EndDate = new DateTime(2026, 1, 31),
                            CompletionAmount = 1000m,
                            InstalmentAmount = 2000m,
                            NumberOfInstalments = 2,
                            Periods = new List<EarningPeriod>
                            {
                                new EarningPeriod
                                {
                                    Employer = new Employer
                                    {
                                        EmployerType = EmployerType.Levy,
                                        AccountId = 10000,
                                        FundingAccountId = 10000
                                    },
                                    Amount = 2000m,
                                    DeliveryPeriod = 1,
                                    EarningType = EarningType.Milestone1,
                                    LearningId = 123456
                                },
                                new EarningPeriod
                                {
                                    Employer = new Employer
                                    {
                                        EmployerType = EmployerType.Levy,
                                        AccountId = 10001,
                                        FundingAccountId = 10001
                                    },
                                    Amount = 1500m,
                                    DeliveryPeriod = 2,
                                    EarningType = EarningType.Completion,
                                    LearningId = 654321
                                }
                            }
                        }
                    }
                }
            };

            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };

            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            earningEvents.Should().ContainSingle();
            var earningEvent = earningEvents.Single();
            var expectedFundingLineType = "GSO Short Courses (Apprenticeship Units) Levy";
            VerifyEarningsAndPricePeriods(earningEvent, expectedFundingLineType, collectionPeriods[0].Period, 2526);
        }

        [Test]
        public void TrainingStatus_is_mapped_correctly_for_completed_courses()
        {
            // Arrange
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };
            _message.Training.TrainingStatus = TrainingStatus.Completed;

            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            earningEvents.Single().PriceEpisodes.Should().OnlyContain(x => x.Completed);
        }

        [Test]
        public void StandardCode_is_zero_when_course_type_is_short_course()
        {
            // Arrange
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };

            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            earningEvents.Single().LearningAim.StandardCode.Should().Be(0);
        }

        [Test]
        public void FundingLineType_is_mapped_correctly_for_non_levy_employers()
        {
            // Arrange
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };
            foreach (var earning in _message.Earnings)
            {
                foreach (var pricePeriod in earning.PricePeriods)
                {
                    foreach (var earningPeriod in pricePeriod.Periods)
                    {
                        earningPeriod.Employer.EmployerType = EmployerType.NonLevy;
                    }
                }
            }

            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            var earningEvent = earningEvents.Single();
            var expectedFundingLineType = "GSO Short Courses (Apprenticeship Units) Non-Levy";
            foreach (var priceEpisode in earningEvent.PriceEpisodes)
            {
                priceEpisode.FundingLineType.Should().Be(expectedFundingLineType);
            }
        }

        [Test]
        public void SfaContributionPercentage_is_mapped_correctly_for_non_levy_employers()
        {
            // Arrange
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };
            foreach (var earning in _message.Earnings)
            {
                foreach (var pricePeriod in earning.PricePeriods)
                {
                    foreach (var earningPeriod in pricePeriod.Periods)
                    {
                        earningPeriod.Employer.EmployerType = EmployerType.NonLevy;
                    }
                }
            }

            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            var earningEvent = earningEvents.Single();
            foreach (var earning in earningEvent.Earnings)
            {
                foreach (var earningPeriod in earning.Periods)
                {
                    earningPeriod.SfaContributionPercentage.Should().Be(1m);
                }
            }
        }

        [Test]
        [TestCase("2026/07/31", 24, 0.95)]
        [TestCase("2026/07/31", 25, 0.95)]
        [TestCase("2026/08/01", 24, 1.0)]
        [TestCase("2026/08/01", 25, 0.75)]
        public void Levy_Employers_Sfa_Contribution_Set_To_2026_Rules(DateTime startDate, byte apprenticeAge, double expectedContribution)
        {
            // Arrange
            decimal sfaContrib = (decimal)expectedContribution;

            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new()
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };

            // Act
            _message.Training.StartDate = startDate;
            _message.Training.AgeAtStartOfTraining = apprenticeAge;
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            var earningEvent = earningEvents.First();
            foreach (var earning in earningEvent.Earnings)
            {
                foreach (var earningPeriod in earning.Periods)
                {
                    earningPeriod.SfaContributionPercentage.Should().Be(sfaContrib);
                }
            }
        }

        [Test]
        public void TransferSenderAccountId_is_mapped_correctly_when_funding_account_id_is_different_to_employer_account_id()
        {
            // Arrange
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };
            foreach (var earning in _message.Earnings)
            {
                foreach (var pricePeriod in earning.PricePeriods)
                {
                    foreach (var earningPeriod in pricePeriod.Periods)
                    {
                        earningPeriod.Employer.FundingAccountId = 1234567;
                    }
                }
            }

            // Act
            var earningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods);

            // Assert
            var earningEvent = earningEvents.Single();
            foreach (var earning in earningEvent.Earnings)
            {
                foreach (var earningPeriod in earning.Periods)
                {
                    earningPeriod.TransferSenderAccountId.Should().Be(1234567);
                }
            }
        }

        [Test]
        public void Subsequent_messages_generate_an_event_id_that_is_sortable()
        {
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };

            var firstEarningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods).ToList();

            Thread.Sleep(100);

            var secondEarningEvents = _sut.MapToShortCourseEarningEvents(_message, collectionPeriods).ToList();

            firstEarningEvents.Should().ContainSingle();
            secondEarningEvents.Should().ContainSingle();
            firstEarningEvents[0].EventId.Should().NotBe(secondEarningEvents[0].EventId);

            var firstEventIdDecodesToTimestamp = UuidDecoder.TryDecodeTimestamp(firstEarningEvents[0].EventId, out var firstEventDateTime);
            var secondEventIdDecodesToTimestamp = UuidDecoder.TryDecodeTimestamp(secondEarningEvents[0].EventId, out var secondEventDateTime);
            firstEventIdDecodesToTimestamp.Should().BeTrue();
            secondEventIdDecodesToTimestamp.Should().BeTrue();
            secondEventDateTime.Should().BeAfter(firstEventDateTime);
        }

        private void VerifyEarningsAndPricePeriods(GSLShortCourseEarningsEvent earningEvent,
                                                   string expectedFundingLineType, byte collectionPeriod, short academicYear)
        {
            var expectedEarnings = _message.Earnings.Where(x => x.AcademicYear == academicYear).ToList();

            VerifyEventHeader(earningEvent, collectionPeriod, academicYear);
            VerifyPriceEpisodes(earningEvent, expectedFundingLineType, expectedEarnings);
            VerifyEarnings(earningEvent, expectedEarnings);
        }

        private void VerifyEventHeader(GSLShortCourseEarningsEvent earningEvent, byte collectionPeriod, short academicYear)
        {
            earningEvent.JobId.Should().Be(0);
            earningEvent.EventTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
            earningEvent.EventId.Should().NotBe(Guid.Empty);
            earningEvent.ExternalEarningsId.Should().Be(_message.EarningsId);
            earningEvent.Ukprn.Should().Be(_message.UKPRN);
            earningEvent.Learner.ReferenceNumber.Should().Be(_message.Learner.Reference);
            earningEvent.Learner.Uln.Should().Be(_message.Learner.ULN);
            earningEvent.LearningAim.Reference.Should().Be(_message.Training.CourseReference);
            earningEvent.LearningAim.ProgrammeType.Should().Be(0);
            earningEvent.LearningAim.StandardCode.Should().Be(0);
            earningEvent.LearningAim.CourseCode.Should().Be(_message.Training.CourseCode);
            earningEvent.LearningAim.FrameworkCode.Should().Be(0);
            earningEvent.LearningAim.PathwayCode.Should().Be(0);
            earningEvent.LearningAim.FundingLineType.Should().Be("");
            earningEvent.LearningAim.SequenceNumber.Should().Be(0);
            earningEvent.LearningAim.StartDate.Should().Be(_message.Training.StartDate);
            earningEvent.LearningAim.LearningType.Should().Be((Common.Entities.LearningType)_message.Training.LearningType);
            earningEvent.AgeAtStartOfLearning.Should().Be(_message.Training.AgeAtStartOfTraining);
            earningEvent.FundingPlatformType.Should().Be(FundingPlatformType.DigitalApprenticeshipService);
            earningEvent.CollectionPeriod.AcademicYear.Should().Be(academicYear);
            earningEvent.CollectionPeriod.Period.Should().Be(collectionPeriod);
            earningEvent.IlrSubmissionDateTime.Should().Be(SqlDateTime.MinValue.Value);
        }

        private void VerifyPriceEpisodes(GSLShortCourseEarningsEvent earningEvent, string expectedFundingLineType, IReadOnlyCollection<Earnings> expectedEarnings)
        {
            var eventPriceEpisodes = earningEvent.PriceEpisodes.ToArray();
            var expectedPricePeriods = expectedEarnings.SelectMany(earning => earning.PricePeriods);
            var expectedPriceEpisodeCount = expectedPricePeriods.Sum(pricePeriod => pricePeriod.Periods.Count());

            eventPriceEpisodes.Should().HaveCount(expectedPriceEpisodeCount);

            var index = 0;

            foreach (var expectedEarning in expectedEarnings)
            {
                foreach (var expectedPricePeriod in expectedEarning.PricePeriods)
                {
                    foreach (var _ in expectedPricePeriod.Periods)
                    {
                        var mappedPriceEpisode = eventPriceEpisodes[index++];

                        mappedPriceEpisode.Identifier.Should().Be($"{_message.Training.CourseCode}-{expectedPricePeriod.StartDate}");
                        mappedPriceEpisode.AgreedPrice.Should().Be(expectedPricePeriod.Price);
                        mappedPriceEpisode.CourseStartDate.Should().Be(_message.Training.StartDate);
                        mappedPriceEpisode.EffectiveTotalNegotiatedPriceStartDate.Should().Be(_message.Training.StartDate);
                        mappedPriceEpisode.PlannedEndDate.Should().Be(_message.Training.PlannedEndDate);
                        mappedPriceEpisode.ActualEndDate.Should().Be(_message.Training.ActualEndDate);
                        mappedPriceEpisode.NumberOfInstalments.Should().Be(expectedPricePeriod.NumberOfInstalments);
                        mappedPriceEpisode.InstalmentAmount.Should().Be(expectedPricePeriod.InstalmentAmount);
                        mappedPriceEpisode.CompletionAmount.Should().Be(expectedPricePeriod.CompletionAmount);
                        mappedPriceEpisode.Completed.Should().BeFalse();
                        mappedPriceEpisode.FundingLineType.Should().Be(expectedFundingLineType);
                    }
                }
            }
        }

        private void VerifyEarnings(GSLShortCourseEarningsEvent earningEvent, IReadOnlyCollection<Earnings> expectedEarnings)
        {
            var eventEarnings = earningEvent.Earnings.ToArray();
            var expectedPricePeriods = expectedEarnings.SelectMany(earning => earning.PricePeriods);
            var expectedEarningCount = expectedPricePeriods.Sum(pricePeriod => pricePeriod.Periods.Count());

            eventEarnings.Should().HaveCount(expectedEarningCount); 

            var index = 0;

            foreach (var expectedEarning in expectedEarnings)
            {
                foreach (var expectedPricePeriod in expectedEarning.PricePeriods)
                {
                    foreach (var expectedEarningPeriod in expectedPricePeriod.Periods)
                    {
                        var mappedEarning = eventEarnings[index++];

                        ((int)mappedEarning.Type).Should().Be((int)expectedEarningPeriod.EarningType);
                        mappedEarning.Periods.Should().ContainSingle();

                        var mappedEarningPeriod = mappedEarning.Periods.Single();
                        mappedEarningPeriod.AccountId.Should().Be(expectedEarningPeriod.Employer.AccountId);
                        mappedEarningPeriod.Amount.Should().Be(expectedEarningPeriod.Amount);
                        mappedEarningPeriod.TransferSenderAccountId.Should().BeNull();
                        var employerTypeValue = (int)mappedEarningPeriod.ApprenticeshipEmployerType;
                        employerTypeValue.Should().Be((int)expectedEarningPeriod.Employer.EmployerType);
                        mappedEarningPeriod.Period.Should().Be(expectedEarningPeriod.DeliveryPeriod);
                        mappedEarningPeriod.SfaContributionPercentage.Should().Be(0.95m);
                        mappedEarningPeriod.ApprenticeshipId.Should().Be(expectedEarningPeriod.LearningId);
                        mappedEarningPeriod.PriceEpisodeIdentifier.Should().Be($"{_message.Training.CourseCode}-{expectedPricePeriod.StartDate}");
                    }
                }
            }
        }
    }
}