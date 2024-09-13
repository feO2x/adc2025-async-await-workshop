using System;

namespace WebApi.Contacts.GetContacts;

public sealed record ContactListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber
);