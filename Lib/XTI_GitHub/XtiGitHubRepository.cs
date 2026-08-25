using XTI_Git.Abstractions;

namespace XTI_GitHub;

public abstract class XtiGitHubRepository
{
    protected readonly string repoOwner;

    protected XtiGitHubRepository(string repoOwner)
    {
        this.repoOwner = repoOwner;
    }

    public Task<GitHubRepo> RepositoryInformation() => _RepositoryInformation();

    protected abstract Task<GitHubRepo> _RepositoryInformation();

    public async Task CreateNewVersion(XtiGitVersion newVersion, CancellationToken ct)
    {
        var branchName = newVersion.BranchName().Value;
        if (!await branchExists(branchName, ct))
        {
            await _CreateBranch(branchName, ct);
        }
        var milestoneName = newVersion.MilestoneName();
        var milestoneTitle = milestoneName.Value;
        var exists = await milestoneExists(milestoneTitle, ct);
        if (!exists)
        {
            await _CreateMilestone(milestoneTitle, $"{milestoneName.Version.Key} [{milestoneName.Version.Type}]", ct);
        }
    }

    public async Task<GitHubIssue[]> OpenIssues(GitHubMilestone milestone, CancellationToken ct)
    {
        var issues = await _Issues
        (
            new FilterIssueRequest
            {
                IncludeOpenOnly = true,
                Milestone = milestone.Number
            },
            ct
        );
        return issues
            .Where(iss => !iss.Labels.Contains(closePending))
            .ToArray();
    }

    public Task<GitHubIssue[]> Issues(CancellationToken ct) => _Issues(new FilterIssueRequest(), ct);

    protected abstract Task<GitHubIssue[]> _Issues(FilterIssueRequest request, CancellationToken ct);

    public Task<GitHubIssue> Issue(int issueNumber, CancellationToken ct) => _Issue(issueNumber, ct);

    public async Task<GitHubIssue> CreateIssue(XtiGitVersion version, string issueTitle, CancellationToken ct)
    {
        var openIssues = await _Issues(new FilterIssueRequest { IncludeOpenOnly = true }, ct);
        var issue = openIssues.FirstOrDefault(iss => iss.Title.Equals(issueTitle));
        if (issue == null)
        {
            var milestones = await Milestones(ct);
            var milestoneName = version.MilestoneName().Value;
            var milestone = milestones
                .Where(m => m.Title.Equals(milestoneName))
                .Select(m => m.Number)
                .FirstOrDefault();
            issue = await _CreateIssue(milestone, issueTitle, ct);
        }
        return issue;
    }

    protected abstract Task<GitHubIssue> _CreateIssue(int milestoneNumber, string issueTitle, CancellationToken ct);

    private const string inProgress = "in progress";

    public async Task<GitHubIssue> StartIssue(XtiGitVersion version, int issueNumber, CancellationToken ct)
    {
        var exists = await _LabelExists(inProgress, ct);
        if (!exists)
        {
            await _CreateLabel(inProgress, "0E8A16", ct);
        }
        var issue = await _Issue(issueNumber, ct);
        var update = issue.ToUpdate();
        if (!issue.Labels.Any(l => l == inProgress))
        {
            update.AddLabel(inProgress);
        }
        if (!issue.Assignees.Any(a => a.Equals(issue.UserLogin, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(issue.UserLogin))
        {
            update.AddAssignee(issue.UserLogin);
        }
        var milestone = await getMilestone(version.MilestoneName().Value, ct);
        if (issue.Milestone.Number != milestone.Number)
        {
            update.MilestoneNumber = milestone.Number;
        }
        await _UpdateIssue(issue, update, ct);
        return issue;
    }

    private const string closePending = "close pending";

    public async Task CompleteIssue(XtiIssueBranchName issueBranchName, CancellationToken ct)
    {
        if (!await _LabelExists(closePending, ct))
        {
            await _CreateLabel(closePending, "BFD4F2", ct);
        }
        var issue = await _Issue(issueBranchName.IssueNumber, ct);
        var update = issue.ToUpdate();
        if (issue.Labels.Contains(inProgress))
        {
            update.RemoveLabel(inProgress);
        }
        if (!issue.Labels.Contains(closePending))
        {
            update.AddLabel(closePending);
        }
        await _UpdateIssue(issue, update, ct);
        var milestoneName = XtiMilestoneName.Parse(issue.Milestone.Title);
        var pullRequest = await _CreatePullRequest
        (
            issue.Title,
            $"Closes #{issue.Number}",
            issueBranchName.Value,
            new XtiVersionBranchName(milestoneName.Version).Value,
            ct
        );
        await _MergePullRequest(pullRequest, ct);
        await _LinkPullRequest(pullRequest, issue, ct);
    }

    protected abstract Task _LinkPullRequest(GitHubPullRequest ghPullRequest, GitHubIssue ghIssue, CancellationToken ct);

    public async Task CompleteVersion(XtiVersionBranchName versionBranchName, CancellationToken ct)
    {
        var milestone = await getMilestone(new XtiMilestoneName(versionBranchName.Version).Value, ct);
        GitHubIssue[] milestoneIssues;
        if (milestone.Number == 0)
        {
            milestoneIssues = [];
        }
        else
        {
            milestoneIssues = await _Issues
            (
                new FilterIssueRequest
                {
                    Milestone = milestone.Number,
                    IncludeOpenOnly = true
                },
                ct
            );
        }
        var repositoryInfo = await RepositoryInformation();
        var branches = await _Branches(ct);
        if (branches.Any(b => b.Equals(versionBranchName.Value, StringComparison.OrdinalIgnoreCase)))
        {
            var prTitle = string.IsNullOrWhiteSpace(milestone.Description)
                ? milestone.Title
                : milestone.Description;
            var pullRequest = await _CreatePullRequest
            (
                prTitle,
                "",
                versionBranchName.Value,
                repositoryInfo.DefaultBranch,
                ct
            );
            await _MergePullRequest(pullRequest, ct);
        }
        foreach (var milestoneIssue in milestoneIssues)
        {
            await close(milestoneIssue, ct);
        }
        if (milestone != null)
        {
            await _CloseMilestone(milestone, ct);
        }
    }

    protected abstract Task _CloseMilestone(GitHubMilestone milestone, CancellationToken ct);

    private Task close(GitHubIssue ghIssue, CancellationToken ct)
    {
        var update = ghIssue.ToUpdate();
        update.Close();
        update.RemoveLabel(closePending);
        return _UpdateIssue(ghIssue, update, ct);
    }

    protected abstract Task _UpdateIssue(GitHubIssue ghIssue, GitHubIssueUpdate ghIssueUpdate, CancellationToken ct);

    protected abstract Task<GitHubMilestone> _Milestone(int number, CancellationToken ct);

    protected abstract Task<bool> _LabelExists(string name, CancellationToken ct);

    protected abstract Task _CreateLabel(string name, string color, CancellationToken ct);

    protected abstract Task<GitHubIssue> _Issue(int number, CancellationToken ct);

    public Task<GitHubPullRequest[]> PullRequests(CancellationToken ct) => _PullRequests(ct);

    protected abstract Task<GitHubPullRequest[]> _PullRequests(CancellationToken ct);

    protected abstract Task<GitHubPullRequest> _CreatePullRequest(string title, string body, string head, string baseRef, CancellationToken ct);

    protected abstract Task _MergePullRequest(GitHubPullRequest pullRequest, CancellationToken ct);

    public Task<string[]> Branches(CancellationToken ct) => _Branches(ct);

    private async Task<bool> branchExists(string name, CancellationToken ct)
    {
        var branches = await _Branches(ct);
        return branches.Any(b => b.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    protected abstract Task<string[]> _Branches(CancellationToken ct);

    protected abstract Task _CreateBranch(string name, CancellationToken ct);

    public Task<GitHubMilestone[]> Milestones(CancellationToken ct) => _Milestones(ct);

    public Task<GitHubMilestone> Milestone(string title, CancellationToken ct) => getMilestone(title, ct);

    private async Task<bool> milestoneExists(string title, CancellationToken ct)
    {
        var milestone = await getMilestone(title, ct);
        return milestone.Number > 0;
    }

    private async Task<GitHubMilestone> getMilestone(string title, CancellationToken ct)
    {
        var milestones = await _Milestones(ct);
        return milestones
            .FirstOrDefault(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            ?? new GitHubMilestone(0, "", "");
    }

    protected abstract Task<GitHubMilestone[]> _Milestones(CancellationToken ct);

    protected abstract Task _CreateMilestone(string name, string description, CancellationToken ct);

    public async Task DeleteReleaseIfExists(string tagName, CancellationToken ct)
    {
        var release = await _Release(tagName, ct);
        if (release != null)
        {
            foreach (var asset in release.Assets)
            {
                await DeleteReleaseAsset(asset, ct);
            }
            await DeleteRelease(release, ct);
        }
    }

    protected abstract Task DeleteRelease(GitHubRelease gitHubRelease, CancellationToken ct);

    protected abstract Task DeleteReleaseAsset(GitHubReleaseAsset asset, CancellationToken ct);

    public async Task<GitHubRelease> LatestRelease(CancellationToken ct) =>
        (await _LatestRelease(ct)) ?? throw new Exception($"Latest Release not found");

    protected abstract Task<GitHubRelease?> _LatestRelease(CancellationToken ct);

    public async Task<GitHubRelease> Release(string tagName, CancellationToken ct) =>
        (await _Release(tagName, ct)) ?? throw new Exception($"Release not found for tag '{tagName}'");

    protected abstract Task<GitHubRelease?> _Release(string tagName, CancellationToken ct);

    public Task<GitHubRelease> CreateRelease(string tagName, string name, string body, CancellationToken ct)
        => _CreateRelease(tagName, name, body, ct);

    protected abstract Task<GitHubRelease> _CreateRelease(string tagName, string name, string body, CancellationToken ct);

    public Task UploadReleaseAsset(GitHubRelease release, GitHubFileUpload asset, CancellationToken ct) => _UploadReleaseAsset(release, asset, ct);

    protected abstract Task _UploadReleaseAsset(GitHubRelease release, GitHubFileUpload asset, CancellationToken ct);

    public Task FinalizeRelease(GitHubRelease release, CancellationToken ct) => _FinalizeRelease(release, ct);

    protected abstract Task _FinalizeRelease(GitHubRelease release, CancellationToken ct);

    public Task<byte[]> DownloadReleaseAsset(GitHubReleaseAsset asset, CancellationToken ct) => _DownloadReleaseAsset(asset, ct);

    protected abstract Task<byte[]> _DownloadReleaseAsset(GitHubReleaseAsset asset, CancellationToken ct);
}