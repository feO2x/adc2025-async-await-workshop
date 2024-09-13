using System;
using Light.GuardClauses;

namespace WebApi.TransactionalOutbox;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MessageTypeAttribute : Attribute
{
    public MessageTypeAttribute(string name)
    {
        name.MustNotBeNullOrWhiteSpace();
        Names = [name];
    }

    public MessageTypeAttribute(string[] names)
    {
        names.MustNotBeNullOrEmpty();
        foreach (var name in names)
        {
            name.MustNotBeNullOrWhiteSpace();
        }

        Names = names;
    }

    public string PrimaryName => Names[0];

    public string[] Names { get; }
}