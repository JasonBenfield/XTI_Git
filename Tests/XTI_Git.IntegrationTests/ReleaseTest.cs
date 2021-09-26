using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using XTI_Configuration.Extensions;
using XTI_GitHub;
using XTI_GitHub.Web;
using XTI_Secrets;

namespace XTI_Git.IntegrationTests
{
    sealed class ReleaseTest
    {
        private static readonly string gitRepoPath = "C:\\XTI\\src\\XTI_GitLab";

        [Test]
        public async Task ShouldDownloadReleaseAsset()
        {
            var services = await setup();
            var repo = getGitHubRepo(services);
            var release = await repo.Release("v1.0-alpha");
            var asset = await repo.DownloadReleaseAsset(release.Assets[0]);
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "hub.zip");
            if (File.Exists(path)) { File.Delete(path); }
            using (var writer = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await writer.WriteAsync(asset);
            }
        }

        [Test]
        public async Task ShouldUploadRelease()
        {
            var services = await setup();
            var repo = getGitHubRepo(services);
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "hub.zip");
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            var release = await repo.CreateRelease
            (
                "v1.1-alpha",
                "Test Upload",
                "Testing"
            );
            await repo.UploadReleaseAsset
            (
                release,
                new FileUpload(stream, "test1.zip", "application/zip")
            );
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
                        services.AddTestServices(hostContext.HostingEnvironment, "JasonBenfield", "XTI_GitLab", gitRepoPath);
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
