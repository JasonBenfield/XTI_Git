namespace XTI_Git.Abstractions;

public interface IGitHubCredentialsAccessor
{
    Task<GitHubCredentials> Value();
}