using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApi.CommonValidation;
using WebApi.DatabaseAccess;

namespace WebApi.Contacts.GetContacts;

public static class GetContactsEndpoint
{
    public static void MapGetContacts(this WebApplication app) =>
        app.MapGet("/api/contacts", GetContacts);

    public static async Task<IResult> GetContacts(
        WebApiDbContext dbContext,
        PagingParametersValidator validator,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default
    )
    {
        if (validator.CheckForErrors(new PagingParameters(skip, take), out var errors))
        {
            return Results.BadRequest(errors);
        }

        var contacts = await dbContext
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
           .ToListAsync(cancellationToken);
        return Results.Ok(contacts);
    }
}