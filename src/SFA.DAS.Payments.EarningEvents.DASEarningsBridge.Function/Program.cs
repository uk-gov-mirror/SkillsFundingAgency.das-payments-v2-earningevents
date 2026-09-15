using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SFA.DAS.Payments.EarningEvents.Data;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Handlers;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Infrastructure.Configuration;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Repositories;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Services;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Application.Validators;
using SFA.DAS.Payments.EarningEvents.EarningsBridge.Function;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddOptions<EarningsBridgeConfiguration>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

builder.Services.AddSingleton<IEarningsBridgeConfiguration>(sp =>
    sp.GetRequiredService<IOptions<EarningsBridgeConfiguration>>().Value);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddDbContext<IEarningsDataContext, EarningsDataContext>((sp, options) =>
{
    var config = sp.GetService<IEarningsBridgeConfiguration>();
    if (String.IsNullOrWhiteSpace(config.PaymentsConnectionString))
    {
        throw new InvalidOperationException("PaymentsConnectionString is required");
    }
    options.UseSqlServer(config.PaymentsConnectionString);
});

builder.Services.AddScoped<IGrowthAndSkillsMapper, GrowthAndSkillsMapper>();
builder.Services.AddScoped<IGSLCalculatePaymentsHandler, GSLCalculatePaymentsHandler>();
builder.Services.AddScoped<ICalculateGSLPaymentsValidator, CalculateGSLPaymentsValidator>();
builder.Services.AddScoped<IEarningsRepository, EarningsRepository>();


builder.Services.AddHttpClient<ICollectionPeriodApiClient, CollectionPeriodApiClient>((sp, client) =>
{
    var config = sp.GetService<IEarningsBridgeConfiguration>();
    client.BaseAddress = new Uri(config.CollectionPeriodApiBaseAddress);
});

builder.Services.AddScoped<IPaymentsServiceBusPublisher, PaymentsServiceBusPublisher>((sp) =>
{
    var config = sp.GetService<IEarningsBridgeConfiguration>();
    return new PaymentsServiceBusPublisher(config.ServiceBusConnectionString);
});

builder.Services.AddScoped<ICollectionPeriodService, CollectionPeriodService>();
builder.Services.AddScoped<IGSLEarningsService, GSLEarningsService>();
builder.Services.AddScoped<IPendingEarningsReprocessor, PendingEarningsReprocessor>();

builder.Services.AddHostedService<ServiceBusQueueManager>();

builder.Build().Run();
