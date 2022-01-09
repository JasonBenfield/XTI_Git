namespace XTI_Git.Fakes;

public sealed class FakeGitFactory : IXtiGitFactory
{
    public Task<IXtiGitRepository> CloneRepository(string repoUrl, string path) =>
        Task.FromResult(CreateRepository(path));

    public IXtiGitRepository CreateRepository(string path) =>
        new FakeXtiGitRepository();
}