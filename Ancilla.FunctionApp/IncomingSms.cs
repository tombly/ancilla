using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ancilla.FunctionApp;

public class SmsFunction(ILogger<SmsFunction> _logger, CosmosClient _cosmosClient)
{
    [Function("IncomingSms")]
    public async Task<HttpResponseData> IncomingSms([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notes")] HttpRequestData request)
    {
        _logger.LogInformation("IncomingSms triggered");

        try
        {
            var requestBody = await request.ReadAsStringAsync();

            var note = new
            {
                id = Guid.NewGuid().ToString(),
                content = requestBody,
                timestamp = DateTimeOffset.Now
            };

            _logger.LogInformation("Saving note with ID: {NoteId}", note.id);

            var database = _cosmosClient.GetDatabase("ancilladb");
            var container = database.GetContainer("notes");

            await container.CreateItemAsync(note, new PartitionKey(note.id)); // TODO: Use phone number as partition key

            _logger.LogInformation("Successfully saved note with ID: {NoteId}", note.id);

            var response = request.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(note);
            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error saving note to Cosmos DB");
            var response = request.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {exception.Message}");
            return response;
        }
    }
}