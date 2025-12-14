
resource_group="<your-resource-group-name>"
cosmos_account="<your-cosmos-account-name>"

echo "Configuring $resource_group/$cosmos_account..."

principal_id=$(az ad signed-in-user show --query id -o tsv)

az cosmosdb sql role assignment create \
  --account-name $cosmos_account \
  --resource-group $resource_group \
  --scope "/" \
  --principal-id $principal_id \
  --role-definition-name "Cosmos DB Built-in Data Contributor"

az cosmosdb sql database create \
  --account-name $cosmos_account \
  --resource-group $resource_group \
  --name "ancilladb"

az cosmosdb sql container create \
  --account-name $cosmos_account \
  --resource-group $resource_group \
  --database-name "ancilladb" \
  --name "notes" \
  --partition-key-path "/agentPhoneNumber"

az cosmosdb sql container create \
  --account-name $cosmos_account \
  --resource-group $resource_group \
  --database-name "ancilladb" \
  --name "history" \
  --partition-key-path "/agentPhoneNumber"

az cosmosdb sql container create \
  --account-name $cosmos_account \
  --resource-group $resource_group \
  --database-name "ancilladb" \
  --name "sessions" \
  --partition-key-path "/agentPhoneNumber"
