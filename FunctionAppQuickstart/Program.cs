using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// local.json.settings = File that defines environment variables used by your project when run locally on your machine.
// host.json = File that defines configuration shared by functions in your project.

// Program.cs file that's the entry point for the app.

// When using .NET isolated functions You're responsible for creating and starting your own host instance.
// As such, you also have direct access to the configuration pipeline for your app.

// The ConfigureFunctionsWorkerDefaults method is used to add the settings required for
//  the function app to run in an isolated worker process; (ex.Set the default JsonSerializerOptions to
//  ignore casing on property names, Integrate with Azure Functions logging, Output binding middleware and features).

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddBlobServiceClient(hostContext.Configuration.GetSection("MyStorageConnection"))
                .WithName("copierOutputBlob");
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();

host.Run();
