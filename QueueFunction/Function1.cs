using System;
using System.Drawing;
using System.IO;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SkiaSharp; // Utilisé pour le redimensionnement ou l'ajout d'un watermark

namespace QueueFunction
{
    public class QueueTriggerFunction
    {
        private readonly ILogger _logger;

        public QueueTriggerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueTriggerFunction>();
        }

        [Function("QueueFunction")]
        public void Run(
            [ServiceBusTrigger("%ServiceBus_QueueName%", Connection = "ServiceBus_ConnectionString")] string fileName)
        {
            _logger.LogInformation($"QueueFunction processing file: {fileName}");

            string storageConnectionString = Environment.GetEnvironmentVariable("Blob_ConnectionString");
            BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("images");
            BlobContainerClient processedContainerClient = blobServiceClient.GetBlobContainerClient("processed");

            BlobClient sourceBlob = containerClient.GetBlobClient(fileName);
            BlobClient destinationBlob = processedContainerClient.GetBlobClient(fileName);

            try
            {
                // Téléchargement du fichier source
                Stream sourceStream = new MemoryStream();
                sourceBlob.DownloadTo(sourceStream);
                sourceStream.Position = 0;

                // Exemple : Ajout d'un watermark
                using (var input = SKBitmap.Decode(sourceStream))
                using (var canvas = new SKCanvas(input))
                {
                    var paint = new SKPaint
                    {
                        TextSize = 48.0f,
                        Color = SKColors.Red,
                        IsAntialias = true
                    };
                    canvas.DrawText("Watermark", 10, input.Height - 20, paint);

                    using (var output = new MemoryStream())
                    {
                        input.Encode(SKEncodedImageFormat.Jpeg, 80).SaveTo(output);
                        output.Position = 0;

                        // Téléchargement dans le conteneur "processed"
                        destinationBlob.Upload(output, true);
                        _logger.LogInformation($"Processed file uploaded: {fileName}");
                    }
                }

                // Suppression du fichier original
                sourceBlob.Delete();
                _logger.LogInformation($"Original file deleted: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file {fileName}: {ex.Message}");
            }
        }
    }
}
