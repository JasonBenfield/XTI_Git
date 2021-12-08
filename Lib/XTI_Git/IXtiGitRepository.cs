namespace XTI_Git;

public interface IXtiGitRepository
{
    void UseCredentials(string userName, string password);
    string CurrentBranchName();
    void CheckoutBranch(string branchName);
    void DeleteBranch(string branchName);
    void CommitChanges(string message);
}