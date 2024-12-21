using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlobFunction
{
    public class BlobTriggerFunction
    {
        private readonly ILogger _logger;

        public BlobTriggerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BlobTriggerFunction>();
        }

        [Function("BlobFunction")]
        public async Task RunAsync(
            [BlobTrigger("images/{name}", Connection = "Blob_ConnectionString")] Stream blobStream,
            string name)
        {
            _logger.LogInformation($"BlobFunction processed blob: {name}");

            // Configuration de la chaîne de connexion à Service Bus
            string serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBus_ConnectionString");
            string queueName = Environment.GetEnvironmentVariable("ServiceBus_QueueName");

            var client = new ServiceBusClient(serviceBusConnectionString);

            try
            {
                var sender = client.CreateSender(queueName);

                var message = new ServiceBusMessage(name);
                await sender.SendMessageAsync(message);
                _logger.LogInformation($"Message sent to queue: {name}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send message: {ex.Message}");
            }
            finally
            {
                await client.DisposeAsync();
            }

        }
    }
}
