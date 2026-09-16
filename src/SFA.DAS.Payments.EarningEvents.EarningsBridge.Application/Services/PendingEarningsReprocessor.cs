using Microsoft.Extensions.Logging;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Handlers;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;

// ReSharper disable once InconsistentNaming
public class PendingEarningsReprocessor : IPendingEarningsReprocessor
{
    private readonly ICollectionPeriodService _collectionPeriodService;
    private readonly IEarningsRepository _repository;
    private readonly IGrowthAndSkillsMapper _mapper;
    private readonly IGSLCalculatePaymentsHandler _handler;
    private readonly ILogger<PendingEarningsReprocessor> _logger;

    public PendingEarningsReprocessor(
        ICollectionPeriodService collectionPeriodService,
        IEarningsRepository repository,
        IGrowthAndSkillsMapper mapper,
        IGSLCalculatePaymentsHandler handler,
        ILogger<PendingEarningsReprocessor> logger)
    {
        _collectionPeriodService = collectionPeriodService;
        _repository = repository;
        _mapper = mapper;
        _handler = handler;
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

                    await _handler.HandleGslCalculatePaymentsMessage(message, isReprocessing: true);

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
