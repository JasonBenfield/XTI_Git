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

    public XtiGitHubRepository CreateGitHubRepository(string owner, string name) =>
        new WebXtiGitHubRepository(owner, name, credentialsAccessor);
}
