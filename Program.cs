using Azure.AI.Projects;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SalesAPI.Models;
using SalesAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Azure AI Project client for the Chat agent
var projectEndpoint = builder.Configuration["AzureAgent:Endpoint"]
    ?? throw new InvalidOperationException("AzureAgent:Endpoint is not configured.");

var credentialOptions = new DefaultAzureCredentialOptions();
var tenantId = builder.Configuration["AzureAgent:TenantId"];

if (!string.IsNullOrWhiteSpace(tenantId))
{
    credentialOptions.TenantId = tenantId;
}

// Keep developer-friendly credential sources in Development so local auth works.
if (!builder.Environment.IsDevelopment())
{
    credentialOptions.ExcludeVisualStudioCredential = true;
    credentialOptions.ExcludeVisualStudioCodeCredential = true;
    credentialOptions.ExcludeAzureDeveloperCliCredential = true;
    credentialOptions.ExcludeAzureCliCredential = true;
    credentialOptions.ExcludeInteractiveBrowserCredential = true;
}

builder.Services.AddSingleton(_ =>
    new AIProjectClient(new Uri(projectEndpoint),
        new DefaultAzureCredential(credentialOptions)));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add HttpClientFactory for Chat proxy
builder.Services.AddHttpClient();

// Register Azure Blob Storage service (Azure AD auth – key-based auth is disabled on this account)
var blobServiceUri = builder.Configuration["AzureBlobStorage:ServiceUri"]
    ?? throw new InvalidOperationException("AzureBlobStorage:ServiceUri is not configured.");
var blobContainerName = builder.Configuration["AzureBlobStorage:ContainerName"]
    ?? throw new InvalidOperationException("AzureBlobStorage:ContainerName is not configured.");

var credential = new DefaultAzureCredential(credentialOptions);

builder.Services.AddSingleton(_ =>
    new BlobServiceClient(new Uri(blobServiceUri), credential)
        .GetBlobContainerClient(blobContainerName));
builder.Services.AddSingleton<BlobStorageService>();

// Register Azure Cosmos DB service (Azure AD auth – local auth is disabled on this account)
var cosmosAccountEndpoint = builder.Configuration["CosmosDb:AccountEndpoint"]
    ?? throw new InvalidOperationException("CosmosDb:AccountEndpoint is not configured.");
var cosmosDatabaseName = builder.Configuration["CosmosDb:DatabaseName"]
    ?? throw new InvalidOperationException("CosmosDb:DatabaseName is not configured.");
var cosmosContainerName = builder.Configuration["CosmosDb:ContainerName"]
    ?? throw new InvalidOperationException("CosmosDb:ContainerName is not configured.");

builder.Services.AddSingleton(_ =>
    new CosmosClient(cosmosAccountEndpoint, credential, new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway
    }));
builder.Services.AddSingleton(sp =>
    new CosmosDbService(sp.GetRequiredService<CosmosClient>(), cosmosDatabaseName, cosmosContainerName));

// Configure Entity Framework with SQL Server (Azure AD authentication)
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

var app = builder.Build();

app.MapOpenApi();
app.MapGet("/openapi.json", () => Results.Redirect("/openapi/v1.json", permanent: false));
app.MapGet("/swagger.json", () => Results.Redirect("/openapi/v1.json", permanent: false));
app.MapGet("/v1/openapi.json", () => Results.Redirect("/openapi/v1.json", permanent: false));
app.MapGet("/v1/swagger.json", () => Results.Redirect("/openapi/v1.json", permanent: false));
app.MapGet("/v1/swagger/v1/swagger.json", () => Results.Redirect("/openapi/v1.json", permanent: false));
app.MapGet("/v1/v1/swagger.json", () => Results.Redirect("/openapi/v1.json", permanent: false));
app.MapGet("/v1/openapi", () => Results.Redirect("/openapi/v1.json", permanent: false));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SalesAPI v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularDev");

app.MapControllers();

app.Run();
