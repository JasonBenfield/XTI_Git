using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTI_Git.Abstractions;

namespace XTI_GitHub.Web;

public sealed class WebGitHubFactory : IGitHubFactory
{
    private readonly IGitHubCredentialsAccessor credentialsAccessor;

    public WebGitHubFactory(IGitHubCredentialsAccessor credentialsAccessor)
    {
        this.credentialsAccessor = credentialsAccessor;
    }

    public async Task<XtiGitHubRepository> CreateNewGitHubRepositoryIfNotExists(string owner, string name)
    {
        var client = new GitHubClient(new ProductHeaderValue("test-xti-github"));
        var credentials = await credentialsAccessor.Value();
        client.Credentials = new Credentials(credentials.UserName, credentials.Password);
        try
        {
            await client.Repository.Get(owner, name);
        }
        catch (NotFoundException)
        {
            await client.Repository.Create
            (
                new NewRepository(name)
                {
                    GitignoreTemplate = "VisualStudio"
                }
            );
        }
        return CreateGitHubRepository(owner, name);
    }

    public XtiGitHubRepository CreateGitHubRepository(string owner, string name) =>
        new WebXtiGitHubRepository(owner, name, credentialsAccessor);

}
