namespace XTI_GitHub
{
    public sealed class FilterIssueRequest
    {
        public bool IncludeOpenOnly { get; set; }
        public int? Milestone { get; set; }
    }
}
