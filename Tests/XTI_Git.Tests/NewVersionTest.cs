using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using XTI_Git.Abstractions;
using XTI_GitHub;

namespace XTI_Git.Tests;

internal sealed class NewVersionTest
{
    private const string RepoOwner = "JasonBenfield";
    private const string RepoName = "XTI_Test";

    [Test]
    public async Task ShouldCreateBranchForNewVersion()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var branches = await repo.Branches();
        Assert.That(branches.Length, Is.EqualTo(1), "Should create branch for new version");
        Assert.That(branches[0], Is.EqualTo($"xti/{newVersion.Type}/{newVersion.Key}"), "Should name the branch after the new version");
    }

    [Test]
    public async Task ShouldNotCreateDuplicateBranchForNewVersion()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        await repo.CreateNewVersion(newVersion);
        var branches = await repo.Branches();
        Assert.That(branches.Length, Is.EqualTo(1), "Should not create duplicate branch for new version");
    }

    [Test]
    public async Task ShouldCreateMilestoneForNewVersion()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        var milestones = await repo.Milestones();
        Assert.That(milestones.Length, Is.EqualTo(1), "Should create milestone for new version");
        Assert.That
        (
            milestones[0].Title,
            Is.EqualTo($"xti_{newVersion.Type}_{newVersion.Key}"),
            "Should name the milestone after the new version"
        );
    }

    [Test]
    public async Task ShouldNotCreateDuplicateMilestoneForNewVersion()
    {
        var services = setup();
        var repo = getRepo(services);
        var newVersion = new XtiGitVersion("Patch", "V1");
        await repo.CreateNewVersion(newVersion);
        await repo.CreateNewVersion(newVersion);
        var milestones = await repo.Milestones();
        Assert.That(milestones.Length, Is.EqualTo(1), "Should not create duplicate milestone for new version");
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