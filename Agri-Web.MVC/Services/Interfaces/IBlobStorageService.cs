using Microsoft.AspNetCore.Http;

namespace Agri_Web.MVC.Services.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(
            IFormFile image,
            string farmName);

        Task<List<string>> GetImagesAsync(string farmName);

        Task<bool> DeleteImageAsync(
            string farmName,
            string imageName);
    }
}