using System.Text.RegularExpressions;

namespace XTI_Git.Abstractions
{
    public sealed class XtiIssueBranchName : XtiBranchName
    {
        private static readonly Regex regex
            = new Regex("issue/(?<IssueNumber>\\d+)/(?<IssueTitle>.*)", RegexOptions.IgnoreCase);

        public static bool CanParse(string text) => regex.IsMatch(text);

        public static new XtiIssueBranchName Parse(string text)
        {
            var match = regex.Match(text);
            return new XtiIssueBranchName
            (
                int.Parse(match.Groups["IssueNumber"].Value),
                match.Groups["IssueTitle"].Value
            );
        }

        public XtiIssueBranchName(int issueNumber, string title)
            : base($"issue/{issueNumber}/{formatTitle(title)}")
        {
            IssueNumber = issueNumber;
            Title = formatTitle(title);
        }

        private static readonly Regex whitespaceRegex = new Regex("\\s+");

        private static string formatTitle(string title)
        {
            title = whitespaceRegex.Replace(title, "-");
            title = title.Replace("/", "-");
            if (title.Length > 50)
            {
                title = title.Substring(0, 50);
            }
            return title.ToLower();
        }

        public int IssueNumber { get; }
        public string Title { get; }
    }
}
