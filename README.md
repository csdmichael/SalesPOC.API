# SalesPOC.API

## Main project

- SalesPOC UI: https://github.com/csdmichael/SalesPOC.UI

A comprehensive REST API for Sales Management built with ASP.NET Core 10.0 and Entity Framework Core. This proof-of-concept application provides endpoints for managing customers, products, sales orders, and sales representatives, with integrated Azure AI capabilities for natural language queries.

## Project Overview

SalesPOC.API is a modern sales management system that demonstrates:
- RESTful API design patterns
- Entity Framework Core with SQL Server
- Azure AI Foundry integration for intelligent chat-based queries
- OpenAPI/Swagger documentation
- CORS support for Angular frontend integration

## Prerequisites

### Grant the App Service Managed Identity access to the SQL Database

Connect to the `ai-db-poc` database as an AAD admin and run:

```sql
CREATE USER [salespoc-api] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [salespoc-api];
ALTER ROLE db_datawriter ADD MEMBER [salespoc-api];
```

### Assign Azure AI roles to the Managed Identity

## Setup required

# Define Variables
SUBSCRIPTION_ID="86b37969-9445-49cf-b03f-d8866235171c"
RESOURCE_GROUP="ai-myaacoub"
AI_ACCOUNT_NAME="001-ai-poc"
APP_SERVICE_NAME="salespoc-api"

# 1. Enable Managed Identity and capture the ID
PRINCIPAL_ID=$(az webapp identity assign --name "$APP_SERVICE_NAME" --resource-group "$RESOURCE_GROUP" --query principalId --output tsv)

# 2. Verify the ID is not empty
if [ -z "$PRINCIPAL_ID" ]; then
    echo "Error: Failed to retrieve Principal ID. Check if the App Service exists."
else
    echo "Principal ID retrieved: $PRINCIPAL_ID"
fi

# Construct the resource scope
SCOPE="/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.CognitiveServices/accounts/$AI_ACCOUNT_NAME"

# 3. Assign the "Azure AI Developer" role
az role assignment create --assignee-object-id "$PRINCIPAL_ID" --role "Azure AI Developer" --scope "$SCOPE" --assignee-principal-type "ServicePrincipal"

# 4. Assign the "Azure AI User" role 
az role assignment create --assignee-object-id "$PRINCIPAL_ID" --role "Azure AI User" --scope "$SCOPE" --assignee-principal-type "ServicePrincipal"

# 5. Assign the "Cognitive Services OpenAI User" role 
az role assignment create --assignee-object-id "$PRINCIPAL_ID" --role "Cognitive Services OpenAI User" --scope "$SCOPE" --assignee-principal-type "ServicePrincipal"

### Assign Azure Blob Storage roles to the Managed Identity

Key-based authentication is disabled on the storage account. The API uses `DefaultAzureCredential` with `ServiceUri` instead of a connection string.

```bash
# Assign "Storage Blob Data Reader" to the App Service managed identity
az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --role "Storage Blob Data Reader" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/aistoragemyaacoub" \
  --assignee-principal-type "ServicePrincipal"
```

### Assign Azure Cosmos DB roles to the Managed Identity

Local (key-based) authentication is disabled on the Cosmos DB account. The API uses `DefaultAzureCredential` with `AccountEndpoint` instead of a connection string.

```bash
# Assign "Cosmos DB Built-in Data Reader" (includes readMetadata, container read, and item read)
az cosmosdb sql role assignment create \
  --account-name cosmos-ai-poc \
  --resource-group "$RESOURCE_GROUP" \
  --role-definition-id "00000000-0000-0000-0000-000000000002" \
  --principal-id "$PRINCIPAL_ID" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.DocumentDB/databaseAccounts/cosmos-ai-poc"
```

### Enable public network access (if needed for local development)

Both the storage account and Cosmos DB account may have public network access disabled by default. To test locally:

```bash
# Enable public network access on the storage account
az storage account update \
  --name aistoragemyaacoub \
  --resource-group "$RESOURCE_GROUP" \
  --public-network-access Enabled

# Enable public network access on Cosmos DB
az cosmosdb update \
  --name cosmos-ai-poc \
  --resource-group "$RESOURCE_GROUP" \
  --public-network-access ENABLED
```

> **Note:** The Cosmos DB client is configured with `ConnectionMode.Gateway` to avoid direct-mode IP firewall issues. Re-disable public access after testing locally if required by policy.

## Technology Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: SQL Server with Entity Framework Core 10.0
- **AI Integration**: Azure AI Projects SDK
- **Authentication**: Azure DefaultAzureCredential
- **API Documentation**: OpenAPI 3.0 with Swagger UI
- **Infrastructure**: Terraform support for Azure deployment

## Architecture

### Layered Architecture

```
┌─────────────────────────────────────┐
│     Controllers (API Layer)         │
│  - REST endpoints                   │
│  - Request/Response handling        │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│     Models (Data Layer)             │
│  - Entity classes                   │
│  - DbContext                        │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│     SQL Server Database             │
│  - Tables                           │
│  - Views (vw_SalesFact)            │
└─────────────────────────────────────┘
```

### Key Components

1. **Controllers**: Handle HTTP requests and orchestrate business logic
2. **Models**: Define data structure and database schema
3. **SalesDbContext**: Entity Framework context managing database operations
4. **Azure AI Integration**: Chat agent for natural language queries about sales data

## Folder/File Structure

```
SalesPOC.API/
├── Controllers/               # API Controllers
│   ├── ChatController.cs     # Azure AI chat endpoint
│   ├── CustomersController.cs # Customer CRUD operations
│   ├── DealStrategyController.cs # Deal strategy analytics and action tools
│   ├── OrderItemsController.cs # Order items management
│   ├── ProductDescriptionsController.cs # Cosmos DB product descriptions
│   ├── ProductDocumentsController.cs # Blob Storage product documents
│   ├── ProductsController.cs  # Product catalog management
│   ├── SalesFactsController.cs # Sales analytics (read-only view)
│   ├── SalesOrdersController.cs # Sales order management
│   └── SalesRepsController.cs  # Sales representative management
│
├── Models/                    # Data Models
│   ├── Customer.cs           # Customer entity
│   ├── OrderItem.cs          # Order line item entity
│   ├── PagedResponse.cs      # Paged response wrapper
│   ├── Product.cs            # Product entity
│   ├── ProductDescription.cs # Cosmos DB product description entity
│   ├── ProductDocument.cs    # Blob Storage product document entity
│   ├── SalesDbContext.cs     # EF Core database context
│   ├── SalesOrder.cs         # Sales order entity
│   ├── SalesRep.cs           # Sales representative entity
│   └── VwSalesFact.cs        # Sales fact view entity (analytics)
│
├── Services/                  # Service Layer
│   ├── BlobStorageService.cs  # Azure Blob Storage document operations
│   └── CosmosDbService.cs     # Azure Cosmos DB product description operations
│
├── Properties/                # Application properties
│   └── launchSettings.json   # Development launch settings
│
├── .github/                   # GitHub configuration
├── .vscode/                   # VS Code settings
├── Program.cs                # Application entry point and configuration
├── SalesAPI.csproj           # Project file
├── appsettings.json          # Configuration settings
├── main.tf                   # Terraform infrastructure definition (core resources)
├── network.tf                # Pointer — networking moved to infra/network/
├── infra/
│   └── network/
│       └── main.tf           # Private VNet, subnets, private endpoints, DNS (separate root module)
├── terraform.tfvars.example  # Terraform variables template
├── openapi.json              # OpenAPI specification
├── swagger.json              # Swagger documentation
├── SalesAPI.http             # HTTP request examples
└── README.md                 # This file
```

## API Operations

### Base URL
- **Development**: `https://localhost:{port}/api`
- **Swagger UI**: `https://localhost:{port}/swagger`

### Customers API (`/api/Customers`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Customers` | Get all customers |
| GET | `/api/Customers/{id}` | Get customer by ID |
| POST | `/api/Customers` | Create new customer |
| PUT | `/api/Customers/{id}` | Update customer |
| DELETE | `/api/Customers/{id}` | Delete customer |

### Products API (`/api/Products`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Products` | Get all products |
| GET | `/api/Products/{id}` | Get product by ID |
| GET | `/api/Products/category/{category}` | Get products by category |
| POST | `/api/Products` | Create new product |
| PUT | `/api/Products/{id}` | Update product |
| DELETE | `/api/Products/{id}` | Delete product |

### Sales Orders API (`/api/SalesOrders`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/SalesOrders` | Get all orders (with customer, rep, and items) |
| GET | `/api/SalesOrders/{id}` | Get order by ID (with details) |
| GET | `/api/SalesOrders/customer/{customerId}` | Get orders by customer |
| GET | `/api/SalesOrders/salesrep/{salesRepId}` | Get orders by sales rep |
| POST | `/api/SalesOrders` | Create new order |
| PUT | `/api/SalesOrders/{id}` | Update order |
| DELETE | `/api/SalesOrders/{id}` | Delete order |

### Order Items API (`/api/OrderItems`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/OrderItems` | Get all order items |
| GET | `/api/OrderItems/{id}` | Get order item by ID |
| GET | `/api/OrderItems/order/{orderId}` | Get items by order ID |
| POST | `/api/OrderItems` | Create new order item |
| PUT | `/api/OrderItems/{id}` | Update order item |
| DELETE | `/api/OrderItems/{id}` | Delete order item |

### Sales Representatives API (`/api/SalesReps`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/SalesReps` | Get all sales reps |
| GET | `/api/SalesReps/{id}` | Get sales rep by ID |
| GET | `/api/SalesReps/region/{region}` | Get sales reps by region |
| POST | `/api/SalesReps` | Create new sales rep |
| PUT | `/api/SalesReps/{id}` | Update sales rep |
| DELETE | `/api/SalesReps/{id}` | Delete sales rep |

### Sales Facts API (`/api/SalesFacts`)

*Read-only analytical view combining sales data*

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/SalesFacts` | Get all sales facts |
| GET | `/api/SalesFacts/customer/{customerName}` | Filter by customer name |
| GET | `/api/SalesFacts/product/{productName}` | Filter by product name |
| GET | `/api/SalesFacts/salesrep/{repName}` | Filter by sales rep name |
| GET | `/api/SalesFacts/region/{region}` | Filter by region |
| GET | `/api/SalesFacts/category/{category}` | Filter by product category |

### Chat API (`/api/Chat`)

*Azure AI-powered natural language query interface*

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Chat` | Send natural language question to AI agent |

**Request Body:**
```json
{
  "question": "What are the total sales for Q1?"
}
```

**Response:**
```json
{
  "reply": "Based on the data, Q1 sales total..."
}
```

### Deal Strategy API (`/api/DealStrategy`)

*Purpose-built APIs for the Deal Strategy Agent (account analysis + action generation)*

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/DealStrategy/getCustomerSummary/{customerId}` | Customer profile, order, revenue, and category snapshot |
| GET | `/api/DealStrategy/getCustomerOrderTrends/{customerId}?timeRange=180d` | Monthly trend points for orders and revenue |
| GET | `/api/DealStrategy/getCustomerProductMix/{customerId}` | Category and top-product spend mix |
| GET | `/api/DealStrategy/getRegionalPerformance/{regionId}` | Region-wide performance and top reps |
| GET | `/api/DealStrategy/getRepPerformance/{repId}` | Individual rep performance metrics |
| GET | `/api/DealStrategy/calculateDealRisk/{customerId}` | Heuristic risk score and risk signals |
| GET | `/api/DealStrategy/identifyCrossSellOpportunities/{customerId}` | Suggested cross-sell products from peer patterns |
| GET | `/api/DealStrategy/generateExecutiveSummary/{customerId}` | Narrative account summary with priorities |
| POST | `/api/DealStrategy/createFollowUpTask/{customerId}` | Create follow-up task payload from action text |
| POST | `/api/DealStrategy/draftCustomerEmail/{customerId}` | Generate draft account email from strategy text |

**Action request examples:**

`POST /api/DealStrategy/createFollowUpTask/42`
```json
{
   "action": "Schedule a QBR to review decline in the last 90 days and propose recovery plan"
}
```

`POST /api/DealStrategy/draftCustomerEmail/42`
```json
{
   "strategy": "Position premium package bundle for Q2 renewal and align on adoption milestones"
}
```

## API Documentation (Swagger)

Interactive API documentation is available via Swagger UI when running in development mode:

**Swagger UI URL**: `https://localhost:{port}/swagger`

The Swagger interface provides:
- Complete API endpoint documentation
- Request/response schemas
- Interactive API testing
- Model definitions
- Example requests

The OpenAPI specification is also available at:
- **OpenAPI JSON**: `https://localhost:{port}/openapi/v1.json`

## Configuration

### Required Settings (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;Encrypt=True;Authentication=Active Directory Default;"
  },
  "AzureBlobStorage": {
    "ServiceUri": "https://<storage-account>.blob.core.windows.net",
    "ContainerName": "<blob-container-name>"
  },
  "CosmosDb": {
    "AccountEndpoint": "https://<cosmos-account>.documents.azure.com:443/",
    "DatabaseName": "<database-name>",
    "ContainerName": "<container-name>"
  },
  "AzureAgent": {
    "Endpoint": "https://your-ai-project.services.ai.azure.com/api/projects/...",
    "AgentName": "your-agent-name",
    "TenantId": "your-tenant-id"
  }
}
```

### Environment Variables

Azure authentication uses `DefaultAzureCredential`, which supports:
- Azure CLI authentication
- Managed Identity
- Environment variables
- Interactive browser authentication

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- SQL Server (local or Azure SQL)
- Azure subscription (for AI features)

### Running Locally

1. **Clone the repository**
   ```bash
   git clone https://github.com/csdmichael/SalesPOC.API.git
   cd SalesPOC.API
   ```

2. **Update configuration**
   - Edit `appsettings.json` with your database connection string
   - Configure Azure AI settings if using chat features

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Run database migrations** (if applicable)
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access Swagger UI**
   - Navigate to `https://localhost:{port}/swagger`
   - Port number will be displayed in console output

## Database Schema

### Main Tables
- **Customers**: Customer information and business details
- **Products**: Product catalog with pricing
- **SalesReps**: Sales representative information
- **SalesOrders**: Order headers with customer and rep references
- **OrderItems**: Order line items with product and quantity details

### Views
- **vw_SalesFact**: Denormalized view for analytics and reporting

## CORS Configuration

The API is configured to accept requests from Angular frontend running on `http://localhost:4200`.

## Private Network & Endpoint Configuration

All data sources — Azure SQL Server, Cosmos DB, and Blob Storage — have **public network access disabled**. All traffic flows through private endpoints inside a shared Virtual Network. The networking resources are defined in `infra/network/main.tf` as a **separate Terraform root module** with its own state, deployed independently via the `deploy-networking` GitHub Actions job.

### Network Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│  VNet: vnet-salespoc-api (10.0.0.0/16)                               │
│                                                                      │
│  ┌───────────────────────┐  ┌──────────────────────────────────────┐ │
│  │ snet-appservice        │  │ snet-private-endpoints              │ │
│  │ 10.0.1.0/24            │  │ 10.0.2.0/24                        │ │
│  │                        │  │                                    │ │
│  │ App Service ◄── VNet   │  │ pe-sql-salespoc    ────────────────┼─┼──► Azure SQL
│  │ Integration            │  │                                    │ │    ai-db-poc
│  │                        │  │ pe-cosmos-salespoc  ───────────────┼─┼──► Cosmos DB
│  │                        │  │                                    │ │    cosmos-ai-poc
│  │                        │  │ pe-blob-salespoc    ───────────────┼─┼──► Blob Storage
│  │                        │  │                                    │ │    aistoragemyaacoub
│  └───────────────────────┘  └──────────────────────────────────────┘ │
│                                                                      │
│  Private DNS Zones:                                                  │
│    privatelink.database.windows.net   (vnetlink-sql)                 │
│    privatelink.documents.azure.com    (vnetlink-cosmos)              │
│    privatelink.blob.core.windows.net  (vnetlink-blob)                │
└──────────────────────────────────────────────────────────────────────┘
```

### Resources (defined in `network.tf`)

| Resource | Name | Purpose |
|----------|------|---------|
| Virtual Network | `vnet-salespoc-api` | Isolated network (10.0.0.0/16) |
| Subnet | `snet-appservice` (10.0.1.0/24) | App Service VNet integration (delegated to `Microsoft.Web/serverFarms`) |
| Subnet | `snet-private-endpoints` (10.0.2.0/24) | Hosts all private endpoints |
| Private DNS Zone | `privatelink.database.windows.net` | Resolves SQL FQDN to private IP |
| Private DNS Zone | `privatelink.documents.azure.com` | Resolves Cosmos DB FQDN to private IP |
| Private DNS Zone | `privatelink.blob.core.windows.net` | Resolves Blob Storage FQDN to private IP |
| DNS Zone VNet Link | `vnetlink-sql` | Links SQL DNS zone to the VNet |
| DNS Zone VNet Link | `vnetlink-cosmos` | Links Cosmos DB DNS zone to the VNet |
| DNS Zone VNet Link | `vnetlink-blob` | Links Blob Storage DNS zone to the VNet |
| Private Endpoint | `pe-sql-salespoc` | Private connection to Azure SQL Server (`sqlServer` sub-resource) |
| Private Endpoint | `pe-cosmos-salespoc` | Private connection to Cosmos DB (`Sql` sub-resource) |
| Private Endpoint | `pe-blob-salespoc` | Private connection to Blob Storage (`blob` sub-resource) |
| VNet Integration | Swift connection | Routes App Service outbound traffic through the VNet |

### Key Settings

- **SQL Server**: `public_network_access_enabled = false`
- **Cosmos DB**: Public network access disabled; private endpoint via `pe-cosmos-salespoc`
- **Blob Storage**: Public network access disabled; private endpoint via `pe-blob-salespoc`
- **App Service**: `WEBSITE_VNET_ROUTE_ALL = 1` — all outbound traffic routed through VNet
- **Private Endpoints**: Auto-approved, DNS auto-registered via zone groups

### CI/CD Pipeline

The GitHub Actions workflow (`main_salespoc-api.yml`) has three jobs:

1. **build** — Compiles and publishes the .NET app
2. **deploy-networking** — Runs `terraform init/plan/apply` in `infra/network/` (VNet, subnets, private endpoints, DNS zones)
3. **deploy** — Deploys the application to Azure App Service (depends on both `build` and `deploy-networking`)

### RBAC Pre-requisites

Before deploying, the GitHub Actions service principal (or whoever runs Terraform) needs these Azure permissions:

| Action | Required Role | Scope |
|--------|--------------|-------|
| Create/manage VNet, subnets, private endpoints, DNS | **Network Contributor** | Resource group `rg-salespoc-api` |
| Auto-approve private endpoint to Cosmos DB | **Network Contributor** | Resource group `ai-myaacoub` |
| Auto-approve private endpoint to Storage Account | **Network Contributor** | Resource group `ai-myaacoub` |

> Resource IDs for Cosmos DB and Storage are constructed from variable names, so **Reader** on `ai-myaacoub` is **not** required.

Grant roles to the service principal used by GitHub Actions (replace `<SP_OBJECT_ID>` with the Github App OIDC service principal object ID):

```bash
SUBSCRIPTION_ID="86b37969-9445-49cf-b03f-d8866235171c"

# Network Contributor on the API resource group (VNet, subnets, endpoints, DNS)
az role assignment create \
  --assignee-object-id "<SP_OBJECT_ID>" \
  --role "Network Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-salespoc-api" \
  --assignee-principal-type "ServicePrincipal"

# Network Contributor on ai-myaacoub RG (to auto-approve private endpoint connections)
az role assignment create \
  --assignee-object-id "<SP_OBJECT_ID>" \
  --role "Network Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/ai-myaacoub" \
  --assignee-principal-type "ServicePrincipal"
```

> **Tip:** If using OIDC federated credentials, the service principal already has `id-token: write` in the workflow. The roles above are additive Azure RBAC assignments.

### Local Development Note

The private endpoints are only reachable from within the VNet. To connect from a local dev machine you must either:
1. Temporarily enable public access and add a firewall rule for your IP
2. Use an Azure VPN Gateway or Point-to-Site VPN into the VNet

To modify CORS settings, update the policy in `Program.cs`:
```csharp
policy.WithOrigins("http://localhost:4200")
      .AllowAnyHeader()
      .AllowAnyMethod();
```

## Deployment

Terraform configuration is included for Azure deployment:
- `main.tf`: Core infrastructure (App Service, SQL Server, App Insights)
- `infra/network/main.tf`: Private VNet, subnets, private endpoints for SQL/Cosmos DB/Blob Storage, DNS zones (separate Terraform root module)
- `terraform.tfvars.example`: Template for deployment variables

### GitHub Actions Workflow

The CI/CD pipeline (`.github/workflows/main_salespoc-api.yml`) runs three jobs:

| Job | Runner | Purpose |
|-----|--------|---------|
| `build` | `windows-latest` | Build & publish .NET 10 app |
| `deploy-networking` | `ubuntu-latest` | Terraform apply `infra/network/` for VNet + private endpoints |
| `deploy` | `windows-latest` | Deploy app to Azure App Service |

`deploy-networking` and `deploy` both depend on `build`; `deploy` also waits for `deploy-networking` to finish so that the private endpoints are in place before the app starts.

## License

[Specify your license here]

## Contributing

[Specify contribution guidelines here]
