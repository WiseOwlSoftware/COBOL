---
title: Lookup — Grammar Constructs
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4
  - src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4
  - src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4
  - src/Cobol.Net.Frontend/Grammar/Core/CobolExpressions.g4
  - tests/version-matrix/constructs.json
tags:
  - cobolsharp
  - spec
  - lookup
---

# Lookup — Grammar Constructs

The COBOL grammar surface, mapped to spec §, the compiler phase that parses it, the IR node it binds to, and the
semantic rule that validates it. The grammar is a **superset** across editions, split into 9 ANTLR fragments (see
[[kb/Compiler/Phases]]); edition legality is a later pass ([[kb/Semantics/Passes]]). The canonical
per-construct edition metadata + citations live in `tests/version-matrix/constructs.json`.

## Divisions
| Construct | Summary | Spec | Compiler / Grammar rule | IR Node | Semantics |
|---|---|---|---|---|---|
| IDENTIFICATION DIVISION | program identity (PROGRAM-ID, comment paras) | §11 | `identificationDivision` | `BoundProgram` | comment-para gating |
| ENVIRONMENT DIVISION | configuration + I/O association | §12 | `environmentDivision` | data/file model | SPECIAL-NAMES/SELECT rules |
| DATA DIVISION | data description (records, tables) | §13 | `dataDivision` (CobolData) | `DataItem`/`RecordLayout` | storage form |
| PROCEDURE DIVISION | executable logic (paras/sections) | §14 | `procedureDivision` (CobolControlFlow) | `BoundParagraph` | flow analysis |

## Sections
| Construct | Summary | Spec | Compiler / Grammar rule | IR Node | Semantics |
|---|---|---|---|---|---|
| CONFIGURATION SECTION | SOURCE/OBJECT-COMPUTER, SPECIAL-NAMES | §12.4.5 | `configurationSection` (CobolSpecialNames) | data model | alphabet/class/currency |
| INPUT-OUTPUT SECTION | FILE-CONTROL / I-O-CONTROL | §12.4.4 | `inputOutputSection` (CobolIO) | file model | SELECT/ASSIGN rules |
| FILE SECTION | FD/SD record descriptions | §13.7 | `fileSection` (CobolData) | file records | record legality |
| WORKING-STORAGE / LOCAL-STORAGE / LINKAGE SECTION | program data by lifetime | §13.7 | `workingStorageSection`,… | fields | scope/lifetime |
| REPORT SECTION | RD report descriptions | §13.18 | `reportSection` (CobolReportWriter) | report groups | RW rules |
| SCREEN SECTION | screen description entries (parse-only) | §13 | `screenSection` (CobolScreen) | — | documented non-support |

## Paragraphs & procedure structure
| Construct | Summary | Spec | Compiler / Grammar rule | IR Node | Semantics |
|---|---|---|---|---|---|
| Paragraph | a named group of sentences; a PC case | §14.4 | `paragraph` | `BoundParagraph` | duplicate-name by symbol |
| Section | a named group of paragraphs | §14.4 | `procedureSection` | `BoundParagraph` (flattened) | section return |
| Sentence | one-or-more statements ended by `.` | §14.4 | `sentence` | `BoundSequence` | sentence boundary |
| DECLARATIVES | USE procedures fired by event | §14.5 | `declaratives`/`useStatement` | `BoundDeclarative` | USE trigger rules |

## Statements (verbs)
| Construct | Summary | Spec | Compiler / Grammar rule | IR Node | Semantics |
|---|---|---|---|---|---|
| MOVE | copy with category conversion | §14.9.25 | `moveStatement` | `BoundMove` | §14.9.25.3 SR1 |
| ADD / SUBTRACT / MULTIPLY / DIVIDE | arithmetic | §14.9.2/.40/.26/.13 | `addStatement`,… | `BoundAddTo`,… | rounding/size-err |
| COMPUTE | expression assignment | §14.9.8 | `computeStatement` | `BoundCompute` | precedence |
| INITIALIZE / SET | field init / index-pointer set | §14.9.20/.37 | `initializeStatement`/`setStatement` | `BoundInitialize`/`BoundSetTo` | category/target |
| IF / EVALUATE | conditional / case | §14.9.18/.15 | `ifStatement`/`evaluateStatement` | `BoundIf`/`BoundEvaluate` | truth/match |
| PERFORM | loop / subroutine | §14.9.28 | `performStatement` (+ inline/until/varying) | `BoundInlinePerform`/`BoundOutOfLinePerform` | range/varying |
| GO TO / ALTER | transfer / alter | §14.9.17/.3 | `goToStatement`/`alterStatement` | `BoundGoTo`/`BoundAlter` | selector / 85-only |
| EXIT / CONTINUE / NEXT SENTENCE | flow no-ops & exits | §14.9.14/.9/.27 | `exitStatement`,… | `BoundExit*`/`BoundNop` | fmt gating |
| SEARCH / SEARCH ALL | table search | §14.9.36 | `searchStatement`/`searchAllStatement` | `BoundSearch` | key ascending |
| STRING / UNSTRING / INSPECT | string ops | §14.9.38/.42/.21 | `stringStatement`,… | `BoundStringStmt`,… | overflow/delimiter |
| DISPLAY / ACCEPT | console I/O | §14.9.12/.1 | `displayStatement`/`acceptStatement` | `BoundDisplay`/`BoundAccept` | operand imaging |
| OPEN/CLOSE/READ/WRITE/REWRITE/DELETE/START | file I/O | §14.9 I-O | `openStatement`,… (CobolIO) | `BoundOpen`,… | FILE STATUS |
| SORT / MERGE / RELEASE / RETURN | ordering | §14.9 SORT | `sortStatement`,… | `BoundSort`/`BoundMerge` | key rules |
| CALL / CANCEL / ENTRY | interprogram | §14.9.7/.6 | `callStatement`,… | `BoundCallProgram`/`BoundCancel` | linkage |
| INVOKE / RAISE / RESUME | OO / EC | §14.9.22/.33/.34 | `invokeStatement`,… (CobolOO) | `BoundInvoke`/`BoundRaise` | resolution / EC |
| INITIATE/GENERATE/TERMINATE/SUPPRESS | Report Writer | §13.18 | RW statements (CobolReportWriter) | `BoundInitiate`,… | control breaks |

## Expressions & conditions
| Construct | Summary | Spec | Compiler / Grammar rule | IR Node | Semantics |
|---|---|---|---|---|---|
| Arithmetic expression | +, -, *, /, ** with precedence | §8.8.1 | `arithmeticExpression` (CobolExpressions) | `BoundBinary`/`BoundPower` | intermediate rounding |
| Relation condition | operand REL operand | §8.8.4.1 | `relationCondition` | `BoundRelational` | comparison by category |
| Combined condition | AND/OR/NOT (+abbreviated) | §8.8.4.2 | `combinedCondition` | `BoundLogical`/`BoundNot` | precedence |
| Class / sign / condition-name | class, sign, 88 tests | §8.8.4.1 | `classCondition`,… | `BoundClassCondition`,… | category / value-set |
| Boolean expression (2002) | bit/boolean operators | §8.8.2 | `booleanExpression` | `BoundBoolBinary`,… | rule-7b precedence |
| FUNCTION reference | intrinsic / user function call | §15 / §9 | `functionReference` | `BoundIntrinsicCall`/`BoundUdfEvaluated` | arity/category window |
| Reference modification | `data(a:b)` substring | §8.5.1.2 | `referenceModifier` (SUBSCRIPT mode) | `RefModPlace` | bounds check |

## Data types (categories & USAGE)
| Construct | Summary | Spec | Compiler / Grammar rule | IR Node | Semantics |
|---|---|---|---|---|---|
| Alphanumeric `PIC X` / `A` | text field → `string` | §13.16 | `pictureClause` | `DataItem` (string) | class ALPHABETIC/ALPHANUMERIC |
| National `PIC N` | UTF-16 national → `string` | §13.16 | `pictureClause` | `DataItem` (string) | national class |
| Numeric `PIC 9`/`S9`/`V`/`P` | fixed-point → `long`/`Int128` | §13.16 | `pictureClause` | `PicInfo` (unscaled) | numeric category |
| Numeric-edited `Z * + - CR DB` | edited display → `string` | §13.16 | `pictureClause` | `DataItem` (edited) | editing rules |
| BINARY/COMP/COMP-3/COMP-5 | packed/binary usage | §13.16 USAGE | `usageClause` | `StorageForm` | usage legality |
| COMP-1 / COMP-2 | IEEE float/double | §13.16 USAGE | `usageClause` | float field | float rules |
| POINTER / OBJECT REFERENCE / INDEX | pointer / object ref / index | §13.16 USAGE | `usageClause` | `ManagedPointer`/`CobolObject`/index | pointer/object typing |
| Boolean `PIC 1` / BIT | boolean → `bool` | §13.16 | `pictureClause` | `DataItem` (bool) | boolean class |
| Group item | nested `record struct` | §13.16 | `dataDescriptionEntry` | `record struct` | group-as-alnum |
| Table `OCCURS` | array (+DEPENDING/DYNAMIC) | §13.16 OCCURS | `occursClause` | `T[]`/`CobolDynTable` | subscript bounds |

## See also
- [[kb/Spec/Lookup/Construct Catalogue]] — the full 183-construct × edition inventory (introduced/removed/diagnostic).
- [[kb/Compiler/Phases]] — the 9 grammar fragments in detail.
- [[kb/Spec/Lookup/Keywords]] · [[kb/Spec/Lookup/IR Mapping]] · [[kb/Spec/Lookup/Semantic Rules]]
- [[kb/Diagrams/Grammar Hierarchy]] — the division→section→statement tree.

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/Compiler/Phases]] — link here.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Parse phase.
