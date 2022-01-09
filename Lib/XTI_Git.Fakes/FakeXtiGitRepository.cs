namespace XTI_Git.Fakes;

public sealed class FakeXtiGitRepository : IXtiGitRepository
{
    private string branchName = "";

    public Task CheckoutBranch(string branchName)
    {
        this.branchName = branchName;
        return Task.CompletedTask;
    }

    public Task CommitChanges(string message) => Task.CompletedTask;

    public string CurrentBranchName() => branchName;

    public void DeleteBranch(string branchName)
    {
    }
}