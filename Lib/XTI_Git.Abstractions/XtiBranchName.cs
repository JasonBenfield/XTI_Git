namespace XTI_Git.Abstractions;

public class XtiBranchName
{
    protected XtiBranchName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => $"{GetType().Name} {Value}";
}