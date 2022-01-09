using XTI_Git.Abstractions;

namespace XTI_GitHub;

public abstract class XtiGitHubRepository
{
    protected readonly string repoOwner;

    protected XtiGitHubRepository(string repoOwner)
    {
        this.repoOwner = repoOwner;
    }

    public Task<string> DefaultBranchName() => _DefaultBranchName();

    protected abstract Task<string> _DefaultBranchName();

    public Task<GitHubRepo> CreateRepositoryIfNotExists(string name) => _CreateRepositoryIfNotExists(name);

    protected abstract Task<GitHubRepo> _CreateRepositoryIfNotExists(string name);

    public async Task CreateNewVersion(XtiGitVersion newVersion)
    {
        var branchName = newVersion.BranchName().Value;
        if (!await branchExists(branchName))
        {
            await _CreateBranch(branchName);
        }
        var milestoneName = newVersion.MilestoneName().Value;
        if (!await milestoneExists(milestoneName))
        {
            await _CreateMilestone(milestoneName);
        }
    }

    public async Task<GitHubIssue[]> OpenIssues(GitHubMilestone milestone)
    {
        var issues = await _Issues
        (
            new FilterIssueRequest
            {
                IncludeOpenOnly = true,
                Milestone = milestone.Number
            }
        );
        return issues
            .Where(iss => !iss.Labels.Contains(closePending))
            .ToArray();
    }

    public Task<GitHubIssue[]> Issues() => _Issues(new FilterIssueRequest());

    protected abstract Task<GitHubIssue[]> _Issues(FilterIssueRequest request);

    public Task<GitHubIssue> Issue(int issueNumber) => _Issue(issueNumber);

    public async Task<GitHubIssue> CreateIssue(XtiGitVersion version, string issueTitle)
    {
        var openIssues = await _Issues(new FilterIssueRequest { IncludeOpenOnly = true });
        var issue = openIssues.FirstOrDefault(iss => iss.Title.Equals(issueTitle));
        if (issue == null)
        {
            var milestones = await Milestones();
            var milestoneName = version.MilestoneName().Value;
            var milestone = milestones
                .Where(m => m.Title.Equals(milestoneName))
                .Select(m => m.Number)
                .FirstOrDefault();
            issue = await _CreateIssue(milestone, issueTitle);
        }
        return issue;
    }

    protected abstract Task<GitHubIssue> _CreateIssue(int milestoneNumber, string issueTitle);

    private const string inProgress = "in progress";

    public async Task<GitHubIssue> StartIssue(XtiGitVersion version, int issueNumber)
    {
        var exists = await _LabelExists(inProgress);
        if (!exists)
        {
            await _CreateLabel(inProgress, "0E8A16");
        }
        var issue = await _Issue(issueNumber);
        var update = issue.ToUpdate();
        if (!issue.Labels.Any(l => l == inProgress))
        {
            update.AddLabel(inProgress);
        }
        if (!issue.Assignees.Any(a => a.Equals(repoOwner, StringComparison.OrdinalIgnoreCase)))
        {
            update.AddAssignee(repoOwner);
        }
        var milestone = await getMilestone(version.MilestoneName().Value);
        if (issue.Milestone.Number != milestone.Number)
        {
            update.MilestoneNumber = milestone.Number;
        }
        await _UpdateIssue(issue, update);
        return issue;
    }

    private const string closePending = "close pending";

    public async Task CompleteIssue(XtiIssueBranchName issueBranchName)
    {
        if (!await _LabelExists(closePending))
        {
            await _CreateLabel(closePending, "BFD4F2");
        }
        var issue = await _Issue(issueBranchName.IssueNumber);
        var update = issue.ToUpdate();
        if (issue.Labels.Contains(inProgress))
        {
            update.RemoveLabel(inProgress);
        }
        if (!issue.Labels.Contains(closePending))
        {
            update.AddLabel(closePending);
        }
        await _UpdateIssue(issue, update);
        var milestoneName = XtiMilestoneName.Parse(issue.Milestone.Title);
        var pullRequest = await _CreatePullRequest
        (
            $"Pull Request for {issue.Title}",
            $"Closes #{issue.Number}",
            issueBranchName.Value,
            new XtiVersionBranchName(milestoneName.Version).Value
        );
        await _MergePullRequest(pullRequest);
        await _LinkPullRequest(pullRequest, issue);
    }

    protected abstract Task _LinkPullRequest(GitHubPullRequest ghPullRequest, GitHubIssue ghIssue);

    public async Task CompleteVersion(XtiVersionBranchName versionBranchName)
    {
        var milestone = await getMilestone(new XtiMilestoneName(versionBranchName.Version).Value);
        GitHubIssue[] milestoneIssues;
        if (milestone.Number == 0)
        {
            milestoneIssues = new GitHubIssue[0];
        }
        else
        {
            milestoneIssues = await _Issues
            (
                new FilterIssueRequest
                {
                    Milestone = milestone.Number,
                    IncludeOpenOnly = true
                }
            );
        }
        var defaultBranchName = await DefaultBranchName();
        var branches = await _Branches();
        if (branches.Any(b => b.Equals(versionBranchName.Value, StringComparison.OrdinalIgnoreCase)))
        {
            var pullRequest = await _CreatePullRequest
            (
                $"Pull Request for {versionBranchName.Version.Key}",
                "",
                versionBranchName.Value,
                defaultBranchName
            );
            await _MergePullRequest(pullRequest);
        }
        foreach (var milestoneIssue in milestoneIssues)
        {
            await close(milestoneIssue);
        }
        if (milestone != null)
        {
            await _CloseMilestone(milestone);
        }
    }

    protected abstract Task _CloseMilestone(GitHubMilestone milestone);

    private Task close(GitHubIssue ghIssue)
    {
        var update = ghIssue.ToUpdate();
        update.Close();
        update.RemoveLabel(closePending);
        return _UpdateIssue(ghIssue, update);
    }

    protected abstract Task _UpdateIssue(GitHubIssue ghIssue, GitHubIssueUpdate ghIssueUpdate);

    protected abstract Task<GitHubMilestone> _Milestone(int number);

    protected abstract Task<bool> _LabelExists(string name);

    protected abstract Task _CreateLabel(string name, string color);

    protected abstract Task<GitHubIssue> _Issue(int number);

    public Task<GitHubPullRequest[]> PullRequests() => _PullRequests();

    protected abstract Task<GitHubPullRequest[]> _PullRequests();

    protected abstract Task<GitHubPullRequest> _CreatePullRequest(string title, string body, string head, string baseRef);

    protected abstract Task _MergePullRequest(GitHubPullRequest pullRequest);

    public Task<string[]> Branches() => _Branches();

    private async Task<bool> branchExists(string name)
    {
        var branches = await _Branches();
        return branches.Any(b => b.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    protected abstract Task<string[]> _Branches();

    protected abstract Task _CreateBranch(string name);

    public Task<GitHubMilestone[]> Milestones() => _Milestones();

    public Task<GitHubMilestone> Milestone(string title) => getMilestone(title);

    private async Task<bool> milestoneExists(string title)
    {
        var milestone = await getMilestone(title);
        return milestone.Number > 0;
    }

    private async Task<GitHubMilestone> getMilestone(string title)
    {
        var milestones = await _Milestones();
        return milestones
            .FirstOrDefault(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            ?? new GitHubMilestone(0, "");
    }

    protected abstract Task<GitHubMilestone[]> _Milestones();

    protected abstract Task _CreateMilestone(string name);

    public async Task DeleteReleaseIfExists(string tagName)
    {
        var release = await _Release(tagName);
        if(release != null)
        {
            foreach (var asset in release.Assets)
            {
                await DeleteReleaseAsset(asset);
            }
            await DeleteRelease(release);
        }
    }

    protected abstract Task DeleteRelease(GitHubRelease gitHubRelease);

    protected abstract Task DeleteReleaseAsset(GitHubReleaseAsset asset);

    public async Task<GitHubRelease> Release(string tagName) => 
        (await _Release(tagName)) ?? throw new Exception($"Release not found for tag '{tagName}'");

    protected abstract Task<GitHubRelease?> _Release(string tagName);

    public Task<GitHubRelease> CreateRelease(string tagName, string name, string body)
        => _CreateRelease(tagName, name, body);

    protected abstract Task<GitHubRelease> _CreateRelease(string tagName, string name, string body);

    public Task UploadReleaseAsset(GitHubRelease release, FileUpload asset) => _UploadReleaseAsset(release, asset);

    protected abstract Task _UploadReleaseAsset(GitHubRelease release, FileUpload asset);

    public Task FinalizeRelease(GitHubRelease release) => _FinalizeRelease(release);

    protected abstract Task _FinalizeRelease(GitHubRelease release);

    public Task<byte[]> DownloadReleaseAsset(GitHubReleaseAsset asset) => _DownloadReleaseAsset(asset);

    protected abstract Task<byte[]> _DownloadReleaseAsset(GitHubReleaseAsset asset);
}