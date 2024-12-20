using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace QueueFunction
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("Function1")]
        public void Run([ServiceBusTrigger("functionqueue", Connection = "Endpoint=sb://algasb.servicebus.windows.net/;SharedAccessKeyName=Policy;SharedAccessKey=GZzXQB2ovTXRo88jYw0gVcGexEc6PY3IY+ASbLucMZU=;EntityPath=functionqueue")] string myQueueItem)
        {
            _logger.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
