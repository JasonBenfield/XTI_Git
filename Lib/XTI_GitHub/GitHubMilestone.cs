using System;

namespace XTI_GitHub
{
    public sealed record GitHubMilestone(int Number, string Title, string State = "Open")
    {
        public bool IsOpen() => State.Equals("Open", StringComparison.OrdinalIgnoreCase);
    }
}
