using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TelemetryFunction.Models;
using System.Net;

namespace TelemetryFunction.Functions
{
    public class GetTelemetryFunction
    {
        private readonly ILogger<GetTelemetryFunction> _logger;
        private readonly TableClient _tableClient;

        public GetTelemetryFunction(
            ILogger<GetTelemetryFunction> logger)
        {
            _logger = logger;

            string connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage is not configured.");

            _tableClient = new TableClient(
                connectionString,
                "TelemetryData");
        }

        [Function("GetTelemetryFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "telemetry/{farmName}")]
            HttpRequestData req,
            string farmName)
        {
            _logger.LogInformation(
                "Retrieving telemetry for farm {FarmName}",
                farmName);

            var response = req.CreateResponse();

            try
            {
                await _tableClient.CreateIfNotExistsAsync();

                string filter =
                    TableClient.CreateQueryFilter(
                        $"PartitionKey eq {farmName}");

                var telemetryRecords = new List<TelemetryResponse>();

                // The table uses reverse chronological RowKeys.
                // This allows the newest records to be read first.
                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                    filter: filter,
                    maxPerPage: 50))
                {
                    telemetryRecords.Add(new TelemetryResponse
                    {
                        FarmName = entity.GetString("FarmName") ?? farmName,
                        SensorId = entity.GetString("SensorId") ?? "",
                        FieldId = entity.GetString("FieldId") ?? "",
                        SoilMoisture = entity.GetDouble("SoilMoisture") ?? 0,
                        Temperature = entity.GetDouble("Temperature") ?? 0,
                        PHLevel = entity.GetDouble("PHLevel") ?? 0,
                        RecordedAt = entity.GetDateTimeOffset("RecordedAt")
                                     ?? entity.Timestamp
                                     ?? DateTimeOffset.MinValue
                    });

                    if (telemetryRecords.Count >= 50)
                        break;
                }

                await response.WriteAsJsonAsync(telemetryRecords);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving telemetry for farm {FarmName}",
                    farmName);

                response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    "An error occurred while retrieving telemetry data.");

                return response;
            }
        }
    }
}