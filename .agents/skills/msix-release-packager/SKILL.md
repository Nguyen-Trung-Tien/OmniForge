---
name: msix-release-packager
description: Packaging, trimming-safe WinUI 3 publishing (Single-Project MSIX and Self-Contained Portable ZIP), and CHANGELOG release note extraction for Phase 5 distribution.
---

# MSIX & Self-Contained Release Packager Skill

Use this skill when configuring, troubleshooting, or executing **Phase 5 Release, Packaging & Deployment** builds (`win-x64`, `win-arm64`) for **OmniForge Utility Suite**.

---

## 1. Directory Structure & Deployment Architecture

All distribution deliverables are produced into the top-level `release/` directory:

```
release/
├── installer/             # Production Windows Setup Executables (Inno Setup)
│   └── OmniForge-Setup-v{Version}.exe
├── portable/              # Self-contained portable ZIP distributions
│   └── OmniForge-v{Version}-{platform}.zip
└── checksums.txt          # Cryptographic SHA-256 integrity verification hashes
```

- Inno Setup Script: [installer/OmniForge.iss](../../../installer/OmniForge.iss)
- Root Batch Packager: [package.bat](../../../package.bat)
- Publish Script: [publish_release.ps1](scripts/publish_release.ps1)
- Manifest Reference: [PackageManifestExample.xml](examples/PackageManifestExample.xml)
- Release Profiles: [release-profile.json](resources/release-profile.json)

---

## 2. Packaging, Trimming & WinUI 3 XAML Asset Rules

1. **XAML Binary & PRI File Preservation (`CopyWinUIResourcesToPublishDir`)**:
   In unpackaged self-contained publish (`WindowsPackageType=None`), MSBuild must automatically copy all compiled `.xbf` files (in `Controls`, `Modules`, `Styles`, `Views`) and the Package Resource Index (`OmniForge.App.pri` and `resources.pri`) to `$(PublishDir)`. Without these, `MainWindow.xaml` fails to load with `XamlParseException: Cannot locate resource from 'ms-appx:///Views/MainWindow.xaml'` (`0xc000027b`).
2. **Trimming Safety**:
   `OmniForge.App.csproj` enforces `<PublishTrimmed>False</PublishTrimmed>` and `<PublishReadyToRun>True</PublishReadyToRun>` in `Release` configurations to ensure WinRT metadata, DI reflection, and XAML types remain intact.
3. **Packaging Commands**:
   - **Unified Single-Command Packager**:
     ```powershell
     .\forge package -Platform x64
     ```
     Builds, publishes to `artifacts/publish/win-x64`, compiles the Inno Setup installer, deploys it to `release/installer/`, and generates `release/checksums.txt`.
   - **Visual Batch Packager**:
     ```cmd
     .\package.bat
     ```
   - **Self-Contained Portable Publish**:
     ```powershell
     .\forge publish -Platform x64
     ```
   - **MSIX Package Generation Command**:
     ```powershell
     dotnet build src/OmniForge.App/OmniForge.App.csproj -c Release -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true
     ```

---

## 3. Automated Release Verification Checklist

Before cutting a release tag (`v0.2.0-beta`, `v1.0.0`):
1. Run `.\forge verify` to ensure all `NetArchTest` and `xUnit` suites pass with zero compiler warnings.
2. Run `.\forge package` to produce the installer in `release/installer/` with updated `release/checksums.txt`.
3. Move items in `CHANGELOG.md` from `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD`.
4. Synchronize version numbers across:
   - `src/OmniForge.App/Package.appxmanifest` (`<Identity Version="X.Y.Z.0" />`)
   - `src/OmniForge.App/ViewModels/SettingsViewModel.cs` (`AppVersion`)
   - `installer/OmniForge.iss` (`#define MyAppVersion "X.Y.Z"`)
   - `CHANGELOG.md`
