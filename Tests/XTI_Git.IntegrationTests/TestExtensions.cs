using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XTI_Core;
using XTI_Git.Abstractions;
using XTI_Git.GitLib;
using XTI_Git.Secrets;
using XTI_GitHub;
using XTI_GitHub.Web;
using XTI_Secrets.Extensions;

namespace XTI_Git.IntegrationTests;

internal static class TestExtensions
{
    public static void AddTestServices(this IServiceCollection services, IHostEnvironment hostEnv, string repoOwner, string repoName, string gitRepoPath)
    {
        services.AddFileSecretCredentials(hostEnv);
        services.AddSingleton<XtiFolder>();
        services.AddScoped<IGitHubCredentialsAccessor, SecretGitHubCredentialsAccessor>();
        services.AddScoped<IGitHubFactory, WebGitHubFactory>();
        services.AddScoped(sp => sp.GetRequiredService<IGitHubFactory>().CreateGitHubRepository(repoOwner, repoName));
        services.AddScoped<GitLibCredentials>();
        services.AddScoped<IXtiGitFactory, GitLibFactory>();
        services.AddScoped(sp => sp.GetRequiredService<IXtiGitFactory>().CreateRepository(gitRepoPath));
    }
}