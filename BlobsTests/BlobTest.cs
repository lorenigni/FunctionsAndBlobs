using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
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
        DeleteBlobContainers();

        Directory.CreateDirectory(localPath);

        // Così nonc c'è bisogno del FileStream
        //StreamWriter writer = File.CreateText(localFilePath);
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
        DeleteBlobContainers();

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
        DeleteBlobContainers();

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
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerBeingDeleted || ex.ErrorCode == BlobErrorCode.ContainerNotFound)
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
        DeleteBlobContainers();
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
    [Test]
    public async Task ListBlobsManual()
    {
        DeleteBlobContainers();

        string data = "hello world";
        string containerName = "nunit" + Guid.NewGuid();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);

        try
        {
            HashSet<string> blobNames = new HashSet<string>();

            foreach (var _ in Enumerable.Range(0, 10))
            {
                string blobName = "nunit" + Guid.NewGuid();
                container.GetBlobClient(blobName).Upload(BinaryData.FromString(data));
                blobNames.Add(blobName);
            }

            // tools to consume blob listing while looking good in the sample snippet
            HashSet<string> downloadedBlobNames = new();
            void MyConsumeBlobItemFunc(BlobItem item)
            {
                downloadedBlobNames.Add(item.Name);
            }

            // set this to already existing continuation token to pick up where you previously left off
            string initialContinuationToken = null;
            AsyncPageable<BlobItem> results = container.GetBlobsAsync();
            IAsyncEnumerable<Page<BlobItem>> pages = results.AsPages(initialContinuationToken);

            await foreach (Page<BlobItem> page in pages)
            {
                // process page
                foreach (BlobItem item in page.Values)
                {
                    MyConsumeBlobItemFunc(item);
                }

                // access continuation token if desired
                string continuationToken = page.ContinuationToken;
            }
            Assert.IsTrue(blobNames.SetEquals(downloadedBlobNames));
        }
        finally
        {
            await container.DeleteIfExistsAsync();
        }
    }

    [Test]
    public async Task EditMetadata()
    {
        string data = "hello world";
        var initialMetadata = new Dictionary<string, string> { { "fizz", "buzz" } };

        string containerName = "nunit" + Guid.NewGuid();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);
        string blobName = "nunit" + Guid.NewGuid();

        try
        {
            BlobClient blob = container.GetBlobClient(blobName);
            await blob.UploadAsync(BinaryData.FromString(data), new BlobUploadOptions { Metadata = initialMetadata });

            IDictionary<string, string> metadata = blob.GetProperties().Value.Metadata;
            metadata.Add("foo", "bar");
            blob.SetMetadata(metadata);

            var expectedMetadata = new Dictionary<string, string> { { "foo", "bar" }, { "fizz", "buzz" } };
            var actualMetadata = (await blob.GetPropertiesAsync()).Value.Metadata;

            Assert.AreEqual(expectedMetadata.Count, actualMetadata.Count);

            foreach (KeyValuePair<string, string> expectedKvp in expectedMetadata)
            {
                Assert.IsTrue(actualMetadata.TryGetValue(expectedKvp.Key, out var actualValue));
                Assert.AreEqual(expectedKvp.Value, actualValue);
            }
        }
        finally
        {
            await container.DeleteIfExistsAsync();
        }
    }

    [Test]
    public async Task UploadBlob()
    {
        //DeleteBlobContainers();
        string data = "hello world";
        string fileName = "nunit" + Guid.NewGuid();
        string locFilePath = Path.Combine(localPath, fileName);
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(fileName);

        try
        {
            FileStream fs = File.OpenWrite(locFilePath);
            var bytes = Encoding.UTF8.GetBytes(data);
            await fs.WriteAsync(bytes, 0, bytes.Length);
            await fs.FlushAsync();
            fs.Close();

            BlobClient blob = container.GetBlobClient(fileName);
            using Stream stream = File.OpenRead(locFilePath);
            await blob.UploadAsync(stream, overwrite: true);

            Stream downloadStream = (await blob.DownloadStreamingAsync()).Value.Content;
            string downloadedData = await new StreamReader(downloadStream).ReadToEndAsync();
            downloadStream.Close();

            Assert.AreEqual(data, downloadedData);
        }
        finally
        {
            await container.DeleteIfExistsAsync();
        }
    }

    [Test]
    public async Task ListBlobsHierarchy()
    {
        string data = "hello world";
        string containerName = "nunit" + Guid.NewGuid().ToString();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);
        string virtualDirName = "example";

        try
        {
            foreach (var blobName in new List<string> { "foo.txt", "bar.txt", virtualDirName + "/fizz.txt", virtualDirName + "/buzz.txt" })
            {
                container.GetBlobClient(blobName).Upload(BinaryData.FromString(data));
            }

            // tools to consume blob listing while looking good in the sample snippet
            HashSet<string> downloadedBlobNames = new HashSet<string>();
            HashSet<string> downloadedPrefixNames = new HashSet<string>();
            void MyConsumeBlobItemFunc(BlobHierarchyItem item)
            {
                if (item.IsPrefix)
                {
                    downloadedPrefixNames.Add(item.Prefix);
                }
                else
                {
                    downloadedBlobNames.Add(item.Blob.Name);
                }
            }

            // show in snippet where the prefix goes, but our test doesn't want a prefix for its data set
            string blobPrefix = null;
            string delimiter = "/";
            IAsyncEnumerable<BlobHierarchyItem> results = container.GetBlobsByHierarchyAsync(prefix: blobPrefix, delimiter: delimiter);

            await foreach (BlobHierarchyItem item in results)
            {
                MyConsumeBlobItemFunc(item);
            }

            var expectedBlobNamesResult = new HashSet<string> { "foo.txt", "bar.txt" };

            Assert.IsTrue(expectedBlobNamesResult.SetEquals(downloadedBlobNames));
            Assert.IsTrue(new HashSet<string> { virtualDirName + '/' }.SetEquals(downloadedPrefixNames));
        }
        finally
        {
            await container.DeleteIfExistsAsync();
        }
    }

    [Test]
    public async Task CreateSharedAccessPolicy()
    {
        string containerName = "nunit" + Guid.NewGuid().ToString();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);
        // Create one or more stored access policies.
        List<BlobSignedIdentifier> signedIdentifiers = new List<BlobSignedIdentifier>
                {
                    new BlobSignedIdentifier
                    {
                        Id = "mysignedidentifier",
                        AccessPolicy = new BlobAccessPolicy
                        {
                            StartsOn = DateTimeOffset.UtcNow.AddHours(-1),
                            ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                            Permissions = "rw"
                        }
                    }
                };
        try
        {
            await container.CreateIfNotExistsAsync();
            // Set the container's access policy.
            await container.SetAccessPolicyAsync(permissions: signedIdentifiers);
            BlobContainerAccessPolicy containerAccessPolicy = await container.GetAccessPolicyAsync();

            Assert.AreEqual(signedIdentifiers.FirstOrDefault().Id, containerAccessPolicy.SignedIdentifiers.FirstOrDefault().Id);
        }
        finally
        {
            await container.DeleteIfExistsAsync();
        }
    }


    [Test]
    public async Task DownloadBlobToStream()
    {
        DeleteBlobContainers();
        string data = "hello world";
        //setup blob
        string containerName = "nunit" + Guid.NewGuid().ToString();
        string blobName = "nunit" + Guid.NewGuid().ToString();
        BlobContainerClient container = await blobServiceClient!.CreateBlobContainerAsync(containerName);
        string downloadFilePath = Path.Combine(localPath, "downloadedStream.txt");

        try
        {
            container.GetBlobClient(blobName).Upload(BinaryData.FromString(data));
            BlobClient blobClient = container.GetBlobClient(blobName);

            //Creo il file, Apro lo stream in locale e ci scrivo il contenuto del blob; poi leggo.
            using (FileStream target = File.OpenWrite(downloadFilePath))
            {
                await blobClient.DownloadToAsync(target);
            }

            FileStream fs = File.OpenRead(downloadFilePath);
            string downloadedData = await new StreamReader(fs).ReadToEndAsync();
            fs.Close();

            string downloadedData2;
            using (StreamReader reader = File.OpenText(downloadFilePath))
            {
                downloadedData2 = await reader.ReadToEndAsync();
            }
            Assert.AreEqual(data, downloadedData2);
            Assert.AreEqual(data, downloadedData);
        }
        finally
        {
            await container.DeleteIfExistsAsync();
        }
    }

    public async void DeleteBlobContainers()
    {
        foreach (BlobContainerItem c in blobServiceClient!.GetBlobContainers())
        {
            BlobContainerClient cont = blobServiceClient!.GetBlobContainerClient(c.Name);
            await cont.DeleteAsync();
        }
    }
}