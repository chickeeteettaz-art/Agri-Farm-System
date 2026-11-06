using System.Net.Http.Json;
using Agri_Web.MVC.Models;
using Agri_Web.MVC.Services.Interfaces;

namespace Agri_Web.MVC.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TelemetryService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<TelemetryViewModel>> GetTelemetryAsync(
            string farmName)
        {
            string? baseUrl =
                _configuration["AzureFunctions:TelemetryUrl"];

            string? functionKey =
                _configuration["AzureFunctions:FunctionKey"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException(
                    "Telemetry function URL is missing.");

            string url =
                $"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(farmName)}";

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                url);

            if (!string.IsNullOrWhiteSpace(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

            using var response =
                await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            return await response.Content
                       .ReadFromJsonAsync<List<TelemetryViewModel>>()
                   ?? new List<TelemetryViewModel>();
        }
    }
}