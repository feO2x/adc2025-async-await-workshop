using Microsoft.Extensions.DependencyInjection;
using WebApi.Contacts.Common;

namespace WebApi.Contacts.UpsertContact;

public static class UpsertContactModule
{
    public static IServiceCollection AddUpsertContact(this IServiceCollection services) =>
        services
           .AddScoped<IUpsertContactSession, NpgsqlUpsertContactSession>()
           .AddSingleton<AddressDtoValidator>()
           .AddSingleton<ContactDetailDtoValidator>();
}