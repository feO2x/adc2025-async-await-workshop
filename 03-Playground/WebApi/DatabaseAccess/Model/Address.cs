using System;
using Light.SharedCore.Entities;

namespace WebApi.DatabaseAccess.Model;

public sealed class Address : GuidEntity
{
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public required string Street { get; set; } = string.Empty;
    public required string ZipCode { get; set; } = string.Empty;
    public required string City { get; set; } = string.Empty;
}