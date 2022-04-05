using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using XTI_Core.Extensions;
using XTI_Git.Abstractions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

public sealed class CompleteIssueTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\XTI_GitLab";

    [Test]
    public async Task ShouldCompleteIssue()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var gitRepo = getGitRepo(services);
        await gitRepo.CheckoutBranch(newVersion.BranchName().Value);
        const string issueTitle = "Test Issue";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        await gitRepo.CheckoutBranch(issue.BranchName().Value);

        var changedLine = Guid.NewGuid().ToString("N");
        using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test.txt"), true))
        {
            await writer.WriteLineAsync(changedLine);
        }
        await gitRepo.CommitChanges($"Complete issue test {changedLine}");

        await repo.CompleteIssue(issue.BranchName());
        var pullRequests = await repo.PullRequests();
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