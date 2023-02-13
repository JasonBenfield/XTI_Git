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
        var gitHubRepo = await gitHubFactory.CreateNewGitHubRepositoryIfNotExists("JasonBenfield", "TestLib2");
        var gitHubInfo = await gitHubRepo.RepositoryInformation();
        gitHubInfo.WriteToConsole();
    }

    private IServiceProvider setup()
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddTestServices("JasonBenfield", "XTI_GitLab", gitRepoPath);
        return hostBuilder.Build().Scope();
    }
}