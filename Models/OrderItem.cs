using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SalesAPI.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceUsd { get; set; }

    public decimal? LineTotalUsd { get; set; }

    [JsonIgnore]
    public virtual SalesOrder Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
