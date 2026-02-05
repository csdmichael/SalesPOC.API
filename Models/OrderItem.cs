using System;
using System.Collections.Generic;

namespace SalesAPI.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceUsd { get; set; }

    public decimal? LineTotalUsd { get; set; }

    public virtual SalesOrder Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
