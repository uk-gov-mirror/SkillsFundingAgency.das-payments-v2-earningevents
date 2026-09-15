using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Function;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.UnitTests
{
    // ReSharper disable once InconsistentNaming
    public class ReprocessPendingEarningsTriggerTests
    {
        private Mock<IPendingEarningsReprocessor> _reprocessor;
        private Mock<ILogger<ReprocessPendingEarningsTrigger>> _logger;
        private ReprocessPendingEarningsTrigger _sut;

        [SetUp]
        public void SetUp()
        {
            _reprocessor = new Mock<IPendingEarningsReprocessor>();
            _logger = new Mock<ILogger<ReprocessPendingEarningsTrigger>>();
            _sut = new ReprocessPendingEarningsTrigger(_logger.Object, _reprocessor.Object);
        }

        [Test]
        public async Task TimerTriggerReprocessPendingEarnings_WhenProcessSucceeds_CallsProcessOnce()
        {
            // Arrange
            _reprocessor.Setup(x => x.Process()).ReturnsAsync(3);

            // Act
            await _sut.TimerTriggerReprocessPendingEarnings(null);

            // Assert
            _reprocessor.Verify(x => x.Process(), Times.Once);
        }

        [Test]
        public async Task TimerTriggerReprocessPendingEarnings_WhenProcessThrows_LogsErrorAndDoesNotRethrow()
        {
            // Arrange
            _reprocessor.Setup(x => x.Process()).ThrowsAsync(new Exception("Database error"));

            // Act
            Func<Task> act = async () => await _sut.TimerTriggerReprocessPendingEarnings(null);

            // Assert
            await act.Should().NotThrowAsync();
            _logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task HttpTriggerReprocessPendingEarnings_WhenProcessSucceeds_ReturnsOkWithProcessedCount()
        {
            // Arrange
            _reprocessor.Setup(x => x.Process()).ReturnsAsync(5);

            // Act
            var result = await _sut.HttpTriggerReprocessPendingEarnings(new DefaultHttpContext().Request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { ProcessedCount = 5 });
        }

        [Test]
        public async Task HttpTriggerReprocessPendingEarnings_WhenProcessThrows_ReturnsInternalServerErrorAndLogsError()
        {
            // Arrange
            _reprocessor.Setup(x => x.Process()).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _sut.HttpTriggerReprocessPendingEarnings(new DefaultHttpContext().Request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            _logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
