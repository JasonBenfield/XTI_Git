﻿namespace XTI_Git.Abstractions;

public class XtiBranchName
{
    public static XtiBranchName Parse(string text)
    {
        if (XtiVersionBranchName.CanParse(text))
        {
            return XtiVersionBranchName.Parse(text);
        }
        if (XtiIssueBranchName.CanParse(text))
        {
            return XtiIssueBranchName.Parse(text);
        }
        throw new Exception($"Branch name '{text}' could not be parsed");
    }

    protected XtiBranchName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => $"{GetType().Name} {Value}";
}