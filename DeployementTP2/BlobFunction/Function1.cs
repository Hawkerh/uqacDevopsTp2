using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlobFunction
{
    public class Function1
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _queueName;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();

            // Récupérer les valeurs des App Settings
            _connectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            _queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName");

            if (string.IsNullOrEmpty(_connectionString) || string.IsNullOrEmpty(_queueName))
            {
                throw new InvalidOperationException("ServiceBusConnectionString or ServiceBusQueueName is not configured in the App Settings.");
            }
        }

        [Function("Function1")]
        public async Task RunAsync(
            [BlobTrigger("Blob/{name}", Connection = "DefaultEndpointsProtocol=https;AccountName=gaaldsa;AccountKey=j+1jnGnPmh/I6iIjZu7YuJKsTQmhiGlH9ySBBQcOMYRFFoE8fQrNHor1OJEse8whswWFwNQeeRNm+ASt28SdqQ==;EndpointSuffix=core.windows.net")] string myBlob,
            string name)
        {
            _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Data: {myBlob.Length} bytes");

            Console.WriteLine("Start : " + DateTime.Now.ToString());

            // Créer le client de la file d'attente
            ServiceBusClient client = new ServiceBusClient(_connectionString);
            ServiceBusSender sender = client.CreateSender(_queueName);

            try
            {
                // Préparer le message à envoyer
                string messageBody = $"File processed: {name}";
                ServiceBusMessage message = new ServiceBusMessage(messageBody);

                // Envoyer le message
                await sender.SendMessageAsync(message);

                Console.WriteLine($"Message sent: {messageBody}");
                _logger.LogInformation($"Message sent to Service Bus queue: {messageBody}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                _logger.LogError($"Error sending message to Service Bus queue: {ex.Message}");
            }
            finally
            {
                // Libérer les ressources du client
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
}
