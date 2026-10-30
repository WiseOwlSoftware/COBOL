---
title: Knowledge Base Architecture & Setup
area: context
status: live
last_updated: 2026-10-29
related_files:
  - .gitignore
  - docs/DOC_INDEX.md
tags:
  - wiseowl-cobol
  - context
---

# Knowledge Base Architecture & Setup

`kb/` is plain Markdown over the whole project: the notes link down into `docs/*` and the source, and agents read them
as files. No tracked file belongs to a note-taking application (kb/Work PB3799); the work register's views are
`python scripts/spec/work.py` subcommands. This note records how the layers fit and how each is maintained.

## The four layers (single source of truth)
| Layer | Where | Authority | Maintained by |
|---|---|---|---|
| **Spec** | [[specs/ISO_COBOL]] | ISO | private submodule — indexed locally, never copied/committed |
| **Docs** | `docs/*` | the docs | humans + `gen-*.ps1` (some are generated) → [[docs/DOC_INDEX]] |
| **Code reference** | `kb/Reference/*` | the **source code** | a build generator (drift-proof) |
| **Synthesis** | `kb/*` (these notes) | the notes | humans — **link/embed down, don't restate** |

Synthesis notes link *down* into the docs and the generated code notes. Change a doc → every note that links it
reflects it. For content that must match verbatim, use a live embed: `![[docs/SomeDoc#Section]]`.

## Guardrails
- **No verbatim ISO text** in any tracked note — paraphrase + cite the §; the spec stays in the `specs/` submodule.
- **Don't hand-edit generated docs** ([[docs/DIAGNOSTICS]], the VCR generated block) or inject `[[ ]]` into them — link *to* them.
- **Keep canonical docs tool-neutral** — they keep standard `[text](path)` links; only synthesis notes use `[[ ]]`.

## Version control
- **Tracked:** `docs/*` and `kb/**` (synthesis notes and the work register `kb/Work/`).
- **Ignored** (`.gitignore`): the generated folders `kb/Reference/`, `kb/Grammar/` and `kb/Conformance/` (build outputs
  and rendered views).

## Generated code-reference layer
A Roslyn build generator emits one note per type from the `///` doc-comments (the `Bound*` nodes and every other type of
the six greenfield projects), replacing a hand-maintained [[kb/Spec/Lookup/IR Mapping]] with a drift-proof generated
version. Live work is in `kb/Work/` (`python scripts/spec/work.py next`).

`scripts/gen-vault-reference.ps1` generates **634 type notes** across all six greenfield projects (Bound, Compiler, SourceGen, Editions, Runtime, Frontend) — class / record / record-struct / struct / enum / interface into `kb/Reference/` (index: [[kb/Reference/Bound/_Index]]) from the source `///` summaries — drift-proof, gitignored
build output, 3 documentation-debt items surfaced. It has an opt-in MSBuild build hook
(`-p:GenerateVaultReference=true` in `WiseOwl.Cobol.Cli.csproj`) and a `VaultReferenceGeneratorDriftTests` "runs clean" CI check.

## Grammar diagrams (railroad)
A second generator — `scripts/gen-grammar-diagrams.ps1` — renders **railroad syntax diagrams** from the ANTLR
`*.g4` parser fragments: clean (strip comments / predicates / actions) → W3C EBNF → the vendored bottlecaps
`tools/rr/rr.war` → Markdown with one inline SVG railroad diagram per rule, into `kb/Grammar/` (gitignored
build output). Covers all **nine** ANTLR parser fragments (386 diagrams) — index at [[kb/Grammar/_Index]].
Run a subset with `-Fragments Name1,Name2`; `-Width` tunes the wrap threshold. Productionized the same way as the
code-reference generator: an opt-in MSBuild build hook (`-p:GenerateGrammarDiagrams=true` in `WiseOwl.Cobol.Cli.csproj`)
and a `GrammarDiagramGeneratorDriftTests` "runs clean" CI check (requires `java` + the vendored `tools/rr/rr.war`).

## See also
- [[docs/DOC_INDEX]] — the authoritative doc map.
- [[kb/Index]] · [[kb/Context/MOC]]

## Backlinks
- [[kb/Index]] — the banner links here.
