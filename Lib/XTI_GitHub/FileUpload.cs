namespace XTI_GitHub;

public sealed record FileUpload(Stream Stream, string FileName, string ContentType);