using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.Payments.EarningEvents.Data;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Model;
using UUIDNext;
using UUIDNext.Tools;
using EarningType = SFA.DAS.Payments.EarningEvents.Messages.External.EarningType;
using EmployerType = SFA.DAS.Payments.EarningEvents.Messages.External.EmployerType;
using LearningType = SFA.DAS.Payments.EarningEvents.Messages.External.LearningType;
using TrainingStatus = SFA.DAS.Payments.EarningEvents.Messages.External.TrainingStatus;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.UnitTests.Repositories
{
    [TestFixture]
    public class EarningsRepositoryTests
    {
        private IEarningsRepository _repository;
        private CalculateGrowthAndSkillsPayments _message;
        private Mock<ILogger<EarningsRepository>> _mockLogger;
        private EarningsDataContext _dataContext;
        private DbContextOptions<EarningsDataContext> _dbContextOptions;

        private long _ukPrn;
        private long _uln;
        private string _courseCode;

        [SetUp]
        public void SetUp()
        {
            _ukPrn = 10001234;
            _uln = 12345678;
            _courseCode = "123456";

            _message = new CalculateGrowthAndSkillsPayments
            {
                EarningsId = Guid.NewGuid(),
                EmployerContribution = 1000m,
                UKPRN = _ukPrn,
                Training = new Training
                {
                    CourseCode = _courseCode,
                    CourseReference = "ZSC00123",
                    LearningType = LearningType.ApprenticeshipUnit,
                    StartDate = new DateTime(2026, 1, 1),
                    TrainingStatus = TrainingStatus.Continuing,
                    AgeAtStartOfTraining = 25,
                    PlannedEndDate = new DateTime(2026, 1, 15),
                    ActualEndDate = new DateTime(2026, 1, 31)
                },
                Learner = new Learner
                {
                    ULN = _uln,
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


            //Set up in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<EarningsDataContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDatabase")
                .Options;
            _dataContext = new EarningsDataContext(_dbContextOptions);
            _mockLogger = new Mock<ILogger<EarningsRepository>>();
            _repository = new EarningsRepository(_dataContext, _mockLogger.Object);

        }

        [TearDown]
        public void TearDown()
        {
            _dataContext.Database.EnsureDeleted();
            _dataContext.Dispose();
        }

        [Test]
        public async Task Empty_Table_Returns_EmptyResults()
        {
            // Act
            var result = await _repository.GetGrowthAndSkillsEarnings(_message.UKPRN, _message.Learner.ULN, _message.Training.CourseCode);
            // Assert
            result.Count.Should().Be(0);
        }

        [Test]
        public async Task Non_Matching_Earning_Returns_EmptyResults()
        {
            // Arrange
            _message.UKPRN = _ukPrn;
            _message.Learner.ULN = _uln;
            _message.Training.CourseCode = _courseCode;

            var tableEntries = new List<GrowthAndSkillsEarningModel>
            {
                CreateGrowthAndSkills(1, 1234,1234, "nonMatching"),
            };
            _dataContext.GrowthAndSkillsEarnings.AddRange(tableEntries);
            await _dataContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetGrowthAndSkillsEarnings(_message.UKPRN, _message.Learner.ULN, _message.Training.CourseCode);
            // Assert
            result.Count.Should().Be(0);
        }

        [Test]
        public async Task Matching_Earning_Returns_DatabaseResults()
        {
            // Arrange
            var tableEntries = new List<GrowthAndSkillsEarningModel>
            {
                CreateGrowthAndSkills(1, _ukPrn,_uln, _courseCode),
                CreateGrowthAndSkills(2, _ukPrn,_uln, _courseCode),
                CreateGrowthAndSkills(3, _ukPrn,_uln, "nonMatching"),
            };
            _dataContext.GrowthAndSkillsEarnings.AddRange(tableEntries);
            await _dataContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetGrowthAndSkillsEarnings(_message.UKPRN, _message.Learner.ULN, _message.Training.CourseCode);
            // Assert
            result.Count.Should().Be(2);
        }

        [Test]
        public async Task GetUnprocessedEarningsForCollectionPeriod_WhenNoProcessingRecordExists_ReturnsTheEarning()
        {
            // Arrange
            var earning = CreateGrowthAndSkillsWithEarningsId(1, _ukPrn, _uln, _courseCode, Uuid.NewDatabaseFriendly(Database.SqlServer), 2526);
            _dataContext.GrowthAndSkillsEarnings.Add(earning);
            await _dataContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUnprocessedEarningsForCollectionPeriod(2526, 1);

            // Assert
            result.Should().ContainSingle(x => x.Id == earning.Id);
        }

        [Test]
        public async Task GetUnprocessedEarningsForCollectionPeriod_WhenAlreadyProcessedForThatPeriod_ExcludesTheEarning()
        {
            // Arrange
            var earning = CreateGrowthAndSkillsWithEarningsId(1, _ukPrn, _uln, _courseCode, Uuid.NewDatabaseFriendly(Database.SqlServer), 2526);
            _dataContext.GrowthAndSkillsEarnings.Add(earning);
            _dataContext.GrowthAndSkillsEarningsProcessing.Add(new GrowthAndSkillsEarningsProcessingModel
            {
                GrowthAndSkillsEarningId = earning.EarningsId,
                AcademicYear = 2526,
                CollectionPeriod = 1,
                ProcessedOn = DateTime.UtcNow
            });
            await _dataContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUnprocessedEarningsForCollectionPeriod(2526, 1);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetUnprocessedEarningsForCollectionPeriod_WhenAProcessingRecordExistsForThatPeriodButProcessedOnIsNull_ReturnsTheEarning()
        {
            // Arrange
            var earning = CreateGrowthAndSkillsWithEarningsId(1, _ukPrn, _uln, _courseCode, Uuid.NewDatabaseFriendly(Database.SqlServer), 2526);
            _dataContext.GrowthAndSkillsEarnings.Add(earning);
            _dataContext.GrowthAndSkillsEarningsProcessing.Add(new GrowthAndSkillsEarningsProcessingModel
            {
                GrowthAndSkillsEarningId = earning.EarningsId,
                AcademicYear = 2526,
                CollectionPeriod = 1,
                ProcessedOn = null
            });
            await _dataContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUnprocessedEarningsForCollectionPeriod(2526, 1);

            // Assert
            result.Should().ContainSingle(x => x.EarningsId == earning.EarningsId);
        }

        [Test]
        public async Task GetUnprocessedEarningsForCollectionPeriod_WhenMultipleUnprocessedEarningsExistForTheSameIdentity_ReturnsAllOfThem()
        {
            // Arrange
            var earningsId1 = Uuid.NewDatabaseFriendly(Database.SqlServer);
            var earningsId2 = Uuid.NewDatabaseFriendly(Database.SqlServer);

            var firstEarning = CreateGrowthAndSkillsWithEarningsId(1, _ukPrn, _uln, _courseCode, earningsId1, 2526);
            var secondEarning = CreateGrowthAndSkillsWithEarningsId(2, _ukPrn, _uln, _courseCode, earningsId2, 2526);
            _dataContext.GrowthAndSkillsEarnings.AddRange(firstEarning, secondEarning);
            await _dataContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUnprocessedEarningsForCollectionPeriod(2526, 1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainSingle(x => x.EarningsId == earningsId1);
            result.Should().ContainSingle(x => x.EarningsId == earningsId2);
        }

        [Test]
        public async Task GetUnprocessedEarningsForCollectionPeriod_WhenOneOfMultipleEarningsForTheSameIdentityIsAlreadyProcessed_ReturnsOnlyTheUnprocessedOne()
        {
            // Arrange
            var processedEarningsId = Uuid.NewDatabaseFriendly(Database.SqlServer);
            var unprocessedEarningsId = Uuid.NewDatabaseFriendly(Database.SqlServer);

            var processedEarning = CreateGrowthAndSkillsWithEarningsId(1, _ukPrn, _uln, _courseCode, processedEarningsId, 2526);
            var unprocessedEarning = CreateGrowthAndSkillsWithEarningsId(2, _ukPrn, _uln, _courseCode, unprocessedEarningsId, 2526);
            _dataContext.GrowthAndSkillsEarnings.AddRange(processedEarning, unprocessedEarning);
            _dataContext.GrowthAndSkillsEarningsProcessing.Add(new GrowthAndSkillsEarningsProcessingModel
            {
                GrowthAndSkillsEarningId = processedEarningsId,
                AcademicYear = 2526,
                CollectionPeriod = 1,
                ProcessedOn = DateTime.UtcNow
            });
            await _dataContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUnprocessedEarningsForCollectionPeriod(2526, 1);

            // Assert
            result.Should().ContainSingle(x => x.EarningsId == unprocessedEarningsId);
        }

        [Test]
        public async Task MarkEarningProcessed_WhenNoRecordExistsForThatPeriod_InsertsANewRecord()
        {
            // Arrange
            var earningsId = Guid.NewGuid();
            var processedOn = DateTime.UtcNow;

            // Act
            await _repository.MarkEarningProcessed(earningsId, 2526, 1, processedOn);

            // Assert
            var result = await _dataContext.GrowthAndSkillsEarningsProcessing.ToListAsync();
            result.Should().ContainSingle();
            result.Single().GrowthAndSkillsEarningId.Should().Be(earningsId);
            result.Single().AcademicYear.Should().Be((short)2526);
            result.Single().CollectionPeriod.Should().Be((byte)1);
            result.Single().ProcessedOn.Should().Be(processedOn);
        }

        [Test]
        public async Task MarkEarningProcessed_WhenARecordAlreadyExistsForThatPeriod_UpdatesProcessedOnInPlaceInsteadOfInserting()
        {
            // Arrange
            var earningsId = Guid.NewGuid();
            var originalProcessedOn = DateTime.UtcNow.AddDays(-1);
            _dataContext.GrowthAndSkillsEarningsProcessing.Add(new GrowthAndSkillsEarningsProcessingModel
            {
                GrowthAndSkillsEarningId = earningsId,
                AcademicYear = 2526,
                CollectionPeriod = 1,
                ProcessedOn = originalProcessedOn
            });
            await _dataContext.SaveChangesAsync();

            var updatedProcessedOn = DateTime.UtcNow;

            // Act
            await _repository.MarkEarningProcessed(earningsId, 2526, 1, updatedProcessedOn);

            // Assert
            var result = await _dataContext.GrowthAndSkillsEarningsProcessing.ToListAsync();
            result.Should().ContainSingle();
            result.Single().ProcessedOn.Should().Be(updatedProcessedOn);
        }

        [Test]
        public async Task MarkEarningProcessed_WhenAnotherCollectionPeriodIsAlreadyMarkedProcessed_InsertsASeparateRecordForThisPeriod()
        {
            // Arrange
            var earningsId = Guid.NewGuid();
            await _repository.MarkEarningProcessed(earningsId, 2526, 1, DateTime.UtcNow.AddDays(-1));

            // Act
            await _repository.MarkEarningProcessed(earningsId, 2526, 2, DateTime.UtcNow);

            // Assert
            var result = await _dataContext.GrowthAndSkillsEarningsProcessing.ToListAsync();
            result.Should().HaveCount(2);
            result.Should().ContainSingle(x => x.CollectionPeriod == 1);
            result.Should().ContainSingle(x => x.CollectionPeriod == 2);
        }

        [Test]
        public void Unhandled_Error_Throws_Exception()
        {
            // Arrange
            var mockContext = new Mock<IEarningsDataContext>();
            mockContext.Setup(x => x.GrowthAndSkillsEarnings).Throws(new Exception("Unhandled error"));
            _repository = new EarningsRepository(mockContext.Object, _mockLogger.Object);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _repository.GetGrowthAndSkillsEarnings(_message.UKPRN, _message.Learner.ULN, _message.Training.CourseCode));
        }

        private GrowthAndSkillsEarningModel CreateGrowthAndSkillsWithEarningsId(long id, long ukprn, long uln, string courseCode, Guid earningsId, short academicYear)
        {
            var earning = CreateGrowthAndSkills(id, ukprn, uln, courseCode);
            earning.EarningsId = earningsId;
            earning.PricePeriods.Single().AcademicYear = academicYear;
            return earning;
        }

        private GrowthAndSkillsEarningModel CreateGrowthAndSkills(long id, long ukprn, long uln, string courseCode)
        {
            return new GrowthAndSkillsEarningModel
            {
                Id = id,
                EarningsId = Guid.NewGuid(),
                UKPRN = ukprn,
                LearnerKey = Guid.NewGuid(),
                LearnerUln = uln,
                LearnerReference = "LEARNREF001",
                LearningType = Model.LearningType.ApprenticeshipUnit,
                CourseCode = courseCode,
                CourseReference = "ZSC00123",
                StartDate = new DateTime(2026, 1, 1),
                AgeAtStartOfTraining = 25,
                PlannedEndDate = new DateTime(2026, 1, 15),
                ActualEndDate = new DateTime(2026, 1, 31),
                TrainingStatus = Model.TrainingStatus.Continuing,
                EmployerContribution = 1000m,
                CourseType = Model.CourseType.Apprenticeship,
                PricePeriods = new List<GrowthAndSkillsEarningPricePeriodModel>
                {
                    new GrowthAndSkillsEarningPricePeriodModel
                    {
                        Id = id,
                        StartDate = new DateTime(2026, 1, 1),
                        EndDate = new DateTime(2026, 1, 31),
                        Price = 5000m,
                    }
                }
            };
        }
    }
}
