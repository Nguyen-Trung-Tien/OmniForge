---
name: changelog-curator
description: Curates, consolidates, prunes unnecessary or redundant entries, and maintains Keep a Changelog standards for CHANGELOG.md in OmniForge.
---

# Changelog Curator & Curation Skill

Use this skill when authoring, reviewing, consolidating, or cleaning up `CHANGELOG.md` in **OmniForge Utility Suite**.

---

## 1. Core Principle: Signal Over Noise

`CHANGELOG.md` is an authoritative, end-user and contributor-facing document—**not** an ephemeral git commit log or internal scratchpad.

### What MUST Be Kept (High Signal):
- **User-Facing Features**: New modular utilities, UI workflows, settings options (`Added`).
- **Architectural & Visual Upgrades**: Significant UX refactors, performance enhancements, theme synchronization (`Changed`).
- **Security & Hardening**: Path traversal defense, ReDoS prevention, XXE limits (`Security`).
- **Critical Bug Fixes**: App crashes, resource loading failures, binding errors, memory leaks (`Fixed`).
- **Deprecations & Removals**: Features marked for retirement or removed (`Deprecated`, `Removed`).

### What MUST Be Pruned / Deleted (Unnecessary Noise):
- **Trivial Micro-Tweaks**: e.g., fixing a 2px padding, adjusting a minor color hex, correcting a code typo, local variable renames.
- **Intermediate Debugging Steps**: e.g., "Attempted fix X", "Reverted fix Y", temporary log statements.
- **Duplicate Section Headers**: Never allow repeating `### Added`, `### Fixed`, or `### Changed` headers within the same release block.
- **Redundant / Overlapping Bullet Points**: When multiple subagents or commits resolve parts of the same feature or bug, consolidate them into a single, cohesive bullet point.
- **Internal Ephemeral Details**: Never include prompt references, agent IDs, internal conversation tokens, or raw crash dump stack traces.

---

## 2. Standard Category Order

Under each version header (`## [Unreleased]` or `## [X.Y.Z] - YYYY-MM-DD`), categorize items strictly in this order:
1. `### Added` — for new features.
2. `### Changed` — for changes in existing functionality or architectural upgrades.
3. `### Deprecated` — for soon-to-be removed features.
4. `### Removed` — for now removed features.
5. `### Fixed` — for any bug fixes.
6. `### Security` — for vulnerability patches and input hardening.

---

## 3. Curation & Consolidation Workflow

1. **Scan**: Inspect `CHANGELOG.md` for duplicate category headers or fragmented entries under `[Unreleased]`.
2. **Merge**: Combine fragmented sections into a single header per category.
3. **Prune**: Delete low-value micro-commits and ephemeral debugging notes.
4. **Condense**: Use bold primary titles with concise, bulleted explanations of impact:
   ```markdown
   - **Feature or Fix Name**:
     - Key user-facing impact or architectural enhancement.
     - Secondary detail if necessary (keep brief).
   ```
5. **Verify**: Ensure the document strictly complies with [Keep a Changelog](https://keepachangelog.com/).
