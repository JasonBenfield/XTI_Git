using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using XTI_Git.Abstractions;
using XTI_GitHub;
using XTI_GitHub.Fakes;

namespace XTI_Git.Tests
{
    public sealed class NewIssueTest
    {
        private const string RepoOwner = "JasonBenfield";

        [Test]
        public async Task ShouldCreateNewIssue()
        {
            var services = setup();
            var repo = getRepo(services);
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
            var repo = getRepo(services);
            var newVersion = new XtiGitVersion("Patch", "V1");
            await repo.CreateNewVersion(newVersion);
            const string issueTitle = "Test Issue";
            await repo.CreateIssue(newVersion, issueTitle);
            await repo.CreateIssue(newVersion, issueTitle);
            var issues = await repo.Issues();
            Assert.That(issues.Length, Is.EqualTo(1), "Should not create duplicate open issue with the same title");
        }

        [Test]
        public async Task ShouldCreateNewIssueForMilestone()
        {
            var services = setup();
            var repo = getRepo(services);
            var newVersion = new XtiGitVersion("Patch", "V1");
            await repo.CreateNewVersion(newVersion);
            var milestone = (await repo.Milestones()).First(m => m.Title == newVersion.MilestoneName().Value);
            const string issueTitle = "Test Issue";
            await repo.CreateIssue(newVersion, issueTitle);
            var issues = await repo.Issues();
            Assert.That(issues[0].Milestone.Number, Is.GreaterThan(0), "Should create new issue for milestone");
            Assert.That(issues[0].Milestone.Number, Is.EqualTo(milestone.Number), "Should create new issue for milestone");
        }

        [Test]
        public async Task ShouldAddInProgressLabel()
        {
            var services = setup();
            var repo = getRepo(services);
            var newVersion = new XtiGitVersion("Patch", "V1");
            await repo.CreateNewVersion(newVersion);
            await repo.CreateIssue(newVersion, "Test Issue");
            var issues = await repo.Issues();
            await repo.StartIssue(newVersion, issues[0].Number);
            issues = await repo.Issues();
            Assert.That(issues[0].Labels, Has.One.EqualTo("in progress"), "Should add in progress label to issue");
        }

        [Test]
        public async Task ShouldAssignToRepoOwner()
        {
            var services = setup();
            var repo = getRepo(services);
            var newVersion = new XtiGitVersion("Patch", "V1");
            await repo.CreateNewVersion(newVersion);
            await repo.CreateIssue(newVersion, "Test Issue");
            var issues = await repo.Issues();
            await repo.StartIssue(newVersion, issues[0].Number);
            issues = await repo.Issues();
            Assert.That(issues[0].Assignees, Has.One.EqualTo(RepoOwner), "Should assign to repo owner");
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
            return services.GetService<XtiGitHubRepository>();
        }

    }
}
