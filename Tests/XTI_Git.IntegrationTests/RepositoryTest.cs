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
    public async Task ShouldGetIssue()
    {
        var services = setup();
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        var gitHubRepo = gitHubFactory.CreateGitHubRepository("GreerCPW", "Gis");
        var repo = await gitHubRepo.Issue(280);
        repo.WriteToConsole();
    }

    [Test]
    public async Task ShouldDownloadAsset()
    {
        var services = setup();
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        var gitHubRepo = gitHubFactory.CreateGitHubRepository("GreerCPW", "Gis");
        var release = await gitHubRepo.Release("v1.2.21");
        var asset = release.Assets.First();
        var bytes = await gitHubRepo.DownloadReleaseAsset(asset);
        File.WriteAllBytes($"c:\\xti\\{asset.Name}", bytes);
    }

    [Test]
    public async Task ShouldCreateRepositoryForOrg()
    {
        var services = setup();
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        var gitHubRepo = await gitHubFactory.CreateNewOrganizationGitHubRepositoryIfNotExists("GreerCPW", "TestLib3");
        var repo = await gitHubRepo.RepositoryInformation();
        repo.WriteToConsole();
    }

    [Test]
    public async Task ShouldCreateRepository()
    {
        var services = setup();
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        const string repoOwner = "JasonBenfield";
        const string repoName = "TestLib4";
        var gitHubRepo = await gitHubFactory.CreateNewGitHubRepositoryIfNotExists(repoOwner, repoName);
        var gitHubInfo = await gitHubRepo.RepositoryInformation();
        gitHubInfo.WriteToConsole();
        var testLibDir = Path.Combine("C:", "xti", "src", repoOwner, repoName);
        if (Directory.Exists(testLibDir)) { Directory.Delete(testLibDir, true); }
        var gitFactory = services.GetRequiredService<IXtiGitFactory>();
        await gitFactory.CloneRepository(gitHubInfo.CloneUrl, testLibDir);


    }

    private IServiceProvider setup()
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddTestServices("JasonBenfield", "XTI_GitLab", gitRepoPath);
        return hostBuilder.Build().Scope();
    }
}