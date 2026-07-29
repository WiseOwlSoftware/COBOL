---
title: Reverse Index — ISO Clause → Phase
area: compiler
status: draft
last_updated: 2026-07-23
related_files:
  - docs/COBOLNET_PIPELINE_DESIGN.md
  - specs/ISO_COBOL.md
tags:
  - cobolsharp
  - compiler
  - spec
---

# Reverse Index — ISO Clause → Phase

The inverse of [[kb/Compiler/Pipeline-to-ISO-Mapping]]: given an ISO/IEC 1989:2023 clause, **which compiler
phase(s) implement or enforce it.** Phases: **Pre** (preprocess), **Lex**, **Parse**, **Bind**, **Val**
(`VersionConformancePass`/`FlagConformancePass`), **Gen** (codegen/emit), **Run** (runtime). See
[[kb/Compiler/Pipeline]].

## By top-level clause
| ISO clause | Topic | Primary phase(s) | Notes |
|---|---|---|---|
| **§4** | Conformance | Val · Bind | §4.2.16 record = [[docs/CONFORMANCE]]; §4.2.6 non-support warns at Val → [[kb/Spec/Overview]] |
| **§6** | Reference format (fixed/free) | **Pre** | only the column-aware pass sees the col-7 indicator → [[kb/Compiler/Phases]] |
| **§7** | Compiler-directing (COPY/REPLACE, `>>`) | **Pre** (+ Val) | COPY/REPLACE/CC at Pre; `>>FLAG-02/14` warnings at Val (`FlagConformancePass`) |
| **§8.3** | Lexical elements (words, literals, PICTURE) | **Lex** | word-formation, PICMODE, literal categories → [[kb/Spec/Lookup/Keywords]] |
| **§8.4** | References / qualification / subscripts | **Bind** | `ReferenceResolver` → `Place` → [[kb/IR/Data Flow]] |
| **§8.5** | Data categories (national, boolean, tables) | Bind · Run | typing at Bind; UTF-16/bit behavior at Run |
| **§8.8.1** | Arithmetic expressions | Bind · Run | tree at Bind (`BoundBinary`/`BoundPower`); `CobolNum` at Run |
| **§8.8.2** | Boolean/bit expressions | Bind · Run | `BoundBool*`; `CobolBool` at Run |
| **§8.8.3** | Concatenation | Bind | `ConcatFolder` (COBOLNET1540/1541/1545) |
| **§8.8.4** | Conditions (relation/class/sign/88/switch) | Bind · Run | `ConditionBinder` → `ConditionRenderer` short-circuit C# |
| **§8.9** | Reserved-word repertoire | **Val** | the §8.9 funnel (`ParseArm`, COBOLNET0901) → [[kb/Spec/Lookup/Semantic Rules]] |
| **§9.1.13** | I-O status codes | **Run** | two-char `FileStatusCode` on the connectors |
| **§9.4** | User-defined functions | Bind · Gen · Run | `IntrinsicBinder`/UDF resolution |
| **§10** | Structured compilation group | Parse · Bind | repository/prototypes |
| **§11** | Identification / OO structure | Bind · Gen · Run | program→class, CLASS-ID/METHOD → `CobolObject` |
| **§11.9** | OPTIONS paragraph | Bind · Val | `OptionsBinder` (ARITHMETIC/ROUNDED/FLOAT); edition-gated |
| **§12** | Environment (SPECIAL-NAMES, FILE-CONTROL) | Bind (+ Pre) | ALPHABET/CLASS/CURRENCY; SELECT/ASSIGN |
| **§12.4.5.3** | External-file consistency | Bind · Val | COBOLNET1573/1575/1624 (2023) |
| **§13** | Data division | Bind · Run | PICTURE/USAGE/OCCURS/REDEFINES → typed-native → [[kb/IR/Data Flow]] |
| **§13.16 / §13.18** | Data-description clauses | Bind | `DataBinder`, `PicInfo`, `StorageForm` |
| **§13.18** | Report Writer descriptions | Bind · Run | `ReportWriterBinder` → `CobolReport` |
| **§14** | Procedure division | Parse · Bind · Gen · Run | statements: syntax (Parse) → bound (Bind) → C# (Gen) → runtime |
| **§14.4 / §14.5** | Procedures / declaratives | Bind · Gen | PC dispatcher (`DispatchEmitter`) → [[kb/IR/Control Flow]] |
| **§14.6.12/.13** | Exception conditions (EC) | Bind · Run | `EcBinder` → `ExceptionCatalog`/`ExceptionState` |
| **§14.7.4 / §14.7.5** | ROUNDED / ON SIZE ERROR | Run | `CobolRounding` / `TryStore` funnel |
| **§14.9.x** | Individual verbs | Bind · Gen · Run | one binder + emitter + runtime call each → [[kb/Spec/Lookup/IR Mapping]] |
| **§15** | Intrinsic functions | Bind · Run | `IntrinsicCatalog` (arity/edition) → `CobolIntrinsics`/`CobolDate` |
| **§16** | Standard classes (predefined) | Bind · Run | `CobolObject` root, NEW/factory |
| **Annex E** | Added features (by edition) | **Val** | introduction gates (`constructs.json`) |
| **Annex F** | Obsolete / archaic | **Val** | 0902 removed / 0903 obsolete |
| **Annex A** | Implementor / optional elements | Val · Bind | conformance dispositions (`CONFORMANCE.md`) |

## Which phase "owns" each concern
```text
Pre    §6 reference format · §7 COPY/REPLACE/CC/directives
Lex    §8.3 words · literals · PICTURE tokenization
Parse  §8-§16 syntax (superset) -> CST
Bind   §8.4 refs · §8.5/§13 data typing · §8.8 expr/cond · §14 verb semantics · §15 resolution
Val    §4 conformance · §8.9 reserved words · Annex E/F edition gates · FLAG incompatibilities
Gen    §14 control flow (PC dispatcher) · §14.9 verb rendering -> C#
Run    §8.8.1 numerics · §9.1.13 I-O status · §14.6-.9 verb behavior · §14.7 ROUNDED/SIZE · §15 intrinsics
```

## See also
- [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the forward mapping (phase → constructs).
- [[kb/Spec/Lookup/Diagnostics]] — the diagnostic→phase map.
- [[kb/Compiler/Pipeline]] · [[kb/Diagrams/Compiler Pipeline Diagram]]

## Backlinks
- [[kb/Compiler/MOC]] · [[kb/Spec/MOC]] — link here.
