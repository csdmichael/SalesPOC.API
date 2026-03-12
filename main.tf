# Terraform configuration to deploy SalesAPI to Azure App Service
# Target URL: https://salespoc-api.azurewebsites.net/

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
  required_version = ">= 1.0"
}

provider "azurerm" {
  features {}
}

# Variables
variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-salespoc-api"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "East US"
}

variable "app_service_plan_name" {
  description = "Name of the App Service Plan"
  type        = string
  default     = "asp-salespoc-api"
}

variable "app_service_name" {
  description = "Name of the App Service (must be globally unique)"
  type        = string
  default     = "salespoc-api"
}

variable "sql_server_name" {
  description = "Name of the SQL Server"
  type        = string
  default     = "sql-salespoc"
}

variable "sql_database_name" {
  description = "Name of the SQL Database"
  type        = string
  default     = "SalesDb"
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
  sensitive   = true
}

variable "azure_agent_endpoint" {
  description = "Azure AI Foundry project endpoint URL"
  type        = string
}

variable "azure_agent_tenant_id" {
  description = "Azure AD tenant ID for the AI agent"
  type        = string
}

variable "azure_agent_name" {
  description = "Name of the Azure AI agent"
  type        = string
  default     = "arrow-sales-agent"
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
  service_plan_id     = azurerm_service_plan.main.id

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
    "ApplicationInsights__ConnectionString"     = azurerm_application_insights.main.connection_string
    "AzureAgent__Endpoint"                      = var.azure_agent_endpoint
    "AzureAgent__TenantId"                      = var.azure_agent_tenant_id
    "AzureAgent__AgentName"                     = var.azure_agent_name
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

# Virtual Network for private connectivity
resource "azurerm_virtual_network" "main" {
  name                = "vnet-salespoc-api"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  address_space       = ["10.0.0.0/16"]

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Subnet for App Service VNet integration
resource "azurerm_subnet" "app_service" {
  name                 = "snet-appservice"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]

  delegation {
    name = "appservice-delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

# Subnet for private endpoints
resource "azurerm_subnet" "private_endpoints" {
  name                 = "snet-private-endpoints"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.2.0/24"]
}

# Private DNS zone for SQL Server
resource "azurerm_private_dns_zone" "sql" {
  name                = "privatelink.database.windows.net"
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Link private DNS zone to the VNet
resource "azurerm_private_dns_zone_virtual_network_link" "sql" {
  name                  = "vnetlink-sql"
  resource_group_name   = azurerm_resource_group.main.name
  private_dns_zone_name = azurerm_private_dns_zone.sql.name
  virtual_network_id    = azurerm_virtual_network.main.id
  registration_enabled  = false

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# SQL Server
resource "azurerm_mssql_server" "main" {
  name                          = var.sql_server_name
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "12.0"
  administrator_login           = var.sql_admin_login
  administrator_login_password  = var.sql_admin_password
  minimum_tls_version           = "1.2"
  public_network_access_enabled = false

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Private Endpoint for SQL Server
resource "azurerm_private_endpoint" "sql" {
  name                = "pe-sql-salespoc"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "psc-sql-salespoc"
    private_connection_resource_id = azurerm_mssql_server.main.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "dns-zone-group-sql"
    private_dns_zone_ids = [azurerm_private_dns_zone.sql.id]
  }

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

# VNet integration for App Service (outbound traffic goes through VNet)
resource "azurerm_app_service_virtual_network_swift_connection" "main" {
  app_service_id = azurerm_linux_web_app.main.id
  subnet_id      = azurerm_subnet.app_service.id
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

output "sql_private_endpoint_ip" {
  description = "Private IP address of the SQL Server private endpoint"
  value       = azurerm_private_endpoint.sql.private_service_connection[0].private_ip_address
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
