namespace XTI_GitHub;

public interface IGitHubFactory
{
    XtiGitHubRepository CreateGitHubRepository(string owner, string name);
}