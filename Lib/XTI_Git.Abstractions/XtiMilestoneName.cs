using System.Text.RegularExpressions;

namespace XTI_Git.Abstractions;

public sealed partial class XtiMilestoneName
{
    public static XtiMilestoneName Parse(string text)
    {
        var match = VersionRegex().Match(text);
        return new XtiMilestoneName
        (
            new XtiGitVersion
            (
                match.Groups["VersionType"].Value,
                match.Groups["VersionKey"].Value
            )
        );
    }

    public XtiMilestoneName(XtiGitVersion version)
    {
        Version = version;
        Value = $"xti_{version.Type}_{version.Key}";
    }

    public XtiGitVersion Version { get; }
    public string Value { get; }

    [GeneratedRegex("xti_(?<VersionType>(major)|(minor)|(patch))_(?<VersionKey>V?\\d+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex VersionRegex();
}