using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using XTI_Core.Extensions;
using XTI_Git.Abstractions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

internal sealed class NewIssueBusinessTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\GreerCPW\\Business";

    [Test]
    public async Task ShouldStartIssue()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V13");
        const string issueTitle = "Test Issue 1";
        var createdIssue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, createdIssue.Number);
    }

    private IServiceProvider setup()
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddTestServices("GreerCPW", "Business", gitRepoPath);
        return hostBuilder.Build().Scope();
    }

    private static XtiGitHubRepository getGitHubRepo(IServiceProvider services)
    {
        return services.GetRequiredService<XtiGitHubRepository>();
    }
}