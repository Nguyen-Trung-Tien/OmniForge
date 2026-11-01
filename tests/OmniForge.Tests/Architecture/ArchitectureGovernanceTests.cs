using NetArchTest.Rules;
using OmniForge.Core.Common;
using OmniForge.Services;

namespace OmniForge.Tests.Architecture;

/// <summary>
/// Automated Architecture Governance Tests enforcing AGENTS.md rules at test time.
/// </summary>
public sealed class ArchitectureGovernanceTests
{
    [Fact]
    public void CoreLayer_ShouldNotDependOn_ServicesOrAppOrWinUI()
    {
        var coreAssembly = typeof(Result).Assembly;

        var result = Types.InAssembly(coreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "OmniForge.Services",
                "OmniForge.App",
                "Microsoft.UI.Xaml",
                "Microsoft.WindowsAppSDK")
            .GetResult();

        Assert.True(result.IsSuccessful, $"OmniForge.Core violated Clean Architecture dependency rules: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void ServicesLayer_ShouldNotDependOn_PresentationAppLayer()
    {
        var servicesAssembly = typeof(FileSystemService).Assembly;

        var result = Types.InAssembly(servicesAssembly)
            .ShouldNot()
            .HaveDependencyOn("OmniForge.App")
            .GetResult();

        Assert.True(result.IsSuccessful, $"OmniForge.Services must never reference OmniForge.App: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void CoreInterfaces_ShouldResideInContractsNamespace_AndStartWithI()
    {
        var coreAssembly = typeof(Result).Assembly;

        var result = Types.InAssembly(coreAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .ResideInNamespace("OmniForge.Core.Contracts")
            .And()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, $"All Core interfaces must reside in OmniForge.Core.Contracts and start with 'I': {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}

