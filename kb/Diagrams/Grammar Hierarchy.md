---
title: Diagram — Grammar Hierarchy
area: diagrams
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - diagram
---

# Diagram — Grammar Hierarchy

The COBOL program hierarchy the parser recognizes (superset grammar, 9 ANTLR fragments). Feeds
[[kb/Spec/Lookup/Grammar]] and [[kb/Compiler/Phases]].

## Program → division → section → paragraph → statement

```mermaid
flowchart TD
    CU["compilationUnit"] --> PROG["program (PROGRAM-ID)"]
    PROG --> ID["IDENTIFICATION DIVISION"]
    PROG --> ENV["ENVIRONMENT DIVISION"]
    PROG --> DAT["DATA DIVISION"]
    PROG --> PRO["PROCEDURE DIVISION"]
    ENV --> CFG["CONFIGURATION SECTION\n(SPECIAL-NAMES)"]
    ENV --> IO["INPUT-OUTPUT SECTION\n(FILE-CONTROL / SELECT)"]
    DAT --> FS["FILE SECTION (FD/SD)"]
    DAT --> WS["WORKING-STORAGE / LOCAL-STORAGE / LINKAGE"]
    DAT --> RS["REPORT SECTION (RD)"]
    DAT --> SS["SCREEN SECTION"]
    PRO --> DECL["DECLARATIVES (USE)"]
    PRO --> SEC["section"]
    SEC --> PAR["paragraph"]
    PAR --> SENT["sentence"]
    SENT --> STMT["statement (verb)"]
    STMT --> EXPR["arithmetic / boolean expression"]
    STMT --> COND["condition"]
```

## Grammar fragments (physical split)

```
CobolParserCore.g4  ── imports ──▶  CobolExpressions · CobolData · CobolSpecialNames
                                    CobolReportWriter · CobolIO · CobolControlFlow
                                    CobolOO · CobolScreen · CobolWords
CobolLexer.g4  ── modes ──▶  DEFAULT · PICMODE · SUBSCRIPT · COMMENT_MODE
```

## Statement families (CobolControlFlow rules → IR)
| Grammar rule | Statement | IR node |
|---|---|---|
| `performStatement` | PERFORM | `BoundInlinePerform` / `BoundOutOfLinePerform` |
| `ifStatement` | IF | `BoundIf` |
| `evaluateStatement` | EVALUATE | `BoundEvaluate` |
| `goToStatement` | GO TO | `BoundGoTo` / `BoundGoToDepending` |
| `searchStatement` / `searchAllStatement` | SEARCH | `BoundSearch` |
| `exitStatement` | EXIT forms | `BoundExit*` |

## See also
- [[kb/Spec/Lookup/Grammar]] · [[kb/Compiler/Phases]]
- [[kb/Diagrams/Compiler Pipeline Diagram]] · [[kb/Diagrams/IR Node Hierarchy]]

## Backlinks
- [[kb/Diagrams/MOC]] · [[kb/Spec/Lookup/Grammar]] — link here.
