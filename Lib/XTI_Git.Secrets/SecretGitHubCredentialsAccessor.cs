using XTI_Git.Abstractions;
using XTI_Secrets;

namespace XTI_Git.Secrets;

public sealed class SecretGitHubCredentialsAccessor : IGitHubCredentialsAccessor
{
    private readonly ISecretCredentialsFactory secretCredentialsFactory;

    public SecretGitHubCredentialsAccessor(ISecretCredentialsFactory secretCredentialsFactory)
    {
        this.secretCredentialsFactory = secretCredentialsFactory;
    }

    public async Task<GitHubCredentials> Value()
    {
        var creds = secretCredentialsFactory.Create("GitHub");
        var value = await creds.Value();
        return new GitHubCredentials(value.UserName, value.Password);
    }
}