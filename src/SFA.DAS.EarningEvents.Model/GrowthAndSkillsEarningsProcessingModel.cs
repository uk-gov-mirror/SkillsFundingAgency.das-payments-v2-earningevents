namespace SFA.DAS.Payments.EarningEvents.Model
{
    public class GrowthAndSkillsEarningsProcessingModel
    {
        public long Id { get; set; }
        public Guid GrowthAndSkillsEarningId { get; set; }
        public byte CollectionPeriod { get; set; }
        public short AcademicYear { get; set; }
        public DateTime? ProcessedOn { get; set; }
    }
}
