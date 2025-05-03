using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Contacts.GetContacts;

public interface IGetContactsSession : IAsyncDisposable
{
    Task<List<ContactListDto>> GetContactsAsync(int skip, int take, CancellationToken cancellationToken = default);
}