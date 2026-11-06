
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BlobStorageFunction
{
    public class BlobFunction
    {
        private readonly ILogger<BlobFunction> _logger;

        public BlobFunction(ILogger<BlobFunction> logger)
        {
            _logger = logger;
        }

        [Function("BlobFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "images/{farmName}")]
            HttpRequestData req,
            string farmName)
        {
            try
            {
                // Validate farm name
                if (string.IsNullOrWhiteSpace(farmName) ||
                    farmName.Contains("..") ||
                    farmName.Contains("/") ||
                    farmName.Contains("\\"))
                {
                    return await CreateResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Invalid farm name.");
                }

                // Check image filename
                if (!req.Headers.TryGetValues(
                    "x-file-name", out var fileNames))
                {
                    return await CreateResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Image filename is required.");
                }

                string fileName = Path.GetFileName(
                    fileNames.First());

                string extension =
                    Path.GetExtension(fileName).ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg", ".jpeg", ".png", ".tif", ".tiff"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    return await CreateResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Unsupported image format.");
                }

                // Validate content type
                if (!req.Headers.TryGetValues(
                    "Content-Type", out var contentTypes))
                {
                    return await CreateResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Content-Type is required.");
                }

                string contentType = contentTypes.First();

                string[] allowedContentTypes =
                {
                    "image/jpeg",
                    "image/png",
                    "image/tiff"
                };

                if (!allowedContentTypes.Contains(
                    contentType,
                    StringComparer.OrdinalIgnoreCase))
                {
                    return await CreateResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Invalid image content type.");
                }

                // Get Azure Storage connection string
                string? connectionString =
                    Environment.GetEnvironmentVariable(
                        "AzureWebJobsStorage");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Azure Storage connection is missing.");
                }

                // Connect to Blob Storage
                var blobService = new BlobServiceClient(
                    connectionString);

                var container = blobService.GetBlobContainerClient(
                    "drone-images");

                await container.CreateIfNotExistsAsync(
                    PublicAccessType.None);

                // Generate unique image name
                string imageName =
                    $"{farmName}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{extension}";

                var blob = container.GetBlobClient(imageName);

                // Upload image
                await blob.UploadAsync(
                    req.Body,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                    });

                // Connect to Table Storage
                var table = new TableClient(
                    connectionString,
                    "DroneImageAudits");

                await table.CreateIfNotExistsAsync();

                // Create audit record
                var audit = new TableEntity(
                    farmName,
                    Guid.NewGuid().ToString())
                {
                    ["FarmName"] = farmName,
                    ["ImageName"] = Path.GetFileName(imageName),
                    ["BlobUrl"] = blob.Uri.ToString(),
                    ["Status"] = "Uploaded",
                    ["UploadedAt"] = DateTimeOffset.UtcNow,
                    ["OriginalFileName"] = fileName,
                    ["ContentType"] = contentType
                };

                await table.AddEntityAsync(audit);

                _logger.LogInformation(
                    "Image {ImageName} uploaded for farm {FarmName}",
                    imageName,
                    farmName);

                // Return successful response
                var response = req.CreateResponse(
                    HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Image uploaded successfully.",
                    farmName = farmName,
                    imageName = imageName,
                    blobUrl = blob.Uri.ToString()
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to upload drone image.");

                return await CreateResponse(
                    req,
                    HttpStatusCode.InternalServerError,
                    "An error occurred while uploading the image.");
            }
        }

        private static async Task<HttpResponseData> CreateResponse(
            HttpRequestData req,
            HttpStatusCode statusCode,
            string message)
        {
            var response = req.CreateResponse(statusCode);

            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = message
            });

            return response;
        }
    }
}
