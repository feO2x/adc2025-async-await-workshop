using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebApi.Contacts.Common;

namespace WebApi.Contacts.GetContact;

public static class GetContactEndpoint
{
    public static WebApplication MapGetContact(this WebApplication app)
    {
        app.MapGet("/api/contacts/{id:required:guid}", GetContact);
        return app;
    }

    public static async Task<IResult> GetContact(
        IGetContactDbSession dbSession,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var contact = await dbSession.GetContactWithAddressesAsync(id, cancellationToken);
        return contact is null ? Results.NotFound() : Results.Ok(ContactDetailDto.FromContact(contact));
    }
}