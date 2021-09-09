namespace XTI_Git.Fakes
{
    public sealed class FakeXtiGitRepository : XtiGitRepository
    {
        private string branchName;

        public void CheckoutBranch(string branchName) => this.branchName = branchName;

        public void CommitChanges(string message)
        {
        }

        public string CurrentBranchName() => branchName;

        public void DeleteBranch(string branchName)
        {
        }

        public void UseCredentials(string userName, string password)
        {
        }
    }
}
