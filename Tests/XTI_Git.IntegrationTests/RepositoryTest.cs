using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text.Json;
using XTI_Core.Extensions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

internal sealed class RepositoryTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\JasonBenfield\\SharedWebApp";

    [Test]
    public async Task ShouldGetIssue()
    {
        var services = Setup();
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        var gitHubRepo = gitHubFactory.CreateGitHubRepository("GreerCPW", "Gis");
        var repo = await gitHubRepo.Issue(280);
        repo.WriteToConsole();
    }

    [Test]
    public async Task ShouldDownloadAsset()
    {
        var services = Setup();
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        var gitHubRepo = gitHubFactory.CreateGitHubRepository("GreerCPW", "Gis");
        var release = await gitHubRepo.Release("v1.2.21");
        var asset = release.Assets.First();
        var bytes = await gitHubRepo.DownloadReleaseAsset(asset);
        var fileName = $"c:\\xti\\{asset.Name}";
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }
        File.WriteAllBytes(fileName, bytes);
    }

    [Test]
    public async Task ShouldCreateRepositoryForOrg()
    {
        var services = Setup();
        var gitHubFactory = services.GetRequiredService<IGitHubFactory>();
        var gitHubRepo = await gitHubFactory.CreateNewOrganizationGitHubRepositoryIfNotExists("GreerCPW", "TestLib3");
        var repo = await gitHubRepo.RepositoryInformation();
        repo.WriteToConsole();
    }

    [Test]
    public async Task ShouldCreateRepository()
    {
        var services = Setup();
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

    [Test]
    public async Task ShouldCheckoutBranch()
    {
        var services = Setup();
        var gitFactory = services.GetRequiredService<IXtiGitFactory>();
        var gitRepo = gitFactory.CreateRepository(gitRepoPath);
        await gitRepo.CheckoutBranch("xti/Patch/V1433");
    }

    private IServiceProvider Setup()
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddTestServices("JasonBenfield", "SharedWebApp", gitRepoPath);
        return hostBuilder.Build().Scope();
    }
}