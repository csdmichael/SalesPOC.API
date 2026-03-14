# =============================================================================
# Network Infrastructure: VNet, Subnets, Private Endpoints, DNS
# Standalone root module — deployed independently from the app infrastructure
# =============================================================================

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
    key                  = "network.terraform.tfstate"
    use_oidc             = true
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  features {}
  resource_provider_registrations = "none"
}

# =============================================================================
# Variables
# =============================================================================

variable "resource_group_name" {
  description = "Resource group where the VNet and endpoints are created"
  type        = string
  default     = "ai-myaacoub"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "West US 2"
}

variable "sql_server_name" {
  description = "Name of the SQL Server (for private endpoint)"
  type        = string
  default     = "ai-db-poc"
}

variable "cosmos_db_account_name" {
  description = "Name of the existing Cosmos DB account"
  type        = string
  default     = "cosmos-ai-poc"
}

variable "storage_account_name" {
  description = "Name of the existing Storage Account"
  type        = string
  default     = "aistoragemyaacoub"
}



# =============================================================================
# Data sources & locals
# =============================================================================

data "azurerm_client_config" "current" {}

locals {
  sql_server_id      = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Sql/servers/${var.sql_server_name}"
  cosmos_account_id  = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.DocumentDB/databaseAccounts/${var.cosmos_db_account_name}"
  storage_account_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Storage/storageAccounts/${var.storage_account_name}"
}

# =============================================================================
# Virtual Network
# =============================================================================

resource "azurerm_virtual_network" "main" {
  name                = "vnet-salespoc-westus2"
  resource_group_name = var.resource_group_name
  location            = var.location
  address_space       = ["10.1.0.0/16"]

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# =============================================================================
# Subnets
# =============================================================================

resource "azurerm_subnet" "app_service" {
  name                 = "snet-appservice"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.1.1.0/24"]

  delegation {
    name = "appservice-delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

resource "azurerm_subnet" "private_endpoints" {
  name                 = "snet-private-endpoints"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.1.2.0/24"]
}

# =============================================================================
# Private Endpoints
# NOTE: Private DNS zones and VNet links are not managed by this module because
# the deployment service principal lacks Microsoft.Network/privateDnsZones/read
# permission. Private DNS zones (privatelink.database.windows.net,
# privatelink.documents.azure.com, privatelink.blob.core.windows.net) and their
# VNet links must be configured separately by a principal with the required
# Network permissions to enable automatic DNS resolution for these endpoints.
# =============================================================================

resource "azurerm_private_endpoint" "sql" {
  name                = "pe-sql-westus2"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "psc-sql-westus2"
    private_connection_resource_id = local.sql_server_id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }

  lifecycle {
    ignore_changes = [private_dns_zone_group]
  }

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

resource "azurerm_private_endpoint" "cosmos" {
  name                = "pe-cosmos-westus2"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "psc-cosmos-westus2"
    private_connection_resource_id = local.cosmos_account_id
    subresource_names              = ["Sql"]
    is_manual_connection           = false
  }

  lifecycle {
    ignore_changes = [private_dns_zone_group]
  }

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

resource "azurerm_private_endpoint" "blob" {
  name                = "pe-blob-westus2"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "psc-blob-westus2"
    private_connection_resource_id = local.storage_account_id
    subresource_names              = ["blob"]
    is_manual_connection           = false
  }

  lifecycle {
    ignore_changes = [private_dns_zone_group]
  }

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# =============================================================================
# Outputs
# =============================================================================

output "app_service_subnet_id" {
  description = "Subnet ID for App Service VNet integration"
  value       = azurerm_subnet.app_service.id
}

output "sql_private_endpoint_ip" {
  description = "Private IP address of the SQL Server private endpoint"
  value       = azurerm_private_endpoint.sql.private_service_connection[0].private_ip_address
}

output "cosmos_private_endpoint_ip" {
  description = "Private IP address of the Cosmos DB private endpoint"
  value       = azurerm_private_endpoint.cosmos.private_service_connection[0].private_ip_address
}

output "blob_private_endpoint_ip" {
  description = "Private IP address of the Blob Storage private endpoint"
  value       = azurerm_private_endpoint.blob.private_service_connection[0].private_ip_address
}
