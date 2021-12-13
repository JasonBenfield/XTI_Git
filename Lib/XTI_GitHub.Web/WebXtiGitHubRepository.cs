using Octokit;

namespace XTI_GitHub.Web;

public sealed class WebXtiGitHubRepository : XtiGitHubRepository
{
    private readonly string repoName;
    private readonly GitHubClient client;

    public WebXtiGitHubRepository(string repoOwner, string repoName)
        : base(repoOwner)
    {
        this.repoName = repoName;
        client = new GitHubClient(new ProductHeaderValue("test-xti-github"));
    }

    public void UseCredentials(string userName, string password)
    {
        client.Credentials = new Credentials(userName, password);
    }

    protected override async Task<string> _DefaultBranchName()
    {
        var repo = await client.Repository.Get(repoOwner, repoName);
        return repo.DefaultBranch;
    }

    protected override async Task<string[]> _Branches()
    {
        var branches = await client.Repository.Branch.GetAll(repoOwner, repoName);
        return branches
            .Select(b => b.Name)
            .ToArray();
    }

    protected override async Task _CreateBranch(string name)
    {
        var repo = await client.Repository.Get(repoOwner, repoName);
        var defaultBranch = await client.Git.Reference.Get(repoOwner, repoName, $"heads/{repo.DefaultBranch}");
        await client.Git.Reference.Create(repoOwner, repoName, new NewReference($"refs/heads/{name}", defaultBranch.Object.Sha));
    }

    protected override async Task<GitHubMilestone[]> _Milestones()
    {
        var milestones = await client.Issue.Milestone.GetAllForRepository(repoOwner, repoName);
        return milestones
            .Select(m => new GitHubMilestone(m.Number, m.Title))
            .ToArray();
    }

    protected override async Task<GitHubMilestone> _Milestone(int number)
    {
        var milestone = await client.Issue.Milestone.Get(repoOwner, repoName, number);
        return createGitHubMilestone(milestone);
    }

    protected override Task _CreateMilestone(string name)
    {
        var milestone = new NewMilestone(name);
        return client.Issue.Milestone.Create(repoOwner, repoName, milestone);
    }

    protected override async Task<GitHubIssue[]> _Issues(FilterIssueRequest request)
    {
        var issueRequest = new RepositoryIssueRequest
        {
            State = request.IncludeOpenOnly ? ItemStateFilter.Open : ItemStateFilter.All,
            Milestone = request.Milestone.HasValue
                ? request.Milestone.ToString()
                : null
        };
        var issues = await client.Issue.GetAllForRepository(repoOwner, repoName, issueRequest);
        return issues
            .Select(i => createGitHubIssue(i))
            .ToArray();
    }

    protected override async Task<GitHubIssue> _CreateIssue(int milestoneNumber, string issueTitle)
    {
        var newIssue = new NewIssue(issueTitle)
        {
            Milestone = milestoneNumber
        };
        var issue = await client.Issue.Create(repoOwner, repoName, newIssue);
        return createGitHubIssue(issue);
    }

    protected override async Task<GitHubIssue> _Issue(int number)
    {
        var issue = await client.Issue.Get(repoOwner, repoName, number);
        return createGitHubIssue(issue);
    }

    private static GitHubIssue createGitHubIssue(Issue issue)
    {
        return new GitHubIssue
        (
            issue.Number,
            issue.Title,
            createGitHubMilestone(issue.Milestone),
            issue.State.StringValue,
            issue.Labels.Select(l => l.Name).ToArray(),
            issue.Assignees.Select(a => a.Login).ToArray()
        );
    }

    private static GitHubMilestone createGitHubMilestone(Milestone milestone)
    {
        return milestone == null
            ? new GitHubMilestone(0, "")
            : new GitHubMilestone(milestone.Number, milestone.Title, milestone.State.StringValue);
    }

    protected override async Task<bool> _LabelExists(string name)
    {
        var labels = await client.Issue.Labels.GetAllForRepository(repoOwner, repoName);
        return labels.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    protected override Task _CreateLabel(string name, string color)
        => client.Issue.Labels.Create(repoOwner, repoName, new NewLabel(name, color));

    protected override async Task _UpdateIssue(GitHubIssue ghIssue, GitHubIssueUpdate ghIssueUpdate)
    {
        var issue = await client.Issue.Get(repoOwner, repoName, ghIssue.Number);
        var update = issue.ToUpdate();
        update.Milestone = ghIssueUpdate.MilestoneNumber;
        update.State = ghIssueUpdate.State.Equals("Open", StringComparison.OrdinalIgnoreCase)
            ? ItemState.Open
            : ItemState.Closed;
        update.ClearLabels();
        foreach (var label in ghIssueUpdate.Labels)
        {
            update.AddLabel(label);
        }
        update.ClearAssignees();
        foreach (var assignee in ghIssueUpdate.Assignees)
        {
            update.AddAssignee(assignee);
        }
        await client.Issue.Update(repoOwner, repoName, ghIssue.Number, update);
    }

    protected override async Task<GitHubPullRequest[]> _PullRequests()
    {
        var pullRequests = await client.PullRequest.GetAllForRepository(repoOwner, repoName);
        return pullRequests
            .Select(pr => createGitHubPullRequest(pr))
            .ToArray();
    }

    protected override async Task<GitHubPullRequest> _CreatePullRequest(string title, string body, string head, string baseRef)
    {
        var pullRequest = await client.PullRequest.Create
        (
            repoOwner,
            repoName,
            new NewPullRequest(title, head, baseRef)
            {
                Body = body
            }
        );
        return createGitHubPullRequest(pullRequest);
    }

    protected override Task _MergePullRequest(GitHubPullRequest pullRequest)
    {
        var mergeRequest = new MergePullRequest();
        return client.PullRequest.Merge(repoOwner, repoName, pullRequest.Number, mergeRequest);
    }

    protected override async Task _LinkPullRequest(GitHubPullRequest ghPullRequest, GitHubIssue ghIssue)
    {
        var commits = await client.PullRequest.Commits(repoOwner, repoName, ghPullRequest.Number);
        foreach (var commit in commits)
        {
            await client.Repository.Comment.Create
            (
                repoOwner,
                repoName,
                commit.Sha,
                new NewCommitComment($"Closes #{ghIssue.Number}")
            );
        }
    }

    private GitHubPullRequest createGitHubPullRequest(PullRequest request)
    {
        return new GitHubPullRequest
        (
            request.Number,
            request.Title,
            request.Body,
            request.Head.Ref,
            request.Base.Ref,
            request.State.StringValue
        );
    }

    protected override async Task _CloseMilestone(GitHubMilestone ghMilestone)
    {
        var milestone = await client.Issue.Milestone.Get(repoOwner, repoName, ghMilestone.Number);
        var update = new MilestoneUpdate
        {
            Title = milestone.Title,
            Description = milestone.Description,
            DueOn = milestone.DueOn,
            State = ItemState.Closed
        };
        await client.Issue.Milestone.Update(repoOwner, repoName, milestone.Number, update);
    }

    protected override async Task<GitHubRelease> _Release(string tagName)
    {
        var release = await client.Repository.Release.Get(repoOwner, repoName, tagName);
        return createGitHubRelease(release);
    }

    protected override Task _DeleteRelease(GitHubRelease gitHubRelease) =>
        client.Repository.Release.Delete(repoOwner, repoName, gitHubRelease.ID);

    protected override async Task<GitHubRelease> _CreateRelease(string tagName, string name, string body)
    {
        var newRelease = new NewRelease(tagName);
        newRelease.Name = name;
        newRelease.Body = body;
        newRelease.Draft = true;
        var release = await client.Repository.Release.Create(repoOwner, repoName, newRelease);
        return createGitHubRelease(release);
    }

    private static GitHubRelease createGitHubRelease(Release release)
    {
        return new GitHubRelease
        (
            release.Id,
            release.TagName,
            release.Assets
                .Select(a => new GitHubReleaseAsset(a.Id, a.Name, a.ContentType, a.Url))
                .ToArray()
        );
    }

    protected override Task _DeleteReleaseAsset(GitHubReleaseAsset asset)
        => client.Repository.Release.DeleteAsset(repoOwner, repoName, asset.ID);

    protected override async Task _UploadReleaseAsset(GitHubRelease gitHubRelease, FileUpload asset)
    {
        var release = await getRelease(gitHubRelease);
        var upload = new ReleaseAssetUpload(asset.FileName, asset.ContentType, asset.Stream, null);
        await client.Repository.Release.UploadAsset(release, upload);
    }

    protected override async Task _FinalizeRelease(GitHubRelease gitHubRelease)
    {
        var release = await getRelease(gitHubRelease);
        var update = release.ToUpdate();
        update.Draft = false;
        await client.Repository.Release.Edit(repoOwner, repoName, release.Id, update);
    }

    protected override async Task<byte[]> _DownloadReleaseAsset(GitHubReleaseAsset asset)
    {
        var response = await client.Connection.Get<object>(new Uri(asset.Url), new Dictionary<string, string>(), "application/octet-stream");
        return (byte[])response.Body;
    }

    private Task<Release> getRelease(GitHubRelease gitHubRelease)
        => client.Repository.Release.Get(repoOwner, repoName, gitHubRelease.ID);

}