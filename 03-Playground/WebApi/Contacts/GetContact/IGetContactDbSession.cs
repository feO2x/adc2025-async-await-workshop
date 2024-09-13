using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.GetContact;

public interface IGetContactDbSession : IAsyncDisposable
{
    Task<Contact?> GetContactWithAddressesAsync(
        Guid contactId,
        CancellationToken cancellationToken = default
    );
}