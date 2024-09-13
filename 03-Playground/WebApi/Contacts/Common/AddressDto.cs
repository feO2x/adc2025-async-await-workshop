using System;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.Common;

public sealed record AddressDto(Guid Id, string Street, string ZipCode, string City)
{
    public static AddressDto FromAddress(Address address) =>
        new (address.Id, address.Street, address.ZipCode, address.City);

    public void UpdateAddress(Address address)
    {
        address.Street = Street;
        address.ZipCode = ZipCode;
        address.City = City;
    }
}