using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.Storage.Blob;
using System.Text;

string text = "hello";
string fileName = "ciao.txt";
Directory.CreateDirectory("data");
string localFilePath = Path.Combine("data", fileName);

var blobServiceClient = new BlobServiceClient(new Uri("https://lorenzo127.blob.core.windows.net"), new DefaultAzureCredential());
string containerName = "nunit" + Guid.NewGuid().ToString();
BlobContainerClient container = blobServiceClient.GetBlobContainerClient("nunit95776e05-77b4-4d19-85ce-d8f9109aa6b6");

BlobSasBuilder sasBuilder = new BlobSasBuilder()
{
    BlobContainerName = containerName,
    StartsOn = DateTimeOffset.UtcNow,
    ExpiresOn = DateTimeOffset.UtcNow.AddHours(24), // Set expiry time
    Resource = "c", // Set resource type (c for container)
};

BlobAccessPolicy accesspolicy = new();

BlobSignedIdentifier identifier = new BlobSignedIdentifier()
{
    Id = "", // Set identifier
    AccessPolicy = accesspolicy // Set access policy
};


List<BlobSignedIdentifier> signedIdentifiers = new List<BlobSignedIdentifier>()
                {
                    //Each SignedIdentifier field, with its unique Id field,
                    //  corresponds to one access policy. 
                    new BlobSignedIdentifier()
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
    //A stored access policy provides an additional level of control over service-level
    //  shared access signatures.
    //Establishing a stored access policy serves to group shared access signatures and
    //  to provide additional restrictions for signatures that are bound by the policy.
    //You can use a stored access policy to change the start time, expiry time, or
    // permissions for a signature. You can also use a stored access policy to revoke
    // a signature after it has been issued.
    //A stored access policy on a container can be associated with a shared access
    // signature that grants permissions to the container itself or to the blobs that it contains.
    //You can set a maximum of five access policies on a container, table, queue, or share at a time. 
    //Changing the signed identifier breaks the associations between any existing signatures and the
    //  stored access policy.

    //await container.SetAccessPolicyAsync();

    //BlobClient blob = container.GetBlobClient(Guid.NewGuid().ToString() + fileName);
    //using (FileStream fs = File.OpenWrite(localFilePath))
    //{
    //    var bytes = Encoding.UTF8.GetBytes(text);
    //    await fs.WriteAsync(bytes, 0, bytes.Length);
    //    await fs.FlushAsync();
    //    fs.Close();

    //    using Stream stream = File.OpenRead(localFilePath);
    //    await blob.UploadAsync(stream, overwrite: true);
    //}
    Console.WriteLine(container.Name);
    BlobContainerAccessPolicy containerAccessPolicy =  container.GetAccessPolicy();
    Console.WriteLine(containerAccessPolicy.SignedIdentifiers.First().Id);
}
catch (RequestFailedException ex)
{
    Console.WriteLine(ex.Message);
    throw;
}
finally
{
    //await container.DeleteIfExistsAsync();
}
