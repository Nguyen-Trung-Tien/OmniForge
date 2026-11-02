---
name: issue-skill-distiller
description: Automatically distills newly solved problems, runtime crashes, architectural bugs, or unique engineering tasks into a standardized 4-part Antigravity AI Agent Skill (.agents/skills/<name>). Use whenever a new problem or bug is resolved, when the user instructs to create a skill for a newly encountered issue, or when establishing reusable runbooks for recurring failure patterns.
---

# Issue-to-Skill Distiller Skill (`issue-skill-distiller`)

This skill defines the standardized procedure for AI agents to analyze a newly resolved issue, bug, or technical hurdle in OmniForge and automatically package it into a permanent, reusable Antigravity AI Agent Skill.

---

## 1. Mandatory Prerequisites Before Execution

Prior to scaffolding or authoring any new skill, the agent **MUST** review the official Antigravity skill specification:
- **Official Documentation**: [https://antigravity.google/docs/skills/](https://antigravity.google/docs/skills/)
- **Local Reference**: `~/.gemini/antigravity/builtin/skills/agy-customizations/docs/skills.md`

---

## 2. Standard 4-Part Directory Architecture

Every skill produced by this distiller must strictly adhere to the standard directory architecture:

```text
.agents/skills/<skill-name>/
├── SKILL.md       # Required: Frontmatter (name, description) & step-by-step resolution runbook
├── scripts/       # Optional: Verification, diagnostic, or repair PowerShell scripts
├── examples/      # Optional: Code snippets, before/after diffs, or pattern examples
└── resources/     # Optional: Templates, error signatures, or schemas
```

> [!NOTE]
> Antigravity defaults to `.agents/skills`, but maintains backward compatibility for `.agent/skills`.
> Within OmniForge, all skills must be placed under `.agents/skills/`.

---

## 3. Step-by-Step Distillation Procedure

Whenever a new problem is solved or the user asks to create a skill for a specific issue:

### Step 3.1: Extract Problem Signatures
Identify and extract:
1. **Symptom**: Exact error message, exception type, HResult code, or UI failure state.
2. **Root Cause**: The underlying architectural mismatch, lifecycle flaw, or timing race condition.
3. **Resolution**: The verified code, configuration, or XAML change that eliminated the failure.
4. **Prevention**: Unit tests, architecture guardrails, or diagnostic checks preventing recurrence.

### Step 3.2: Determine Skill Identity
- Generate a concise, lowercase, hyphenated identifier (`kebab-case`, e.g., `winui3-startup-navigation`, `unhandled-thread-marshal-guard`).
- Write an informative, third-person YAML frontmatter `description` explaining **WHAT** the skill solves and **WHEN** an agent should activate it.

### Step 3.3: Scaffold via CLI Automation
Execute the automated scaffolding tool from solution root:

```powershell
pwsh -File .agents/skills/issue-skill-distiller/scripts/distill_issue_skill.ps1 `
    -Name "<skill-slug>" `
    -Description "<Informative description of problem and activation conditions>" `
    -Symptom "<Observable error message or glitch>" `
    -RootCause "<Underlying reason for failure>" `
    -Resolution "<Actionable step-by-step fix>" `
    -Category "Architecture"
```

### Step 3.4: Author Progressive Disclosure Content
- Refine the generated `SKILL.md` to be compact and procedural.
- Place extensive code snippets into `examples/` and automation scripts into `scripts/`.
- Ensure all relative markdown links (`[script](scripts/...)`) are functional.

### Step 3.5: Mandatory Documentation Propagation
Whenever a new skill is generated:
1. **`.agents/AGENTS.md`**: Ensure the skill's architectural rules are documented.
2. **`.agents/MEMORY.md`**: Verify the learned pattern is recorded.
3. **`.agents/skills/cli-task-runner/SKILL.md`**: Register any new CLI tasks if applicable.
4. **`CHANGELOG.md`**: Add an entry under `[Unreleased] -> ### Added`.

---

## 4. Verification Gate
Always execute:
```powershell
.\forge doctor
.\forge verify
```
Verify that all 143+ tests pass and the skills inventory count increments cleanly.

