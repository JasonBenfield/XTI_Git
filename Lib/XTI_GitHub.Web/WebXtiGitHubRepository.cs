using Octokit;
using XTI_Git.Abstractions;

namespace XTI_GitHub.Web;

internal sealed class WebXtiGitHubRepository : XtiGitHubRepository
{
    private readonly string repoName;
    private readonly IGitHubCredentialsAccessor credentialsAccessor;
    private GitHubClient? cachedClient;

    internal WebXtiGitHubRepository(string repoOwner, string repoName, IGitHubCredentialsAccessor credentialsAccessor)
        : base(repoOwner)
    {
        this.repoName = repoName;
        this.credentialsAccessor = credentialsAccessor;
    }

    private async Task<GitHubClient> fetchClient()
    {
        if (cachedClient == null)
        {
            cachedClient = new GitHubClient(new ProductHeaderValue("test-xti-github"));
            var credentials = await credentialsAccessor.Value();
            cachedClient.Credentials = new Credentials(credentials.UserName, credentials.Password);
        }
        return cachedClient;
    }

    protected override async Task<GitHubRepo> _RepositoryInformation()
    {
        var client = await fetchClient();
        var repo = await client.Repository.Get(repoOwner, repoName);
        return new GitHubRepo(repo.Name, repo.CloneUrl, repo.DefaultBranch);
    }

    protected override async Task<string[]> _Branches()
    {
        var client = await fetchClient();
        var branches = await client.Repository.Branch.GetAll(repoOwner, repoName);
        return branches
            .Select(b => b.Name)
            .ToArray();
    }

    protected override async Task _CreateBranch(string name)
    {
        var client = await fetchClient();
        var repo = await client.Repository.Get(repoOwner, repoName);
        var defaultBranch = await client.Git.Reference.Get(repoOwner, repoName, $"heads/{repo.DefaultBranch}");
        await client.Git.Reference.Create(repoOwner, repoName, new NewReference($"refs/heads/{name}", defaultBranch.Object.Sha));
    }

    protected override async Task<GitHubMilestone[]> _Milestones()
    {
        var client = await fetchClient();
        var milestones = await client.Issue.Milestone.GetAllForRepository(repoOwner, repoName);
        return milestones
            .Select(m => new GitHubMilestone(m.Number, m.Title))
            .ToArray();
    }

    protected override async Task<GitHubMilestone> _Milestone(int number)
    {
        var client = await fetchClient();
        var milestone = await client.Issue.Milestone.Get(repoOwner, repoName, number);
        return createGitHubMilestone(milestone);
    }

    protected override async Task _CreateMilestone(string name)
    {
        var client = await fetchClient();
        var newMilestone = new NewMilestone(name);
        await client.Issue.Milestone.Create(repoOwner, repoName, newMilestone);
    }

    protected override async Task<GitHubIssue[]> _Issues(FilterIssueRequest request)
    {
        var client = await fetchClient();
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
        var client = await fetchClient();
        var newIssue = new NewIssue(issueTitle)
        {
            Milestone = milestoneNumber
        };
        var issue = await client.Issue.Create(repoOwner, repoName, newIssue);
        return createGitHubIssue(issue);
    }

    protected override async Task<GitHubIssue> _Issue(int number)
    {
        var client = await fetchClient();
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
        var client = await fetchClient();
        var labels = await client.Issue.Labels.GetAllForRepository(repoOwner, repoName);
        return labels.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    protected override async Task _CreateLabel(string name, string color)
    {
        var client = await fetchClient();
        await client.Issue.Labels.Create(repoOwner, repoName, new NewLabel(name, color));
    }

    protected override async Task _UpdateIssue(GitHubIssue ghIssue, GitHubIssueUpdate ghIssueUpdate)
    {
        var client = await fetchClient();
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
        var client = await fetchClient();
        var pullRequests = await client.PullRequest.GetAllForRepository(repoOwner, repoName);
        return pullRequests
            .Select(pr => createGitHubPullRequest(pr))
            .ToArray();
    }

    protected override async Task<GitHubPullRequest> _CreatePullRequest(string title, string body, string head, string baseRef)
    {
        var client = await fetchClient();
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

    protected override async Task _MergePullRequest(GitHubPullRequest pullRequest)
    {
        var client = await fetchClient();
        var mergeRequest = new MergePullRequest();
        await client.PullRequest.Merge(repoOwner, repoName, pullRequest.Number, mergeRequest);
    }

    protected override async Task _LinkPullRequest(GitHubPullRequest ghPullRequest, GitHubIssue ghIssue)
    {
        var client = await fetchClient();
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
        var client = await fetchClient();
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

    protected override async Task<GitHubRelease?> _Release(string tagName)
    {
        var client = await fetchClient();
        GitHubRelease? release;
        try
        {
            var r = await client.Repository.Release.Get(repoOwner, repoName, tagName);
            release = createGitHubRelease(r);
        }
        catch (NotFoundException)
        {
            release = null;
        }
        return release;
    }

    protected override async Task DeleteRelease(GitHubRelease gitHubRelease)
    {
        var client = await fetchClient();
        await client.Repository.Release.Delete(repoOwner, repoName, gitHubRelease.ID);
    }

    protected override async Task<GitHubRelease> _CreateRelease(string tagName, string name, string body)
    {
        var newRelease = new NewRelease(tagName);
        newRelease.Name = name;
        newRelease.Body = body;
        newRelease.Draft = true;
        var client = await fetchClient();
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

    protected override async Task DeleteReleaseAsset(GitHubReleaseAsset asset)
    {
        var client = await fetchClient();
        await client.Repository.Release.DeleteAsset(repoOwner, repoName, asset.ID);
    }

    protected override async Task _UploadReleaseAsset(GitHubRelease gitHubRelease, FileUpload asset)
    {
        var client = await fetchClient();
        var release = await getRelease(gitHubRelease);
        var upload = new ReleaseAssetUpload(asset.FileName, asset.ContentType, asset.Stream, null);
        await client.Repository.Release.UploadAsset(release, upload);
    }

    protected override async Task _FinalizeRelease(GitHubRelease gitHubRelease)
    {
        var client = await fetchClient();
        var release = await getRelease(gitHubRelease);
        var update = release.ToUpdate();
        update.Draft = false;
        await client.Repository.Release.Edit(repoOwner, repoName, release.Id, update);
    }

    protected override async Task<byte[]> _DownloadReleaseAsset(GitHubReleaseAsset asset)
    {
        var client = await fetchClient();
        var response = await client.Connection.Get<object>(new Uri(asset.Url), new Dictionary<string, string>(), "application/octet-stream");
        return (byte[])response.Body;
    }

    private async Task<Release> getRelease(GitHubRelease gitHubRelease)
    {
        var client = await fetchClient();
        var release = await client.Repository.Release.Get(repoOwner, repoName, gitHubRelease.ID);
        return release;
    }

}