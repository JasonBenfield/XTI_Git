namespace XTI_GitHub;

public interface IGitHubFactory
{
    Task<XtiGitHubRepository> CreateNewGitHubRepositoryIfNotExists(string owner, string name);
    XtiGitHubRepository CreateGitHubRepository(string owner, string name);
}