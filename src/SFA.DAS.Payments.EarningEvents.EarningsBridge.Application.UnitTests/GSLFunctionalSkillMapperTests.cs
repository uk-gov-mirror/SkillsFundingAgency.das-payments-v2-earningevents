using FluentAssertions;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Mapping;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.Model.Core.Entities;
using CourseType = SFA.DAS.Payments.EarningEvents.Messages.External.CourseType;
using EarningType = SFA.DAS.Payments.EarningEvents.Messages.External.EarningType;
using EmployerType = SFA.DAS.Payments.EarningEvents.Messages.External.EmployerType;
using LearningType = SFA.DAS.Payments.EarningEvents.Messages.External.LearningType;
using TrainingStatus = SFA.DAS.Payments.EarningEvents.Messages.External.TrainingStatus;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.UnitTests
{
    [TestFixture]
    public class GSLFunctionalSkillMapperTests
    {
        private CalculateGrowthAndSkillsPayments sourceMessage;
        private GSLFunctionalSkillMapper mapper;

        [SetUp]
        public void SetUp()
        {
            mapper = new GSLFunctionalSkillMapper();
            sourceMessage = new CalculateGrowthAndSkillsPayments
            {
                EarningsId = Guid.NewGuid(),
                EmployerContribution = 1000m,
                UKPRN = 10002233,
                Training = new Training
                {
                    CourseCode = "123456",
                    CourseType = CourseType.FunctionalSkill,
                    CourseReference = "ZSC00123",
                    LearningType = LearningType.MathsAndEnglish,
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
                        AcademicYear = 2627,
                        PricePeriods = new List<PricePeriod>
                        {
                            new PricePeriod
                            {
                                StartDate = new DateTime(2026, 1, 1),
                                Price = 0,
                                EndDate = new DateTime(2026, 1, 31),
                                CompletionAmount = 0,
                                InstalmentAmount = 0,
                                NumberOfInstalments = 0,
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
                                        Amount = 600m,
                                        DeliveryPeriod = 1,
                                        EarningType = EarningType.OnProgrammeMathsAndEnglish,
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
                                        Amount = 1200m,
                                        DeliveryPeriod = 2,
                                        EarningType = EarningType.BalancingMathsAndEnglish,
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
        public void Maps_Collection_Period()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);
            
            // Assert
            destinationMessage.CollectionPeriod.AcademicYear.Should().Be(collectionPeriod.AcademicYear);
            destinationMessage.CollectionPeriod.Period.Should().Be(collectionPeriod.Period);
        }

        [Test]
        public void Maps_Funding_Platform()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.FundingPlatformType.Should().Be(FundingPlatformType.DigitalApprenticeshipService);
        }

        [Test]
        public void Maps_Start_Date()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.StartDate.Should().Be(sourceMessage.Training.StartDate);
        }

        [Test]
        public void Maps_Age_At_Start_Of_Training()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.AgeAtStartOfLearning.Should().Be(sourceMessage.Training.AgeAtStartOfTraining);
        }

        [Test]
        public void Does_Not_Map_Job_Id()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.JobId.Should().Be(0);
        }

        [Test]
        public void Does_Not_Map_Price_Episodes()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.PriceEpisodes.Should().NotBeNull();
            destinationMessage.PriceEpisodes.Should().BeEmpty();
        }

        [Test]
        public void Maps_Ukprn()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.Ukprn.Should().Be(sourceMessage.UKPRN);
        }

        [Test]
        public void Maps_ExternalEarningsId()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.ExternalEarningsId.Should().Be(sourceMessage.EarningsId);
        }

        [Test]
        public void Maps_Learner_Details()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.Learner.Should().NotBeNull();
            destinationMessage.Learner.Uln.Should().Be(sourceMessage.Learner.ULN);
            destinationMessage.Learner.ReferenceNumber.Should().Be(sourceMessage.Learner.Reference);
        }

        [Test]
        public void Maps_Contract_Type()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.ContractType.Should().Be(ContractType.Act1);
        }

        [Test]
        public void Maps_Training_Details()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.LearningAim.Should().NotBeNull();
            destinationMessage.LearningAim.Reference.Should().Be(sourceMessage.Training.CourseReference);
            destinationMessage.LearningAim.ProgrammeType.Should().Be(25);
            destinationMessage.LearningAim.StandardCode.ToString().Should().Be(sourceMessage.Training.CourseCode);
            destinationMessage.LearningAim.FrameworkCode.Should().Be(0);
            destinationMessage.LearningAim.PathwayCode.Should().Be(0);
            destinationMessage.LearningAim.SequenceNumber.Should().Be(0);
            destinationMessage.LearningAim.StartDate.Should().Be(sourceMessage.Training.StartDate);
            destinationMessage.LearningAim.LearningType.Should().Be(Payments.Model.Core.Entities.LearningType.MathsAndEnglish);
            destinationMessage.LearningAim.CourseCode.Should().Be(sourceMessage.Training.CourseCode);
        }

        [TestCase(15, "16-18 Apprenticeship (Employer on App Service)")]
        [TestCase(16, "16-18 Apprenticeship (Employer on App Service)")]
        [TestCase(17, "16-18 Apprenticeship (Employer on App Service)")]
        [TestCase(18, "16-18 Apprenticeship (Employer on App Service)")]
        [TestCase(19, "19+ Apprenticeship (Employer on App Service)")]
        [TestCase(25, "19+ Apprenticeship (Employer on App Service)")]
        public void Maps_Funding_Line_Type(byte age, string fundingLineType)
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };
            sourceMessage.Training.AgeAtStartOfTraining = age;
            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.LearningAim.Should().NotBeNull();
            destinationMessage.LearningAim.FundingLineType.Should().Be(fundingLineType);
        }

        [Test]
        public void Maps_Functional_SKill_Earnings()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };
            var onProg = new EarningPeriod
            {
                Employer = new Employer
                {
                    EmployerType = EmployerType.Levy,
                    AccountId = 10000,
                    FundingAccountId = 10000
                },
                Amount = 600m,
                DeliveryPeriod = 1,
                EarningType = EarningType.OnProgrammeMathsAndEnglish,
                LearningId = 123456
            };
            var balancing = new EarningPeriod
            {
                Employer = new Employer
                {
                    EmployerType = EmployerType.NonLevy,
                    AccountId = 10000,
                    FundingAccountId = 10000
                },
                Amount = 600m,
                DeliveryPeriod = 2,
                EarningType = EarningType.BalancingMathsAndEnglish,
                LearningId = 123456
            };

            sourceMessage.Earnings = new List<Earnings>
                {
                    new Earnings
                    {
                        AcademicYear = 2627,
                        PricePeriods = new List<PricePeriod>
                        {
                            new PricePeriod
                            {
                                StartDate = new DateTime(2026, 1, 1),
                                Price = 0,
                                EndDate = new DateTime(2026, 3, 1),
                                CompletionAmount = 0,
                                InstalmentAmount = 0,
                                NumberOfInstalments = 0,
                                Periods = new List<EarningPeriod>
                                {
                                    onProg,
                                    balancing
                                }
                            }
                        }
                    }
                };

            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.Earnings.Should().NotBeNull();
            destinationMessage.Earnings.Should().NotBeEmpty();

            var onProgEarning = destinationMessage.Earnings.FirstOrDefault(earning => earning.Type == Payments.Model.Core.Incentives.FunctionalSkillType.OnProgrammeMathsAndEnglish);
            onProgEarning.Should().NotBeNull();
            onProgEarning.Periods.Count.Should().Be(1);
            onProgEarning.Periods.First().Amount.Should().Be(onProg.Amount);
            onProgEarning.Periods.First().Period.Should().Be(onProg.DeliveryPeriod);
            onProgEarning.Periods.First().SfaContributionPercentage.Should().Be(1);
            onProgEarning.Periods.First().AccountId.Should().Be(onProg.Employer.AccountId);
            onProgEarning.Periods.First().ApprenticeshipEmployerType.Should().Be(ApprenticeshipEmployerType.Levy);


            var balancingEarning = destinationMessage.Earnings.FirstOrDefault(earning => earning.Type == Payments.Model.Core.Incentives.FunctionalSkillType.BalancingMathsAndEnglish);
            balancingEarning.Should().NotBeNull();
            balancingEarning.Periods.Count.Should().Be(1);
            balancingEarning.Periods.First().Amount.Should().Be(balancing.Amount);
            balancingEarning.Periods.First().Period.Should().Be(balancing.DeliveryPeriod);
            balancingEarning.Periods.First().SfaContributionPercentage.Should().Be(1);
            balancingEarning.Periods.First().AccountId.Should().Be(balancing.Employer.AccountId);
            balancingEarning.Periods.First().ApprenticeshipEmployerType.Should().Be(ApprenticeshipEmployerType.NonLevy);

        }

        [Test]
        public void Only_Maps_Current_Academic_Year()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel
            {
                AcademicYear = 2627,
                Period = 1,
                Status = CollectionPeriodStatus.Open
            };

            var onProg = new EarningPeriod
            {
                Employer = new Employer
                {
                    EmployerType = EmployerType.Levy,
                    AccountId = 10000,
                    FundingAccountId = 10000
                },
                Amount = 600m,
                DeliveryPeriod = 1,
                EarningType = EarningType.OnProgrammeMathsAndEnglish,
                LearningId = 123456
            };
            var balancing = new EarningPeriod
            {
                Employer = new Employer
                {
                    EmployerType = EmployerType.Levy,
                    AccountId = 10000,
                    FundingAccountId = 10000
                },
                Amount = 600m,
                DeliveryPeriod = 2,
                EarningType = EarningType.BalancingMathsAndEnglish,
                LearningId = 123456
            };
            var support = new EarningPeriod
            {
                Employer = new Employer
                {
                    EmployerType = EmployerType.Levy,
                    AccountId = 10000,
                    FundingAccountId = 10000
                },
                Amount = 600m,
                DeliveryPeriod = 2,
                EarningType = EarningType.LearningSupport,
                LearningId = 123456
            };

            sourceMessage.Earnings = new List<Earnings>
            {
                new Earnings
                {
                    AcademicYear = 2526,
                    PricePeriods = new List<PricePeriod>
                    {
                        new PricePeriod
                        {
                            StartDate = new DateTime(2026, 1, 1),
                            Price = 0,
                            EndDate = new DateTime(2026, 3, 30),
                            CompletionAmount = 0,
                            InstalmentAmount = 0,
                            NumberOfInstalments = 0,
                            Periods = new List<EarningPeriod>
                            {
                                onProg,
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
                            StartDate = new DateTime(2026, 1, 1),
                            Price = 0,
                            EndDate = new DateTime(2026, 3, 30),
                            CompletionAmount = 0,
                            InstalmentAmount = 0,
                            NumberOfInstalments = 0,
                            Periods = new List<EarningPeriod>
                            {
                                balancing,
                            }
                        }
                    }
                },                    
                new Earnings
                {
                    AcademicYear = 2728,
                    PricePeriods = new List<PricePeriod>
                    {
                        new PricePeriod
                        {
                            StartDate = new DateTime(2026, 1, 1),
                            Price = 0,
                            EndDate = new DateTime(2026, 3, 30),
                            CompletionAmount = 0,
                            InstalmentAmount = 0,
                            NumberOfInstalments = 0,
                            Periods = new List<EarningPeriod>
                            {
                                support,
                            }
                        }
                    }
                }
            };


            var destinationMessage = new GSLFunctionalSkillEarningsEvent();

            // Act
            mapper.Map(sourceMessage, collectionPeriod, destinationMessage);

            // Assert
            destinationMessage.Earnings.Should().NotBeNull();
            destinationMessage.Earnings.Count().Should().Be(1);
            destinationMessage.Earnings.FirstOrDefault().Type.Should().Be(Payments.Model.Core.Incentives.FunctionalSkillType.BalancingMathsAndEnglish);
        }
    }
}