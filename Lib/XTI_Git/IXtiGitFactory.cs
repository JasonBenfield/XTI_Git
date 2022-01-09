namespace XTI_Git;

public interface IXtiGitFactory
{
    Task<IXtiGitRepository> CloneRepository(string repoUrl, string path);
    IXtiGitRepository CreateRepository(string path);
}