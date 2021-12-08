namespace XTI_GitHub;

public sealed record GitHubPullRequest(int Number, string Title, string Body, string Head, string Base, string State)
{
    public bool IsOpen() => State.Equals("Open", StringComparison.OrdinalIgnoreCase);
}