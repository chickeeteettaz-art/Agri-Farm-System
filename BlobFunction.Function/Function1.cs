
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

namespace BlobFunction.Function
{
    public static class Function1
    {
        [FunctionName("Function1")]
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
                log.LogInformation("Drone image upload started.");

                // 1. Get Azure Storage connection string
                string connectionString =
                    Environment.GetEnvironmentVariable(
                        "AzureWebJobsStorage");

                if (string.IsNullOrEmpty(connectionString))
                {
                    return new BadRequestObjectResult(
                        "Azure Storage connection string is missing.");
                }

                // 2. Validate farm name
                if (string.IsNullOrWhiteSpace(farmName) ||
                    farmName.Contains("..") ||
                    farmName.Contains("/") ||
                    farmName.Contains("\\"))
                {
                    return new BadRequestObjectResult(
                        "Invalid farm name.");
                }

                // 3. Get image filename from request header
                string fileName = req.Headers["x-file-name"].ToString();

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return new BadRequestObjectResult(
                        "Image filename is required.");
                }

                fileName = Path.GetFileName(fileName);

                // 4. Validate image format
                string extension =
                    Path.GetExtension(fileName).ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg", ".jpeg", ".png", ".tif", ".tiff"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    return new BadRequestObjectResult(
                        "Only JPG, JPEG, PNG and TIFF images are allowed.");
                }

                if (req.ContentLength == 0)
                {
                    return new BadRequestObjectResult(
                        "The uploaded image is empty.");
                }

                // 5. Get image content type
                string contentType = req.ContentType
                    ?? "application/octet-stream";

                // 6. Connect to Azure Blob Storage
                var blobServiceClient =
                    new BlobServiceClient(connectionString);

                var containerClient =
                    blobServiceClient.GetBlobContainerClient(
                        "drone-images");

                // Create container if it does not exist
                await containerClient.CreateIfNotExistsAsync();

                // 7. Generate unique image name
                string imageName =
                    $"{farmName}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{extension}";

                var blobClient =
                    containerClient.GetBlobClient(imageName);

                // 8. Upload image to Blob Storage
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
                    "Image uploaded successfully: {ImageName}",
                    imageName);

                // 9. Connect to Azure Table Storage
                var tableClient = new TableClient(
                    connectionString,
                    "DroneImageAudits");

                await tableClient.CreateIfNotExistsAsync();

                // 10. Create image audit record
                var imageAudit = new TableEntity(
                    farmName,
                    Guid.NewGuid().ToString())
                {
                    ["FarmName"] = farmName,
                    ["ImageName"] = Path.GetFileName(imageName),
                    ["OriginalFileName"] = fileName,
                    ["BlobUrl"] = blobClient.Uri.ToString(),
                    ["ContentType"] = contentType,
                    ["Status"] = "Uploaded",
                    ["UploadedAt"] = DateTimeOffset.UtcNow
                };

                // Save audit record
                await tableClient.AddEntityAsync(imageAudit);

                log.LogInformation(
                    "Image audit saved successfully.");

                // 11. Return response to MVC application
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
                    message = "An error occurred while uploading the image.",
                    error = ex.Message
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
