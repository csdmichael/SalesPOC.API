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
│   ├── OrderItemsController.cs # Order items management
│   ├── ProductsController.cs  # Product catalog management
│   ├── SalesFactsController.cs # Sales analytics (read-only view)
│   ├── SalesOrdersController.cs # Sales order management
│   └── SalesRepsController.cs  # Sales representative management
│
├── Models/                    # Data Models
│   ├── Customer.cs           # Customer entity
│   ├── OrderItem.cs          # Order line item entity
│   ├── Product.cs            # Product entity
│   ├── SalesDbContext.cs     # EF Core database context
│   ├── SalesOrder.cs         # Sales order entity
│   ├── SalesRep.cs           # Sales representative entity
│   └── VwSalesFact.cs        # Sales fact view entity (analytics)
│
├── Properties/                # Application properties
│   └── launchSettings.json   # Development launch settings
│
├── .github/                   # GitHub configuration
├── .vscode/                   # VS Code settings
├── Program.cs                # Application entry point and configuration
├── SalesAPI.csproj           # Project file
├── appsettings.json          # Configuration settings
├── main.tf                   # Terraform infrastructure definition
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
    "DefaultConnection": "Server=...;Database=...;User Id=...;Password=...;"
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

To modify CORS settings, update the policy in `Program.cs`:
```csharp
policy.WithOrigins("http://localhost:4200")
      .AllowAnyHeader()
      .AllowAnyMethod();
```

## Deployment

Terraform configuration is included for Azure deployment:
- `main.tf`: Infrastructure as Code definition
- `terraform.tfvars.example`: Template for deployment variables

## License

[Specify your license here]

## Contributing

[Specify contribution guidelines here]
