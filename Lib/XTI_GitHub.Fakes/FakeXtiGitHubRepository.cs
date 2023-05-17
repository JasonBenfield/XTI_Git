namespace XTI_GitHub.Fakes;

internal sealed class FakeXtiGitHubRepository : XtiGitHubRepository
{
    private readonly string repoName;
    private readonly List<string> branches = new();

    public FakeXtiGitHubRepository(string repoOwner, string repoName)
        : base(repoOwner)
    {
        this.repoName = repoName;
    }

    protected override Task<GitHubRepo> _RepositoryInformation() =>
        Task.FromResult(new GitHubRepo(repoName, $"https://example.com/{repoOwner}/{repoName}.git", "main"));

    protected override Task<string[]> _Branches() => Task.FromResult(branches.ToArray());

    protected override Task _CreateBranch(string name)
    {
        branches.Add(name);
        return Task.CompletedTask;
    }

    private readonly List<GitHubMilestone> milestones = new();

    private int milestoneNumber = 321;

    protected override Task _CreateMilestone(string name, string description)
    {
        milestones.Add
        (
            new GitHubMilestone(milestoneNumber, name, description)
        );
        milestoneNumber++;
        return Task.CompletedTask;
    }

    protected override Task<GitHubMilestone> _Milestone(int number)
        => Task.FromResult(milestones.FirstOrDefault(m => m.Number == number) ?? new GitHubMilestone(0, "", ""));

    protected override Task<GitHubMilestone[]> _Milestones()
        => Task.FromResult(milestones.ToArray());

    private readonly List<GitHubIssue> issues = new();

    private int issueNumber = 234;

    protected override Task<GitHubIssue[]> _Issues(FilterIssueRequest request)
        => Task.FromResult(issues.Where(i => isMatch(request, i)).ToArray());

    private bool isMatch(FilterIssueRequest request, GitHubIssue issue)
    {
        if (request.IncludeOpenOnly && !issue.IsOpen())
        {
            return false;
        }
        if (request.Milestone.HasValue && issue.Milestone.Number != request.Milestone)
        {
            return false;
        }
        return true;
    }

    protected override async Task<GitHubIssue> _CreateIssue(int milestoneNumber, string issueTitle)
    {
        var milestone = await _Milestone(milestoneNumber);
        var gitHubIssue = new GitHubIssue
        (
            issueNumber,
            issueTitle,
            "",
            milestone,
            "Open",
            new string[0],
            new string[0]
        );
        issues.Add(gitHubIssue);
        issueNumber++;
        return gitHubIssue;
    }

    private readonly List<string> labels = new List<string>();

    protected override Task<bool> _LabelExists(string name)
    {
        var exists = labels.Any(l => l == name);
        return Task.FromResult(exists);
    }

    protected override Task _CreateLabel(string name, string color)
    {
        labels.Add(name);
        return Task.CompletedTask;
    }

    protected override Task<GitHubIssue> _Issue(int number)
    {
        var issue = issues.FirstOrDefault(iss => iss.Number == number);
        return Task.FromResult
        (
            issue
            ?? new GitHubIssue(0, "", "", new GitHubMilestone(0, "", ""), "Open", new string[0], new string[0])
        );
    }

    protected override async Task _UpdateIssue(GitHubIssue ghIssue, GitHubIssueUpdate ghIssueUpdate)
    {
        var milestone = await _Milestone(ghIssueUpdate.MilestoneNumber ?? 0);
        var updatedIssue = ghIssue with
        {
            Milestone = milestone,
            State = ghIssueUpdate.State,
            Labels = ghIssueUpdate.Labels,
            Assignees = ghIssueUpdate.Assignees
        };
        replaceIssue(ghIssue, updatedIssue);
    }

    private void replaceIssue(GitHubIssue issue, GitHubIssue updatedIssue)
    {
        issues.RemoveAll(iss => iss.Number == issue.Number);
        issues.Add(updatedIssue);
    }

    private readonly List<GitHubPullRequest> pullRequests = new List<GitHubPullRequest>();

    protected override Task<GitHubPullRequest[]> _PullRequests()
        => Task.FromResult(pullRequests.ToArray());

    private int pullRequestID = 454;

    protected override Task<GitHubPullRequest> _CreatePullRequest(string title, string body, string head, string baseRef)
    {
        var pullRequest = new GitHubPullRequest(pullRequestID, title, body, head, baseRef, "Open");
        pullRequests.Add(pullRequest);
        pullRequestID++;
        return Task.FromResult(pullRequest);
    }

    protected override Task _MergePullRequest(GitHubPullRequest pullRequest)
    {
        var updatedPullRequest = pullRequest with { State = "Closed" };
        pullRequests.RemoveAll(pr => pr.Number == pullRequest.Number);
        pullRequests.Add(updatedPullRequest);
        return Task.CompletedTask;
    }

    protected override Task _LinkPullRequest(GitHubPullRequest ghPullRequest, GitHubIssue ghIssue)
    {
        return Task.CompletedTask;
    }

    protected override Task _CloseMilestone(GitHubMilestone milestone)
    {
        var updated = milestone with { State = "Closed" };
        milestones.RemoveAll(m => m.Number == milestone.Number);
        milestones.Add(updated);
        return Task.CompletedTask;
    }

    private int releaseID = 2233;
    private readonly List<GitHubRelease> releases = new();

    protected override Task<GitHubRelease?> _LatestRelease()
    {
        var release = releases.LastOrDefault();
        return Task.FromResult(release);
    }

    protected override Task<GitHubRelease?> _Release(string tagName)
    {
        var release = releases.FirstOrDefault(r => r.TagName == tagName);
        return Task.FromResult(release);
    }

    protected override Task DeleteRelease(GitHubRelease gitHubRelease)
    {
        releases.RemoveAll(r => r.ID == gitHubRelease.ID);
        return Task.CompletedTask;
    }

    protected override Task<GitHubRelease> _CreateRelease(string tagName, string name, string body)
    {
        var release = new GitHubRelease(releaseID, tagName, new GitHubReleaseAsset[0]);
        releaseID++;
        releases.Add(release);
        return Task.FromResult(release);
    }

    protected override Task DeleteReleaseAsset(GitHubReleaseAsset asset) => Task.CompletedTask;

    protected override Task _UploadReleaseAsset(GitHubRelease release, GitHubFileUpload asset)
    {
        return Task.CompletedTask;
    }

    protected override Task _FinalizeRelease(GitHubRelease release)
    {
        return Task.CompletedTask;
    }

    protected override Task<byte[]> _DownloadReleaseAsset(GitHubReleaseAsset asset)
    {
        return Task.FromResult(new byte[0]);
    }
}