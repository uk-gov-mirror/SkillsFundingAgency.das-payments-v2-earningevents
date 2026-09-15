using Microsoft.Extensions.Logging;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;
using SFA.DAS.Payments.EarningEvents.Messages.Events;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;

// ReSharper disable once InconsistentNaming
public class PendingEarningsReprocessor : IPendingEarningsReprocessor
{
    private readonly ICollectionPeriodService _collectionPeriodService;
    private readonly IEarningsRepository _repository;
    private readonly IGrowthAndSkillsMapper _mapper;
    private readonly IPaymentsServiceBusPublisher _publisher;
    private readonly ILogger<PendingEarningsReprocessor> _logger;

    public PendingEarningsReprocessor(
        ICollectionPeriodService collectionPeriodService,
        IEarningsRepository repository,
        IGrowthAndSkillsMapper mapper,
        IPaymentsServiceBusPublisher publisher,
        ILogger<PendingEarningsReprocessor> logger)
    {
        _collectionPeriodService = collectionPeriodService;
        _repository = repository;
        _mapper = mapper;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<int> Process()
    {
        var openCollectionPeriods = await _collectionPeriodService.GetOpenCollectionPeriods();
        var processedCount = 0;

        foreach (var collectionPeriod in openCollectionPeriods)
        {
            var pendingEarnings = await _repository.GetUnprocessedEarningsForCollectionPeriod(collectionPeriod.AcademicYear, (byte)collectionPeriod.Period);

            foreach (var earning in pendingEarnings)
            {
                try
                {
                    var message = _mapper.MapToCalculateGrowthAndSkillsPayments(earning);
                    var relevantCollectionPeriods = new[] { collectionPeriod };

                    var shortCourseEarningsEvents = _mapper.MapToShortCourseEarningEvents(message, relevantCollectionPeriods);
                    var dasEarningsReceivedEvents = _mapper.MapToDasEarningsReceivedEvents(message, relevantCollectionPeriods);

                    foreach (var shortCourseEarningsEvent in shortCourseEarningsEvents)
                    {
                        await _publisher.Publish(shortCourseEarningsEvent);
                    }

                    foreach (var dasEarningsReceivedEvent in dasEarningsReceivedEvents)
                    {
                        await _publisher.Publish(dasEarningsReceivedEvent);
                    }

                    await _repository.MarkEarningProcessed(earning.EarningsId, collectionPeriod.AcademicYear, (byte)collectionPeriod.Period, DateTime.UtcNow);

                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to reprocess pending GrowthAndSkills earning {EarningsId} for AcademicYear {AcademicYear}, CollectionPeriod {CollectionPeriod}",
                        earning.EarningsId, collectionPeriod.AcademicYear, collectionPeriod.Period);
                }
            }
        }

        return processedCount;
    }
}
