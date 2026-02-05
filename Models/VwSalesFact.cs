using System;
using System.Collections.Generic;

namespace SalesAPI.Models;

public partial class VwSalesFact
{
    public int OrderId { get; set; }

    public DateOnly OrderDate { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerType { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductCategory { get; set; }

    public int? ProcessNodeNm { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceUsd { get; set; }

    public decimal? LineTotalUsd { get; set; }

    public string? RepName { get; set; }

    public string? Region { get; set; }
}
