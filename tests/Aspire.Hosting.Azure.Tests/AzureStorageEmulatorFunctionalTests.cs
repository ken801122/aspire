// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageEmulatorFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyAzureStorageEmulatorResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var storage = builder.AddAzureStorage("storage").RunAsEmulator().AddBlobs("BlobConnection");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration["ConnectionStrings:BlobConnection"] = await storage.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        hb.AddAzureBlobClient("BlobConnection");

        using var host = hb.Build();
        await host.StartAsync();

        var serviceClient = host.Services.GetRequiredService<BlobServiceClient>();
        var blobContainer = (await serviceClient.CreateBlobContainerAsync("container")).Value;
        var blobClient = blobContainer.GetBlobClient("testKey");

        await blobClient.UploadAsync(BinaryData.FromString("testValue"));

        var downloadResult = (await blobClient.DownloadContentAsync()).Value;
        Assert.Equal("testValue", downloadResult.Content.ToString());
    }
}
