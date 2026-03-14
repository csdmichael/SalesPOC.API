# Terraform configuration to deploy SalesAPI to Azure App Service
# Target URL: https://salespoc-api.azurewebsites.net/

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
  required_version = ">= 1.11"

  backend "azurerm" {
    resource_group_name  = "ai-myaacoub"
    storage_account_name = "tfstatesalespoc"
    container_name       = "tfstate"
    key                  = "app.terraform.tfstate"
    use_oidc             = true
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  features {}
  resource_provider_registrations = "none"
}

data "azurerm_client_config" "current" {}

# Variables
variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "ai-myaacoub"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "West US 2"
}

variable "app_service_plan_name" {
  description = "Name of the App Service Plan"
  type        = string
  default     = "ASP-aimyaacoub-87dc"
}

variable "app_service_name" {
  description = "Name of the App Service (must be globally unique)"
  type        = string
  default     = "SalesPOC-API"
}

variable "sql_server_name" {
  description = "Name of the SQL Server"
  type        = string
  default     = "ai-db-poc"
}

variable "sql_database_name" {
  description = "Name of the SQL Database"
  type        = string
  default     = "ai-db-poc"
}

variable "sql_admin_login" {
  description = "SQL Server admin login"
  type        = string
  default     = "sqladmin"
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server admin password"
  type        = string
  default     = null
  sensitive   = true
}

variable "azure_agent_endpoint" {
  description = "Azure AI Foundry project endpoint URL"
  type        = string
  default     = null
}

variable "azure_agent_tenant_id" {
  description = "Azure AD tenant ID for the AI agent"
  type        = string
  default     = null
}

variable "azure_agent_name" {
  description = "Name of the Azure AI agent"
  type        = string
  default     = "arrow-sales-agent"
}

variable "blob_container_name" {
  description = "Name of the Blob Storage container"
  type        = string
  default     = "semiconductor-product-documents"
}

variable "cosmos_database_name" {
  description = "Name of the Cosmos DB database"
  type        = string
  default     = "sales"
}

variable "cosmos_container_name" {
  description = "Name of the Cosmos DB container"
  type        = string
  default     = "products"
}

variable "cosmos_db_account_name" {
  description = "Name of the existing Cosmos DB account (used in app settings)"
  type        = string
  default     = "cosmos-ai-poc"
}

variable "storage_account_name" {
  description = "Name of the existing Storage Account (used in app settings)"
  type        = string
  default     = "aistoragemyaacoub"
}

variable "vnet_name" {
  description = "Name of the VNet (managed by infra/network)"
  type        = string
  default     = "vnet-salespoc-westus2"
}

variable "app_service_subnet_name" {
  description = "Name of the App Service subnet (managed by infra/network)"
  type        = string
  default     = "snet-appservice"
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# App Service Plan (Linux for .NET 10)
resource "azurerm_service_plan" "main" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "B1" # Basic tier, adjust as needed (B1, B2, B3, S1, S2, S3, P1v2, P2v2, P3v2)

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# App Service (Web App)
resource "azurerm_linux_web_app" "main" {
  name                = var.app_service_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id          = azurerm_service_plan.main.id
  virtual_network_subnet_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Network/virtualNetworks/${var.vnet_name}/subnets/${var.app_service_subnet_name}"

  site_config {
    always_on = true

    application_stack {
      dotnet_version = "10.0"
    }

    # CORS configuration
    cors {
      allowed_origins = ["*"]
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                    = "Production"
    "WEBSITE_RUN_FROM_PACKAGE"                  = "1"
    "WEBSITE_VNET_ROUTE_ALL"                    = "1"
    "ApplicationInsights__ConnectionString"     = azurerm_application_insights.main.connection_string
    "AzureAgent__Endpoint"                      = var.azure_agent_endpoint
    "AzureAgent__TenantId"                      = var.azure_agent_tenant_id
    "AzureAgent__AgentName"                     = var.azure_agent_name
    "AzureBlobStorage__ServiceUri"              = "https://${var.storage_account_name}.blob.core.windows.net"
    "AzureBlobStorage__ContainerName"           = var.blob_container_name
    "CosmosDb__AccountEndpoint"                 = "https://${var.cosmos_db_account_name}.documents.azure.com:443/"
    "CosmosDb__DatabaseName"                    = var.cosmos_database_name
    "CosmosDb__ContainerName"                   = var.cosmos_container_name
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }

  identity {
    type = "SystemAssigned"
  }

  https_only = true

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Application Insights for monitoring
resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-salespoc-api"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

resource "azurerm_application_insights" "main" {
  name                = "appi-salespoc-api"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# SQL Server
resource "azurerm_mssql_server" "main" {
  name                              = var.sql_server_name
  resource_group_name               = azurerm_resource_group.main.name
  location                          = azurerm_resource_group.main.location
  version                           = "12.0"
  administrator_login               = var.sql_admin_login
  administrator_login_password_wo   = var.sql_admin_password
  administrator_login_password_wo_version = var.sql_admin_password != null ? 1 : null
  minimum_tls_version               = "1.2"
  public_network_access_enabled     = false

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# SQL Database
resource "azurerm_mssql_database" "main" {
  name           = var.sql_database_name
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 2
  sku_name       = "Basic" # Basic, S0, S1, S2, S3, P1, P2, P4, P6, P11, P15

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Outputs
output "app_service_url" {
  description = "The URL of the deployed App Service"
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
}

output "app_service_name" {
  description = "The name of the App Service"
  value       = azurerm_linux_web_app.main.name
}

output "resource_group_name" {
  description = "The name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "sql_server_fqdn" {
  description = "The fully qualified domain name of the SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "application_insights_instrumentation_key" {
  description = "Application Insights instrumentation key"
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}

output "application_insights_connection_string" {
  description = "Application Insights connection string"
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}
