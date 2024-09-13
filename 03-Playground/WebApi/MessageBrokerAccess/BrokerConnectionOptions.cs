using System.ComponentModel.DataAnnotations;

namespace WebApi.MessageBrokerAccess;

public sealed record BrokerConnectionOptions
{
    public const string DefaultSectionName = "BrokerConnectionSettings";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, ushort.MaxValue)]
    public ushort Port { get; init; }

    [Required]
    public string UserName { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string VirtualHostName { get; init; } = "/";
}