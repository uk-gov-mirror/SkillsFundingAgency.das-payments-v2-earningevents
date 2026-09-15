namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Infrastructure.Configuration
{
    public interface IEarningsBridgeConfiguration
    {
        string PaymentsConnectionString { get; set; }
        string DASServiceBusConnectionString { get; set; }
        string DASServiceBusQueueName { get; set; }
        string ServiceBusConnectionString { get; set; }
        string CollectionPeriodApiBaseAddress { get; set; }
        string CollectionPeriodApiKey { get; set; }
        string ReprocessPendingEarningsSchedule { get; set; }
    }
}
