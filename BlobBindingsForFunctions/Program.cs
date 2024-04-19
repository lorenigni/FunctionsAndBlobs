
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;

BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri("https://lorenzo127.blob.core.windows.net"), new DefaultAzureCredential());


BlobCreationTrigger();


async void BlobCreationTrigger()
{
    DeleteBlobContainers();
    string text = "hello!";
    string containerName = "container-trigger-input";
    BlobContainerClient container = await blobServiceClient.CreateBlobContainerAsync(containerName);
    string blobName = "blob-name-input";
    container.GetBlobClient(blobName).Upload(BinaryData.FromString(text));    
}



async void DeleteBlobContainers()
{
    foreach (BlobContainerItem c in blobServiceClient.GetBlobContainers())
    {
        BlobContainerClient cont = blobServiceClient.GetBlobContainerClient(c.Name);
        await cont.DeleteAsync();
    }
}