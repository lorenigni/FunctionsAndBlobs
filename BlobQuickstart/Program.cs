using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using Azure.Identity;
using static System.Console;
using System.Text.Unicode;
using Azure;

//Every object that you store in Azure Storage has an address that includes your unique account name.
//The combination of the account name and the Blob Storage endpoint forms the base address for the objects in your storage account.
//A container organizes a set of blobs, similar to a directory in a file system. A storage account can include an unlimited number of 
//  containers, and a container can store an unlimited number of blobs.
//Azure Storage supports three types of blobs:
//  Block blobs store text and binary data.
//  Append blobs are made up of blocks like block blobs, but are are ideal for scenarios such as logging data from virtual machines
//  Page blobs store random access files up to 8 TiB in size. Page blobs store virtual hard drive (VHD) files and serve as disks for Azure virtual machines.


//Assign the Storage Blob Data Contributor role to your user account,
//  which provides both read and write access to blob data in your storage account
//Application requests to Azure Blob Storage must be authorized.
//  Using the DefaultAzureCredential class provided by the Azure Identity client library
//  is the recommended approach for implementing passwordless connections to Azure services in your code.
//  This approach enables your app to use different authentication methods in different environments (local vs. production)
//  without implementing environment-specific code
//When deployed to Azure, this same code can be used to authorize requests
// to Azure Storage from an application running in Azure. However, you'll need to
// enable managed identity on your app in Azure. Then configure your storage
// account to allow that managed identity to connect.

var blobServiceClient = new BlobServiceClient(
        new Uri("https://lorenzo127.blob.core.windows.net"),
        new DefaultAzureCredential());

//Container names must be lowercase
string containerName = "quickstartblobs" + Guid.NewGuid().ToString();

BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);

// Create a local file in the ./data/ directory for uploading and downloading
string localPath = "data";
Directory.CreateDirectory(localPath);
string fileName = "quickstart" + Guid.NewGuid().ToString() + ".txt";
string localFilePath = Path.Combine(localPath, fileName);

// Write text to the file
await File.WriteAllTextAsync(localFilePath, "Hello, World!");

// Get a reference to a blob, appending the input parameter string to the BlobContainerUri
BlobClient blobClient = containerClient.GetBlobClient(fileName);
WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

// Upload data from the local file, overwrite the blob if it already exists
await blobClient.UploadAsync(localFilePath, true);

WriteLine("Listing blobs...");

await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
{
    WriteLine("\t" + blobItem.Name);
}

string downloadFilePath = localFilePath.Replace(".txt", "DOWNLOADED.txt");
WriteLine("\nDownloading blob to\n\t{0}\n", downloadFilePath);
await blobClient.DownloadToAsync(downloadFilePath);

WriteLine("Would you like to continue? Y/N");
string? answer = ReadLine();
string? modifiedFilePath = null;

if (answer?.ToUpper() == "Y")
{
    WriteLine("What you wanna write in the blob?");
    string? additionalText = ReadLine();

    string modifiedFileName = "quickstart" + Guid.NewGuid().ToString() + "MODIFIED.txt";
    modifiedFilePath = Path.Combine(localPath, modifiedFileName);

    string downloadedFileText = File.ReadAllText(downloadFilePath);

    using (StreamWriter writer = File.CreateText(modifiedFilePath))
    {
        writer.WriteLine(downloadedFileText + additionalText);
    }

    await blobClient.UploadAsync(modifiedFilePath, true);
    WriteLine("\nDownloading blob to\n\t{0}\n", modifiedFilePath);
    await blobClient.DownloadToAsync(modifiedFilePath);
}

// Clean up
Write("Press any key to begin clean up");
ReadLine();
WriteLine("Deleting blob container...");

try
{
     await containerClient.DeleteAsync();
}
catch (RequestFailedException ex)
    when (ex.ErrorCode == BlobErrorCode.ContainerBeingDeleted ||
          ex.ErrorCode == BlobErrorCode.ContainerNotFound)
{
    WriteLine($"{ex.Message}");
}

WriteLine("Deleting the local source and downloaded files...");
File.Delete(localFilePath);
File.Delete(downloadFilePath);
if(modifiedFilePath is not null) File.Delete(modifiedFilePath);

WriteLine("Done");