
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BlobCodeFunction.Functions
{
    public class BlobFunction
    {
        private readonly ILogger<BlobFunction> _logger;

        public BlobFunction(ILogger<BlobFunction> logger)
        {
            _logger = logger;
        }

        [Function("BlobFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                Microsoft.Azure.Functions.Worker.AuthorizationLevel.Function,
                "post",
                Route = "images/{farmName}")]
            HttpRequest req,
            string farmName)
        {
            try
            {
                _logger.LogInformation("Drone image upload started.");

                // 1. Validate farm name
                if (string.IsNullOrWhiteSpace(farmName) ||
                    farmName.Length > 100 ||
                    !Regex.IsMatch(farmName, @"^[a-zA-Z0-9_-]+$"))
                {
                    return new BadRequestObjectResult(
                        "Invalid farm name.");
                }

                // 2. Get image filename
                string fileName = req.Headers["x-file-name"]
                    .FirstOrDefault() ?? "";

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return new BadRequestObjectResult(
                        "Image filename is required.");
                }

                fileName = Path.GetFileName(fileName);

                // 3. Validate image extension
                string extension =
                    Path.GetExtension(fileName).ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg", ".jpeg", ".png", ".tif", ".tiff"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    return new BadRequestObjectResult(
                        "Only JPG, PNG and TIFF images are allowed.");
                }

                if (req.ContentLength == 0)
                {
                    return new BadRequestObjectResult(
                        "The image file is empty.");
                }

                // 4. Get content type
                string contentType = req.ContentType ?? "";

                string expectedContentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".tif" or ".tiff" => "image/tiff",
                    _ => ""
                };

                if (!string.Equals(
                    contentType,
                    expectedContentType,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return new BadRequestObjectResult(
                        "Invalid image content type.");
                }

                // 5. Get Azure Storage connection string
                string connectionString =
                    Environment.GetEnvironmentVariable(
                        "AzureWebJobsStorage") ?? "";

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new Exception(
                        "Azure Storage connection string is missing.");
                }

                // 6. Connect to Blob Storage
                var blobServiceClient =
                    new BlobServiceClient(connectionString);

                var containerClient =
                    blobServiceClient.GetBlobContainerClient(
                        "drone-images");

                await containerClient.CreateIfNotExistsAsync();

                // 7. Generate unique image name
                string imageName =
                    $"{farmName}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{extension}";

                var blobClient =
                    containerClient.GetBlobClient(imageName);

                // 8. Upload image
                await blobClient.UploadAsync(
                    req.Body,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                    });

                _logger.LogInformation(
                    "Image uploaded: {ImageName}", imageName);

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

                await tableClient.AddEntityAsync(imageAudit);

                _logger.LogInformation(
                    "Image audit record saved successfully.");

                // 11. Return response to MVC
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
                _logger.LogError(
                    ex, "Drone image upload failed.");

                return new ObjectResult(new
                {
                    success = false,
                    message = "An error occurred while uploading the drone image."
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
