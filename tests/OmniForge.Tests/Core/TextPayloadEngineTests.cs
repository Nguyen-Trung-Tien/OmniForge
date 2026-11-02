using OmniForge.Core.Modules.TextInspector;
using Xunit;

namespace OmniForge.Tests.Core;

public class TextPayloadEngineTests
{
    [Fact]
    public async Task FormatJsonAsync_ValidJson_ShouldFormatIndented()
    {
        string raw = "{\"name\":\"OmniForge\",\"version\":1,\"enabled\":true}";

        var result = await TextPayloadEngine.FormatJsonAsync(raw, indented: true);

        Assert.True(result.IsSuccess);
        Assert.Contains("\n", result.Value);
        Assert.Contains("\"name\": \"OmniForge\"", result.Value);
    }

    [Fact]
    public async Task FormatJsonAsync_Minify_ShouldRemoveWhitespace()
    {
        string raw = """
        {
            "name": "OmniForge",
            "active": true
        }
        """;

        var result = await TextPayloadEngine.FormatJsonAsync(raw, indented: false);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("\n", result.Value);
        Assert.Equal("{\"name\":\"OmniForge\",\"active\":true}", result.Value);
    }

    [Fact]
    public async Task FormatJsonAsync_InvalidJson_ShouldReturnStructuredFailure()
    {
        string broken = "{\"name\": 'OmniForge',}";

        var result = await TextPayloadEngine.FormatJsonAsync(broken);

        Assert.True(result.IsFailure);
        Assert.Contains("Json.SyntaxError", result.Error.Code);
    }

    [Fact]
    public async Task FormatXmlAsync_ValidXml_ShouldFormatIndented()
    {
        string raw = "<root><item id=\"1\">test</item></root>";

        var result = await TextPayloadEngine.FormatXmlAsync(raw);

        Assert.True(result.IsSuccess);
        Assert.Contains("<root>", result.Value);
        Assert.Contains("  <item id=\"1\">test</item>", result.Value);
    }

    [Fact]
    public void Base64_EncodeAndDecode_ShouldBeRoundTripExact()
    {
        string plain = "OmniForge Power Tools Suite 2026!";

        var encoded = TextPayloadEngine.EncodeBase64(plain);
        Assert.True(encoded.IsSuccess);

        var decoded = TextPayloadEngine.DecodeBase64(encoded.Value);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(plain, decoded.Value);
    }

    [Fact]
    public void Url_EncodeAndDecode_ShouldBeRoundTripExact()
    {
        string plain = "https://example.com/search?q=C# 13 & WinUI 3=amazing!";

        var encoded = TextPayloadEngine.EncodeUrl(plain);
        Assert.True(encoded.IsSuccess);
        Assert.Contains("%20", encoded.Value);

        var decoded = TextPayloadEngine.DecodeUrl(encoded.Value);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(plain, decoded.Value);
    }

    [Fact]
    public void InspectJwt_ValidJwt_ShouldParseHeaderAndPayload()
    {
        // Standard sample JWT with header {"alg":"HS256","typ":"JWT"} and payload {"sub":"1234567890","name":"Alice"}
        string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkFsaWNlIn0.signature_dummy";

        var result = TextPayloadEngine.InspectJwt(jwt);

        Assert.True(result.IsSuccess);
        Assert.Contains("HS256", result.Value.HeaderJson);
        Assert.Contains("Alice", result.Value.PayloadJson);
        Assert.Equal("signature_dummy", result.Value.Signature);
    }

    [Fact]
    public void ComputeLineDiff_IdenticalStrings_ShouldProduceAllUnchangedLines()
    {
        string textA = "Line 1\nLine 2\nLine 3";
        string textB = "Line 1\nLine 2\nLine 3";

        var diff = TextPayloadEngine.ComputeLineDiff(textA, textB);

        Assert.Equal(3, diff.Count);
        Assert.All(diff, d => Assert.True(d.IsUnchanged));
    }

    [Fact]
    public void ComputeLineDiff_ModifiedLines_ShouldIdentifyAddAndRemove()
    {
        string textA = "Alpha\nBeta";
        string textB = "Alpha\nGamma";

        var diff = TextPayloadEngine.ComputeLineDiff(textA, textB);

        Assert.Contains(diff, d => d.IsRemoved && d.Content == "Beta");
        Assert.Contains(diff, d => d.IsAdded && d.Content == "Gamma");
    }

    [Fact]
    public async Task FormatXmlAsync_WithDtdDeclaration_ShouldProhibitXxeAndReturnFailure()
    {
        string maliciousXml = "<?xml version=\"1.0\"?><!DOCTYPE foo [<!ENTITY xxe \"secret\">]><root>&xxe;</root>";

        var result = await TextPayloadEngine.FormatXmlAsync(maliciousXml);

        Assert.True(result.IsFailure);
        Assert.Equal("Xml.SyntaxError", result.Error.Code);
    }

    [Fact]
    public void SecurityGuard_ValidateSafePath_WithIntermediateReservedDeviceName_ShouldReject()
    {
        var result = OmniForge.Core.Common.SecurityGuard.ValidateSafePath(@"C:\SafeWorkspace\CON\nested\file.txt");

        Assert.True(result.IsFailure);
        Assert.Equal("Security.ReservedDeviceName", result.Error.Code);
    }
}

