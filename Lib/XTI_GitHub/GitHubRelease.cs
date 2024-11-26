namespace XTI_GitHub;

public sealed record GitHubRelease(long ID, string TagName, GitHubReleaseAsset[] Assets);