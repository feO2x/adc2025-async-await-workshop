using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Contacts.GetContact;

public static class GetContactModule
{
    public static IServiceCollection AddGetContact(this IServiceCollection services) =>
        services.AddScoped<IGetContactDbSession, EfGetContactSession>();
}