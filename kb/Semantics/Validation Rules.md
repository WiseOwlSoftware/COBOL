---
title: Semantics — Validation Rules
area: semantics
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs
  - src/Cobol.Net.Compiler/Validation/FlagConformancePass.cs
  - src/Cobol.Net.Editions/ConstructDialectStatus.cs
  - src/Cobol.Net.Editions/ReservedWords.cs
  - src/Cobol.Net.Editions/EditionSeverityPolicy.cs
  - src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs
  - docs/COBOLNET_VALIDATION_DESIGN.md
  - docs/DIAGNOSTICS.md
tags:
  - cobolsharp
  - semantics
---

# Semantics — Validation Rules

The compiler's semantic checks fall into two families: **edition-invariant validation** (rules true in every ISO
edition, enforced in the binder) and **edition-conditional gating** (rules that depend on `--std`, funnelled through
one post-bind pass — see [[kb/Semantics/Passes]]). Both write to a shared `IDiagnosticSink` as structured
`EditionDiagnostic` records; the driver halts before emit if any error was reported.

## Flow-sensitive file-state / FILE STATUS checking
File-connector semantics are bound in `SequentialIoBinder`/`KeyedIoBinder` over the `FileModel` (organization, access
mode, `FileStatusItem`, `Linage`). At COBOL-2023 a cross-connector consistency rule set fires: for an external file
every corresponding SELECT must name the same external FILE STATUS item (`COBOLNET1573`), the same external RELATIVE
KEY item (`COBOLNET1575`), and those items must themselves be external (`COBOLNET1624`). `FlagConformancePass` also
tags FILE-STATUS references testing `'04'`/`'07'` for the FLAG-14 migration net.

## Category / MOVE compatibility
`MoveBinder` enforces the version-invariant §14.9.25.3 SR1 class check first — a MOVE operand of class
index/message-tag/object/pointer is rejected at every edition (`COBOLNET0809`). The edition-conditional SR5
figurative-category rules (ALL-digit→integer obsolete-2023; QUOTE→numeric obsolete-2014; other→numeric removed-2023)
are gated in the pass's `GateMove`, re-deriving classification from the bound MOVE's source × each receiver's resolved
PICTURE. See [[kb/IR/Data Flow]].

## The §8.9 reserved-word funnel (`ParseArm.VisitCobolWord`)
Every user-defined word reaches the tree through the `cobolWord` rule; one text-based check gates 2023-new words that
lex as IDENTIFIER (COMMIT, FINALLY) plus the EC-band context tokens. It is **position-blind** for a fixed
`CheckedTokenTypes` set and **position-aware** elsewhere via `IsProvableUserWordPosition` (only slots where no
clause/statement keyword can legally sit). Reserved-word truth comes from `ReservedWordEntry` rows generated from
**four sources** (in-repo ISO 2023 §8.9, VCR row 32 / Annex E.2, GnuCOBOL 85/2002/2014 lists), consumed through
`ReservedWordSet` (composing the `>>COBOL-WORDS` overlay). Only `Confidence:"high"` rows reject — the no-false-reject
policy.

## Strict vs permissive channels + the Removed() seam
`EditionSeverityPolicy.For(verdict, edition)` is the single strict/permissive decision — never a local
`if(permissive)`: NotYetIntroduced ⇒ Error on both axes; **Removed ⇒ Error strict / Warning permissive**; Obsolete ⇒
Warning always.

## ConstructDialectStatus registry + drift
Each construct row carries `IntroducedIn/RemovedIn/ObsoleteIn/DiagnosticCode/Citation`; `StatusAt(year)` yields the
verdict. `ConstructRegistryDriftTests` asserts registry ↔ `constructs.json` both directions, so no gate lands without
its matrix row (and vice versa). `>>COBOL-WORDS` SR3/SR4 category validation runs in the pass.

## Diagnostic catalog structure
`DiagnosticDescriptor(Code, Id, Severity, Title, IsoSection, SuppressKey?)` — a stable kebab-case `Id` survives code
renumbering; `Code` may repeat (the `COBOLNET0899` recognized-not-implemented family). `DiagnosticCatalog.All` is
reflected, feeding [[docs/DIAGNOSTICS]] and `DiagnosticRegistryDriftTests`. Notable code bands:
- `0900/0901/0902/0903` — edition introduction / reserved-word / removal / obsolete.
- `0801/0802/0809/0810/0811` — digit-capacity & MOVE class checks.
- `1533/1535` — strong-type rules.
- `1560`+ / `1518` — §4.2.6 non-support warnings / locale reject.
- `1573/1575/1624` — external-file consistency; `1620–1625` — directive & 2023 rules.

## Key concepts
- Two validation families: edition-invariant (binder) vs edition-conditional (one pass).
- Reserved-word funnel: superset grammar + per-edition `cobolWord` check; four-source tables; high-confidence-only rejection.
- `EditionSeverityPolicy` = the ONE strict/permissive seam.
- Registry ↔ `constructs.json` drift discipline; reserved-words ↔ `reserved-words.json` drift.
- Descriptor catalog: stable `Id` decoupled from emitted `Code`; suppress families.

## See also
- [[kb/Semantics/Passes]] — where these rules run in the pipeline.
- [[kb/Spec/Version Targeting]] — the edition model behind the gates.
- [[kb/Diagrams/Semantic Validation Flow]] — the funnel & severity seam visualized.
- [[kb/Runtime/Execution Model]] — FILE STATUS / EC at execution.

## Backlinks
- [[kb/Semantics/MOC]] · [[kb/Index]] — link here.
- Lookup: [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Spec/Lookup/Constraints]] · [[kb/Diagrams/Semantic Rule Flow]].
