namespace XTI_Git;

public interface IXtiGitRepository
{
    string CurrentBranchName();
    Task CheckoutBranch(string branchName);
    void DeleteBranch(string branchName);
    Task CommitChanges(string message);
}