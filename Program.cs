using Azure.AI.Projects;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Models;
using SalesAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Azure AI Project client for the Chat agent
var projectEndpoint = builder.Configuration["AzureAgent:Endpoint"]
    ?? throw new InvalidOperationException("AzureAgent:Endpoint is not configured.");

builder.Services.AddSingleton(_ =>
    new AIProjectClient(new Uri(projectEndpoint),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = builder.Configuration["AzureAgent:TenantId"],
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureDeveloperCliCredential = true,
            ExcludeInteractiveBrowserCredential = true,
            ExcludeSharedTokenCacheCredential = true
        })));

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

// Register Azure Blob Storage service
var blobConnectionString = builder.Configuration["AzureBlobStorage:ConnectionString"]
    ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");
var blobContainerName = builder.Configuration["AzureBlobStorage:ContainerName"]
    ?? throw new InvalidOperationException("AzureBlobStorage:ContainerName is not configured.");

builder.Services.AddSingleton(_ =>
    new BlobContainerClient(blobConnectionString, blobContainerName));
builder.Services.AddSingleton<BlobStorageService>();

// Register Azure Cosmos DB service
var cosmosConnectionString = builder.Configuration["CosmosDb:ConnectionString"]
    ?? throw new InvalidOperationException("CosmosDb:ConnectionString is not configured.");
var cosmosDatabaseName = builder.Configuration["CosmosDb:DatabaseName"]
    ?? throw new InvalidOperationException("CosmosDb:DatabaseName is not configured.");
var cosmosContainerName = builder.Configuration["CosmosDb:ContainerName"]
    ?? throw new InvalidOperationException("CosmosDb:ContainerName is not configured.");

builder.Services.AddSingleton(_ =>
    new CosmosClient(cosmosConnectionString));
builder.Services.AddSingleton(sp =>
    new CosmosDbService(sp.GetRequiredService<CosmosClient>(), cosmosDatabaseName, cosmosContainerName));

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SalesAPI v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularDev");

app.MapControllers();

app.Run();
