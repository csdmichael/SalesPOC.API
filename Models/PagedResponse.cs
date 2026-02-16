namespace SalesAPI.Models;

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalRecords { get; set; }

    public int TotalPages { get; set; }
}
