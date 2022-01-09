using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using XTI_Configuration.Extensions;
using XTI_Git.Abstractions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

public sealed class CompleteVersionTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\HubWebApp";
    private static readonly string repoName = "HubWebApp";

    [Test]
    public async Task ShouldCompleteVersion()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Minor", "V1169");
        await repo.CreateNewVersion(newVersion);
        var gitRepo = getGitRepo(services);
        await gitRepo.CheckoutBranch(newVersion.BranchName().Value);
        const string issueTitle = "Test Complete Version";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        await gitRepo.CheckoutBranch(issue.BranchName().Value);
        var changedLine = Guid.NewGuid().ToString("N");
        using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test_V6_issue.txt"), true))
        {
            await writer.WriteLineAsync(changedLine);
        }
        await gitRepo.CommitChanges($"Complete version test issue: {changedLine}");
        await repo.CompleteIssue(issue.BranchName());
        await gitRepo.CheckoutBranch(newVersion.BranchName().Value);
        gitRepo.DeleteBranch(issue.BranchName().Value);
        changedLine = Guid.NewGuid().ToString("N");
        using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test_V6_version.txt"), true))
        {
            await writer.WriteLineAsync(changedLine);
        }
        await gitRepo.CommitChanges($"Complete version test: {changedLine}");
        await repo.CompleteVersion(newVersion.BranchName());
        var defaultBranchName = await repo.DefaultBranchName();
        await gitRepo.CheckoutBranch(defaultBranchName);
        gitRepo.DeleteBranch(newVersion.BranchName().Value);
    }

    [Test]
    public async Task ShouldCompleteHubVersion()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Minor", "V1169");
        await repo.CompleteVersion(newVersion.BranchName());
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
                    services.AddTestServices(hostContext.HostingEnvironment, "JasonBenfield", repoName, gitRepoPath);
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

    private static IXtiGitRepository getGitRepo(IServiceProvider services)
    {
        return services.GetRequiredService<IXtiGitRepository>();
    }
}