using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Handlers;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Model;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.UnitTests
{
    // ReSharper disable once InconsistentNaming
    public class PendingEarningsReprocessorTests
    {
        private Mock<ICollectionPeriodService> _collectionPeriodService;
        private Mock<IEarningsRepository> _repository;
        private Mock<IGrowthAndSkillsMapper> _mapper;
        private Mock<IGSLCalculatePaymentsHandler> _handler;
        private Mock<ILogger<PendingEarningsReprocessor>> _logger;
        private PendingEarningsReprocessor _sut;

        [SetUp]
        public void SetUp()
        {
            _collectionPeriodService = new Mock<ICollectionPeriodService>();
            _repository = new Mock<IEarningsRepository>();
            _mapper = new Mock<IGrowthAndSkillsMapper>();
            _handler = new Mock<IGSLCalculatePaymentsHandler>();
            _logger = new Mock<ILogger<PendingEarningsReprocessor>>();

            _sut = new PendingEarningsReprocessor(_collectionPeriodService.Object, _repository.Object, _mapper.Object, _handler.Object, _logger.Object);
        }

        [Test]
        public async Task Process_WhenNoOpenCollectionPeriods_ReturnsZeroAndDoesNothing()
        {
            // Arrange
            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(new List<CollectionPeriodModel>());

            // Act
            var result = await _sut.Process();

            // Assert
            result.Should().Be(0);
            _repository.Verify(x => x.GetUnprocessedEarningsForCollectionPeriod(It.IsAny<short>(), It.IsAny<byte>()), Times.Never);
        }

        [Test]
        public async Task Process_WhenThereIsAPendingEarning_SubmitsItToTheEstablishedHandler()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel { AcademicYear = 2526, Period = 3, Status = CollectionPeriodStatus.Open };
            var earning = CreateEarning();
            var message = new CalculateGrowthAndSkillsPayments();

            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(new List<CollectionPeriodModel> { collectionPeriod });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2526, 3)).ReturnsAsync(new List<GrowthAndSkillsEarningModel> { earning });
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(earning)).Returns(message);

            // Act
            var result = await _sut.Process();

            // Assert
            result.Should().Be(1);
            _handler.Verify(x => x.HandleGslCalculatePaymentsMessage(message, true), Times.Once);
        }

        [Test]
        public async Task Process_WhenMultipleCollectionPeriodsAreOpen_ProcessesThePendingEarningsForEachOfThem()
        {
            // Arrange
            var firstCollectionPeriod = new CollectionPeriodModel { AcademicYear = 2526, Period = 3, Status = CollectionPeriodStatus.Open };
            var secondCollectionPeriod = new CollectionPeriodModel { AcademicYear = 2627, Period = 1, Status = CollectionPeriodStatus.Open };
            var firstEarning = CreateEarning();
            var secondEarning = CreateEarning();
            var firstMessage = new CalculateGrowthAndSkillsPayments();
            var secondMessage = new CalculateGrowthAndSkillsPayments();

            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods())
                .ReturnsAsync(new List<CollectionPeriodModel> { firstCollectionPeriod, secondCollectionPeriod });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2526, 3)).ReturnsAsync(new List<GrowthAndSkillsEarningModel> { firstEarning });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2627, 1)).ReturnsAsync(new List<GrowthAndSkillsEarningModel> { secondEarning });
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(firstEarning)).Returns(firstMessage);
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(secondEarning)).Returns(secondMessage);

            // Act
            var result = await _sut.Process();

            // Assert
            result.Should().Be(2);
            _handler.Verify(x => x.HandleGslCalculatePaymentsMessage(firstMessage, true), Times.Once);
            _handler.Verify(x => x.HandleGslCalculatePaymentsMessage(secondMessage, true), Times.Once);
        }

        [Test]
        public async Task Process_WhenOneEarningFailsToProcess_LogsTheErrorAndContinuesWithTheRemainingEarnings()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel { AcademicYear = 2526, Period = 3, Status = CollectionPeriodStatus.Open };
            var failingEarning = CreateEarning();
            var succeedingEarning = CreateEarning();
            var failingMessage = new CalculateGrowthAndSkillsPayments();
            var succeedingMessage = new CalculateGrowthAndSkillsPayments();

            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(new List<CollectionPeriodModel> { collectionPeriod });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2526, 3))
                .ReturnsAsync(new List<GrowthAndSkillsEarningModel> { failingEarning, succeedingEarning });
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(failingEarning)).Returns(failingMessage);
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(succeedingEarning)).Returns(succeedingMessage);
            _handler.Setup(x => x.HandleGslCalculatePaymentsMessage(failingMessage, true)).ThrowsAsync(new Exception("Handler failed"));

            // Act
            var result = await _sut.Process();

            // Assert
            result.Should().Be(1);
            _handler.Verify(x => x.HandleGslCalculatePaymentsMessage(succeedingMessage, true), Times.Once);
            _logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        private GrowthAndSkillsEarningModel CreateEarning()
        {
            return new GrowthAndSkillsEarningModel
            {
                EarningsId = Guid.NewGuid(),
                UKPRN = 10002233,
                LearnerUln = 12345678,
                LearnerReference = "LEARNREF001",
                CourseCode = "123456",
                CourseReference = "ZSC00123"
            };
        }
    }
}
