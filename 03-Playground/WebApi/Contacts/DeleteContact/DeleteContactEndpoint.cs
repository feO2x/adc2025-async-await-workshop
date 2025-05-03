using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using WebApi.Contacts.Common;

namespace WebApi.Contacts.DeleteContact;

public static class DeleteContactEndpoint
{
    public static void MapDeleteContact(this WebApplication app) =>
        app.MapDelete("/api/contacts/{id:required:guid}", DeleteContact);

    public static async Task<IResult> DeleteContact(
        IDeleteContactSession deleteContactSession,
        ILogger logger,
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var contact = await deleteContactSession.GetContactAsync(id, cancellationToken);
        if (contact is null)
        {
            return Results.NotFound();
        }

        deleteContactSession.RemoveContact(contact);
        await deleteContactSession.SaveChangesAsync(cancellationToken);

        logger.Information("{@Contact} was deleted successfully", contact);
        return Results.Ok(ContactDetailDto.FromContact(contact));
    }
}

