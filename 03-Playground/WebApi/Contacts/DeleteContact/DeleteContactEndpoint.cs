using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebApi.Contacts.Common;
using WebApi.DatabaseAccess;

namespace WebApi.Contacts.DeleteContact;

public static class DeleteContactEndpoint
{
    public static void MapDeleteContact(this WebApplication app) =>
        app.MapDelete("/api/contacts/{id:required:guid}", DeleteContact);

    public static async Task<IResult> DeleteContact(
        WebApiDbContext dbContext,
        ILogger logger,
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var contact = await dbContext
           .Contacts
           .Include(c => c.Addresses)
           .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contact is null)
        {
            return Results.NotFound();
        }

        dbContext.Addresses.RemoveRange(contact.Addresses);
        dbContext.Contacts.Remove(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.Information("{@Contact} was deleted successfully", contact);
        return Results.Ok(ContactDetailDto.FromContact(contact));
    }
}