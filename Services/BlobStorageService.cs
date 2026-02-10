using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SalesAPI.Models;

namespace SalesAPI.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    /// <summary>
    /// Lists all blobs in the container whose name contains the given product name prefix.
    /// Each product typically has: Engineering DataSheet, Marketing Brief, Sales One Pager.
    /// </summary>
    public async Task<List<ProductDocument>> GetDocumentsByProductAsync(string productName)
    {
        var documents = new List<ProductDocument>();

        // Search for blobs whose path/name contains the product name (case-insensitive)
        await foreach (BlobItem blob in _containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, productName, CancellationToken.None))
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);
            var docType = InferDocumentType(blob.Name);

            documents.Add(new ProductDocument
            {
                FileName = blob.Name,
                ProductName = productName,
                DocumentType = docType,
                Size = blob.Properties.ContentLength ?? 0,
                ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                LastModified = blob.Properties.LastModified,
                Url = blobClient.Uri.ToString()
            });
        }

        return documents;
    }

    /// <summary>
    /// Lists all blobs in the container grouped by product name.
    /// </summary>
    public async Task<List<ProductDocument>> GetAllDocumentsAsync()
    {
        var documents = new List<ProductDocument>();

        await foreach (BlobItem blob in _containerClient.GetBlobsAsync(new GetBlobsOptions(), CancellationToken.None))
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);
            var productName = InferProductName(blob.Name);
            var docType = InferDocumentType(blob.Name);

            documents.Add(new ProductDocument
            {
                FileName = blob.Name,
                ProductName = productName,
                DocumentType = docType,
                Size = blob.Properties.ContentLength ?? 0,
                ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                LastModified = blob.Properties.LastModified,
                Url = blobClient.Uri.ToString()
            });
        }

        return documents;
    }

    /// <summary>
    /// Downloads a blob by its full name and returns the stream with content type.
    /// </summary>
    public async Task<(Stream Content, string ContentType, string FileName)?> DownloadDocumentAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
            return null;

        var download = await blobClient.DownloadStreamingAsync();
        var contentType = download.Value.Details.ContentType ?? "application/octet-stream";

        return (download.Value.Content, contentType, Path.GetFileName(blobName));
    }

    /// <summary>
    /// Returns distinct product names found in the container based on blob naming convention.
    /// </summary>
    public async Task<List<string>> GetProductNamesAsync()
    {
        var productNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await foreach (BlobItem blob in _containerClient.GetBlobsAsync(new GetBlobsOptions(), CancellationToken.None))
        {
            var name = InferProductName(blob.Name);
            if (!string.IsNullOrEmpty(name))
                productNames.Add(name);
        }

        return productNames.OrderBy(n => n).ToList();
    }

    /// <summary>
    /// Infers the document type from the blob name.
    /// Expected types: Engineering DataSheet, Marketing Brief, Sales One Pager.
    /// </summary>
    private static string InferDocumentType(string blobName)
    {
        var lower = blobName.ToLowerInvariant();

        if (lower.Contains("engineering") || lower.Contains("datasheet") || lower.Contains("data-sheet") || lower.Contains("data_sheet"))
            return "Engineering DataSheet";

        if (lower.Contains("marketing") || lower.Contains("brief"))
            return "Marketing Brief";

        if (lower.Contains("sales") || lower.Contains("one-pager") || lower.Contains("one_pager") || lower.Contains("onepager"))
            return "Sales One Pager";

        return "Other";
    }

    /// <summary>
    /// Infers the product name from the blob name.
    /// Assumes blobs are either in a folder per product (e.g. Chip-1/doc.pdf)
    /// or prefixed with the product name (e.g. Chip-1_Engineering_DataSheet.pdf).
    /// </summary>
    private static string InferProductName(string blobName)
    {
        // If blob is in a subfolder, use the first folder segment as product name
        if (blobName.Contains('/'))
        {
            return blobName.Split('/')[0];
        }

        // Otherwise try to extract product name from underscore/dash-delimited filename
        // e.g. "Chip-1_Engineering_DataSheet.pdf" → "Chip-1"
        var separators = new[] { '_', ' ' };
        var parts = Path.GetFileNameWithoutExtension(blobName).Split(separators, 2);
        return parts.Length > 0 ? parts[0] : blobName;
    }
}
