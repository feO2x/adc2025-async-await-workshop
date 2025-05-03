using System;
using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApi.DatabaseAccess;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.DeleteContact;

public sealed class EfDeleteContactSession : EfSession<WebApiDbContext>, IDeleteContactSession
{
    public EfDeleteContactSession(WebApiDbContext dbContext) : base(dbContext) { }
    
    public Task<Contact?> GetContactAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbContext
           .Contacts
           .Include(c => c.Addresses)
           .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void RemoveContact(Contact contact)
    {
        DbContext.Addresses.RemoveRange(contact.Addresses);
        DbContext.Contacts.Remove(contact);
    }
}