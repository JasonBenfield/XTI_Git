﻿using LibGit2Sharp;

namespace XTI_Git.GitLib;

public sealed class GitLibFactory : IXtiGitFactory
{
    private readonly GitLibCredentials credentials;

    public GitLibFactory(GitLibCredentials credentials)
    {
        this.credentials = credentials;
    }

    public async Task<IXtiGitRepository> CloneRepository(string repoUrl, string path)
    {
        var credentialsHandler = await credentials.CredentialsHandler();
        var cloneOptions = new CloneOptions();
        cloneOptions.FetchOptions.CredentialsProvider = credentialsHandler;
        Repository.Clone
        (
            repoUrl,
            path,
            cloneOptions
        );
        using (var git = new Repository(path))
        {
            var signature = await credentials.Signature();
            git.Commit("Initial Commit", signature, signature);
            var branch = git.CreateBranch("main");
            git.Branches.Update
            (
                branch,
                (bu) => bu.Remote = git.Network.Remotes["origin"].Name,
                (bu) => bu.UpstreamBranch = branch.CanonicalName
            );
            Commands.Checkout(git, branch);
        }
        return CreateRepository(path);
    }

    public IXtiGitRepository CreateRepository(string path) =>
        new GitLibXtiGitRepository(path, credentials);
}