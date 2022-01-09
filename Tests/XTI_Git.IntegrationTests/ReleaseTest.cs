using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using XTI_Configuration.Extensions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

internal sealed class ReleaseTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\XTI_GitLab";

    [Test]
    public async Task ShouldDownloadReleaseAsset()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var release = await repo.Release("v1.0-alpha");
        var asset = await repo.DownloadReleaseAsset(release.Assets[0]);
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "hub.zip");
        if (File.Exists(path)) { File.Delete(path); }
        using (var writer = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
        {
            await writer.WriteAsync(asset);
        }
    }

    [Test]
    public async Task ShouldUploadRelease()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "hub.zip");
        using var stream = new MemoryStream(File.ReadAllBytes(path));
        var release = await repo.CreateRelease
        (
            "v1.1-alpha",
            "Test Upload",
            "Testing"
        );
        await repo.UploadReleaseAsset
        (
            release,
            new FileUpload(stream, "test1.zip", "application/zip")
        );
    }

    private IServiceProvider setup()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration
            (
                (hostContext, config) =>
                {
                    config.UseXtiConfiguration(hostContext.HostingEnvironment, new string[] { });
                }
            )
            .ConfigureServices
            (
                (hostContext, services) =>
                {
                    services.AddTestServices(hostContext.HostingEnvironment, "JasonBenfield", "XTI_GitLab", gitRepoPath);
                }
            )
            .Build();
        var scope = host.Services.CreateScope();
        return scope.ServiceProvider;
    }

    private static XtiGitHubRepository getGitHubRepo(IServiceProvider services)
    {
        return services.GetRequiredService<XtiGitHubRepository>();
    }
}