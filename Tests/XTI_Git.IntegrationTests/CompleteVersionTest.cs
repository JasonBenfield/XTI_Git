using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using XTI_Configuration.Extensions;
using XTI_Git.Abstractions;
using XTI_Git.GitLib;
using XTI_GitHub;
using XTI_GitHub.Web;
using XTI_Secrets.Extensions;
using XTI_Secrets.Files;

namespace XTI_Git.IntegrationTests
{
    public sealed class CompleteVersionTest
    {
        private static readonly string gitRepoPath = "C:\\XTI\\src\\XTI_GitLab";

        [Test]
        public async Task ShouldCompleteVersion()
        {
            var services = await setup();
            var repo = getGitHubRepo(services);
            var newVersion = new XtiGitVersion("Patch", "V19");
            await repo.CreateNewVersion(newVersion);
            var gitRepo = getGitRepo(services);
            gitRepo.CheckoutBranch(newVersion.BranchName().Value);
            const string issueTitle = "Test Complete Version";
            var issue = await repo.CreateIssue(newVersion, issueTitle);
            await repo.StartIssue(newVersion, issue.Number);
            gitRepo.CheckoutBranch(issue.BranchName().Value);
            var changedLine = Guid.NewGuid().ToString("N");
            using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test_V6_issue.txt"), true))
            {
                await writer.WriteLineAsync(changedLine);
            }
            gitRepo.CommitChanges($"Complete version test issue: {changedLine}");
            await repo.CompleteIssue(issue.BranchName());
            gitRepo.CheckoutBranch(newVersion.BranchName().Value);
            gitRepo.DeleteBranch(issue.BranchName().Value);
            changedLine = Guid.NewGuid().ToString("N");
            using (var writer = new StreamWriter(Path.Combine(gitRepoPath, "test_V6_version.txt"), true))
            {
                await writer.WriteLineAsync(changedLine);
            }
            gitRepo.CommitChanges($"Complete version test: {changedLine}");
            await repo.CompleteVersion(newVersion.BranchName());
            var defaultBranchName = await repo.DefaultBranchName();
            gitRepo.CheckoutBranch(defaultBranchName);
            gitRepo.DeleteBranch(newVersion.BranchName().Value);
        }

        private async Task<IServiceProvider> setup()
        {
            var sp = configureServices();
            var gitHubRepo = (WebXtiGitHubRepository)sp.GetService<XtiGitHubRepository>();
            var credentialsFactory = sp.GetService<SharedFileSecretCredentialsFactory>();
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
                        services.AddSharedFileSecretCredentials();
                        services.AddScoped<XtiGitHubRepository>(sp =>
                        {
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
