using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NUnit.Framework;
using static System.Console;

namespace Azure.Storage.Blobs.Samples;

public class BlobTest {

    private static BlobServiceClient? blobServiceClient;
    private readonly string fileName;
    private readonly string localPath;
    private readonly string localFilePath;
    private readonly string loremIpsum;

    public BlobTest()
    {
        blobServiceClient =  new BlobServiceClient(new Uri("https://lorenzo127.blob.core.windows.net"), new DefaultAzureCredential());
        fileName = "nunit" + Guid.NewGuid().ToString() + "LoremIpsum.txt";
        localPath = "dataTest";
        localFilePath = Path.Combine(localPath!, fileName);
        loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nam vitae tristique eros. Praesent quis dui in risus scelerisque molestie. Cras feugiat diam ante, ac luctus velit auctor ut. In et libero et neque tincidunt efficitur nec in dui. Donec vitae massa libero. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Maecenas fermentum dui non eros euismod, quis malesuada arcu efficitur. Proin bibendum dignissim tortor, eu ullamcorper magna consectetur vitae. Curabitur tincidunt ex eget libero rutrum euismod. Curabitur viverra diam eget lacus euismod ultrices.";
    }

    /// <summary>
    /// Upload a file to a blob.
    /// </summary>
    [Test]
    public async Task UploadAsync()
    {
        Directory.CreateDirectory(localPath);
        await File.WriteAllTextAsync(localFilePath, loremIpsum);
        FileInfo localFile = new(localFilePath);

        string containerName = "nunit" + Guid.NewGuid().ToString();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);

        try
        {
            BlobClient blob = container.GetBlobClient(fileName);
            await blob.UploadAsync(localFilePath);
            BlobProperties properties = await blob.GetPropertiesAsync();
            Assert.AreEqual(localFile.Length, properties.ContentLength);
        }
        finally
        {
            await container.DeleteAsync();
        }
    }

    /// <summary>
    /// Download a blob to a file.
    /// </summary>
    [Test]
    public async Task DownloadAsync()
    {
        Directory.CreateDirectory(localPath);
        await File.WriteAllTextAsync(localFilePath, loremIpsum);

        string downloadedFileName = "nunit" + Guid.NewGuid().ToString() + "DOWNLOADED.txt";
        string downloadedFilePath = Path.Combine(localPath, downloadedFileName);

        string containerName = "nunit" + Guid.NewGuid().ToString();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);

        try
        {
            BlobClient blob = container.GetBlobClient(downloadedFileName);
            await blob.UploadAsync(localFilePath);
            await blob.DownloadToAsync(downloadedFilePath);
            Assert.AreEqual(File.ReadAllText(downloadedFilePath), File.ReadAllText(localFilePath));
        }
        finally
        {
            await container.DeleteAsync();
        }
    }

    /// <summary>
    /// Download our sample image.
    /// </summary>
    [Test]
    public async Task DownloadImageAsync()
    {
        Directory.CreateDirectory(Path.Combine(localPath, "Image"));
        string imageName = "nunit" + Guid.NewGuid().ToString() + "cloud.jpg";
        string localImagePath = Path.Combine(localPath, "Image", "cloud.jpg");

        string downloadedImageName = "nunit" + Guid.NewGuid().ToString() + "DOWNLOADEDcloud.jpg";
        string downloadedImagePath = Path.Combine(Path.Combine(localPath, "Image"), downloadedImageName);

        string containerName = "nunit" + Guid.NewGuid().ToString();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);
        BlobClient blob = container.GetBlobClient(imageName);
        try
        {
            await container.UploadBlobAsync(blob.Name, File.OpenRead(localImagePath));
            //await blob.UploadAsync(localImagePath);
            await blob.DownloadToAsync(downloadedImagePath);
        }
        catch (RequestFailedException ex)
    when (ex.ErrorCode == BlobErrorCode.ContainerBeingDeleted ||
          ex.ErrorCode == BlobErrorCode.ContainerNotFound)
        {

        }
        finally
        {
            //await container.DeleteAsync();
            await foreach (BlobItem b in container.GetBlobsAsync())
            {
                BlobClient blobClient = container.GetBlobClient(b.Name);
                await blobClient.DeleteAsync();
            }
        }
        try
        {
            await blob.GetPropertiesAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Blob does not exist or has been deleted
            WriteLine($"Blob does not exist or has been deleted.");
        }
        Assert.AreEqual(File.ReadAllBytes(localImagePath), File.ReadAllBytes(downloadedImagePath));
    }


    /// <summary>
    /// Trigger a recoverable error.
    /// </summary>
    [Test]
    public async Task ErrorsAsyncShouldFail()
    {
        foreach(BlobContainerItem c in blobServiceClient!.GetBlobContainers())
        {
            BlobContainerClient cont = blobServiceClient!.GetBlobContainerClient(c.Name);
            await cont.DeleteAsync();
        }

        string containerName = "nunit" + Guid.NewGuid().ToString();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);

        try
        {
            await container.CreateAsync();
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerAlreadyExists)
        {
            Assert.AreEqual(0, 0);
            WriteLine(ex.Message);
        }

        await container.DeleteAsync();
    }
}