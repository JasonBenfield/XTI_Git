namespace XTI_GitHub
{
    public sealed record GitHubRelease(int ID, string TagName, GitHubReleaseAsset[] Assets);
}
