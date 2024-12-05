using LibGit2Sharp;

namespace XTI_Git.GitLib;

public sealed class GitLibXtiGitRepository : IXtiGitRepository
{
    private readonly string path;
    private readonly GitLibCredentials gitLibCredentials;
    private Repository? cachedRepo;

    public GitLibXtiGitRepository(string path, GitLibCredentials gitLibCredentials)
    {
        this.path = path;
        this.gitLibCredentials = gitLibCredentials;
    }

    private Repository FetchRepo() => cachedRepo ??= new Repository(path);

    public string CurrentBranchName() => FetchRepo().Head.FriendlyName;

    public async Task CheckoutBranch(string branchName)
    {
        if (CurrentBranchName() != branchName)
        {
            var repo = FetchRepo();
            try
            {
                await Pull(repo);
            }
            catch (MergeFetchHeadNotFoundException) { }
            var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
            if (branch == null)
            {
                branch = repo.CreateBranch(branchName);
                repo.Branches.Update
                (
                    branch,
                    (bu) => bu.Remote = repo.Network.Remotes["origin"].Name,
                    (bu) => bu.UpstreamBranch = branch.CanonicalName
                );
            }
            Commands.Checkout(repo, branch);
            await Pull(repo);
        }
    }

    public void DeleteBranch(string branchName)
    {
        var repo = FetchRepo();
        var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
        if (branch != null)
        {
            repo.Branches.Remove(branch);
        }
    }

    private async Task Pull(Repository repo)
    {
        var credentialsHandler = await gitLibCredentials.CredentialsHandler();
        var remoteRefs = repo.Network.ListReferences
        (
            repo.Network.Remotes["origin"],
            credentialsHandler
        );
        var currentBranchRefs = remoteRefs
            .Where(r => r.CanonicalName.Equals($"refs/heads/{repo.Head.FriendlyName}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (currentBranchRefs.Any())
        {
            var signature = await gitLibCredentials.Signature();
            var options = new PullOptions
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = credentialsHandler
                }
            };
            Commands.Pull(repo, signature, options);
        }
    }

    public async Task CommitChanges(string message)
    {
        var repo = FetchRepo();
        Commands.Stage(repo, "*");
        var diff = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.Index);
        if (diff.Any())
        {
            var signature = await gitLibCredentials.Signature();
            repo.Commit(message, signature, signature);
            var options = new PushOptions();
            options.CredentialsProvider = await gitLibCredentials.CredentialsHandler();
            repo.Network.Push(repo.Head, options);
        }
    }
}