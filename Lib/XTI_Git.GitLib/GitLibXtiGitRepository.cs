using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace XTI_Git.GitLib;

public sealed class GitLibXtiGitRepository : IXtiGitRepository
{
    private readonly Repository repo;
    private string userName = "";
    private string password = "";

    public GitLibXtiGitRepository(string path)
    {
        repo = new Repository(path);
    }

    public void UseCredentials(string userName, string password)
    {
        this.userName = userName;
        this.password = password;
    }

    public string CurrentBranchName() => repo.Head.FriendlyName;

    public void CheckoutBranch(string branchName)
    {
        if (CurrentBranchName() != branchName)
        {
            pull();
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
            pull();
        }
    }

    public void DeleteBranch(string branchName)
    {
        var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
        if (branch != null)
        {
            repo.Branches.Remove(branch);
        }
    }

    private void pull()
    {
        var remoteRefs = repo.Network.ListReferences
        (
            repo.Network.Remotes["origin"], getCredentials()
        );
        if (remoteRefs.Any(r => r.CanonicalName.Equals($"refs/heads/{repo.Head.FriendlyName}", StringComparison.OrdinalIgnoreCase)))
        {
            var signature = getSignature();
            var options = new PullOptions
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = getCredentials()
                }
            };
            Commands.Pull(repo, signature, options);
        }
    }

    public void CommitChanges(string message)
    {
        Commands.Stage(repo, "*");
        var diff = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.Index);
        if (diff.Any())
        {
            var signature = getSignature();
            repo.Commit(message, signature, signature);
            var options = new PushOptions();
            options.CredentialsProvider = getCredentials();
            repo.Network.Push(repo.Head, options);
        }
    }

    private CredentialsHandler getCredentials()
    {
        return new CredentialsHandler
        (
            (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials()
                {
                    Username = userName,
                    Password = password
                }
        );
    }

    private Signature getSignature()
    {
        return new Signature
        (
            new Identity(userName, userName),
            DateTimeOffset.Now
        );
    }
}