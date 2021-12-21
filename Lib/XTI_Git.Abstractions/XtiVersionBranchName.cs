using System.Text.RegularExpressions;

namespace XTI_Git.Abstractions;

public sealed class XtiVersionBranchName : XtiBranchName
{
    private static readonly Regex regex
        = new Regex("xti/(?<VersionType>(major)|(minor)|(patch))/(?<VersionKey>V?\\d+)", RegexOptions.IgnoreCase);

    public static bool CanParse(string text) => regex.IsMatch(text);

    public static new XtiVersionBranchName Parse(string text)
    {
        var match = regex.Match(text);
        var version = new XtiGitVersion
        (
            match.Groups["VersionType"].Value,
            match.Groups["VersionKey"].Value
        );
        return new XtiVersionBranchName(version);
    }

    public XtiVersionBranchName(XtiGitVersion version)
        : base($"xti/{version.Type}/{version.Key}")
    {
        Version = version;
    }

    public XtiGitVersion Version { get; }
}