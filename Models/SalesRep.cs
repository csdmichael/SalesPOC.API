using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SalesAPI.Models;

public partial class SalesRep
{
    public int SalesRepId { get; set; }

    public string RepName { get; set; } = null!;

    public string? Region { get; set; }

    public string? Email { get; set; }

    public DateOnly? HireDate { get; set; }

    [JsonIgnore]
    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
