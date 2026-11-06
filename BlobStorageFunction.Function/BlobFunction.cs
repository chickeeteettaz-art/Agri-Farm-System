
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlobStorageFunction.Function
{
    public static class BlobFunction
    {
        [FunctionName("BlobFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "images/{farmName}")]
            HttpRequest req,
            string farmName,
            ILogger log)
        {
            try
            {
                log.LogInformation("Processing drone image upload.");

                // Get storage connection string
                string connectionString =
                    Environment.GetEnvironmentVariable(
                        "AzureWebJobsStorage");

                if (string.IsNullOrEmpty(connectionString))
                {
                    return new BadRequestObjectResult(
                        "Azure Storage connection string is missing.");
                }

                // Validate farm name
                if (string.IsNullOrWhiteSpace(farmName))
                {
                    return new BadRequestObjectResult(
                        "Farm name is required.");
                }

                // Get uploaded filename
                string fileName = req.Headers["x-file-name"].ToString();

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return new BadRequestObjectResult(
                        "Image filename is required.");
                }

                fileName = Path.GetFileName(fileName);

                // Validate image extension
                string extension =
                    Path.GetExtension(fileName).ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg", ".jpeg", ".png", ".tif", ".tiff"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    return new BadRequestObjectResult(
                        "Invalid image format.");
                }

                if (req.ContentLength == 0)
                {
                    return new BadRequestObjectResult(
                        "Image file is empty.");
                }

                // Get content type
                string contentType = req.ContentType
                    ?? "application/octet-stream";

                // Connect to Blob Storage
                var blobServiceClient =
                    new BlobServiceClient(connectionString);

                var containerClient =
                    blobServiceClient.GetBlobContainerClient(
                        "drone-images");

                await containerClient.CreateIfNotExistsAsync(
                    PublicAccessType.None);

                // Generate unique image name
                string imageName =
                    $"{farmName}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{extension}";

                var blobClient =
                    containerClient.GetBlobClient(imageName);

                // Upload image
                await blobClient.UploadAsync(
                    req.Body,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                    });

                log.LogInformation(
                    "Image uploaded to Blob Storage: {ImageName}",
                    imageName);

                // Connect to Azure Table Storage
                var tableClient = new TableClient(
                    connectionString,
                    "DroneImageAudits");

                await tableClient.CreateIfNotExistsAsync();

                // Create image audit record
                var audit = new TableEntity(
                    farmName,
                    Guid.NewGuid().ToString())
                {
                    ["FarmName"] = farmName,
                    ["ImageName"] = Path.GetFileName(imageName),
                    ["BlobUrl"] = blobClient.Uri.ToString(),
                    ["OriginalFileName"] = fileName,
                    ["ContentType"] = contentType,
                    ["Status"] = "Uploaded",
                    ["UploadedAt"] = DateTimeOffset.UtcNow
                };

                await tableClient.AddEntityAsync(audit);

                log.LogInformation(
                    "Image audit record saved successfully.");

                // Return successful response
                return new OkObjectResult(new
                {
                    success = true,
                    message = "Drone image uploaded successfully.",
                    farmName = farmName,
                    imageName = imageName,
                    blobUrl = blobClient.Uri.ToString()
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Drone image upload failed.");

                return new ObjectResult(new
                {
                    success = false,
                    message = "An error occurred while uploading the image."
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
