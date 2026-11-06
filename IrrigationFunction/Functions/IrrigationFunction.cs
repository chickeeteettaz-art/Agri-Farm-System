using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using IrrigationFunction.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace IrrigationFunction.Functions
{
    public class IrrigationFunction
    {
        private readonly ILogger<IrrigationFunction> _logger;
        private readonly QueueClient _queueClient;

        public IrrigationFunction(
            ILogger<IrrigationFunction> logger)
        {
            _logger = logger;

            string connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage is not configured.");

            _queueClient = new QueueClient(
                connectionString,
                "irrigation-commands");
        }

        [Function("IrrigationFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "irrigation")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var command = await req.ReadFromJsonAsync<IrrigationCommand>();

                if (command == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(
                        "A valid irrigation command is required.");

                    return response;
                }

                if (string.IsNullOrWhiteSpace(command.DeviceId))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(
                        "DeviceId is required.");

                    return response;
                }

                if (command.Command != "START_IRRIGATION" &&
                    command.Command != "STOP_IRRIGATION")
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(
                        "Command must be START_IRRIGATION or STOP_IRRIGATION.");

                    return response;
                }

                if (command.Command == "START_IRRIGATION" &&
                    (command.DurationMinutes < 1 ||
                     command.DurationMinutes > 1440))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(
                        "DurationMinutes must be between 1 and 1440.");

                    return response;
                }

                if (command.Command == "STOP_IRRIGATION")
                {
                    command.DurationMinutes = 0;
                }

                command.CommandId = Guid.NewGuid().ToString();
                command.Status = "Pending";
                command.CreatedAt = DateTime.UtcNow;

                await _queueClient.CreateIfNotExistsAsync();

                string message = JsonSerializer.Serialize(command);

                await _queueClient.SendMessageAsync(message);

                _logger.LogInformation(
                    "Irrigation command {CommandId} added to the queue for device {DeviceId}.",
                    command.CommandId,
                    command.DeviceId);

                response.StatusCode = HttpStatusCode.Accepted;

                await response.WriteAsJsonAsync(new
                {
                    message = "Irrigation command successfully queued.",
                    commandId = command.CommandId,
                    deviceId = command.DeviceId,
                    command = command.Command,
                    status = command.Status
                });

                return response;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid irrigation request JSON.");

                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(
                    "The request body contains invalid JSON.");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to submit irrigation command.");

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(
                    "An error occurred while submitting the irrigation command.");

                return response;
            }
        }
    }
}