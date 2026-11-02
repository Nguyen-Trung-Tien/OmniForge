# OmniForge Agent Skills Protocol & Standards

This document establishes the mandatory rules for creating, structuring, and maintaining AI Agent Skills in the OmniForge workspace.

---

## 1. Mandatory Prerequisites Before Creating Skills
Before creating or scaffolding any new skill, agents MUST read the official Antigravity skill instructions:
- **Web Reference**: [https://antigravity.google/docs/skills/](https://antigravity.google/docs/skills/)
- **Local Reference**: `~/.gemini/antigravity/builtin/skills/agy-customizations/docs/skills.md`

---

## 2. Standard Directory Layout
Every skill MUST follow the standard 4-part directory layout:

```
my-skill/
├── SKILL.md       # Required: Main instructions and YAML frontmatter
├── scripts/       # Optional: Executable automation scripts
├── examples/      # Optional: Reference implementations
└── resources/     # Optional: Templates, schemas, or data files
```

> [!NOTE]
> Antigravity defaults to `.agents/skills`, but maintains backward compatibility for `.agent/skills`.
> In this repository, all skills reside under `.agents/skills/`.

---

## 3. Required Metadata (`SKILL.md`)
- `SKILL.md` must start with valid YAML frontmatter:
  - `name`: Lowercase, hyphenated (e.g. `cli-task-runner`).
  - `description`: High-impact summary explaining WHAT the skill does and WHEN the agent should activate it.
- **Progressive Disclosure**: Keep `SKILL.md` concise. Offload schemas to `resources/`, code samples to `examples/`, and scripts to `scripts/`.

---

## 4. Updates & Propagation
Whenever a new skill is created:
1. Update `.agents/AGENTS.md` (add or update rules).
2. Update `.agents/skills/cli-task-runner/SKILL.md` and `forge.ps1` if CLI commands are involved.
3. Update `.agents/MEMORY.md`.
4. Update `CHANGELOG.md` under `[Unreleased] -> ### Added`.

