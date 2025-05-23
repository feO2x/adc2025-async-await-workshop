﻿using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AsyncVsSync.App;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = Logging.CreateLogger();

        try
        {
            using var app = new CommandLineApplication();
            app.Name = "Batch HTTP Client";
            app.Description = "Sends a batch of requests to the backend at once.";

            var numberOfRequestsOption = app.Option(
                "-n|--number-of-requests <number>",
                "The number of requests that should be performed against the backend. The default value is 1000 requests.",
                CommandOptionType.SingleOrNoValue
            );

            var waitIntervalOption = app.Option(
                "-w|--wait-interval <milliseconds>",
                "The time span in milliseconds that the backend will wait during a single request. The default value is 1000ms.",
                CommandOptionType.SingleOrNoValue
            );

            var targetOption = app.Option(
                "-t|--target <api>",
                "The target API. Can be either \"sync\" or \"async\". The default value is \"sync\".",
                CommandOptionType.SingleOrNoValue
            );

            var logLevelOption = app.Option(
                "-l|--log-level <log-level>",
                "The log level. Default is \"Warning\". Set to \"Debug\" or \"Information\" to see more messages from the HTTP client",
                CommandOptionType.SingleOrNoValue
            );

            var urlOption = app.Option(
                "-u|--url <URL>",
                "The URL to target. The default is \"http://localhost:5203\".",
                CommandOptionType.SingleOrNoValue
            );

            app.HelpOption();

            app.OnExecuteAsync(async cancellationToken =>
                {
                    await using var serviceProvider = DependencyInjection.CreateServiceProvider();

                    // Validate and set parameters
                    if (Enum.TryParse(logLevelOption.Value(), true, out LogEventLevel logLevel))
                    {
                        serviceProvider.GetRequiredService<LoggingLevelSwitch>().MinimumLevel = logLevel;
                    }

                    var numberOfRequests = 1000;
                    if (int.TryParse(numberOfRequestsOption.Value(), out var parsedNumberOfRequests) &&
                        parsedNumberOfRequests > 0)
                    {
                        numberOfRequests = parsedNumberOfRequests;
                    }

                    var waitInterval = 1000;
                    if (int.TryParse(waitIntervalOption.Value(), out var parsedWaitInterval) && parsedWaitInterval > 0)
                    {
                        waitInterval = parsedWaitInterval;
                    }

                    var endpointRelativeUrl = "/sync";
                    if ("async".Equals(targetOption.Value(), StringComparison.OrdinalIgnoreCase))
                    {
                        endpointRelativeUrl = "/async";
                    }

                    var baseAddressText =
                        serviceProvider.GetRequiredService<IConfiguration>()["url"] ??
                        "http://localhost:5203";
                    var baseAddress = new Uri(baseAddressText, UriKind.Absolute);
                    if (Uri.TryCreate(urlOption.Value(), UriKind.Absolute, out var parsedUri))
                    {
                        baseAddress = parsedUri;
                    }

                    // Perform the calls against the service
                    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                    using var httpClient = httpClientFactory.CreateClient();
                    var url = new Uri(baseAddress, $"{endpointRelativeUrl}?waitIntervalInMilliseconds={waitInterval}");

                    Console.WriteLine($"Performing {numberOfRequests} requests against {url}...");

                    var requestTasks = new Task<bool>[numberOfRequests];

                    var startTimestamp = Stopwatch.GetTimestamp();
                    for (var i = 0; i < numberOfRequests; i++)
                    {
                        requestTasks[i] = PerformRequestAsync(httpClient, url, cancellationToken);
                    }

                    await Task.WhenAll(requestTasks);
                    var elapsedTime = Stopwatch.GetElapsedTime(startTimestamp);

                    // Assemble the results
                    var resultsUrl = new Uri(baseAddress, "/results");
                    var threadPoolResults = await httpClient.GetFromJsonAsync<ThreadPoolResults>(
                        resultsUrl,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                        cancellationToken
                    );

                    if (threadPoolResults is null)
                    {
                        Log.Error("Could not obtain thread pool results");
                        return 1;
                    }

                    // Determine the number of OK responses and failed responses (likely timeouts)
                    var numberOfOkResponses = 0;
                    var numberOfFailedResponses = 0;
                    for (var i = 0; i < requestTasks.Length; i++)
                    {
                        if (requestTasks[i].Result)
                        {
                            numberOfOkResponses++;
                        }
                        else
                        {
                            numberOfFailedResponses++;
                        }
                    }

                    Console.WriteLine($"Number of requests: {numberOfRequests}");
                    Console.WriteLine($"Wait interval per request: {waitInterval}ms");
                    Console.WriteLine($"Number of successful requests: {numberOfOkResponses}");
                    Console.WriteLine($"Number of failed requests: {numberOfFailedResponses}");
                    Console.WriteLine(
                        $"Backend maximum number of concurrent worker threads: {threadPoolResults.MaximumUsedWorkerThreads}"
                    );
                    Console.WriteLine($"Backend total number of threads: {threadPoolResults.TotalNumberOfThreads}");
                    Console.WriteLine(
                        $"Backend Possible number of worker threads: {threadPoolResults.MaximumWorkerThreads}"
                    );
                    Console.WriteLine($"Backend running on {threadPoolResults.OsDescription}");
                    Console.WriteLine($"All done in {elapsedTime.TotalSeconds:N2} seconds");

                    return 0;
                }
            );

            return await app.ExecuteAsync(args);
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "An error occurred");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static async Task<bool> PerformRequestAsync(
        HttpClient httpClient,
        Uri url,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Request could not be completed successfully");
            return false;
        }
    }
}

public sealed record ThreadPoolResults(
    int MaximumUsedWorkerThreads,
    int MaximumWorkerThreads,
    int TotalNumberOfThreads,
    string OsDescription
);