using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Contacts.DeleteContact;
using WebApi.Contacts.GetContact;
using WebApi.Contacts.GetContacts;
using WebApi.Contacts.UpsertContact;

namespace WebApi.Contacts;

public static class ContactsModule
{
    public static IServiceCollection AddContacts(this IServiceCollection services) =>
        services
           .AddGetContact()
           .AddGetContacts()
           .AddUpsertContact()
           .AddDeleteContact();

    public static void MapContactsEndpoints(this WebApplication app)
    {
        app.MapGetContact();
        app.MapGetContacts();
        app.MapUpsertContact();
        app.MapDeleteContact();
    }
}