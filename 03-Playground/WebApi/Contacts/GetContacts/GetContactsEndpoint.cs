using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebApi.CommonValidation;
using WebApi.DatabaseAccess;

namespace WebApi.Contacts.GetContacts;

public static class GetContactsEndpoint
{
    public static void MapGetContacts(this WebApplication app) =>
        app.MapGet("/api/contacts", GetContacts);

    public static IResult GetContacts(
        WebApiDbContext dbContext,
        PagingParametersValidator validator,
        int skip = 0,
        int take = 20
    )
    {
        if (validator.CheckForErrors(new PagingParameters(skip, take), out var errors))
        {
            return Results.BadRequest(errors);
        }

        var contacts = dbContext
           .Contacts
           .OrderBy(contact => contact.LastName)
           .Skip(skip)
           .Take(take)
           .Select(contact => new ContactListDto(
                       contact.Id,
                       contact.FirstName,
                       contact.LastName,
                       contact.Email,
                       contact.PhoneNumber
                   )
            )
           .ToList();
        return Results.Ok(contacts);
    }
}