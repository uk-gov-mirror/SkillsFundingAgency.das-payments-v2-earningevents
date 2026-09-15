namespace SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Infrastructure.Configuration
{
    public class EarningsBridgeConfiguration : IEarningsBridgeConfiguration
    {
        public string PaymentsConnectionString { get; set; }
        public string DASServiceBusConnectionString { get; set; }
        public string DASServiceBusQueueName { get; set; }
        public string ServiceBusConnectionString { get; set; }
        public string CollectionPeriodApiBaseAddress { get; set; }
        public string CollectionPeriodApiKey { get; set; }
        public string ReprocessPendingEarningsSchedule { get; set; }
    }
}
