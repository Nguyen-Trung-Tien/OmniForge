using OmniForge.Core.Models;
using OmniForge.Modules.BatchOps;

namespace OmniForge.Tests.Core;

public sealed class RenameRulesTests
{
    private static FileMetadata CreateMetadata(string name = "test.txt", string dir = @"C:\Sample\Folder", long size = 1024)
    {
        return new FileMetadata(
            FullPath: Path.Combine(dir, name),
            DirectoryName: dir,
            FileName: name,
            Extension: Path.GetExtension(name),
            SizeBytes: size,
            CreatedAt: new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero),
            ModifiedAt: new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void PrefixSuffixRule_ShouldAddPrefixAndSuffix()
    {
        var rule = new PrefixSuffixRule("PRE_", "_POST");
        var meta = CreateMetadata("document.docx");

        var result = rule.Apply("document", 0, meta);

        Assert.Equal("PRE_document_POST", result);
    }

    [Fact]
    public void PrefixSuffixRule_WhenDisabled_ShouldReturnOriginalName()
    {
        var rule = new PrefixSuffixRule("PRE_", "_POST") { IsEnabled = false };
        var meta = CreateMetadata("document.docx");

        var result = rule.Apply("document", 0, meta);

        Assert.Equal("document", result);
    }

    [Fact]
    public void ReplaceRule_PlainSearchAndReplace_ShouldReplaceMatches()
    {
        var rule = new ReplaceRule("IMG", "PHOTO", matchCase: false, useRegex: false);
        var meta = CreateMetadata("img_001.jpg");

        var result = rule.Apply("img_001", 0, meta);

        Assert.Equal("PHOTO_001", result);
    }

    [Fact]
    public void ReplaceRule_MatchCase_ShouldRespectCasing()
    {
        var rule = new ReplaceRule("IMG", "PHOTO", matchCase: true, useRegex: false);
        var meta = CreateMetadata("img_001.jpg");

        var result = rule.Apply("img_001", 0, meta);

        // Case does not match "img" vs "IMG", so it remains unchanged
        Assert.Equal("img_001", result);
    }

    [Fact]
    public void ReplaceRule_Regex_ShouldReplacePattern()
    {
        var rule = new ReplaceRule(@"\d+", "NUM", matchCase: false, useRegex: true);
        var meta = CreateMetadata("data_12345.csv");

        var result = rule.Apply("data_12345", 0, meta);

        Assert.Equal("data_NUM", result);
    }

    [Fact]
    public void ReplaceRule_RegexCatastrophicBacktracking_ShouldTimeoutGracefully()
    {
        // Vulnerable ReDoS pattern: (a+)+$ evaluated against 'aaaaaaaaaaaaaaaaaaaaaaaaaaaa!'
        var rule = new ReplaceRule(@"^(a+)+$", "EXPLOIT", matchCase: false, useRegex: true);
        var meta = CreateMetadata();
        string maliciousInput = new string('a', 30) + "!";

        var result = rule.Apply(maliciousInput, 0, meta);

        // Should handle timeout gracefully and return unchanged
        Assert.Equal(maliciousInput, result);
    }

    [Fact]
    public void ReplaceRule_TokenInterpolation_ShouldSubstituteTokens()
    {
        var rule = new ReplaceRule("item", "{parent}_{date}_{index}_{ext}", matchCase: false, useRegex: false);
        var meta = CreateMetadata("item.png", dir: @"C:\Photos\Vacation");

        var result = rule.Apply("item", 2, meta);

        Assert.Equal("Vacation_2026-11-01_3_png", result);
    }

    [Theory]
    [InlineData(CaseMode.Lowercase, "My Document File", "my document file")]
    [InlineData(CaseMode.Uppercase, "my document file", "MY DOCUMENT FILE")]
    [InlineData(CaseMode.TitleCase, "my document file", "My Document File")]
    public void ChangeCaseRule_ShouldTransformCorrectly(CaseMode mode, string input, string expected)
    {
        var rule = new ChangeCaseRule(mode);
        var meta = CreateMetadata();

        var result = rule.Apply(input, 0, meta);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NumberingRule_DefaultSuffix_ShouldFormatWithPadding()
    {
        var rule = new NumberingRule(startNumber: 1, step: 1, paddingFormat: "001", NumberingPosition.Suffix, separator: "_");
        var meta = CreateMetadata();

        Assert.Equal("file_001", rule.Apply("file", 0, meta));
        Assert.Equal("file_002", rule.Apply("file", 1, meta));
        Assert.Equal("file_003", rule.Apply("file", 2, meta));
    }

    [Fact]
    public void NumberingRule_PrefixWithCustomStep_ShouldFormatCorrectly()
    {
        var rule = new NumberingRule(startNumber: 10, step: 5, paddingFormat: "0000", NumberingPosition.Prefix, separator: "-");
        var meta = CreateMetadata();

        Assert.Equal("0010-file", rule.Apply("file", 0, meta));
        Assert.Equal("0015-file", rule.Apply("file", 1, meta));
    }

    [Fact]
    public void NumberingRule_PlaceholderToken_ShouldReplaceInPlace()
    {
        var rule = new NumberingRule(startNumber: 1, step: 1, paddingFormat: "001");
        var meta = CreateMetadata();

        var result = rule.Apply("Report_{num}_Draft", 4, meta);

        Assert.Equal("Report_005_Draft", result);
    }
}

