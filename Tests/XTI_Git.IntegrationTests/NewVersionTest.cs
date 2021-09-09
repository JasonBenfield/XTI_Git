using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XTI_Configuration.Extensions;
using XTI_Git.Abstractions;
using XTI_Git.GitLib;
using XTI_GitHub;
using XTI_GitHub.Web;
using XTI_Secrets;
using XTI_Secrets.Extensions;
using XTI_Secrets.Files;

namespace XTI_Git.IntegrationTests
{
    public sealed class NewVersionTest
    {
        private static readonly string gitRepoPath = "C:\\XTI\\src\\XTI_GitLab";

        [Test]
        public async Task ShouldCreateBranchForNewVersion()
        {
            var services = await setup();
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
            var services = await setup();
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
            var services = await setup();
            var repo = getGitRepo(services);
            repo.CheckoutBranch("main");
            var currentBranchName = repo.CurrentBranchName();
            Assert.That(currentBranchName, Is.EqualTo("main"));
            var newVersion = new XtiGitVersion("Patch", "V1");
            var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
            repo.CheckoutBranch(newVersionBranchName);
            currentBranchName = repo.CurrentBranchName();
            Assert.That(currentBranchName, Is.EqualTo(newVersionBranchName));
        }

        [Test]
        public async Task ShouldDeleteNewVersionBranch()
        {
            var services = await setup();
            var repo = getGitRepo(services);
            repo.CheckoutBranch("main");
            var newVersion = new XtiGitVersion("Patch", "V1");
            var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
            repo.CheckoutBranch(newVersionBranchName);
            repo.CheckoutBranch("main");
            repo.DeleteBranch(newVersionBranchName);
            var currentBranchName = repo.CurrentBranchName();
            Assert.That(currentBranchName, Is.EqualTo("main"));
        }

        [Test]
        public async Task ShouldCommitChanges()
        {
            var services = await setup();
            var repo = getGitRepo(services);
            repo.CheckoutBranch("main");
            var newVersion = new XtiGitVersion("Patch", "V1");
            var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
            repo.CheckoutBranch(newVersionBranchName);
            var changedLine = Guid.NewGuid().ToString("N");
            using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test.txt"), true))
            {
                await writer.WriteLineAsync(changedLine);
            }
            repo.CommitChanges($"Added line {changedLine}");
        }

        [Test]
        public async Task ShouldCommit_WhenThereAreNoChanges()
        {
            var services = await setup();
            var repo = getGitRepo(services);
            repo.CheckoutBranch("main");
            var newVersion = new XtiGitVersion("Patch", "V1");
            var newVersionBranchName = $"xti/{newVersion.Type}/{newVersion.Key}";
            repo.CheckoutBranch(newVersionBranchName);
            repo.CommitChanges("Should be no changes");
        }

        private async Task<IServiceProvider> setup()
        {
            var sp = configureServices();
            var gitHubRepo = (WebXtiGitHubRepository)sp.GetService<XtiGitHubRepository>();
            var credentialsFactory = sp.GetService<ISecretCredentialsFactory>();
            var credentials = credentialsFactory.Create("GitHub");
            var credentialsValue = await credentials.Value();
            gitHubRepo.UseCredentials(credentialsValue.UserName, credentialsValue.Password);
            var gitRepo = sp.GetService<XtiGitRepository>();
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
                        services.AddXtiDataProtection();
                        services.AddFileSecretCredentials();
                        services.AddScoped<XtiGitHubRepository>(sp =>
                        {
                            return new WebXtiGitHubRepository("JasonBenfield", "SharedWebApp");
                            return new WebXtiGitHubRepository("JasonBenfield", "XTI_GitLab");
                        });
                        services.AddScoped<XtiGitRepository>(sp =>
                        {
                            return new GitLibXtiGitRepository(gitRepoPath);
                        });
                    }
                )
                .Build();
            var scope = host.Services.CreateScope();
            return scope.ServiceProvider;
        }

        private static XtiGitHubRepository getGitHubRepo(IServiceProvider services)
        {
            return services.GetService<XtiGitHubRepository>();
        }

        private static XtiGitRepository getGitRepo(IServiceProvider services)
        {
            return services.GetService<XtiGitRepository>();
        }
    }
}
