using System;
using System.Linq;
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

    public static IResult DeleteContact(
        WebApiDbContext dbContext,
        ILogger logger,
        Guid id
    )
    {
        var contact = dbContext
           .Contacts
           .Include(c => c.Addresses)
           .FirstOrDefault(c => c.Id == id);

        if (contact is null)
        {
            return Results.NotFound();
        }

        dbContext.Addresses.RemoveRange(contact.Addresses);
        dbContext.Contacts.Remove(contact);
        dbContext.SaveChanges();

        logger.Information("{@Contact} was deleted successfully", contact);
        return Results.Ok(ContactDetailDto.FromContact(contact));
    }
}