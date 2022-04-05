using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using XTI_Core.Extensions;
using XTI_Git.Abstractions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

public sealed class CompleteVersionTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\HubWebApp";

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
        var repoInfo = await repo.RepositoryInformation();
        await gitRepo.CheckoutBranch(repoInfo.DefaultBranch);
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
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddTestServices("JasonBenfield", "XTI_GitLab", gitRepoPath);
        return hostBuilder.Build().Scope();
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