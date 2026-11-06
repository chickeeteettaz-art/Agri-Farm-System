using System.Net.Http.Json;
using Agri_Web.MVC.Models;
using Agri_Web.MVC.Services.Interfaces;

namespace Agri_Web.MVC.Services
{
    public class ProcessIrrigationService : IProcessIrrigationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ProcessIrrigationService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<IrrigationResultViewModel> ProcessCommandAsync(
            IrrigationCommandViewModel command,
            string commandId)
        {
            string? url =
                _configuration["AzureFunctions:ProcessIrrigationUrl"];

            string? functionKey =
                _configuration["AzureFunctions:FunctionKey"];

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException(
                    "Process irrigation function URL is missing.");

            var requestBody = new
            {
                CommandId = commandId,
                command.DeviceId,
                command.Command,
                command.DurationMinutes,
                command.RequestedByUserId
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                url);

            if (!string.IsNullOrWhiteSpace(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

            request.Content = JsonContent.Create(requestBody);

            using var response =
                await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await response.Content.ReadAsStringAsync();

                throw new HttpRequestException(
                    $"Irrigation processing failed: {error}");
            }

            return await response.Content
                       .ReadFromJsonAsync<IrrigationResultViewModel>()
                   ?? throw new InvalidOperationException(
                       "The processing function returned an empty response.");
        }
    }
}