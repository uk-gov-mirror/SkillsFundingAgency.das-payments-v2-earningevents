using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Function
{
    public class ReprocessPendingEarningsTrigger
    {
        private readonly ILogger<ReprocessPendingEarningsTrigger> _logger;
        private readonly IPendingEarningsReprocessor _reprocessor;

        public ReprocessPendingEarningsTrigger(ILogger<ReprocessPendingEarningsTrigger> logger, IPendingEarningsReprocessor reprocessor)
        {
            _logger = logger;
            _reprocessor = reprocessor;
        }

        [Function("ReprocessPendingEarnings")]
        public async Task TimerTriggerReprocessPendingEarnings([TimerTrigger("%ReprocessPendingEarningsSchedule%")] TimerInfo timer)
        {
            _logger.LogInformation("ReprocessPendingEarnings timer trigger function started at: {executionTime}", DateTime.UtcNow);

            try
            {
                var processedCount = await _reprocessor.Process();
                _logger.LogInformation("ReprocessPendingEarnings timer trigger function completed at: {completionTime}. Processed {processedCount} earnings.", DateTime.UtcNow, processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReprocessPendingEarnings timer trigger function failed");
            }
        }

        [Function("ReprocessPendingEarningsHttp")]
        public async Task<IActionResult> HttpTriggerReprocessPendingEarnings(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "ReprocessPendingEarnings")] HttpRequest req)
        {
            _logger.LogInformation("ReprocessPendingEarnings HTTP trigger invoked");

            try
            {
                var processedCount = await _reprocessor.Process();
                return new OkObjectResult(new { ProcessedCount = processedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReprocessPendingEarnings HTTP trigger failed");
                return new ObjectResult(new { Error = "An error occurred while reprocessing pending earnings." }) { StatusCode = 500 };
            }
        }
    }
}
