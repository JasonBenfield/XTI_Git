using Microsoft.Extensions.DependencyInjection;
using XTI_GitHub;
using XTI_GitHub.Fakes;

namespace XTI_Git.Tests;

internal static class TestExtensions
{
    public static void AddTestServices(this IServiceCollection services, string repoOwner, string repoName)
    {
        services.AddScoped<IGitHubFactory, FakeGitHubFactory>();
        services.AddScoped(sp => sp.GetRequiredService<IGitHubFactory>().CreateGitHubRepository(repoOwner, repoName));
    }
}