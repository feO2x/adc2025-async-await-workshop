using Light.SharedCore.Entities;

namespace WebApi.DatabaseAccess.Model;

public sealed class Order : GuidEntity
{
    public OrderState State { get; set; }
}

public enum OrderState
{
    New,
    Completed
}