using System;
using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.DeleteContact;

public interface IDeleteContactSession : ISession
{
    Task<Contact?> GetContactAsync(Guid id, CancellationToken cancellationToken = default);
    
    void RemoveContact(Contact contact);
}