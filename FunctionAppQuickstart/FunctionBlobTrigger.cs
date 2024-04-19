using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using BlobTriggerAttribute = Microsoft.Azure.Functions.Worker.BlobTriggerAttribute;

namespace FunctionAppQuickStart
{
    public class FunctionBlobTrigger
    {
        private readonly ILogger<FunctionBlobTrigger> _logger;

        public FunctionBlobTrigger(ILogger<FunctionBlobTrigger> logger)
        {
            _logger = logger;
        }

        [Function(nameof(FunctionBlobTrigger))]
        public async Task Run(
            [BlobTrigger("container-trigger-input/{name}", 
            Connection = "AzureWebJobsStorage")] Stream stream, 
            string name,
            Stream outBlob)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}");

        }
    }
}
