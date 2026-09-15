using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
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
        private Mock<IPaymentsServiceBusPublisher> _publisher;
        private Mock<ILogger<PendingEarningsReprocessor>> _logger;
        private PendingEarningsReprocessor _sut;

        [SetUp]
        public void SetUp()
        {
            _collectionPeriodService = new Mock<ICollectionPeriodService>();
            _repository = new Mock<IEarningsRepository>();
            _mapper = new Mock<IGrowthAndSkillsMapper>();
            _publisher = new Mock<IPaymentsServiceBusPublisher>();
            _logger = new Mock<ILogger<PendingEarningsReprocessor>>();

            _sut = new PendingEarningsReprocessor(_collectionPeriodService.Object, _repository.Object, _mapper.Object, _publisher.Object, _logger.Object);
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
        public async Task Process_WhenThereIsAPendingEarning_PublishesBothEventTypesAndMarksItProcessed()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel { AcademicYear = 2526, Period = 3, Status = CollectionPeriodStatus.Open };
            var earning = CreateEarning();
            var message = new CalculateGrowthAndSkillsPayments();
            var shortCourseEvent = new GSLShortCourseEarningsEvent();
            var dasEarningsReceivedEvent = new DasEarningsReceivedEvent();

            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(new List<CollectionPeriodModel> { collectionPeriod });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2526, 3)).ReturnsAsync(new List<GrowthAndSkillsEarningModel> { earning });
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(earning)).Returns(message);
            _mapper.Setup(x => x.MapToShortCourseEarningEvents(message, It.Is<IEnumerable<CollectionPeriodModel>>(p => p.Single() == collectionPeriod)))
                .Returns(new List<GSLShortCourseEarningsEvent> { shortCourseEvent });
            _mapper.Setup(x => x.MapToDasEarningsReceivedEvents(message, It.Is<IEnumerable<CollectionPeriodModel>>(p => p.Single() == collectionPeriod)))
                .Returns(new List<DasEarningsReceivedEvent> { dasEarningsReceivedEvent });

            // Act
            var result = await _sut.Process();

            // Assert
            result.Should().Be(1);
            _publisher.Verify(x => x.Publish(shortCourseEvent), Times.Once);
            _publisher.Verify(x => x.Publish(dasEarningsReceivedEvent), Times.Once);
            _repository.Verify(x => x.MarkEarningProcessed(earning.EarningsId, 2526, 3, It.IsAny<DateTime>()), Times.Once);
        }

        [Test]
        public async Task Process_WhenMultipleCollectionPeriodsAreOpen_ProcessesThePendingEarningsForEachOfThem()
        {
            // Arrange
            var firstCollectionPeriod = new CollectionPeriodModel { AcademicYear = 2526, Period = 3, Status = CollectionPeriodStatus.Open };
            var secondCollectionPeriod = new CollectionPeriodModel { AcademicYear = 2627, Period = 1, Status = CollectionPeriodStatus.Open };
            var firstEarning = CreateEarning();
            var secondEarning = CreateEarning();

            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods())
                .ReturnsAsync(new List<CollectionPeriodModel> { firstCollectionPeriod, secondCollectionPeriod });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2526, 3)).ReturnsAsync(new List<GrowthAndSkillsEarningModel> { firstEarning });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2627, 1)).ReturnsAsync(new List<GrowthAndSkillsEarningModel> { secondEarning });
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(It.IsAny<GrowthAndSkillsEarningModel>())).Returns(new CalculateGrowthAndSkillsPayments());
            _mapper.Setup(x => x.MapToShortCourseEarningEvents(It.IsAny<CalculateGrowthAndSkillsPayments>(), It.IsAny<IEnumerable<CollectionPeriodModel>>()))
                .Returns(new List<GSLShortCourseEarningsEvent> { new GSLShortCourseEarningsEvent() });
            _mapper.Setup(x => x.MapToDasEarningsReceivedEvents(It.IsAny<CalculateGrowthAndSkillsPayments>(), It.IsAny<IEnumerable<CollectionPeriodModel>>()))
                .Returns(new List<DasEarningsReceivedEvent> { new DasEarningsReceivedEvent() });

            // Act
            var result = await _sut.Process();

            // Assert
            result.Should().Be(2);
            _repository.Verify(x => x.MarkEarningProcessed(firstEarning.EarningsId, 2526, 3, It.IsAny<DateTime>()), Times.Once);
            _repository.Verify(x => x.MarkEarningProcessed(secondEarning.EarningsId, 2627, 1, It.IsAny<DateTime>()), Times.Once);
        }

        [Test]
        public async Task Process_WhenOneEarningFailsToProcess_LogsTheErrorAndContinuesWithTheRemainingEarnings()
        {
            // Arrange
            var collectionPeriod = new CollectionPeriodModel { AcademicYear = 2526, Period = 3, Status = CollectionPeriodStatus.Open };
            var failingEarning = CreateEarning();
            var succeedingEarning = CreateEarning();

            _collectionPeriodService.Setup(x => x.GetOpenCollectionPeriods()).ReturnsAsync(new List<CollectionPeriodModel> { collectionPeriod });
            _repository.Setup(x => x.GetUnprocessedEarningsForCollectionPeriod(2526, 3))
                .ReturnsAsync(new List<GrowthAndSkillsEarningModel> { failingEarning, succeedingEarning });
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(failingEarning)).Throws(new Exception("Mapping failed"));
            _mapper.Setup(x => x.MapToCalculateGrowthAndSkillsPayments(succeedingEarning)).Returns(new CalculateGrowthAndSkillsPayments());
            _mapper.Setup(x => x.MapToShortCourseEarningEvents(It.IsAny<CalculateGrowthAndSkillsPayments>(), It.IsAny<IEnumerable<CollectionPeriodModel>>()))
                .Returns(new List<GSLShortCourseEarningsEvent> { new GSLShortCourseEarningsEvent() });
            _mapper.Setup(x => x.MapToDasEarningsReceivedEvents(It.IsAny<CalculateGrowthAndSkillsPayments>(), It.IsAny<IEnumerable<CollectionPeriodModel>>()))
                .Returns(new List<DasEarningsReceivedEvent> { new DasEarningsReceivedEvent() });

            // Act
            var result = await _sut.Process();

            // Assert
            result.Should().Be(1);
            _repository.Verify(x => x.MarkEarningProcessed(failingEarning.EarningsId, It.IsAny<short>(), It.IsAny<byte>(), It.IsAny<DateTime>()), Times.Never);
            _repository.Verify(x => x.MarkEarningProcessed(succeedingEarning.EarningsId, 2526, 3, It.IsAny<DateTime>()), Times.Once);
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
