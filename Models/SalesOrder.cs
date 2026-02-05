using System;
using System.Collections.Generic;

namespace SalesAPI.Models;

public partial class SalesOrder
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int? SalesRepId { get; set; }

    public DateOnly OrderDate { get; set; }

    public string? OrderStatus { get; set; }

    public decimal? TotalAmountUsd { get; set; }

    public string? Currency { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual SalesRep? SalesRep { get; set; }
}
