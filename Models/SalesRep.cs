using System;
using System.Collections.Generic;

namespace SalesAPI.Models;

public partial class SalesRep
{
    public int SalesRepId { get; set; }

    public string RepName { get; set; } = null!;

    public string? Region { get; set; }

    public string? Email { get; set; }

    public DateOnly? HireDate { get; set; }

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
