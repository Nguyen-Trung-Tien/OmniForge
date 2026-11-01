namespace OmniForge.Core.Modules.TextInspector;

public enum TextInspectorMode
{
    Json,
    Xml,
    Base64,
    Jwt,
    Url,
    Diff
}

public sealed record DiffLine(int LineNumber, string Prefix, string Content)
{
    public bool IsAdded => Prefix == "+";
    public bool IsRemoved => Prefix == "-";
    public bool IsUnchanged => Prefix == " ";
}

public sealed record JwtInspectionResult(
    string HeaderJson,
    string PayloadJson,
    string Signature);

