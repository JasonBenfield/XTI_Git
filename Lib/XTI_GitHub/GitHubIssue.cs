using XTI_Git.Abstractions;

namespace XTI_GitHub;

public sealed record GitHubIssue
(
    int Number,
    string Title,
    string UserLogin,
    GitHubMilestone Milestone,
    string State,
    string[] Labels,
    string[] Assignees
)
{
    public bool IsOpen() => State.Equals("Open", StringComparison.OrdinalIgnoreCase);

    public XtiIssueBranchName BranchName() => new XtiIssueBranchName(Number, Title);

    public GitHubIssueUpdate ToUpdate() => new GitHubIssueUpdate(this);
}