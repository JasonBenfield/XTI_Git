namespace XTI_GitHub.Fakes;

public sealed class FakeGitHubFactory : IGitHubFactory
{
    public Task<XtiGitHubRepository> CreateNewGitHubRepositoryIfNotExists(string owner, string name) =>
        Task.FromResult(CreateGitHubRepository(owner, name));

    public XtiGitHubRepository CreateGitHubRepository(string owner, string name) =>
        new FakeXtiGitHubRepository(owner, name);

    public Task<XtiGitHubRepository> CreateNewOrganizationGitHubRepositoryIfNotExists(string organization, string name) =>
        Task.FromResult(CreateGitHubRepository(organization, name));
}
