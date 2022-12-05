namespace XTI_GitHub;

public sealed record GitHubMilestone(int Number, string Title, string Description, string State = "Open")
{
    public bool IsOpen() => State.Equals("Open", StringComparison.OrdinalIgnoreCase);
}