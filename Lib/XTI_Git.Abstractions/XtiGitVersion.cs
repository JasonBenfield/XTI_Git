namespace XTI_Git.Abstractions
{
    public sealed class XtiGitVersion
    {
        public XtiGitVersion(string type, string key)
        {
            Type = type;
            Key = key;
        }

        public string Type { get; }
        public string Key { get; }

        public XtiVersionBranchName BranchName() => new XtiVersionBranchName(this);

        public XtiMilestoneName MilestoneName() => new XtiMilestoneName(this);
    }
}
