using System.Collections.Generic;
using Light.SharedCore.Entities;

namespace WebApi.DatabaseAccess.Model;

public sealed class Contact : GuidEntity
{
    private List<Address>? _addresses;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public List<Address> Addresses
    {
        get => _addresses ??= [];
        set => _addresses = value;
    }
}