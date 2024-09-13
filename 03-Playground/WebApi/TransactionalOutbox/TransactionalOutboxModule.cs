using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace WebApi.TransactionalOutbox;

public static class TransactionalOutboxModule
{
    public static IServiceCollection AddTransactionalOutbox(
        this IServiceCollection services,
        string configSectionPath = OutboxProcessorOptions.DefaultConfigSectionPath,
        bool registerDefaultResiliencePipeline = true
    )
    {
        if (registerDefaultResiliencePipeline)
        {
            services.AddResiliencePipeline(
                OutboxConstants.ResiliencePipelineKey,
                pipelineBuilder =>
                {
                    pipelineBuilder
                       .AddRetry(
                            new RetryStrategyOptions
                            {
                                Delay = TimeSpan.FromSeconds(1),
                                MaxDelay = TimeSpan.FromMinutes(1),
                                MaxRetryAttempts = 25,
                                BackoffType = DelayBackoffType.Linear,
                                UseJitter = true
                            }
                        );
                }
            );
        }

        services
           .AddOptions<OutboxProcessorOptions>()
           .BindConfiguration(configSectionPath)
           .ValidateOnStart()
           .Services
           .AddSingleton(sp => sp.GetRequiredService<IOptions<OutboxProcessorOptions>>().Value);

        return services
           .AddSingleton<IValidateOptions<OutboxProcessorOptions>, OutboxProcessorOptionsValidator>()
           .AddSingleton<OutboxProcessor>()
           .AddSingleton<IOutboxProcessor>(sp => sp.GetRequiredService<OutboxProcessor>())
           .AddSingleton<IOutboxTrigger>(sp => sp.GetRequiredService<OutboxProcessor>())
           .AddSingleton<IAwaitOutboxCompletion>(sp => sp.GetRequiredService<OutboxProcessor>())
           .AddHostedService<OutboxProcessorHostedService>()
           .AddSingleton<IOutboxItemFactory, OutboxItemFactory>()
           .AddSingleton(MessageTypes.CreateDefault())
           .AddScoped<IOutboxProcessorDbSession, EfOutboxProcessorSession>()
           .AddSingleton<IOutboxItemPublisher, MassTransitPublisher>();
    }
}