using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XTI_Core;
using XTI_Git.GitLib;
using XTI_GitHub;
using XTI_GitHub.Web;
using XTI_Secrets.Extensions;

namespace XTI_Git.IntegrationTests
{
    public static class TestExtensions
    {
        public static void AddTestServices(this IServiceCollection services, IHostEnvironment hostEnv, string repoOwner, string repoName, string gitRepoPath)
        {
            services.AddFileSecretCredentials(hostEnv);
            services.AddSingleton<XtiFolder>();
            services.AddScoped<XtiGitHubRepository>(sp =>
            {
                return new WebXtiGitHubRepository(repoOwner, repoName);
            });
            services.AddScoped<XtiGitRepository>(sp =>
            {
                return new GitLibXtiGitRepository(gitRepoPath);
            });
        }
    }
}
