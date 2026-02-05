using System;
using System.Collections.Generic;

namespace SalesAPI.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductCategory { get; set; }

    public int? ProcessNodeNm { get; set; }

    public string? PackageType { get; set; }

    public decimal UnitPriceUsd { get; set; }

    public string? LifecycleStatus { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
