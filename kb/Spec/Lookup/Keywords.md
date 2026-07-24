---
title: Lookup — Keywords
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Editions/ReservedWords.Table.cs
  - tests/version-matrix/reserved-words.json
  - specs/ISO_COBOL.md
tags:
  - cobolsharp
  - spec
  - lookup
---

# Lookup — Keywords

A **curated, classified** cross-reference of the significant COBOL keywords. (The full reserved-word repertoire —
~500 words × edition flags — is generated data in `src/Cobol.Net.Editions/ReservedWords.Table.cs` /
`tests/version-matrix/reserved-words.json`, drift-guarded; it is *not* reproduced here. See
[[kb/Semantics/Validation Rules]] for the §8.9 reserved-word funnel.)

Columns: **Spec** = ISO § or [[kb/Spec/Language Features]] · **Compiler** = pipeline/phase note · **IR Node**
= bound node · **Semantic Rule** = [[kb/Spec/Lookup/Semantic Rules]] · **Runtime** =
[[kb/Spec/Lookup/Runtime Mapping]].

## Divisions & sections
| Keyword | Description | Spec | Compiler | IR Node | Semantic Rule | Runtime |
|---|---|---|---|---|---|---|
| IDENTIFICATION DIVISION | program identity (PROGRAM-ID) | §11 | [[kb/Spec/Lookup/Grammar]] | `BoundProgram` | program structure | — |
| ENVIRONMENT DIVISION | config + I/O association | §12 | [[kb/Spec/Lookup/Grammar]] | data model | SPECIAL-NAMES/FILE-CONTROL | — |
| DATA DIVISION | data description | §13 | [[kb/IR/Data Flow]] | `DataItem` | storage form | — |
| PROCEDURE DIVISION | executable logic | §14 | [[kb/IR/Control Flow]] | `BoundParagraph` | flow | PC dispatch |
| WORKING-STORAGE / LOCAL-STORAGE / LINKAGE | storage sections | §13.7 | [[kb/IR/Data Flow]] | fields | scope/lifetime | field storage |
| FILE / REPORT / SCREEN SECTION | FD / RD / screen items | §13 | [[kb/Compiler/Phases]] | file/report nodes | section rules | file/RW |
| SPECIAL-NAMES | switches, ALPHABET, CLASS, CURRENCY | §12.4.5 | [[kb/Compiler/Phases]] | data model | alphabet/class defs | collation |
| FILE-CONTROL / SELECT | file association | §12.4.4 | [[kb/Runtime/Execution Model]] | file model | SELECT rules | connectors |

## Data description clauses
| Keyword | Description | Spec | Compiler | IR Node | Semantic Rule | Runtime |
|---|---|---|---|---|---|---|
| PICTURE / PIC | data category & size | §13.16 PICTURE | [[kb/IR/Data Flow]] | `PicInfo` | category rules | numeric/string |
| USAGE | physical form (DISPLAY/COMP/…) | §13.16 USAGE | [[kb/IR/Data Flow]] | `StorageForm` | usage legality | native types |
| OCCURS | table dimension (+DEPENDING/DYNAMIC) | §13.16 OCCURS | [[kb/IR/Data Flow]] | array/`CobolDynTable` | subscript bounds | table access |
| REDEFINES | storage overlay | §13.16 REDEFINES | [[kb/IR/Data Flow]] | `RedefViewPlace` | 4-tier model | view accessor |
| RENAMES (66) | regroup elementary items | §13.16 RENAMES | [[kb/IR/Data Flow]] | `RenamesPlace` | span rules | composed view |
| VALUE | initial value / 88 value-set | §13.16 VALUE | [[kb/IR/Data Flow]] | init nodes | figurative/THRU | VALUE init |
| BLANK WHEN ZERO / JUSTIFIED / SIGN | edit/justify/sign | §13.16 | [[kb/IR/Data Flow]] | `PicInfo` | edit rules | imaging |
| 88 (condition-name) | named condition over a value-set | §13.16 level-88 | [[kb/Semantics/Validation Rules]] | `BoundCondition88` | VALUE/THRU | bool property |

## Verbs — data movement & arithmetic
| Keyword | Description | Spec | Compiler | IR Node | Semantic Rule | Runtime |
|---|---|---|---|---|---|---|
| MOVE | copy with category conversion | §14.9.25 | [[kb/Spec/Lookup/Grammar]] | `BoundMove` | §14.9.25.3 SR1 | category convert |
| INITIALIZE | set default/replacing values | §14.9.20 | — | `BoundInitialize` | category defaults | field fill |
| ADD / SUBTRACT / MULTIPLY / DIVIDE | arithmetic verbs | §14.9.2/.40/.26/.13 | — | `BoundAddTo`,… | rounding/size-err | `CobolNum` |
| COMPUTE | expression assignment | §14.9.8 | — | `BoundCompute` | precedence | `CobolNum` |
| SET | index/pointer/88/switch assignment | §14.9.37 | — | `BoundSetTo`,… | target legality | index/pointer |

## Verbs — control flow
| Keyword | Description | Spec | Compiler | IR Node | Semantic Rule | Runtime |
|---|---|---|---|---|---|---|
| IF / END-IF | conditional | §14.9.18 | [[kb/IR/Control Flow]] | `BoundIf` | truth-value | branch |
| EVALUATE | multi-branch (case) | §14.9.15 | [[kb/IR/Control Flow]] | `BoundEvaluate` | subject/object match | branch |
| PERFORM | loop / subroutine (THRU/TIMES/UNTIL/VARYING) | §14.9.28 | [[kb/IR/Control Flow]] | `BoundInlinePerform`/`BoundOutOfLinePerform` | range/varying | Dispatch/loop |
| GO TO / DEPENDING | unconditional transfer | §14.9.17 | [[kb/IR/Control Flow]] | `BoundGoTo`/`BoundGoToDepending` | selector GR2 | pc set |
| ALTER | change alterable GO TO (85-only) | §14.9.3 | [[kb/IR/Control Flow]] | `BoundAlter` | removed 2002 | alter field |
| EXIT (PARAGRAPH/SECTION/PERFORM/PROGRAM) | structured exits | §14.9.14 | [[kb/IR/Control Flow]] | `BoundExit*` | fmt gating | pc/return |
| CONTINUE / NEXT SENTENCE | no-op / sentence jump | §14.9.9/.27 | — | `BoundNop`/`BoundNextSentence` | — | goto |
| SEARCH / SEARCH ALL | table search (serial/binary) | §14.9.36 | — | `BoundSearch` | key ascending | scan |
| STOP RUN / GOBACK | terminate run-unit / program | §14.9.39/.16 | [[kb/Runtime/Execution Model]] | `BoundStop`/`BoundGoback` | end semantics | signal |

## Verbs — strings, I/O, calls, OO
| Keyword | Description | Spec | Compiler | IR Node | Semantic Rule | Runtime |
|---|---|---|---|---|---|---|
| STRING / UNSTRING | assemble / split strings | §14.9.38/.42 | — | `BoundStringStmt`/`BoundUnstringStmt` | pointer/overflow | `CobolStringOps` |
| INSPECT | tally/replace/convert | §14.9.21 | — | `BoundInspect*` | before/after | `CobolInspect` |
| DISPLAY / ACCEPT | console / device I/O | §14.9.12/.1 | — | `BoundDisplay`/`BoundAccept` | operand imaging | console |
| OPEN / CLOSE / READ / WRITE / REWRITE / DELETE / START | file verbs | §14.9 I-O | [[kb/Runtime/Execution Model]] | `BoundOpen`,… | FILE STATUS | connectors |
| SORT / MERGE / RELEASE / RETURN | ordering | §14.9 SORT | — | `BoundSort`/`BoundMerge` | key rules | `CobolSort` |
| CALL / CANCEL / ENTRY | interprogram | §14.9.7/.6 | [[kb/Runtime/Execution Model]] | `BoundCallProgram`/`BoundCancel` | linkage | `ICobolProgram` |
| INVOKE | method call (OO) | §14.9.22 | [[kb/Runtime/Execution Model]] | `BoundInvoke` | resolution | dispatch |
| RAISE / RESUME | EC control | §14.9.33/.34 | — | `BoundRaise`/`BoundResume` | EC hierarchy | EC engine |
| INITIATE / GENERATE / TERMINATE / SUPPRESS | Report Writer | §13.18 | [[kb/Runtime/Execution Model]] | `BoundInitiate`,… | control breaks | `CobolReport` |

## Directives & conditional compilation
| Keyword | Description | Spec | Compiler | IR Node | Semantic Rule | Runtime |
|---|---|---|---|---|---|---|
| COPY / REPLACE | text manipulation | §7.2 | [[kb/Compiler/Phases]] | (preprocess) | REPLACING rules | — |
| >>IF / >>DEFINE / >>EVALUATE | conditional compilation | §7.2.1 | [[kb/Compiler/Phases]] | (preprocess) | branch selection | — |
| >>TURN | EC checking on/off | §7.3.13 | [[kb/Semantics/Passes]] | `EcFeatures` | directive fold | EC engine |
| >>COBOL-WORDS | reserved-word table edit | §7.3.10 | [[kb/Semantics/Validation Rules]] | (lex retype) | SR1–SR5 | — |
| >>FLAG-02 / >>FLAG-14 | migration flagging | §7.3.14/.15 | [[kb/Semantics/Passes]] | `FlagConformancePass` | warning only | — |

## See also
- [[kb/Spec/Lookup/Grammar]] — the constructs these keywords form.
- [[kb/Spec/Language Features]] · [[kb/Search/Glossary]]
- [[kb/Semantics/Validation Rules]] — §8.9 reserved-word funnel.

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/Spec/Language Features]] — link here.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Lex phase.
