using Microsoft.Extensions.Configuration;
using Reqnroll;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.Messages.Common;
using System.Security.Cryptography;
using System.Text;

namespace SFA.DAS.Payments.EarningEvents.Specs.StepDefinitions
{
    [Binding]
    public class TestRunBindings
    {
        public static IEndpointInstance PV2Endpoint { get; private set; }
        public static IEndpointInstance DASEndpoint { get; private set; }
        public static IConfiguration Config { get; private set; }


        [BeforeTestRun]
        public static async Task SetUpMessaging()
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appSettings.json"))
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appSettings.development.json"), true)
                .Build();
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            DASEndpoint = await CreateEndpoint("DASServiceBusConnectionString", sendOnly: true);
            PV2Endpoint = await CreateEndpoint("ServiceBusConnectionString", sendOnly: false,
                eventsToSubscribeTo: new[] { typeof(GSLShortCourseEarningsEvent), typeof(GSLApprenticeshipEarningsEvent) });
        }

        public static async Task<IEndpointInstance> CreateEndpoint(string connectionName, bool sendOnly = false, Type[] eventsToSubscribeTo = null)
        {
            var endpointConfig = new EndpointConfiguration("sfa-das-payments-earningevents-bridge-specs");
            var conventions = endpointConfig.Conventions();
            conventions.DefiningMessagesAs(type => type.IsMessage());
            endpointConfig.UseSerialization<NewtonsoftJsonSerializer>();
            if (sendOnly)
                endpointConfig.SendOnly();
            var storageConnectionString = Config["ConnectionStrings:StorageConnectionString"];
            endpointConfig.UsePersistence<AzureTablePersistence>().ConnectionString(storageConnectionString);
            var connectionConfig = $"ConnectionStrings:{connectionName}";
            var connectionString = Config[connectionConfig];
            Console.WriteLine($"Config: {connectionConfig}, ConnectionString: {connectionString}");
            var transport = new AzureServiceBusTransport(connectionString)
                {
                    UseWebSockets = Config["UseWebSockets"]?.ToLower() == "true",
                    SubscriptionRuleNamingConvention = ShortRuleName
            };

            endpointConfig.UseTransport(transport);
            endpointConfig.EnableInstallers();
            var startable = await Endpoint.Create(endpointConfig);
            var endpoint = await startable.Start();

            if (!sendOnly && eventsToSubscribeTo != null)
            {
                foreach (var eventToSubscribeTo in eventsToSubscribeTo)
                {
                    await endpoint.Subscribe(eventToSubscribeTo);
                }
            }

            return endpoint;
        }

        private static string ShortRuleName(Type type)
        {
            var name = type.FullName;

            if (name.Length <= 50)
                return name;

            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(name));

            return new Guid(bytes).ToString(); // 36 chars
        }
    }
}
