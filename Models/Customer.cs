using System;
using System.Collections.Generic;

namespace SalesAPI.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerType { get; set; }

    public string? Industry { get; set; }

    public string? Country { get; set; }

    public string? State { get; set; }

    public string? City { get; set; }

    public decimal? AnnualRevenueUsd { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
