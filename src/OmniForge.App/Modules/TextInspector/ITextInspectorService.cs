using OmniForge.Core.Common;
using OmniForge.Core.Modules.TextInspector;

namespace OmniForge.App.Modules.TextInspector;

public interface ITextInspectorService
{
    Task<Result<string>> FormatJsonAsync(string input, bool indented = true, CancellationToken cancellationToken = default);
    Task<Result<string>> FormatXmlAsync(string input, CancellationToken cancellationToken = default);
    Result<string> EncodeBase64(string plainText);
    Result<string> DecodeBase64(string base64Text);
    Result<string> EncodeUrl(string plainText);
    Result<string> DecodeUrl(string urlEncodedText);
    Result<JwtInspectionResult> InspectJwt(string jwtToken);
    IReadOnlyList<DiffLine> ComputeLineDiff(string leftText, string rightText);
}

public sealed class TextInspectorService : ITextInspectorService
{
    public Task<Result<string>> FormatJsonAsync(string input, bool indented = true, CancellationToken cancellationToken = default) =>
        TextPayloadEngine.FormatJsonAsync(input, indented, cancellationToken);

    public Task<Result<string>> FormatXmlAsync(string input, CancellationToken cancellationToken = default) =>
        TextPayloadEngine.FormatXmlAsync(input, cancellationToken);

    public Result<string> EncodeBase64(string plainText) =>
        TextPayloadEngine.EncodeBase64(plainText);

    public Result<string> DecodeBase64(string base64Text) =>
        TextPayloadEngine.DecodeBase64(base64Text);

    public Result<string> EncodeUrl(string plainText) =>
        TextPayloadEngine.EncodeUrl(plainText);

    public Result<string> DecodeUrl(string urlEncodedText) =>
        TextPayloadEngine.DecodeUrl(urlEncodedText);

    public Result<JwtInspectionResult> InspectJwt(string jwtToken) =>
        TextPayloadEngine.InspectJwt(jwtToken);

    public IReadOnlyList<DiffLine> ComputeLineDiff(string leftText, string rightText) =>
        TextPayloadEngine.ComputeLineDiff(leftText, rightText);
}

