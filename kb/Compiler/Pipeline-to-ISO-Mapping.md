---
title: Pipeline → ISO Construct Mapping
area: compiler
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Frontend/Pipeline/Frontend.cs
  - src/Cobol.Net.Compiler/CompilerDriver.cs
  - src/Cobol.Net.Compiler/Binding/BinderDriver.cs
  - src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs
  - src/Cobol.Net.Compiler/CodeGen/RoslynBackend.cs
tags:
  - cobolsharp
  - compiler
  - spec
---

# Pipeline → ISO Construct Mapping

**The bridge note.** For every compiler phase, this records exactly which ISO/IEC 1989:2023 constructs it recognizes,
which rules and constraints it enforces, and the IR nodes / semantic rules it connects to. It is the join between
[[kb/Compiler/Pipeline]] (the *mechanism*) and the [[kb/Spec/Lookup/Index]] tables (the *spec*).

## Overview

COBOL.NET runs **6 conceptual phases** (see [[kb/Compiler/Pipeline]]). Mapped onto the classic
lex→parse→AST→IR→validate→optimize→codegen→runtime frame, two collapses matter:

- **AST-building and IR-generation are one step** — `Bind` produces the typed **bound tree**, which is *both* the AST
  and the IR. There is **no separate AST and no lowered IR** ([[kb/IR/Node Types]]).
- **There is no bespoke optimization phase** — optimization is delegated to the C# compiler and the .NET JIT, because
  the Roslyn backend emits idiomatic C# ([[kb/Architecture/High-Level Design]]).

| # | Phase | ISO clause centre of gravity | Produces |
|---|---|---|---|
| 0 | [[#Phase 0 — Preprocess (text manipulation)\|Preprocess]] | §6 reference format · §7 directives | normalized source text |
| 1 | [[#Phase 1 — Lexing / Tokenization\|Lex]] | §8.3 lexical elements | token stream |
| 2 | [[#Phase 2 — Parsing\|Parse]] | §8–§16 syntax (superset) | CST |
| 3 | [[#Phase 3 — AST building + IR generation (Bind)\|Bind]] | §8.4–§8.8, §13, §14 semantics | bound tree |
| 4 | [[#Phase 4 — Semantic validation (edition gating)\|Validate]] | §4 conformance · §8.9 · Annex E/F | gated bound tree (HALT on error) |
| 5 | [[#Phase 5 — Optimization (delegated)\|Optimize]] | — (delegated) | — |
| 6 | [[#Phase 6 — Code generation\|Codegen]] | §14 verb semantics → C# | C# source → assembly |
| 7 | [[#Phase 7 — Runtime integration\|Runtime]] | §8.8, §9.1.13, §14.6–14.9, §15 | observable behavior |

---

## Phase 0 — Preprocess (text manipulation)

**Description.** Turns raw source into a single normalized token-ready text: reference-format normalization,
conditional compilation, COPY/REPLACE expansion, and NIST placeholder fixups. Runs before the lexer sees anything.

- **Related source files:** `src/Cobol.Net.Frontend/Pipeline/Frontend.cs` (`Preprocess`); `Preprocessor/ReferenceFormatProcessor.cs`, `ConditionalCompilationProcessor.cs`, `CopyProcessor.cs`, `NistPreprocessor.cs`, `TurnDirectiveProcessor.cs`, `FlagDirectiveProcessor.cs`, `CobolWordsDirectiveProcessor.cs`, `RefModZeroLengthDirectiveProcessor.cs`, `PropagateDirectiveProcessor.cs`.
- **Related notes:** [[kb/Compiler/Phases]] · [[kb/Spec/Constraints]]
- **ISO constructs handled:** COPY / REPLACE (§7.2); `>>DEFINE`/`>>IF`/`>>EVALUATE` conditional compilation (§7.2.1); `>>TURN` (§7.3.13), `>>COBOL-WORDS` (§7.3.10), `>>FLAG-02`/`>>FLAG-14` (§7.3.14/.15), `>>REF-MOD-ZERO-LENGTH` (§7.3.23). See [[kb/Spec/Lookup/Construct Catalogue]] §13.
- **ISO rules enforced:** fixed vs free reference format and the column-7 indicator (§6); directive edition gates fire here because only the column-aware pass sees the indicator (VCR rows 2/94).
- **Constraints applied:** each directive collector must be **line-count-neutral** (hazard H3) so TURN/FLAG anchoring stays aligned. See [[kb/Spec/Lookup/Constraints]] (reference format).
- **IR / semantic links:** no bound residue — directives resolve to text or to `TurnState`/`FlagState`/`CobolWordsMap` carriers consumed downstream.

## Phase 1 — Lexing / Tokenization

**Description.** The ANTLR `CobolLexer` (case-insensitive) tokenizes the normalized text using 4 modes
(`DEFAULT`/`PICMODE`/`SUBSCRIPT`/`COMMENT_MODE`), then two rewriters retype tokens.

- **Related source files:** `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4`; `Parsing/ZeroTokenRewriter.cs` (`ZERO→ZERO_ARITH`), `Parsing/CobolWordsRewriter.cs` (`>>COBOL-WORDS` synonyms), `Parsing/CobolKeywordTokens.cs`, `Parsing/CobolLexerWordSet.g.cs`.
- **Related notes:** [[kb/Compiler/Phases]] · [[kb/Spec/Lookup/Keywords]]
- **ISO constructs handled:** lexical elements — COBOL words, literals (alphanumeric/national/boolean/numeric), PICTURE character-strings, separators, the `*>` comment (§8.3).
- **ISO rules enforced:** word-formation (§8.3.2, incl. the 2023 63-character word length, §8.3.2.1); PICTURE captured as a single token (PICMODE); the subscript-vs-grouping `(` disambiguation via SUBSCRIPT mode (§8.4.2 references).
- **Constraints applied:** case-insensitivity; word-length limit (COBOLNET1567 for the 2023 relaxation).
- **IR / semantic links:** feeds the parser; reserved-word truth is applied later by the §8.9 funnel ([[kb/Spec/Lookup/Semantic Rules]]).

## Phase 2 — Parsing

**Description.** The **superset** `CobolParserCore` (recognizes the union of all editions) parses tokens into a pure
syntactic **CST**, two-stage (SLL bail → LL recover). No edition predicates in the grammar (save two forward-detects).

- **Related source files:** `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4` + `Grammar/Core/*.g4` (9 fragments); `Parsing/CobolParserCoreBase.cs`, `Parsing/CobolErrorStrategy.cs`, `Parsing/CobolErrorListener.cs`.
- **Related notes:** [[kb/Compiler/Phases]] · [[kb/Diagrams/Grammar Hierarchy]] · [[kb/Spec/Lookup/Grammar]]
- **ISO constructs handled:** the full grammar surface — the four **divisions** (§11–§14), **sections**, **paragraphs**, **statements**, **expressions/conditions** (§8.8), **data-description** (§13), and OO/Report Writer/Screen. See [[kb/Spec/Lookup/Grammar]] and the 9 fragments in [[kb/Compiler/Phases]].
- **ISO rules enforced:** context-free syntax only; formats/phrases of each statement (§14.9.x), data clauses (§13.18.x).
- **Constraints applied:** superset acceptance — *edition legality is deliberately deferred* to Phase 4 (so a below-edition construct still parses and can be reported on recognition).
- **IR / semantic links:** produces `CompilationUnitContext`; carries **no** semantics — the CST is the input to Bind.

## Phase 3 — AST building + IR generation (Bind)

**Description.** `Bind` walks the CST and produces the typed **bound tree** — the single semantic model that is *both*
the AST and the IR. It resolves every reference to a `Place`, types and categorizes every operand, and classifies each
statement. Edition-agnostic (a closed exception ledger aside).

- **Related source files:** `src/Cobol.Net.Compiler/Binding/BinderDriver.cs`, `BindSession.cs`, `DataBinder*.cs`, `ReferenceResolver.cs`, `Procedure/` (StatementBinder + `Verbs/*Binder.cs`), `Binding/Bound/BoundTree.cs`, `Binding/Model/{Place,DataItem,PicInfo,StorageForm}.cs`, `Binding/Passes/{BindPipeline,StorageFormPass,UsageCollectionPass}.cs`; visitor generator `src/Cobol.Net.Compiler.SourceGen/BoundVisitorGenerator.cs`.
- **Related notes:** [[kb/IR/Node Types]] · [[kb/IR/Data Flow]] · [[kb/IR/Control Flow]] · [[kb/Spec/Lookup/IR Mapping]]
- **ISO constructs handled:** every construct becomes a bound node — data items → `DataItem`/`record struct` (§13.16 PICTURE/USAGE/OCCURS/REDEFINES); statements → `Bound*` (§14.9.x); expressions/conditions → `BoundExpr`/`BoundCondition` (§8.8); references → `Place` (§8.4.2). Full node map: [[kb/Spec/Lookup/IR Mapping]].
- **ISO rules enforced (edition-invariant):** reference qualification/subscript resolution (§8.4.2.2); category assignment & PICTURE analysis (§13.16); MOVE class legality (§14.9.25.3 SR1, `MoveBinder`, COBOLNET0809); condition precedence (§8.8.4.2); digit-capacity (§8.8, COBOLNET0801/0802); REDEFINES 4-tier legality (§13.16). See [[kb/Spec/Lookup/Semantic Rules]].
- **Constraints applied:** the bound tree is **total** (error nodes keep it complete); the binder emits no C# text (backend neutrality); `Place` is the single lvalue for all verbs.
- **IR / semantic links:** ⇒ [[kb/Spec/Lookup/IR Mapping]] (node ↔ construct) and [[kb/IR/Data Flow]] (PIC → .NET type).

## Phase 4 — Semantic validation (edition gating)

**Description.** `VersionConformancePass` — the **sole edition gate** — runs as the bind manifest's terminal pass, in
two arms, and **halts before emit** on any error. `FlagConformancePass` runs after as an orthogonal warning axis.

- **Related source files:** `src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs`, `Validation/FlagConformancePass.cs`; `Binding/EditionContext.cs`; `src/Cobol.Net.Editions/{ConstructRegistry,ConstructDialectStatus,EditionInfo,EditionSeverityPolicy,ReservedWords}.cs`.
- **Related notes:** [[kb/Semantics/Passes]] · [[kb/Semantics/Validation Rules]] · [[kb/Diagrams/Semantic Rule Flow]]
- **ISO constructs handled:** every edition-varying construct (see the full inventory [[kb/Spec/Lookup/Construct Catalogue]] — 183 constructs × edition).
- **ISO rules enforced:** introduction gating (§ per construct → COBOLNET0900); removal gating (Annex F/VCR → 0902); §8.9 reserved-word funnel (→ 0901); obsolete/archaic flags (Annex F → 0903); the §4 conformance posture (documented non-support → 1560 band / 1518); external-file consistency (§9.1/§12.4 → 1573/1575/1624); migration flags §7.3.14/.15 (→ 1620/1621). See [[kb/Spec/Lookup/Semantic Rules]].
- **Constraints applied:** strict vs `--permissive` via the one `EditionSeverityPolicy` seam; `constructs.json` single-sources version numbers; **no codegen on errors** (D8). See [[kb/Spec/Lookup/Constraints]].
- **IR / semantic links:** two-arm design — ParseArm (recognition-based) + bound-arm (resolved facts); all ~88 sites funnel through `ConstructRegistry.Check`. See [[kb/Spec/Version Targeting]].

## Phase 5 — Optimization (delegated)

**Description.** COBOL.NET performs **no bespoke optimization pass**. Because the Roslyn backend emits idiomatic C#,
optimization is delegated to the **C# compiler** (constant folding, dead-code, etc.) and the **.NET JIT** (inlining,
register allocation). The only bound→bound normalization is small desugaring folded inside binder verbs
(MOVE CORRESPONDING, PERFORM VARYING AFTER, condition-names) — not a separate optimizing tree.

- **Related source files:** — (delegated to Roslyn / JIT); minor desugar lives in `Binding/Procedure/Verbs/*`.
- **Related notes:** [[kb/Architecture/High-Level Design]] (bound-tree-only, no lowered IR)
- **ISO constructs handled / rules enforced / constraints:** none directly — the semantic contract is fixed at Bind/Validate; optimization must preserve observable behavior (§8.8 arithmetic exactness is guaranteed by native scaled integers, not by an optimizer).
- **IR / semantic links:** n/a.

## Phase 6 — Code generation

**Description.** The selected `ICodeGenBackend` renders the bound tree. **RoslynBackend** (primary) emits idiomatic,
typed-native **C# source** (`.g.cs`) via a decomposed emitter and compiles it with Roslyn to an assembly. Control flow
is emitted as the **PC dispatcher**. (A future CIL backend does its own private lowering.)

- **Related source files:** `src/Cobol.Net.Compiler/CodeGen/ICodeGenBackend.cs`, `RoslynBackend.cs`, `CSharpEmitter.cs`, `ProgramEmitter.cs`, `StatementEmitter.cs`, `DispatchEmitter.cs`, `EcEmitter.cs`, `CodeGen/DataDivision/*`, `CodeGen/Verbs/*`, `CodeGen/Roslyn/{PlaceRenderer,RuntimeApi,FigurativeConstants}.cs`, `AssemblyPackager.cs`; driver `CompilerDriver.cs`.
- **Related notes:** [[kb/IR/Control Flow]] (dispatcher) · [[kb/Diagrams/Runtime Behavior Flow]] · [[kb/Architecture/Module Overview]]
- **ISO constructs handled:** every verb/expression is rendered — arithmetic (§14.9), MOVE (§14.9.25), control flow (§14.4/§14.9.14–.28), files (§14.9 I-O), CALL/INVOKE (§14.9.4/.23), Report Writer (§13.18). Node → render map: [[kb/Spec/Lookup/IR Mapping]].
- **ISO rules enforced:** faithful lowering of PC control flow (GO TO/PERFORM THRU/ALTER, §14.9.17/.28/.3); paragraph fall-through (§14.4); DECLARATIVES/USE firing (§14.5).
- **Constraints applied:** the **loud-failure invariant** — bind-success must yield compilable C#; a Roslyn error on generated code is an ICE, never a silent `// TODO` (D8). Emitters render only (never mutate the model).
- **IR / semantic links:** consumes the bound tree via the source-generated exhaustive visitor; renders `Place` reads/writes through `PlaceRenderer`.

## Phase 7 — Runtime integration

**Description.** The generated C# calls into **`Cobol.Net.Runtime`** for all value/behavior semantics. Typed-native —
no byte `ProgramState`; byte images are transient at file/CODE-SET boundaries only.

- **Related source files:** `src/Cobol.Net.Runtime/Values/*` (`CobolNum`, `CobolString`, `CobolDynTable`), `Verbs/*` (`CobolInspect`, `CobolStringOps`), `IO/*` (`CobolFile`, connectors, `CobolSort`), `Intrinsics/*` (`CobolIntrinsics`, `CobolDate`), `Control/*` (`ManagedPointer`, `RunUnit`, signals), `Exceptions/*` (`ExceptionCatalog`, `ExceptionState`).
- **Related notes:** [[kb/Runtime/Execution Model]] · [[kb/Spec/Lookup/Runtime Mapping]]
- **ISO constructs handled:** runtime behavior of numerics (§8.8.1), strings (§14.9.21/.38/.42), files (§14.9 I-O, §9.1.13 status), intrinsics (§15), interprogram (§14.9.4), OO INVOKE (§14.9.23), EC engine (§14.6.12 Table 13), Report Writer (§13.18).
- **ISO rules enforced:** ROUNDED (§14.7.4) + ON SIZE ERROR (§14.6.6) via the `TryStore` funnel; FILE STATUS state machines (§9.1.13); condition evaluation & short-circuit (§8.8.4); EC hierarchy/fatality (§14.6.12).
- **Constraints applied:** typed-native only (no byte substrate); deterministic clock for golden output; EC checking off by default.
- **IR / semantic links:** every runtime call traces back to a bound node — see [[kb/Spec/Lookup/Runtime Mapping]] and [[kb/Spec/Lookup/IR Mapping]].

---

## Phase → spec-note cross-reference

| Phase | Spec notes | Lookup tables |
|---|---|---|
| Preprocess | [[kb/Spec/Constraints]] | [[kb/Spec/Lookup/Constraints]] |
| Lex | [[kb/Spec/Language Features]] | [[kb/Spec/Lookup/Keywords]] |
| Parse | [[kb/Spec/Language Features]] | [[kb/Spec/Lookup/Grammar]] |
| Bind | [[kb/Spec/Language Features]] | [[kb/Spec/Lookup/IR Mapping]] · [[kb/Spec/Lookup/Semantic Rules]] |
| Validate | [[kb/Spec/Overview]] · [[kb/Spec/Version Targeting]] | [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Spec/Lookup/Construct Catalogue]] |
| Codegen | [[kb/Spec/Language Features]] | [[kb/Spec/Lookup/IR Mapping]] |
| Runtime | [[kb/Spec/Language Features]] | [[kb/Spec/Lookup/Runtime Mapping]] |

## See also
- [[kb/Compiler/Pipeline]] · [[kb/Compiler/Phases]] — the mechanism.
- [[kb/Diagrams/Compiler Pipeline Diagram]] — the annotated visual.
- [[kb/Spec/Lookup/Index]] — the ISO lookup layer.

- [[kb/Compiler/ISO-Clause-to-Phase]] — the **reverse** index (ISO clause → phase).
- [[kb/Spec/Lookup/Diagnostics]] — the diagnostic-code → phase map.

## Backlinks
- [[kb/Compiler/MOC]] · [[kb/Spec/MOC]] · [[kb/Index]] — link here.
