namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;

public interface IPendingEarningsReprocessor
{
    Task<int> Process();
}
