terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.100.0"
    }
  }
  # Note: For a true enterprise setup, backend state should be in an Azure Storage Account.
  # For local/initial pipeline execution, we will configure this in the pipeline step.
}

provider "azurerm" {
  features {}
}

# 1. Resource Group
resource "azurerm_resource_group" "rg" {
  name     = "rg-nexuslogix-enterprise"
  location = "East US" # Change to your preferred region
}

# 2. Existing ACR Reference
data "azurerm_container_registry" "acr" {
  name                = "acrnexuslogix5608"
  resource_group_name = "rg-nexuslogix-prod" # The original RG where ACR lives
}

# 3. Log Analytics (For Container Logs & Health)
resource "azurerm_log_analytics_workspace" "law" {
  name                = "law-nexuslogix"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# 4. Container Apps Environment
resource "azurerm_container_app_environment" "env" {
  name                       = "cae-nexuslogix"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.law.id
}

# 5. Managed Identity (For Secure ACR Pulls)
resource "azurerm_user_assigned_identity" "aca_identity" {
  name                = "mi-nexuslogix-aca"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

# 6. Grant Managed Identity AcrPull access to ACR
resource "azurerm_role_assignment" "acr_pull" {
  scope                = data.azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.aca_identity.principal_id
}

# 7. Local configuration matrix for all 6 Container Apps
locals {
  services = {
    "frontend-qa"   = { is_public = true, target_port = 80, env = "qa" }
    "backend-qa"    = { is_public = false, target_port = 80, env = "qa" } # Private internal ingress
    "gateway-qa"    = { is_public = true, target_port = 80, env = "qa" }
    "frontend-prod" = { is_public = true, target_port = 80, env = "prod" }
    "backend-prod"  = { is_public = false, target_port = 80, env = "prod" } # Private internal ingress
    "gateway-prod"  = { is_public = true, target_port = 80, env = "prod" }
  }
}

# 8. Create the Container Apps dynamically
resource "azurerm_container_app" "services" {
  for_each                     = local.services
  name                         = each.key
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.aca_identity.id]
  }

  registry {
    server   = data.azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.aca_identity.id
  }

  ingress {
    allow_insecure_connections = false
    external                   = each.value.is_public
    target_port                = each.value.target_port
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    # Using Microsoft's placeholder image for initial infrastructure creation.
    # The CD pipeline will deploy the actual application images later.
    container {
      name   = each.key
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.5
      memory = "1Gi"
      
      env {
        name  = "ENVIRONMENT"
        value = each.value.env
      }
    }
  }

  # CRITICAL: Ignore changes to the template so Terraform doesn't overwrite 
  # the real application images pushed by the CD pipeline on future runs.
  lifecycle {
    ignore_changes = [
      template
    ]
  }
}