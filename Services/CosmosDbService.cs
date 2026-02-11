using Microsoft.Azure.Cosmos;
using SalesAPI.Models;

namespace SalesAPI.Services;

public class CosmosDbService
{
    private readonly Container _container;

    public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    /// <summary>
    /// Retrieves a product description by its ID (e.g. "chip-1", "Chip-100").
    /// The partition key is /id.
    /// </summary>
    public async Task<ProductDescription?> GetProductDescriptionByIdAsync(string productId)
    {
        try
        {
            // Use a query to find by id, avoiding partition key mismatch issues
            var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", productId);

            var query = _container.GetItemQueryIterator<ProductDescription>(queryDef);

            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves all product descriptions from the container.
    /// </summary>
    public async Task<List<ProductDescription>> GetAllProductDescriptionsAsync()
    {
        var query = _container.GetItemQueryIterator<ProductDescription>(
            new QueryDefinition("SELECT c.id, c.description FROM c"));

        var results = new List<ProductDescription>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    /// <summary>
    /// Searches product descriptions by partial ID match (case-insensitive).
    /// </summary>
    public async Task<List<ProductDescription>> SearchProductDescriptionsAsync(string searchTerm)
    {
        var queryDef = new QueryDefinition(
            "SELECT c.id, c.description FROM c WHERE CONTAINS(LOWER(c.id), LOWER(@search))")
            .WithParameter("@search", searchTerm);

        var query = _container.GetItemQueryIterator<ProductDescription>(queryDef);

        var results = new List<ProductDescription>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }
}
