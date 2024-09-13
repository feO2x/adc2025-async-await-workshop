using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WebApi.CommonValidation;
using WebApi.Contacts;
using WebApi.DatabaseAccess;
using WebApi.MessageBrokerAccess;
using WebApi.Orders;
using WebApi.TransactionalOutbox;

namespace WebApi.CompositionRoot;

public static class DependencyInjection
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder, ILogger logger)
    {
        builder.Host.UseSerilog(logger);
        builder
           .Services
           .AddSingleton(TimeProvider.System)
           .AddDatabaseAccess(builder.Configuration)
           .AddMessageBroker()
           .AddCommonValidation()
           .AddContacts()
           .AddTransactionalOutbox()
           .AddOrders()
           .AddHealthChecks();
        return builder;
    }
}