using Agri_Web.MVC.Services.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Agri_Web.MVC.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            // Read Azure Storage configuration
            string connectionString =
                configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");

            string containerName =
                configuration["AzureStorage:BlobContainerName"]
                ?? throw new InvalidOperationException(
                    "Azure Blob container name is missing.");

            // Create the Blob Service Client
            var blobServiceClient =
                new BlobServiceClient(connectionString);

            // Get the configured container
            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }

        // Upload an image to Azure Blob Storage
        public async Task<string> UploadImageAsync(
            IFormFile file,
            string farmName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Please select an image.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".tif", ".tiff"
            };

            if (Array.IndexOf(allowedExtensions, extension) < 0)
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and TIFF images are allowed.");

            await _containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None);

            string fileName = $"{Guid.NewGuid()}{extension}";
            string blobName = $"{farmName}/{fileName}";

            BlobClient blobClient =
                _containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

            return blobName;
        }

        // Retrieve images belonging to a particular farm
        public async Task<List<string>> GetImagesAsync(string farmName)
        {
            var images = new List<string>();

            await _containerClient.CreateIfNotExistsAsync();

            await foreach (var blob in _containerClient.GetBlobsAsync(
                traits: BlobTraits.None,
                states: BlobStates.None,
                prefix: $"{farmName}/",
                cancellationToken: CancellationToken.None))
            {
                images.Add(blob.Name);
            }

            return images;
        }

        // Delete an image from Azure Blob Storage
        public async Task<bool> DeleteImageAsync(
            string farmName,
            string imageName)
        {
            string blobName = $"{farmName}/{imageName}";

            BlobClient blobClient =
                _containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();

            return response.Value;
        }
    }
}