using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using XTI_Git.Abstractions;
using XTI_GitHub;
using XTI_GitHub.Fakes;

namespace XTI_Git.Tests;

internal sealed class CompleteVersionTest
{
    private const string RepoOwner = "JasonBenfield";
    private const string RepoName = "XTI_Test";

    [Test]
    public async Task ShouldCreatePullRequest()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var versionBranchName = new XtiVersionBranchName(newVersion);
        await repo.CompleteVersion(versionBranchName);
        var pullRequests = await repo.PullRequests();
        Assert.That(pullRequests.Length, Is.EqualTo(1), "Should create pull request");
        Assert.That(pullRequests[0].Head, Is.EqualTo("xti/Patch/V1"), "Should create pull request");
        Assert.That(pullRequests[0].Base, Is.EqualTo("main"), "Should create pull request");
    }

    [Test]
    public async Task ShouldClosePullRequest()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var versionBranchName = new XtiVersionBranchName(newVersion);
        await repo.CompleteVersion(versionBranchName);
        var pullRequests = await repo.PullRequests();
        Assert.That(pullRequests[0].IsOpen(), Is.False, "Should close pull request");
    }

    [Test]
    public async Task ShouldRemoveClosePendingLabelFromIssue()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        await repo.CompleteIssue(new XtiIssueBranchName(issue.Number, issue.Title));
        var versionBranchName = new XtiVersionBranchName(newVersion);
        await repo.CompleteVersion(versionBranchName);
        var issues = await repo.Issues();
        Assert.That(issues[0].Labels, Has.None.EqualTo("close pending"), "Should remove close pending label from issue");
    }

    [Test]
    public async Task ShouldCloseIssue()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        await repo.CompleteIssue(new XtiIssueBranchName(issue.Number, issue.Title));
        var versionBranchName = new XtiVersionBranchName(newVersion);
        await repo.CompleteVersion(versionBranchName);
        var issues = await repo.Issues();
        Assert.That(issues[0].IsOpen(), Is.False, "Should close issue");
    }

    [Test]
    public async Task ShouldCloseMilestone()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        await repo.CompleteIssue(new XtiIssueBranchName(issue.Number, issue.Title));
        var versionBranchName = new XtiVersionBranchName(newVersion);
        await repo.CompleteVersion(versionBranchName);
        var milestones = await repo.Milestones();
        var milestone = milestones
            .First(m => m.Title == newVersion.MilestoneName().Value);
        Assert.That(milestone.IsOpen(), Is.False, $"Should close milestone {milestone.Title}");
    }

    private IServiceProvider setup()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices
            (
                (hostContext, services) =>
                {
                    services.AddTestServices(RepoOwner, RepoName);
                }
            )
            .Build();
        var scope = host.Services.CreateScope();
        return scope.ServiceProvider;
    }

    private static XtiGitHubRepository getRepo(IServiceProvider services)
    {
        return services.GetRequiredService<XtiGitHubRepository>();
    }

}