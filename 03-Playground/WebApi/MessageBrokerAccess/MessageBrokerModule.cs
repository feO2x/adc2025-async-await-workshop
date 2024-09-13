using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebApi.MessageBrokerAccess;

public static class MessageBrokerModule
{
    public static IServiceCollection AddMessageBroker(this IServiceCollection services) =>
        services
           .AddBrokerConnectionOptions()
           .AddMassTransit(x =>
                {
                    x.AddConsumer<OrderCompletedConsumer>();
                    x.UsingRabbitMq((context, configurator) =>
                        {
                            var options = context.GetRequiredService<BrokerConnectionOptions>();
                            configurator.Host(
                                options.Host,
                                options.Port,
                                options.VirtualHostName,
                                h =>
                                {
                                    h.Username(options.UserName);
                                    h.Password(options.Password);
                                }
                            );
                            configurator.ConfigureEndpoints(context);
                        }
                    );
                }
            );

    private static IServiceCollection AddBrokerConnectionOptions(
        this IServiceCollection services,
        string sectionName = BrokerConnectionOptions.DefaultSectionName
    ) =>
        services
           .AddOptions<BrokerConnectionOptions>()
           .BindConfiguration(sectionName)
           .ValidateDataAnnotations()
           .ValidateOnStart()
           .Services
           .AddSingleton(sp => sp.GetRequiredService<IOptions<BrokerConnectionOptions>>().Value);
}