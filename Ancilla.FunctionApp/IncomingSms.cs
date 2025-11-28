using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Twilio.Security;

namespace Ancilla.FunctionApp;

public class SmsFunction(ILogger<SmsFunction> _logger, CosmosClient _cosmosClient)
{
    [Function("IncomingSms")]
    public async Task<HttpResponseData> IncomingSms([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request)
    {
        try
        {
            _logger.LogInformation("IncomingSms triggered");

            //if (!await ValidateRequest(request))
            //    return request.CreateResponse(HttpStatusCode.Forbidden);

            var bodyString = await request.ReadAsStringAsync();
            var formValues = System.Web.HttpUtility.ParseQueryString(bodyString ?? string.Empty);
            var body = formValues["Body"];
            var from = formValues["From"];
            var to = formValues["To"];

            var note = new
            {
                id = Guid.NewGuid().ToString(),
                content = body,
                fromPhoneNumber = from,
                aiPhoneNumber = to,
                timestamp = DateTimeOffset.Now,
                partitionKey = to
            };

            _logger.LogInformation("Saving note with ID: {NoteId}", note.id);

            var database = _cosmosClient.GetDatabase("ancilladb");
            var container = database.GetContainer("notes");

            await container.CreateItemAsync(note, new PartitionKey(note.partitionKey));

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

    private static async Task<bool> ValidateRequest(HttpRequest request)
    {
        var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? throw new Exception("TWILIO_AUTH_TOKEN not set");

        Dictionary<string, string>? parameters = null;
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
        }

        var requestUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        var requestValidator = new RequestValidator(authToken);
        var signature = request.Headers["X-Twilio-Signature"];
        var isValid = requestValidator.Validate(requestUrl, parameters, signature);
        return isValid;
    }
}