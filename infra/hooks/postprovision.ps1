#!/usr/bin/env pwsh
# Post-provision hook: reads the Document Intelligence API key
# from the provisioned resource and sets it in azd env so it can
# be injected into the backend service.

$ErrorActionPreference = "Stop"

Write-Host "Retrieving Document Intelligence API key..." -ForegroundColor Cyan

$resourceGroup = azd env get-value DOCUMENT_INTELLIGENCE_RESOURCE_GROUP
$resourceName  = azd env get-value DOCUMENT_INTELLIGENCE_NAME

$key = az cognitiveservices account keys list `
    --name $resourceName `
    --resource-group $resourceGroup `
    --query "key1" -o tsv

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to retrieve Document Intelligence API key"
    exit 1
}

azd env set DOCUMENT_INTELLIGENCE_API_KEY $key
azd env set DOCUMENT_INTELLIGENCE_ENDPOINT (azd env get-value DOCUMENT_INTELLIGENCE_ENDPOINT)

Write-Host "Document Intelligence API key saved to azd env." -ForegroundColor Green
