using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using XTI_Configuration.Extensions;
using XTI_Git.Abstractions;
using XTI_GitHub;
using XTI_GitHub.Web;
using XTI_Secrets.Files;

namespace XTI_Git.IntegrationTests;

public sealed class CompleteIssueTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\XTI_GitLab";

    [Test]
    public async Task ShouldCompleteIssue()
    {
        var services = await setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var gitRepo = getGitRepo(services);
        gitRepo.CheckoutBranch(newVersion.BranchName().Value);
        const string issueTitle = "Test Issue";
        var issue = await repo.CreateIssue(newVersion, issueTitle);
        await repo.StartIssue(newVersion, issue.Number);
        gitRepo.CheckoutBranch(issue.BranchName().Value);

        var changedLine = Guid.NewGuid().ToString("N");
        using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test.txt"), true))
        {
            await writer.WriteLineAsync(changedLine);
        }
        gitRepo.CommitChanges($"Complete issue test {changedLine}");

        await repo.CompleteIssue(issue.BranchName());
        var pullRequests = await repo.PullRequests();
    }

    private async Task<IServiceProvider> setup()
    {
        var sp = configureServices();
        var gitHubRepo = (WebXtiGitHubRepository)sp.GetRequiredService<XtiGitHubRepository>();
        var credentialsFactory = sp.GetRequiredService<SharedFileSecretCredentialsFactory>();
        var credentials = credentialsFactory.Create("GitHub");
        var credentialsValue = await credentials.Value();
        gitHubRepo.UseCredentials(credentialsValue.UserName, credentialsValue.Password);
        var gitRepo = sp.GetRequiredService<IXtiGitRepository>();
        gitRepo.UseCredentials(credentialsValue.UserName, credentialsValue.Password);
        return sp;
    }

    private IServiceProvider configureServices()
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
                    services.AddTestServices(hostContext.HostingEnvironment, "JasonBenfield", "XTI_GitLab", gitRepoPath);
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