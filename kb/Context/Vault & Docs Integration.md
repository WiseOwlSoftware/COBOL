---
title: Vault Architecture & Setup
area: context
status: live
last_updated: 2026-07-23
related_files:
  - .gitignore
  - docs/DOC_INDEX.md
tags:
  - cobolsharp
  - context
---

# Vault Architecture & Setup

**As of 2026-07-23 the Obsidian vault root IS the repo root (`E:\CobolSharp\`)** — one vault, one graph over the whole
project. Confirmed: the pre-existing `[[kb/…]]` links resolve unchanged (Obsidian suffix-matches the new
`kb/…` paths), and `docs/*` is now in the graph. This note records how the layers fit and how each is
maintained.

## The four layers (single source of truth; Obsidian is the lens)
| Layer | Where | Authority | Maintained by |
|---|---|---|---|
| **Spec** | [[specs/ISO_COBOL]] | ISO | private submodule — indexed locally, never copied/committed |
| **Docs** | `docs/*` | the docs | humans + `gen-*.ps1` (some are generated) → [[docs/DOC_INDEX]] |
| **Code reference** | `kb/Reference/*` *(planned)* | the **source code** | a build generator (drift-proof) |
| **Synthesis** | `kb/*` (these notes) | the notes | humans — **link/embed down, don't restate** |

Synthesis notes link *down* into the docs and (soon) the generated code notes; Obsidian backlinks make it
bidirectional. Change a doc → every note that links it reflects it. For content that must match verbatim, use a live
embed: `![[docs/SomeDoc#Section]]`.

## Guardrails
- **No verbatim ISO text** in any tracked note — paraphrase + cite the §; the spec stays in the `specs/` submodule.
- **Don't hand-edit generated docs** ([[docs/DIAGNOSTICS]], the VCR generated block) or inject `[[ ]]` into them — link *to* them.
- **Keep canonical docs tool-neutral** — they keep standard `[text](path)` links; only synthesis notes use `[[ ]]`.

## Version control
- **Tracked:** `docs/*`, `kb/**` (synthesis notes), and reproducible `.obsidian/*.json` settings.
- **Ignored** (`.gitignore`): `.obsidian/workspace*.json`, `.obsidian/cache`, `.obsidian/plugins/` (incl. the Local
  REST API key/cert `data.json`), `.trash/`, and the planned generated `Reference/` folder (a build output).
- ⛔ The Local REST API key lives in `.obsidian/plugins/.../data.json` — **gitignored; never commit it.**

## Obsidian settings
Keep the graph & search to docs + notes: Settings → **Files & Links → Excluded files** → add
`src/`, `tests/`, `bin/`, `obj/`, `.git/`, `Generated/`.

## Planned: generated code-reference layer
A Roslyn build generator emits one note per type from the `///` doc-comments — starting with the 158 `Bound*` nodes
(`Binding/Bound/*.cs`), replacing the hand-maintained [[kb/Spec/Lookup/IR Mapping]] with a drift-proof
generated version. Tracked in [[kb/Remaining Work Tracker]].


**✅ Pilot LIVE (2026-07-23):** `scripts/gen-vault-reference.ps1` generates **302 type notes** across Bound + Runtime + Frontend into `kb/Reference/` (index: [[kb/Reference/Bound/_Index]]) from the source `///` summaries — drift-proof, gitignored
build output, 3 documentation-debt items surfaced. The PowerShell generator now covers Bound + Runtime + Frontend, with an opt-in MSBuild build hook
(`-p:GenerateVaultReference=true` in `Cobol.Net.Cli.csproj`) and a `VaultReferenceGeneratorDriftTests` "runs clean" CI check.

## See also
- [[docs/DOC_INDEX]] — the authoritative doc map (now in-graph).
- [[kb/Index]] · [[kb/Context/MOC]]

## Backlinks
- [[kb/Index]] — the vault banner links here.
