---
name: msix-release-packager
description: Packaging, trimming-safe WinUI 3 publishing (Single-Project MSIX and Self-Contained Portable ZIP), and CHANGELOG release note extraction for Phase 5 distribution.
---

# MSIX & Self-Contained Release Packager Skill

Use this skill when configuring, troubleshooting, or executing **Phase 5 Release & Packaging** builds (`win-x64`, `win-arm64`) for **OmniForge Utility Suite**.

---

## 1. Directory Structure & Assets

- Publish Script: [publish_release.ps1](scripts/publish_release.ps1)
- Manifest Reference: [PackageManifestExample.xml](examples/PackageManifestExample.xml)
- Release Profiles: [release-profile.json](resources/release-profile.json)

---

## 2. Trimming & AOT Safety Rules for WinUI 3 + DI Reflection

Notice that `OmniForge.App.csproj` enables `<PublishTrimmed>True</PublishTrimmed>` and `<PublishReadyToRun>True</PublishReadyToRun>` in `Release` configurations. Because `ModuleAutoDiscoveryExtensions` and `System.Text.Json` inspect types at runtime, follow these rules:

1. **Preserve Module & ViewModel Constructors for Trimmer**:
   When `PublishTrimmed` is enabled, annotate `ModuleAutoDiscoveryExtensions` or module base classes with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]` if trimmer warnings (`IL2026`, `IL2072`) appear.
2. **Self-Contained Portable Build Command**:
   ```powershell
   .\forge publish -Platform x64
   ```
   Or invoke the dedicated script:
   ```powershell
   pwsh -File .agents/skills/msix-release-packager/scripts/publish_release.ps1 -Platform x64
   ```
3. **MSIX Package Generation Command**:
   ```powershell
   dotnet build src/OmniForge.App/OmniForge.App.csproj -c Release -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true
   ```

---

## 3. Automated Release Verification Checklist

Before cutting a release tag (`v0.2.0-beta`, `v1.0.0`):
1. Run `.\forge verify` to ensure all `NetArchTest` and `xUnit` suites pass with zero compiler warnings.
2. Move items in `CHANGELOG.md` from `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD`.
3. Synchronize version numbers across:
   - `src/OmniForge.App/Package.appxmanifest` (`<Identity Version="X.Y.Z.0" />`)
   - `src/OmniForge.App/ViewModels/SettingsViewModel.cs` (`AppVersion`)
   - `CHANGELOG.md`
