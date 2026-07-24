---
title: Glossary
area: search
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - search
---

# Glossary

Vocabulary of the COBOL.NET project. Each term links to where it is developed.

## Architecture & pipeline
- **Bound tree** — the typed semantic model the binder produces; the compiler's single IR (there is no separate
  lowered IR). See [[kb/IR/Node Types]].
- **`[BoundNode]`** — the marker attribute on abstract bound-node roots that drives the source-generated exhaustive
  visitor. See [[kb/IR/Node Types]].
- **`ICodeGenBackend`** — the selectable backend interface (`--backend roslyn|cil`) over the one bound tree;
  Roslyn primary, CIL future. See [[kb/Architecture/High-Level Design]].
- **RoslynBackend / CilBackend** — the two backends; Roslyn emits idiomatic C# source, CIL (future) emits typed-native
  IL via Mono.Cecil with its own private lowering.
- **`Place`** — the one typed-lvalue model (structural addressing replacing byte offsets), built by
  `ReferenceResolver`. See [[kb/IR/Data Flow]].
- **`ReferenceResolver`** — the single operand-resolution entry point producing a `Place`.
- **PC dispatcher / `__Dispatch`** — the program-counter state machine for COBOL control flow. See
  [[kb/IR/Control Flow]].
- **CST** — the pure syntactic parse tree (`CompilationUnitContext`); carries no semantics.
- **Superset grammar** — the ANTLR grammar recognizing the *union* of all editions; edition gating is a later pass.
- **Bind→emit boundary** — the hard layering rule: the binder owns semantics, the emitter only renders. See
  [[kb/Architecture/Module Overview]].

## Editions & conformance
- **`--std 85|2002|2014|2023`** — selects the ISO edition; default 2023. "Four compilers in one." See
  [[kb/Spec/Version Targeting]].
- **`--permissive`** — downgrades removed-construct rejections to warnings for migration.
- **`VersionConformancePass`** — the sole edition gate (two arms: parse-tree + bound-tree). See
  [[kb/Semantics/Passes]].
- **`FlagConformancePass`** — the sibling migration-flagging pass (`>>FLAG-02`/`>>FLAG-14`, always Warning).
- **`ConstructRegistry` / `ConstructDialectStatus`** — per-construct `IntroducedIn/RemovedIn/ObsoleteIn` records;
  `StatusAt(year)` gives the verdict.
- **`EditionInfo`** — the immutable dialect-year carrier (`MaxDigits` 18 pre-2002 / 31 after).
- **`EditionSeverityPolicy`** — the single strict/permissive decision seam.
- **§8.9 reserved-word funnel** — the per-edition user-word check via the `cobolWord` rule. See
  [[kb/Semantics/Validation Rules]].
- **VCR** — `VERSION_CHANGE_REFERENCE.md`, the ~130-row edition-change checklist.
- **§4.2.16 conformance record** — the implementor documentation (`CONFORMANCE.md`) required for conformance.
- **COBOLNET0900–0903** — the edition-gating diagnostic band (intro / reserved / removed / obsolete).

## Data & numerics
- **Typed-native** — a record IS a `record struct`, an elementary item IS a native field; no byte substrate. See
  [[kb/Architecture/High-Level Design]].
- **`CobolNum`** — the Int128-monomorphic numeric engine; unscaled integer + compile-time scale; `TryStore` funnel.
  See [[kb/Runtime/Execution Model]].
- **Unscaled value** — a fixed-point number stored as an integer with scale kept as compile-time metadata.
- **`StorageForm` / `CharImage`** — the computed decision of how an item is stored (native vs whole-group image).
- **REDEFINES 4-tier model** — one canonical backing per redefines class; views are computed accessors (A>B>C>D). See
  [[kb/IR/Data Flow]].
- **`ManagedPointer<T>`** — the one managed-reference carrier (BY REFERENCE, POINTER, ADDRESS OF, BASED, ALLOCATE).

## Runtime & execution
- **`StopRun` / `ProgramReturn` / `MethodReturn`** — the exception signals for run-unit / program / method termination.
- **EC (exception condition) model** — the ISO Table 13 exception engine; off by default (`>>TURN` folds at compile
  time). See [[kb/Runtime/Execution Model]].
- **`CobolReport`** — the Report Writer (RWCS) engine (compose-at-presentation).
- **`IRecordCodec`** — the generated per-layout serializer at the file/disk boundary.
- **`IClock` / `SystemClock`** — the injectable clock for deterministic CURRENT-DATE.

## Process & project
- **Legacy oracle** — `src/CobolSharp.*`, the byte-engine kept only as a differential oracle until the G8 cut-over
  (opt-in `COBOLSHARP_LEGACY_DIFFERENTIAL=1`). See [[kb/Context/Project History]].
- **G0–G8** — the greenfield build order (G0 reorg … G8 legacy retirement).
- **P00–P16** — the 17-phase rearchitecture plan. See [[kb/Modernization/Tasks]].
- **CONFORMANCE-FIX-QUEUE** — the verified, spec-derived fix-ready defect queue (30 landed / 16 remain).
- **CA1–CA38 / V54–V59** — code↔spec / design↔spec audit finding IDs. See
  [[kb/Modernization/Audit Artifacts]].
- **Spec-derived golden** — an expected-output test whose value is computed from the ISO §, not copied from the oracle.
- **Singular pattern** — one mechanism per job (the best one); the master design principle. See
  [[kb/Context/Doctrine & Anti-Patterns]].
- **guard.sh / guard-fast.sh** — the full / parallel regression gates. See [[kb/Compiler/Build System]].


## ISO COBOL lookup cross-reference
The lookup tables (`Spec/Lookup/`) hold the full classified inventory — keywords, grammar constructs, semantic rules,
IR nodes, runtime behaviors, and constraints — each linked across Spec → Compiler → IR → Semantics → Runtime:
- **Divisions / Sections / Paragraphs / Statements / Expressions / Data types** → [[kb/Spec/Lookup/Grammar]]
- **Keywords** (verbs, clauses, directives) → [[kb/Spec/Lookup/Keywords]]
- **Semantic rules** (category, gating, condition precedence, EC) → [[kb/Spec/Lookup/Semantic Rules]]
- **IR nodes** (157 `Bound*` types) → [[kb/Spec/Lookup/IR Mapping]]
- **Runtime behaviors** (numerics, files, signals, EC) → [[kb/Spec/Lookup/Runtime Mapping]]
- **Constraints** (edition limits, digit caps, anti-patterns) → [[kb/Spec/Lookup/Constraints]]
- **Master index + glossary** → [[kb/Spec/Lookup/Index]]

## Backlinks
- [[kb/Search/MOC]] · [[kb/Index]] — link here.
