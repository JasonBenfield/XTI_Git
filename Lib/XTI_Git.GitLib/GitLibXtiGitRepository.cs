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

    private Repository fetchRepo() => cachedRepo ??= new Repository(path);

    public string CurrentBranchName() => fetchRepo().Head.FriendlyName;

    public async Task CheckoutBranch(string branchName)
    {
        if (CurrentBranchName() != branchName)
        {
            var repo = fetchRepo();
            await pull(repo);
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
            await pull(repo);
        }
    }

    public void DeleteBranch(string branchName)
    {
        var repo = fetchRepo();
        var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
        if (branch != null)
        {
            repo.Branches.Remove(branch);
        }
    }

    private async Task pull(Repository repo)
    {
        var credentialsHandler = await gitLibCredentials.CredentialsHandler();
        var remoteRefs = repo.Network.ListReferences
        (
            repo.Network.Remotes["origin"],
            credentialsHandler
        );
        if (remoteRefs.Any(r => r.CanonicalName.Equals($"refs/heads/{repo.Head.FriendlyName}", StringComparison.OrdinalIgnoreCase)))
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
        var repo = fetchRepo();
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