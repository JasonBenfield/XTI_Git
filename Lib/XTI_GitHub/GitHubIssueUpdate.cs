using System.Collections.Generic;

namespace XTI_GitHub
{
    public sealed class GitHubIssueUpdate
    {
        internal GitHubIssueUpdate(GitHubIssue source)
        {
            MilestoneNumber = source.Milestone.Number;
            State = source.State;
            Labels = source.Labels;
            Assignees = source.Assignees;
        }

        public int? MilestoneNumber { get; set; }
        public string State { get; private set; }

        public void Close() => State = "Closed";

        private readonly List<string> labels = new List<string>();
        public string[] Labels
        {
            get => labels.ToArray();
            private set
            {
                labels.Clear();
                labels.AddRange(value ?? new string[] { });
            }
        }

        public void AddLabel(string label)
        {
            labels.Add(label);
        }

        public void RemoveLabel(string label)
        {
            labels.Remove(label);
        }

        private readonly List<string> assignees = new List<string>();
        public string[] Assignees
        {
            get => assignees.ToArray();
            private set
            {
                assignees.Clear();
                assignees.AddRange(value ?? new string[] { });
            }
        }

        public void AddAssignee(string assignee)
        {
            assignees.Add(assignee);
        }

        public void RemoveAssignee(string assignee)
        {
            assignees.Remove(assignee);
        }
    }
}
