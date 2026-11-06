using System.Net;
using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ProcessIrrigationFunction.Models;

namespace ProcessIrrigationFunction.Functions
{
    public class ProcessIrrigationFunction
    {
        private readonly ILogger<ProcessIrrigationFunction> _logger;
        private readonly string _connectionString;

        public ProcessIrrigationFunction(
            ILogger<ProcessIrrigationFunction> logger)
        {
            _logger = logger;

            _connectionString =
                Environment.GetEnvironmentVariable("SqlConnectionString")
                ?? throw new InvalidOperationException(
                    "SqlConnectionString is not configured.");
        }

        [Function("ProcessIrrigationFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "irrigation/process")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var command =
                    await req.ReadFromJsonAsync<IrrigationCommand>();

                if (command == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;

                    await response.WriteStringAsync(
                        "Invalid irrigation command.");

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
                        "Invalid irrigation command.");

                    return response;
                }

                if (command.Command == "START_IRRIGATION" &&
                    (command.DurationMinutes < 1 ||
                     command.DurationMinutes > 1440))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;

                    await response.WriteStringAsync(
                        "Duration must be between 1 and 1440 minutes.");

                    return response;
                }

                string status = command.Command switch
                {
                    "START_IRRIGATION" => "Irrigating",
                    "STOP_IRRIGATION" => "Online",
                    _ => "Offline"
                };

                await using var connection =
                    new SqlConnection(_connectionString);

                await connection.OpenAsync();

                const string sql = @"
                    UPDATE Devices
                    SET
                        Status = @Status,
                        LastCommand = @LastCommand,
                        LastUpdated = @LastUpdated
                    WHERE DeviceId = @DeviceId;";

                await using var sqlCommand =
                    new SqlCommand(sql, connection);

                sqlCommand.Parameters.Add(
                    "@Status",
                    SqlDbType.NVarChar,
                    50).Value = status;

                sqlCommand.Parameters.Add(
                    "@LastCommand",
                    SqlDbType.NVarChar,
                    50).Value = command.Command;

                sqlCommand.Parameters.Add(
                    "@LastUpdated",
                    SqlDbType.DateTime2).Value = DateTime.UtcNow;

                sqlCommand.Parameters.Add(
                    "@DeviceId",
                    SqlDbType.NVarChar,
                    100).Value = command.DeviceId;

                int rowsAffected =
                    await sqlCommand.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    response.StatusCode = HttpStatusCode.NotFound;

                    await response.WriteStringAsync(
                        "Device was not found.");

                    return response;
                }

                _logger.LogInformation(
                    "Irrigation command processed for device {DeviceId}.",
                    command.DeviceId);

                response.StatusCode = HttpStatusCode.OK;

                await response.WriteAsJsonAsync(new
                {
                    message = "Irrigation command processed successfully.",
                    commandId = command.CommandId,
                    deviceId = command.DeviceId,
                    command = command.Command,
                    status = status,
                    durationMinutes = command.DurationMinutes
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing irrigation command.");

                response.StatusCode =
                    HttpStatusCode.InternalServerError;

                await response.WriteStringAsync(
                    "An error occurred while processing the irrigation command.");

                return response;
            }
        }
    }
}