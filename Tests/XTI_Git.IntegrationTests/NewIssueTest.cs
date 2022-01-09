using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using XTI_Configuration.Extensions;
using XTI_Git.Abstractions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

internal sealed class NewIssueTest
{
    [Test]
    public async Task ShouldCreateNewIssue()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        await repo.CreateIssue(newVersion, issueTitle);
        var issues = await repo.Issues();
        Assert.That(issues.Length, Is.EqualTo(1), "Should create new issue");
        Assert.That(issues[0].Title, Is.EqualTo(issueTitle), "Should create new issue");
    }

    [Test]
    public async Task ShouldNotCreateDuplicateOpenIssue()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        await repo.CreateIssue(newVersion, issueTitle);
        await repo.CreateIssue(newVersion, issueTitle);
        var issues = await repo.Issues();
        Assert.That(issues.Length, Is.EqualTo(1), "Should not create duplicate open issue with the same title");
    }

    [Test]
    public async Task ShouldStartIssue()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        const string issueTitle = "Test Issue";
        await repo.CreateIssue(newVersion, issueTitle);
        var issues = await repo.Issues();
        await repo.StartIssue(newVersion, issues[0].Number);
        issues = await repo.Issues();
        Assert.That(issues[0].Labels, Has.One.EqualTo("in progress"), "Should add in progress label when starting issue");
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
                    services.AddTestServices(hostContext.HostingEnvironment, "JasonBenfield", "XTI_GitLab", "c:\\xti\\src\\XTI_GitLab");
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