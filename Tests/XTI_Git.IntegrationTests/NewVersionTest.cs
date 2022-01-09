using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using XTI_Configuration.Extensions;
using XTI_Git.Abstractions;
using XTI_GitHub;

namespace XTI_Git.IntegrationTests;

internal sealed class NewVersionTest
{
    private static readonly string gitRepoPath = "C:\\XTI\\src\\XTI_Core";
    private static readonly string repoName = "XTI_Core";

    [Test]
    public async Task ShouldCreateBranchForNewVersion()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V13");
        await repo.CreateNewVersion(newVersion);
        var branches = await repo.Branches();
        var newVersionBranch = branches.FirstOrDefault(b => b == $"xti/{newVersion.Type}/{newVersion.Key}");
        Assert.That(newVersionBranch, Is.Not.Null, "Should name the branch after the new version");
    }

    [Test]
    public async Task ShouldCreateMilestoneForNewVersion()
    {
        var services = setup();
        var repo = getGitHubRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var milestones = await repo.Milestones();
        var milestone = milestones.FirstOrDefault(m => m.Title == $"xti_{newVersion.Type}_{newVersion.Key}");
        Assert.That(milestone, Is.Not.Null, "Should create milestone for new version");
    }

    [Test]
    public async Task ShouldCheckoutNewVersionBranch()
    {
        var services = setup();
        var repo = getGitRepo(services);
        await repo.CheckoutBranch("main");
        var currentBranchName = repo.CurrentBranchName();
        Assert.That(currentBranchName, Is.EqualTo("main"));
        var newVersion = new XtiGitVersion("Patch", "V1");
        var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
        await repo.CheckoutBranch(newVersionBranchName);
        currentBranchName = repo.CurrentBranchName();
        Assert.That(currentBranchName, Is.EqualTo(newVersionBranchName));
    }

    [Test]
    public async Task ShouldDeleteNewVersionBranch()
    {
        var services = setup();
        var repo = getGitRepo(services);
        await repo.CheckoutBranch("main");
        var newVersion = new XtiGitVersion("Patch", "V1");
        var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
        await repo.CheckoutBranch(newVersionBranchName);
        await repo.CheckoutBranch("main");
        repo.DeleteBranch(newVersionBranchName);
        var currentBranchName = repo.CurrentBranchName();
        Assert.That(currentBranchName, Is.EqualTo("main"));
    }

    [Test]
    public async Task ShouldCommitChanges()
    {
        var services = setup();
        var repo = getGitRepo(services);
        await repo.CheckoutBranch("main");
        var newVersion = new XtiGitVersion("Patch", "V1");
        var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
        await repo.CheckoutBranch(newVersionBranchName);
        var changedLine = Guid.NewGuid().ToString("N");
        using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test.txt"), true))
        {
            await writer.WriteLineAsync(changedLine);
        }
        await repo.CommitChanges($"Added line {changedLine}");
    }

    [Test]
    public async Task ShouldCommit_WhenThereAreNoChanges()
    {
        var services = setup();
        var repo = getGitRepo(services);
        await repo.CheckoutBranch("main");
        var newVersion = new XtiGitVersion("Patch", "V1");
        var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
        await repo.CheckoutBranch(newVersionBranchName);
        await repo.CommitChanges("Should be no changes");
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