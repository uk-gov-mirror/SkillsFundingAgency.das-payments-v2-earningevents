using SFA.DAS.Payments.Messages.Common.Events;
using SFA.DAS.Payments.Model.Core.Entities;
using System;

namespace SFA.DAS.Payments.EarningEvents.Messages.Events
{
    public class GSLFunctionalSkillEarningsEvent: FunctionalSkillEarningsEvent, IFunctionalSkillEarningEvent 
    {
        public GSLFunctionalSkillEarningsEvent()
        {
            ContractType = ContractType.Act1;
            FundingPlatformType = FundingPlatformType.DigitalApprenticeshipService;
        }
        public Guid ExternalEarningsId { get; set; }
    }
}