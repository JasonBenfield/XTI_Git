using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text.Json;
using XTI_Core.Extensions;
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
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddTestServices("JasonBenfield", "XTI_GitLab", gitRepoPath);
        return hostBuilder.Build().Scope();
    }
}