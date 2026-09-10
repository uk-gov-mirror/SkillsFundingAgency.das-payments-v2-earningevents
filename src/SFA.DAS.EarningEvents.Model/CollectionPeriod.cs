
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.EarningEvents.Model
{
    public class CollectionPeriod
    {
        public int Id { get; set; }
        public byte Period { get; set; }
        public int? CalendarMonth { get; set; }
        public int? CalendarYear { get; set; }
        public CollectionPeriodStatus Status { get; set; }
    }
}
