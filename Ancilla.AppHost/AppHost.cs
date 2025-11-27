using Aspire.Hosting.Azure;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ancilla.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.Configure<AzureProvisioningOptions>(options =>
{
    options.ProvisioningBuildOptions.InfrastructureResolvers.Insert(0, new FixedNameInfrastructureResolver());
});

builder.AddAzureContainerAppEnvironment("env");

var cosmosDb = builder.AddAzureCosmosDB("cosmos")
.RunAsPreviewEmulator(configureContainer: container =>
    { container.WithDataExplorer(); });

var storage = builder.AddAzureStorage("storage").RunAsEmulator()
    .ConfigureInfrastructure((infrastructure) =>
    {
        var storageAccount = infrastructure.GetProvisionableResources().OfType<StorageAccount>().FirstOrDefault(r => r.BicepIdentifier == "storage")
            ?? throw new InvalidOperationException($"Could not find configured storage account with name 'storage'");
        storageAccount.AllowBlobPublicAccess = false;
    });

var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");
var tables = storage.AddTables("tables");

var functionApp = builder.AddAzureFunctionsProject<Projects.Ancilla_FunctionApp>("functionapp")
    .WithReference(cosmosDb)
    .WithReference(blobs)
    .WithReference(queues)
    .WithReference(tables)
    .WithHostStorage(storage)
    .WithExternalHttpEndpoints();

var database = cosmosDb.AddCosmosDatabase("ancilladb");
var container = database.AddContainer("notes", "/id");

builder.Build().Run();