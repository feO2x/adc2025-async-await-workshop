using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApi.DatabaseAccess;

namespace WebApi.Contacts.GetContacts;

public sealed class EfGetContactsSession : EfClient<WebApiDbContext>, IGetContactsSession
{
    public EfGetContactsSession(WebApiDbContext dbContext) : base(dbContext) { }
    
    public Task<List<ContactListDto>> GetContactsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return DbContext
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
    }
}