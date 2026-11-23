namespace StoreApi.Domain.Enums;

/// <summary>Lifecycle states of a customer order.</summary>
public enum OrderStatus
{
    /// <summary>Order created, awaiting payment.</summary>
    Pending = 0,

    /// <summary>Payment received.</summary>
    Paid = 1,

    /// <summary>Order handed to the carrier.</summary>
    Shipped = 2,

    /// <summary>Order received by the customer.</summary>
    Delivered = 3,

    /// <summary>Order cancelled; stock has been restored.</summary>
    Cancelled = 4
}
