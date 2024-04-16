using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using Azure.Identity;
using static System.Console;
using System.Text.Unicode;

//Assign the Storage Blob Data Contributor role to your user account,
// which provides both read and write access to blob data in your storage account
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

// List all blobs in the container
await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
{
    WriteLine("\t" + blobItem.Name);
}

// Download the blob to a local file
// Append the string "DOWNLOADED" before the .txt extension 
// so you can compare the files in the data directory
string downloadFilePath = localFilePath.Replace(".txt", "DOWNLOADED.txt");

WriteLine("\nDownloading blob to\n\t{0}\n", downloadFilePath);

// Download the blob's contents and save it to a file
await blobClient.DownloadToAsync(downloadFilePath);

WriteLine("Would you like to continue? Y/N");
string? answer = ReadLine();

if (answer?.ToUpper() == "Y")
{
    WriteLine("What you wanna write in the blob?");
    string? additionalText = ReadLine();

    string modifiedFileName = "quickstart" + Guid.NewGuid().ToString() + "MODIFIED.txt";

    string modifiedFilePath = Path.Combine(localPath, modifiedFileName);

    string downloadedFileText = File.ReadAllText(downloadFilePath);

    using (StreamWriter writer = File.CreateText(modifiedFilePath))
    {
        writer.WriteLine(downloadedFileText + additionalText);
    }

    await blobClient.UploadAsync(modifiedFilePath, true);

    //string modifiedFilePath = downloadFilePath.Replace("DOWNLOADED.txt", "MODIFIED.txt");

    WriteLine("\nDownloading blob to\n\t{0}\n", modifiedFilePath);

    await blobClient.DownloadToAsync(modifiedFilePath);

}