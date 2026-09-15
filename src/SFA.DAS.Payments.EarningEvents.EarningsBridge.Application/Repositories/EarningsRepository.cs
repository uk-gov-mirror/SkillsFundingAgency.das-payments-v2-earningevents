using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.Payments.EarningEvents.Data;
using SFA.DAS.Payments.EarningEvents.Model;

namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories
{
    public class EarningsRepository : IEarningsRepository
    {
        private readonly IEarningsDataContext _earningsDataContext;
        private readonly ILogger<EarningsRepository> _logger;
        
        public EarningsRepository(IEarningsDataContext earningsDataContext, ILogger<EarningsRepository> logger)
        {
            _earningsDataContext = earningsDataContext;
            _logger = logger;
        }

        public async Task SaveEarnings(GrowthAndSkillsEarningModel growthAndSkillsEarningModel)
        {
            try
            {
                _earningsDataContext.GrowthAndSkillsEarnings.Add(growthAndSkillsEarningModel);
                await _earningsDataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while storing earnings to database", ex);
            }
        }

        public async Task<List<GrowthAndSkillsEarningModel>> GetGrowthAndSkillsEarnings(long ukPrn, long uln, string courseCode)
        {
            try
            {
                var results =  await _earningsDataContext.GrowthAndSkillsEarnings
                    .Where(x => x.UKPRN == ukPrn && x.LearnerUln == uln && x.CourseCode == courseCode)
                    .ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while querying GrowthAndSkills data. Exception: {ex.Message}");
                throw;
            }
        }

        public async Task<List<GrowthAndSkillsEarningModel>> GetUnprocessedEarningsForCollectionPeriod(short academicYear, byte collectionPeriod)
        {
            try
            {
                return await _earningsDataContext.GrowthAndSkillsEarnings
                    .Include(x => x.PricePeriods)
                    .Where(x => x.PricePeriods.Any(p => p.AcademicYear == academicYear)
                                && !_earningsDataContext.GrowthAndSkillsEarningsProcessing.Any(p =>
                                    p.GrowthAndSkillsEarningId == x.EarningsId
                                    && p.AcademicYear == academicYear
                                    && p.CollectionPeriod == collectionPeriod
                                    && p.ProcessedOn != null))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while querying unprocessed GrowthAndSkills earnings for AcademicYear {academicYear}, CollectionPeriod {collectionPeriod}. Exception: {ex.Message}");
                throw;
            }
        }

        public async Task MarkEarningProcessed(Guid earningsId, short academicYear, byte collectionPeriod, DateTime processedOn)
        {
            try
            {
                var existingRecord = await _earningsDataContext.GrowthAndSkillsEarningsProcessing
                    .FirstOrDefaultAsync(x => x.GrowthAndSkillsEarningId == earningsId
                                               && x.AcademicYear == academicYear
                                               && x.CollectionPeriod == collectionPeriod);

                if (existingRecord == null)
                {
                    _earningsDataContext.GrowthAndSkillsEarningsProcessing.Add(new GrowthAndSkillsEarningsProcessingModel
                    {
                        GrowthAndSkillsEarningId = earningsId,
                        AcademicYear = academicYear,
                        CollectionPeriod = collectionPeriod,
                        ProcessedOn = processedOn
                    });
                }
                else
                {
                    existingRecord.ProcessedOn = processedOn;
                }

                await _earningsDataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while marking GrowthAndSkills earning {earningsId} as processed for AcademicYear {academicYear}, CollectionPeriod {collectionPeriod}. Exception: {ex.Message}");
                throw;
            }
        }
    }
}
