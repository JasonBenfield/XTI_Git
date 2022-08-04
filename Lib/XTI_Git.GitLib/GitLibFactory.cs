using LibGit2Sharp;

namespace XTI_Git.GitLib;

public sealed class GitLibFactory : IXtiGitFactory
{
    private readonly GitLibCredentials credentials;

    public GitLibFactory(GitLibCredentials credentials)
    {
        this.credentials = credentials;
    }

    public async Task<IXtiGitRepository> CloneRepository(string repoUrl, string path)
    {
        var credentialsHandler = await credentials.CredentialsHandler();
        Repository.Clone
        (
            repoUrl,
            path,
            new CloneOptions
            {
                CredentialsProvider = credentialsHandler,
                BranchName = "main"
            }
        );
        return CreateRepository(path);
    }

    public IXtiGitRepository CreateRepository(string path) =>
        new GitLibXtiGitRepository(path, credentials);
}