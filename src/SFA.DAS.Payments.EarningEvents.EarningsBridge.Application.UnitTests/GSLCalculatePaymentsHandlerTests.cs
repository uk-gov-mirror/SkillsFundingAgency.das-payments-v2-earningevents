using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Handlers;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Validators;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Model;
using SFA.DAS.Payments.Model.Core.Entities;
using UUIDNext;
using UUIDNext.Tools;
using EarningType = SFA.DAS.Payments.EarningEvents.Messages.External.EarningType;
using EmployerType = SFA.DAS.Payments.EarningEvents.Messages.External.EmployerType;
using LearningType = SFA.DAS.Payments.EarningEvents.Messages.External.LearningType;
using TrainingStatus = SFA.DAS.Payments.EarningEvents.Messages.External.TrainingStatus;
// ReSharper disable InconsistentNaming

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.UnitTests
{
    public class GSLCalculatePaymentsHandlerTests
    {
        private CalculateGrowthAndSkillsPayments _message;

        private CalculateGSLPaymentsValidator _validator;
        private GrowthAndSkillsMapper _mapper;
        private Mock<IEarningsRepository> _repository;
        private Mock<IPaymentsServiceBusPublisher> _publisher;
        private Mock<IGSLEarningsService> _gslService;
        private Mock<ICollectionPeriodService> _collectionPeriodService;
        private Mock<ILogger<GSLCalculatePaymentsHandler>> _logger;
        
        [SetUp]
        public void SetUp()
        {
            _message = new CalculateGrowthAndSkillsPayments
            {
                EarningsId = Guid.NewGuid(),
                EmployerContribution = 1000m,
                UKPRN = 10002233,
                Training = new Training
                {
                    CourseCode = "123456",
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
                            }
                        }
                    }
                }
            };

            _validator = new CalculateGSLPaymentsValidator();
            _mapper = new GrowthAndSkillsMapper();
            _repository = new Mock<IEarningsRepository>();
            _publisher = new Mock<IPaymentsServiceBusPublisher>();
            _collectionPeriodService = new Mock<ICollectionPeriodService>();
            _logger = new Mock<ILogger<GSLCalculatePaymentsHandler>>();
            _gslService = new Mock<IGSLEarningsService>();

            _repository.Setup(x => x.GetGrowthAndSkillsEarnings(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new List<GrowthAndSkillsEarningModel>());
            _gslService.Setup(x => x.CheckEarningsAreLatest(It.IsAny<List<GrowthAndSkillsEarningModel>>(), It.IsAny<Guid>())).Returns(true);
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 2,
                    Status = CollectionPeriodStatus.Open
                }
            };
            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(collectionPeriods);
        }
        
        [Test]
        public async Task Earnings_are_not_processed_if_validation_fails()
        {
            // Arrange
            _message.UKPRN = 0;
            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object, 
                                                          _collectionPeriodService.Object, _logger.Object);
            
            // Act 
            Func<Task> act = async () => await handler.HandleGslCalculatePaymentsMessage(_message);
            act.Should().Throw<ArgumentException>()
                .WithMessage("UKPRN is required");

            // Assert
            _logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<ArgumentException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Never);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()),
                Times.Never);
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()),
                Times.Never);
            _repository.Verify(r => r.SaveEarnings(It.IsAny<GrowthAndSkillsEarningModel>()), Times.Never);
        }

        [Test]
        public async Task Earnings_are_sent_to_service_bus_and_stored_to_database_cache()
        {
            // Arrange          
            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object, 
                                                          _collectionPeriodService.Object, _logger.Object);

            // Act
            await handler.HandleGslCalculatePaymentsMessage(_message);

            // Assert
            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Once);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()),
                Times.Once);
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()),
                Times.Once);
            _repository.Verify(r => r.SaveEarnings(It.Is<GrowthAndSkillsEarningModel>(
                y => y.PricePeriods.All(p => p.ProcessedOn != null))), Times.Once);
            _repository.Verify(r => r.MarkEarningProcessed(_message.EarningsId, 2526, 2, It.IsAny<DateTime>()), Times.Once);
        }

        [Test]
        public async Task Earnings_are_not_sent_to_service_bus_if_collection_period_not_open()
        {
            // Arrange
            _message.Earnings.ToList()[0].AcademicYear = 2425;
            var collectionPeriods = new List<CollectionPeriodModel>();
            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(collectionPeriods);
            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object,
                _collectionPeriodService.Object, _logger.Object);

            // Act
            await handler.HandleGslCalculatePaymentsMessage(_message);

            // Assert
            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Once);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()),
                Times.Never);
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()),
                Times.Never);
            _repository.Verify(r => r.SaveEarnings(It.Is<GrowthAndSkillsEarningModel>(
                y => y.PricePeriods.All(p => p.ProcessedOn == null))), Times.Once);
            _repository.Verify(r => r.MarkEarningProcessed(It.IsAny<Guid>(), It.IsAny<short>(), It.IsAny<byte>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Test]
        public async Task Earnings_for_the_open_collection_period_are_processed_and_others_are_cached()
        {
            // Arrange
            _message.Earnings = new List<Earnings>
            {
                new Earnings
                {
                    AcademicYear = 2425,
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
                }
            };

            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object,
                _collectionPeriodService.Object, _logger.Object);

            // Act
            await handler.HandleGslCalculatePaymentsMessage(_message);

            // Assert
            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Once);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()),
                Times.Once);
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()),
                Times.Once);
            _repository.Verify(r => r.SaveEarnings(It.Is<GrowthAndSkillsEarningModel>(
                y => y.PricePeriods.Where(x => x.AcademicYear == 2425).
                    All(p => p.ProcessedOn == null))), Times.Once);
            _repository.Verify(r => r.SaveEarnings(It.Is<GrowthAndSkillsEarningModel>(
                y => y.PricePeriods.Where(x => x.AcademicYear == 2526)
                    .All(p => p.ProcessedOn != null))), Times.Once);
            _repository.Verify(r => r.MarkEarningProcessed(_message.EarningsId, 2526, 2, It.IsAny<DateTime>()), Times.Once);
            _repository.Verify(r => r.MarkEarningProcessed(_message.EarningsId, 2425, It.IsAny<byte>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Test]
        public async Task Earnings_are_sent_for_both_academic_years_if_two_collection_periods_are_open()
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
                }
            };
            var collectionPeriods = new List<CollectionPeriodModel>
            {
                new CollectionPeriodModel
                {
                    AcademicYear = 2526,
                    Period = 13,
                    Status = CollectionPeriodStatus.Open
                },
                new CollectionPeriodModel
                {
                    AcademicYear = 2627,
                    Period = 1,
                    Status = CollectionPeriodStatus.Open
                }
            };
            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(collectionPeriods);

            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object,
                _collectionPeriodService.Object, _logger.Object);
            
            // Act
            await handler.HandleGslCalculatePaymentsMessage(_message);

            // Assert
            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Once);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()),
                Times.Exactly(2));
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()),
                Times.Exactly(2));
            _repository.Verify(r => r.SaveEarnings(It.Is<GrowthAndSkillsEarningModel>(
                y => y.PricePeriods.Where(x => x.AcademicYear == 2425).
                    All(p => p.ProcessedOn != null))), Times.Once);
            _repository.Verify(r => r.SaveEarnings(It.Is<GrowthAndSkillsEarningModel>(
                y => y.PricePeriods.Where(x => x.AcademicYear == 2526)
                    .All(p => p.ProcessedOn != null))), Times.Once);
        }

        [Test]
        public async Task Earnings_Are_Older_Than_Latest_DB_Earnings_Should_Ignore_Message()
        {
            // Arrange
            var dateTimeNow = DateTime.UtcNow;
            var oldGuid = UuidToolkit.CreateUuidV7FromSpecificDate(dateTimeNow);

            // Mocking the repository to return existing earnings with a newer EarningsId
            _repository.Setup(repo => repo.GetGrowthAndSkillsEarnings(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()))
                       .ReturnsAsync(new List<GrowthAndSkillsEarningModel>()); // Simulate that there are existing earnings

            _gslService.Setup(service => service.CheckEarningsAreLatest(It.IsAny<List<GrowthAndSkillsEarningModel>>(), It.IsAny<Guid>()))
                       .Returns(false); // Simulate that the earnings are older than the latest in DB

            _message.EarningsId = oldGuid;

            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object,
                _collectionPeriodService.Object, _logger.Object);

            // Act
            await handler.HandleGslCalculatePaymentsMessage(_message);

            // Assert
            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Never);
            _repository.Verify(r => r.SaveEarnings(It.IsAny<GrowthAndSkillsEarningModel>()), Times.Never);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()), Times.Never);
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()), Times.Never);
        }

        [Test]
        public async Task Earnings_Are_Latest_Should_Process_And_Save()
        {
            // Arrange
            var dateTimeNow = DateTime.UtcNow;
            var newGuid = UuidToolkit.CreateUuidV7FromSpecificDate(dateTimeNow);

            // Mocking the repository to return that the earnings are the latest
            _repository.Setup(repo => repo.GetGrowthAndSkillsEarnings(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()))
                       .ReturnsAsync(new List<GrowthAndSkillsEarningModel>()); // Simulate that there are existing earnings

            _gslService.Setup(service => service.CheckEarningsAreLatest(It.IsAny<List<GrowthAndSkillsEarningModel>>(), It.IsAny<Guid>()))
                       .Returns(true); // Simulate that the earnings are the latest in DB

            _message.EarningsId = newGuid;

            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object,
                _collectionPeriodService.Object, _logger.Object);

            // Act
            await handler.HandleGslCalculatePaymentsMessage(_message);

            // Assert
            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Once);
            _repository.Verify(r => r.SaveEarnings(It.Is<GrowthAndSkillsEarningModel>(model => model.PricePeriods.Any())), Times.Once);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()), Times.Once);
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()), Times.Once);
        }

        [Test]
        public void Exception_In_GetGrowthAndSkillsEarnings_Should_Log_Error_And_Abort()
        {
            // Arrange
            _repository.Setup(repo => repo.GetGrowthAndSkillsEarnings(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()))
                .Throws(new Exception("Database error"));

            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object,
                _collectionPeriodService.Object, _logger.Object);

            // Act
            Assert.ThrowsAsync<Exception>(async () => await handler.HandleGslCalculatePaymentsMessage(_message));

            // Assert
            _collectionPeriodService.Verify(x => x.GetOpenCollectionPeriods(), Times.Never);
            _repository.Verify(r => r.SaveEarnings(It.IsAny<GrowthAndSkillsEarningModel>()), Times.Never);
            _publisher.Verify(p => p.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()), Times.Never);
            _publisher.Verify(p => p.Publish<DasEarningsReceivedEvent>(It.IsAny<DasEarningsReceivedEvent>()), Times.Never);
        }

        [Test]
        public async Task Subsequent_messages_generate_an_event_id_that_is_sortable()
        {
            // Arrange
            var firstEarningsId = Uuid.NewDatabaseFriendly(Database.SqlServer);
            var secondEarningsId = Uuid.NewDatabaseFriendly(Database.SqlServer);
            var events = new List<GSLShortCourseEarningsEvent>();
            var handler = new GSLCalculatePaymentsHandler(_validator, _mapper, _repository.Object, _gslService.Object, _publisher.Object,
                _collectionPeriodService.Object, _logger.Object);

            _publisher.Setup(x => x.Publish<GSLShortCourseEarningsEvent>(It.IsAny<GSLShortCourseEarningsEvent>()))
                .Callback<GSLShortCourseEarningsEvent>(events.Add);

            // Act
            _message.EarningsId = firstEarningsId;
            await handler.HandleGslCalculatePaymentsMessage(_message);
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            _message.EarningsId = secondEarningsId;
            await handler.HandleGslCalculatePaymentsMessage(_message);

            // Assert
            events[0].EventId.Should().NotBe(firstEarningsId);
            events[1].EventId.Should().NotBe(secondEarningsId);
            events[0].ExternalEarningsId.Should().Be(firstEarningsId);
            events[1].ExternalEarningsId.Should().Be(secondEarningsId);

            var firstEventIdDecodesToTimestamp = UuidDecoder.TryDecodeTimestamp(events[0].EventId, out var firstEventDateTime);
            var secondEventIdDecodesToTimestamp = UuidDecoder.TryDecodeTimestamp(events[1].EventId, out var secondEventDateTime);
            firstEventIdDecodesToTimestamp.Should().BeTrue();
            secondEventIdDecodesToTimestamp.Should().BeTrue();

            firstEventDateTime.Should().NotBe(secondEventDateTime);
            secondEventDateTime.Should().BeAfter(firstEventDateTime);
        }
    }
}
