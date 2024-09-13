using System;
using System.Collections.Generic;

namespace WebApi.TransactionalOutbox;

public readonly record struct MessageTypesMappings(
    Dictionary<string, Type> MessageTypeToDotnetTypeMapping,
    Dictionary<Type, string> DotnetTypeToMessageTypeMapping
);