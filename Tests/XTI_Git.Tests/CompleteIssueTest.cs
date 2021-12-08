using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using XTI_Git.Abstractions;
using XTI_GitHub;
using XTI_GitHub.Fakes;

namespace XTI_Git.Tests;

internal sealed class CompleteIssueTest
{
    private const string RepoOwner = "JasonBenfield";

    [Test]
    public async Task ShouldRemoveInProgressLabel()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        await repo.CompleteIssue(new XtiIssueBranchName(issue.Number, issue.Title));
        var issues = await repo.Issues();
        Assert.That(issues[0].Labels, Has.None.EqualTo("in progress"), "Should remove in progress label");
    }

    [Test]
    public async Task ShouldAddClosePendingLabel()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        await repo.CompleteIssue(new XtiIssueBranchName(issue.Number, issue.Title));
        var issues = await repo.Issues();
        Assert.That(issues[0].Labels, Has.One.EqualTo("close pending"), "Should add 'close pending' label");
    }

    [Test]
    public async Task ShouldCreatePullRequest()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var issue = await repo.CreateIssue(newVersion, "Test Issue");
        await repo.StartIssue(newVersion, issue.Number);
        await repo.CompleteIssue(new XtiIssueBranchName(issue.Number, issue.Title));
        var pullRequests = await repo.PullRequests();
        Assert.That(pullRequests.Length, Is.EqualTo(1), "Should create pull request");
        var issueBranchName = new XtiIssueBranchName(issue.Number, issue.Title);
        Assert.That(pullRequests[0].Head, Is.EqualTo(issueBranchName.Value), "Should create pull request");
        Assert.That(pullRequests[0].Base, Is.EqualTo(newVersion.BranchName().Value), "Should create pull request");
    }

    [Test]
    public async Task ShouldClosePullRequest()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var issue = await repo.CreateIssue(newVersion, "Test Issue");
        await repo.StartIssue(newVersion, issue.Number);
        await repo.CompleteIssue(new XtiIssueBranchName(issue.Number, issue.Title));
        var pullRequests = await repo.PullRequests();
        Assert.That(pullRequests[0].IsOpen(), Is.False, "Should close pull request");
    }

    private IServiceProvider setup()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices
            (
                (hostContext, services) =>
                {
                    services.AddScoped<XtiGitHubRepository>
                    (
                        _ => new FakeXtiGitHubRepository(RepoOwner)
                    );
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