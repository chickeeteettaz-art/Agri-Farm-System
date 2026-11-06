using System.Net.Http.Json;
using Agri_Web.MVC.Models;
using Agri_Web.MVC.Services.Interfaces;

namespace Agri_Web.MVC.Services
{
    public class IrrigationService : IIrrigationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public IrrigationService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<IrrigationResultViewModel> SendCommandAsync(
            IrrigationCommandViewModel command)
        {
            string? url =
                _configuration["AzureFunctions:IrrigationUrl"];

            string? functionKey =
                _configuration["AzureFunctions:FunctionKey"];

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException(
                    "Irrigation function URL is missing.");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                url);

            if (!string.IsNullOrWhiteSpace(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

            request.Content = JsonContent.Create(command);

            using var response =
                await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await response.Content.ReadAsStringAsync();

                throw new HttpRequestException(
                    $"Irrigation request failed: {error}");
            }

            return await response.Content
                       .ReadFromJsonAsync<IrrigationResultViewModel>()
                   ?? throw new InvalidOperationException(
                       "The irrigation function returned an empty response.");
        }
    }
}