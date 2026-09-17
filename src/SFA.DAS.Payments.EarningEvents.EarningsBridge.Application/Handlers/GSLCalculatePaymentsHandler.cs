using Microsoft.Extensions.Logging;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Validators;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Handlers
{
    public class GSLCalculatePaymentsHandler : IGSLCalculatePaymentsHandler
    {
        private ICalculateGSLPaymentsValidator _validator;
        private IGrowthAndSkillsMapper _mapper;
        private IEarningsRepository _repository;
        private IGSLEarningsService _gslEarningsService;
        private IPaymentsServiceBusPublisher _publisher;
        private ICollectionPeriodService _collectionPeriodService;
        private ILogger<GSLCalculatePaymentsHandler> _logger;

        public GSLCalculatePaymentsHandler(
            ICalculateGSLPaymentsValidator validator,
            IGrowthAndSkillsMapper mapper,
            IEarningsRepository repository,
            IGSLEarningsService gslEarningsService,
            IPaymentsServiceBusPublisher publisher,
            ICollectionPeriodService collectionPeriodService,
            ILogger<GSLCalculatePaymentsHandler> logger)
        {
            _validator = validator;
            _mapper = mapper;
            _repository = repository;
            _gslEarningsService = gslEarningsService;
            _publisher = publisher;
            _collectionPeriodService = collectionPeriodService;
            _logger = logger;
        }
        
        public async Task HandleGslCalculatePaymentsMessage(CalculateGrowthAndSkillsPayments message, bool isReprocessing = false)
        {
            try
            {
                if (!_validator.Validate(message))
                {
                    return;
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate GSL calculate payments message");
                throw;
            }

            if (!isReprocessing)
            {
                try
                {
                    // Check if earnings in DB are the latest
                    var dbEarnings = await _repository.GetGrowthAndSkillsEarnings(ukPrn: message.UKPRN, uln: message.Learner.ULN, courseCode: message.Training.CourseCode);
                    var earningsAreLatest = _gslEarningsService.CheckEarningsAreLatest(dbEarnings, message.EarningsId);
                    if (!earningsAreLatest)
                    {
                        _logger.LogWarning("Earnings received are not the latest. " +
                                               "Skipping processing for message with EarningsId: {EarningsId}, UKPRN: {UKPRN}, ULN: {ULN}, CourseCode: {CourseCode}",
                            message.EarningsId, message.UKPRN, message.Learner.ULN, message.Training.CourseCode);
                        return; // If earnings are not the latest, don't proceed
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing CalculateGrowthAndSkillsPayments with " +
                                         "EarningsId: {EarningsId}, UKPRN: {UKPRN}, ULN: {ULN}, CourseCode: {CourseCode}",
                        message.EarningsId, message.UKPRN, message.Learner.ULN, message.Training.CourseCode);
                    throw;
                }
            }


            var growthAndSkillsEarningModel = _mapper.MapToGrowthAndSkillsEarningModel(message);

            var openCollectionPeriods = await _collectionPeriodService.GetOpenCollectionPeriods();

            if(!openCollectionPeriods.Any())
            {
                await _repository.SaveEarnings(growthAndSkillsEarningModel);
                return;
            }

            foreach (var earning in growthAndSkillsEarningModel.PricePeriods)
            {
                if (openCollectionPeriods.Any(x => x.AcademicYear == earning.AcademicYear))
                {
                    earning.ProcessedOn = DateTime.UtcNow;
                }
            }

            var matchingOpenPeriods = openCollectionPeriods
                .Where(period => growthAndSkillsEarningModel.PricePeriods.Any(pricePeriod => pricePeriod.AcademicYear == period.AcademicYear))
                .ToList();

            var requiredPaymentsEvents = _mapper.MapToShortCourseEarningEvents(message, openCollectionPeriods);

            var fundingSourceEvents = _mapper.MapToDasEarningsReceivedEvents(message, openCollectionPeriods);


            foreach (var requiredPaymentsEvent in requiredPaymentsEvents)
            {
                await _publisher.Publish<GSLShortCourseEarningsEvent>(requiredPaymentsEvent);
            }

            foreach (var fundingSourceEvent in fundingSourceEvents)
            {
                await _publisher.Publish<DasEarningsReceivedEvent>(fundingSourceEvent);
            }

            foreach (var period in matchingOpenPeriods)
            {
                await _repository.MarkEarningProcessed(growthAndSkillsEarningModel.EarningsId, period.AcademicYear, (byte)period.Period, DateTime.UtcNow);
            }

            if (!isReprocessing)
            {
                await _repository.SaveEarnings(growthAndSkillsEarningModel);
            }
        }
    }
}

