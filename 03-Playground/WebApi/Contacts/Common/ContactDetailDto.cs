using System;
using System.Linq;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.Common;

public sealed record ContactDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    AddressDto[] Addresses
)
{
    public static ContactDetailDto FromContact(Contact contact) =>
        new (
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.Email,
            contact.PhoneNumber,
            contact.Addresses.Select(AddressDto.FromAddress).ToArray()
        );
}