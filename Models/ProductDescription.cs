using System.Text.Json.Serialization;

namespace SalesAPI.Models;

public class ProductDescription
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
