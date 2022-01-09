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
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        var gitHubRepo = await gitHubFactory.CreateNewGitHubRepositoryIfNotExists("JasonBenfield", "TestLib");
        var gitHubInfo = await gitHubRepo.RepositoryInformation();
        Console.WriteLine(JsonSerializer.Serialize(gitHubInfo, new JsonSerializerOptions { WriteIndented = true }));
        var testLibDir = Path.Combine(gitRepoPath);
        if (Directory.Exists(testLibDir)) { Directory.Delete(testLibDir, true); }
        var gitFactory = services.GetRequiredService<IXtiGitFactory>();
        await gitFactory.CloneRepository(gitHubInfo.CloneUrl, gitRepoPath);
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