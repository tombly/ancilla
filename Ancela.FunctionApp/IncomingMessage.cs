using System.Net;
using Ancela.Agent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ancela.FunctionApp;

/// <summary>
/// Handles incoming messages via HTTP trigger. Used for development and testing.
/// </summary>
/// <param name="_logger"></param>
/// <param name="_chatInterceptor"></param>
public class MessageFunction(ILogger<MessageFunction> _logger, CommandInterceptor _chatInterceptor)
{
    [Function("IncomingMessage")]
    public async Task<HttpResponseData> IncomingMessage([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData request)
    {
        try
        {
            _logger.LogInformation("IncomingMessage triggered");

            var bodyString = await request.ReadAsStringAsync();
            var formValues = System.Web.HttpUtility.ParseQueryString(bodyString ?? string.Empty);
            var body = formValues["Body"];
            var userPhoneNumber = formValues["From"];
            var agentPhoneNumber = formValues["To"];

            if (string.IsNullOrWhiteSpace(userPhoneNumber) || string.IsNullOrWhiteSpace(agentPhoneNumber))
            {
                var badResponse = request.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing required parameters.");
                return badResponse;
            }

            // Extract media URLs if present
            var mediaUrls = new List<string>();
            if (int.TryParse(formValues["NumMedia"], out var numMedia) && numMedia > 0)
            {
                for (int i = 0; i < numMedia; i++)
                {
                    var mediaUrl = formValues[$"MediaUrl{i}"];
                    if (!string.IsNullOrWhiteSpace(mediaUrl))
                        mediaUrls.Add(mediaUrl);
                }
            }

            var reply = await _chatInterceptor.HandleMessage(body ?? string.Empty, userPhoneNumber, agentPhoneNumber, mediaUrls.ToArray());

            var response = request.CreateResponse(HttpStatusCode.OK);
            if (reply != null)
                await response.WriteStringAsync(reply);
            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error processing incoming message");
            var response = request.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {exception.Message}");
            return response;
        }
    }
}