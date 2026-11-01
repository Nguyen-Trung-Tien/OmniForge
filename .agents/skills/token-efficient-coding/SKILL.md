---
name: token-efficient-coding
description: Enforces ultra-compact, high-precision C# 13 and XAML coding patterns to minimize token consumption while preserving 100% Clean Architecture compliance.
---

# Token-Efficient & High-Precision Coding Skill

Use this skill whenever writing, refactoring, or scaffolding C# and XAML code in **OmniForge**. Its goal is to reduce context and output token usage by **40–65%** while maintaining zero-warning compilation.

---

## 1. Directory Structure & Assets

- Density Analysis Script: [analyze_token_density.ps1](scripts/analyze_token_density.ps1)
- Reference Implementation: [CompactModuleExample.cs](examples/CompactModuleExample.cs)
- Token Saving Guidelines: [token-saving-guidelines.json](resources/token-saving-guidelines.json)

---

## 2. Context Discovery Rules (Save Input Tokens)

1. **Never Read Generated or Binary Directories**: Never inspect `bin/`, `obj/`, `.vs/`, or `Assets/*.png`.
2. **Use Scaffolding Scripts Over Manual Multi-File Writes**:
   - To create a new utility module, run `.agents/skills/winui3-module-generator/scripts/scaffold_module.ps1` instead of manually writing 4 boilerplate files.
   - To create a unit test, run `.agents/skills/unit-test-scaffolder/scripts/scaffold_test.ps1`.
3. **Zero-Touch Module Registration**:
   - Do **not** read or edit `ServiceCollectionExtensions.cs` or `App.xaml.cs` when adding a new `IToolModule`. `ModuleAutoDiscoveryExtensions.AddDiscoveredToolModules` registers `IToolModule`, `ViewModelType`, `ViewType`, and `I[Module]Service` automatically.

---

## 3. High-Density C# 13 Patterns (Save Output Tokens)

See [CompactModuleExample.cs](examples/CompactModuleExample.cs) for full primary constructor, `ToolViewModelBase`, and `Result<T>` service patterns.
