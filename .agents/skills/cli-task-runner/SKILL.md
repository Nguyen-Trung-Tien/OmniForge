---
name: cli-task-runner
description: Unified root terminal CLI workflow (.\forge) for building, running, testing, scaffolding modules/tests, inspecting logs, and persisting learned patterns without directory hopping.
---

# OmniForge Unified CLI Task Runner (`.\forge`)

Always use the root `.\forge.ps1` (or `.\forge`) script from `d:\Tools\OmniForge` instead of navigating into `src/`, `tests/`, or `.agents/scripts/` subdirectories. This saves turns, avoids path resolution errors, and enforces `Platform=x64` and `TreatWarningsAsErrors=true` automatically.

---

## 1. Core Terminal Commands (Run from Workspace Root)

| Task | Single Root Command | Description |
| :--- | :--- | :--- |
| **Full Verification** | `.\forge verify` | Runs Static Arch Guardrails + Build (`x64`) + `xUnit`/`NetArchTest` |
| **Fast Arch Check** | `.\forge arch` | Scans C#/XAML for `AGENTS.md` violations in `< 1s` (no build) |
| **Build Solution** | `.\forge build` | Builds `OmniForge.sln` (`Debug \| x64`, `TreatWarningsAsErrors=true`) |
| **Launch App** | `.\forge run` | Builds and launches `OmniForge.App` directly from root |
| **Run All Tests** | `.\forge test` | Executes all unit, integration, and architecture tests |
| **Run Filtered Test** | `.\forge test -Filter "ResultTests"` | Runs a specific test class or method for rapid TDD |
| **Scaffold Module** | `.\forge new-module <Name> -DisplayName "..."` | Generates `Service`, `ViewModel`, `Page.xaml`, `Module`, and `Tests` |
| **Scaffold Test** | `.\forge new-test <Name> -SubFolder "Services"` | Generates a deterministic xUnit test class |
| **Learn & Persist** | `.\forge learn "<Insight>" -Category "WinUI3"` | Appends a learned pattern or fix to `.agents/MEMORY.md` |
| **Read Runtime Logs** | `.\forge logs -Tail 30` | Prints the latest structured JSONL log lines from `%LocalAppData%` |
| **Environment Check** | `.\forge doctor` | Verifies `.NET SDK`, skills inventory, rules, and static guardrails |
| **Clean Artifacts** | `.\forge clean` | Purges all `bin/` and `obj/` directories across the solution |
| **Publish Release** | `.\forge publish -Platform x64` | Publishes a self-contained `Release` build |

---

## 2. Directory Structure & Assets

- Helper Script: [invoke_task.ps1](scripts/invoke_task.ps1)
- Command Schema & Catalog: [cli-commands.json](resources/cli-commands.json)
- Sample Output: [sample-cli-output.json](examples/sample-cli-output.json)

---

## 3. Mandatory Agent Workflow Rules

1. **Zero Directory Hopping**:
   - Do **not** run `cd src/OmniForge.App` or `cd tests/OmniForge.Tests`.
   - Always keep the working directory at `d:\Tools\OmniForge` and invoke `.\forge <command>`.
2. **Continuous Knowledge Capture (`learn`)**:
   - Whenever you resolve a tricky compiler warning, WinUI 3 XAML binding issue, or architectural edge case, record it immediately via:
     ```powershell
     .\forge learn "Short actionable rule or insight" -Category "Architecture"
     ```
   - Before starting complex debugging, check existing project memory with `.\forge learn`.
3. **Pre-Completion Gate**:
   - Always run `.\forge verify` as the final verification step before completing any code modification task.
