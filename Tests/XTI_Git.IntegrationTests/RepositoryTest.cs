using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System.Text.Json;
using XTI_Configuration.Extensions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

internal sealed class RepositoryTest
{
    private static readonly string gitRepoPath = "c:\\xti\\appdata\\Test\\GitTest";

    [Test]
    public async Task ShouldCreateRepository()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        await repo.CreateRepositoryIfNotExists("TestLib");
        var gitHubRepo = await repo.CreateRepositoryIfNotExists("TestLib");
        Console.WriteLine(JsonSerializer.Serialize(gitHubRepo, new JsonSerializerOptions { WriteIndented = true }));
        var testLibDir = Path.Combine(gitRepoPath);
        if (Directory.Exists(testLibDir)) { Directory.Delete(testLibDir, true); }
        var gitFactory = services.GetRequiredService<IXtiGitFactory>();
        await gitFactory.CloneRepository(gitHubRepo.Url, gitRepoPath);
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