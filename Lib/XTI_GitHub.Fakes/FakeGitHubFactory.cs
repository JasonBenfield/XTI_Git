namespace XTI_GitHub.Fakes;

public sealed class FakeGitHubFactory : IGitHubFactory
{
    public XtiGitHubRepository CreateGitHubRepository(string owner, string name) =>
        new FakeXtiGitHubRepository(owner);
}
