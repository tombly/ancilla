using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Storage;

namespace Ancela.AppHost;

public sealed class FixedNameInfrastructureResolver() : InfrastructureResolver
{
    private const string UniqueNamePrefix = "ancela";

    /// <summary>
    /// Resolves the configuration for cloud resources, including a fixed name for the resource at provisioning time.
    /// This does require care with the resource names as they need to be compatible with Azure naming rules for 
    /// the resource type.
    /// </summary>
    public override void ResolveProperties(ProvisionableConstruct construct, ProvisioningBuildOptions options)
    {
        // Because some resource names need to be globally unique (beyong the scope of the subscription or resource group),
        // the prefix and environment based suffix is used here to get a name unique to the environment.
        // If additional environments are needed, that will need to be taken into an account here.
        string environmentSuffix = "-dev";

        switch (construct)
        {
            case CosmosDBAccount cosmosAccount:
                cosmosAccount.Name = $"{UniqueNamePrefix}-{cosmosAccount.BicepIdentifier.ToLowerInvariant()}{environmentSuffix.Replace("-", string.Empty)}";
                break;

            case StorageAccount storageAccount:
                storageAccount.Name = $"{UniqueNamePrefix}{storageAccount.BicepIdentifier.ToLowerInvariant()}{environmentSuffix.Replace("-", string.Empty)}";
                break;

            case ContainerAppManagedEnvironment containerAppEnvironment:
                containerAppEnvironment.Name = $"cae-{UniqueNamePrefix}-{containerAppEnvironment.BicepIdentifier.ToLowerInvariant()}{environmentSuffix}";
                break;

            //case ContainerApp functionApp when functionApp.BicepIdentifier == "functionapp":
            //    functionApp.Name = $"func-{UniqueNamePrefix}-{functionApp.BicepIdentifier.ToLowerInvariant()}{environmentSuffix}";
            //    break;

            //case CognitiveServicesAccount cognitiveServicesAccount:
            //    ConfigureOpenAi(cognitiveServicesAccount, environmentSuffix);
            //    break;

            case KeyVaultService keyVault when keyVault.BicepIdentifier == "secrets":
                keyVault.Name = $"kv-{UniqueNamePrefix}-{keyVault.BicepIdentifier.ToLowerInvariant()}{environmentSuffix}";
                break;

            //case ApplicationInsightsComponent insightsComponent:
            //    insightsComponent.Name = $"{insightsComponent.BicepIdentifier.ToLowerInvariant()}{environmentSuffix}".Replace('_', '-');
            //    break;

            default:
                break;
        }
    }
}