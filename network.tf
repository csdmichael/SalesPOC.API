# =============================================================================
# Network Infrastructure: VNet, Subnets, Private Endpoints, DNS
# Deployed via workflow — overwrites existing settings or creates if not exist
# Resources: SQL Server, Cosmos DB, Blob Storage private endpoints
# =============================================================================

# ---------- Variables for existing resources ---------------------------------

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

variable "network_resource_group_name" {
  description = "Resource group that contains the Cosmos DB and Storage accounts"
  type        = string
  default     = "ai-myaacoub"
}

# ---------- Construct resource IDs (avoids Reader role on ai-myaacoub) -------

data "azurerm_client_config" "current" {}

locals {
  cosmos_account_id  = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.network_resource_group_name}/providers/Microsoft.DocumentDB/databaseAccounts/${var.cosmos_db_account_name}"
  storage_account_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.network_resource_group_name}/providers/Microsoft.Storage/storageAccounts/${var.storage_account_name}"
}

# =============================================================================
# Virtual Network
# =============================================================================

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

# =============================================================================
# Subnets
# =============================================================================

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

# Subnet for private endpoints (SQL, Cosmos DB, Blob Storage)
resource "azurerm_subnet" "private_endpoints" {
  name                 = "snet-private-endpoints"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.2.0/24"]
}

# =============================================================================
# Private DNS Zones
# =============================================================================

# Private DNS zone for SQL Server
resource "azurerm_private_dns_zone" "sql" {
  name                = "privatelink.database.windows.net"
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Private DNS zone for Cosmos DB
resource "azurerm_private_dns_zone" "cosmos" {
  name                = "privatelink.documents.azure.com"
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Private DNS zone for Blob Storage
resource "azurerm_private_dns_zone" "blob" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# =============================================================================
# Private DNS Zone VNet Links
# =============================================================================

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

resource "azurerm_private_dns_zone_virtual_network_link" "cosmos" {
  name                  = "vnetlink-cosmos"
  resource_group_name   = azurerm_resource_group.main.name
  private_dns_zone_name = azurerm_private_dns_zone.cosmos.name
  virtual_network_id    = azurerm_virtual_network.main.id
  registration_enabled  = false

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  name                  = "vnetlink-blob"
  resource_group_name   = azurerm_resource_group.main.name
  private_dns_zone_name = azurerm_private_dns_zone.blob.name
  virtual_network_id    = azurerm_virtual_network.main.id
  registration_enabled  = false

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# =============================================================================
# Private Endpoints
# =============================================================================

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

# Private Endpoint for Cosmos DB
resource "azurerm_private_endpoint" "cosmos" {
  name                = "pe-cosmos-salespoc"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "psc-cosmos-salespoc"
    private_connection_resource_id = local.cosmos_account_id
    subresource_names              = ["Sql"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "dns-zone-group-cosmos"
    private_dns_zone_ids = [azurerm_private_dns_zone.cosmos.id]
  }

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# Private Endpoint for Blob Storage
resource "azurerm_private_endpoint" "blob" {
  name                = "pe-blob-salespoc"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "psc-blob-salespoc"
    private_connection_resource_id = local.storage_account_id
    subresource_names              = ["blob"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "dns-zone-group-blob"
    private_dns_zone_ids = [azurerm_private_dns_zone.blob.id]
  }

  tags = {
    environment = "production"
    application = "SalesAPI"
  }
}

# =============================================================================
# Outputs
# =============================================================================

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
