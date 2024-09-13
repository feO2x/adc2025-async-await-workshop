using System;

namespace WebApi.Tests.TransactionalOutbox;

[Flags]
public enum OutboxFailure
{
    None,
    ErrorAtLoadNextOutboxItems,
    ErrorAtRemoveOutboxItems,
    ErrorAtSaveChanges,
    ErrorAtPublishOutboxItem
}

public static class OutboxFailureExtensions
{
    public static bool HasFlagValue(this OutboxFailure sourceValue, OutboxFailure singleValue) =>
        (sourceValue & singleValue) == singleValue;
}