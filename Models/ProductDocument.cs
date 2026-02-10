namespace SalesAPI.Models;

public class ProductDocument
{
    public string FileName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset? LastModified { get; set; }
    public string Url { get; set; } = string.Empty;
}
