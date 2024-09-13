using System;
using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApi.DatabaseAccess;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.GetContact;

public sealed class EfGetContactSession : EfClient<WebApiDbContext>, IGetContactDbSession
{
    public EfGetContactSession(WebApiDbContext dbContext) : base(dbContext) { }

    public Task<Contact?> GetContactWithAddressesAsync(Guid contactId, CancellationToken cancellationToken = default) =>
        DbContext
           .Contacts
           .Include(c => c.Addresses)
           .FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);
}