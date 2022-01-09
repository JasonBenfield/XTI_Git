using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using XTI_Git.Abstractions;

namespace XTI_Git.GitLib;

public sealed class GitLibCredentials
{
    private readonly IGitHubCredentialsAccessor gitHubCredentialsAccessor;
    private bool hasLoaded;
    private CredentialsHandler? credentialsHandler;
    private Signature? signature;

    public GitLibCredentials(IGitHubCredentialsAccessor gitHubCredentialsAccessor)
    {
        this.gitHubCredentialsAccessor = gitHubCredentialsAccessor;
    }

    public async Task<CredentialsHandler> CredentialsHandler()
    {
        await loadOnce();
        return credentialsHandler ?? throw new ArgumentNullException(nameof(credentialsHandler));
    }

    public async Task<Signature> Signature()
    {
        await loadOnce();
        return signature ?? throw new ArgumentNullException(nameof(signature));
    }

    private async Task loadOnce()
    {
        if (!hasLoaded)
        {
            var credentials = await gitHubCredentialsAccessor.Value();
            credentialsHandler = new CredentialsHandler
            (
                (url, usernameFromUrl, types) =>
                {
                    return new UsernamePasswordCredentials()
                    {
                        Username = credentials.UserName,
                        Password = credentials.Password
                    };
                }
            );
            signature = new Signature
            (
                new Identity(credentials.UserName, credentials.UserName),
                DateTimeOffset.Now
            );
            hasLoaded = true;
        }
    }
}
