---
title: Diagram — Compiler Pipeline
area: diagrams
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - diagram
---

# Diagram — Compiler Pipeline

Visualizes [[kb/Compiler/Pipeline]]. Source `.cob` → typed bound tree → C# → Roslyn → `.dll`.

## Flowchart (Mermaid)

```mermaid
flowchart TD
    SRC[["source.cob"]] --> PRE
    PRE["PREPROCESS\nref-format · >>directives · COPY/REPLACE · NIST fixups"] --> LEX
    LEX["LEX (ANTLR)\n4 modes · ZERO→ZERO_ARITH · >>COBOL-WORDS retype"] --> PARSE
    PARSE["PARSE (superset grammar)\nSLL bail → LL recover · 9 fragments"] --> CST[/"CST (CompilationUnitContext)"/]
    CST --> BIND
    BIND["BIND (edition-agnostic)\nsymbols · Place · types · categories"] --> BT[/"BOUND TREE = all semantics\n(no lowered IR)"/]
    BT --> VCP
    VCP{"VersionConformancePass\nSOLE edition gate · 2 arms"}
    VCP -->|"errors"| HALT["HALT — no codegen"]
    VCP -->|"clean"| FLAG["FlagConformancePass\n(>>FLAG-02/14 warnings)"]
    FLAG --> EMIT
    EMIT["EMIT via ICodeGenBackend\n--backend roslyn|cil"] --> CS[/".g.cs (idiomatic C#)"/]
    CS --> ROS["Roslyn CSharpCompilation"]
    ROS --> ASM[["assembly (.dll + PDB)"]]
    ASM -.calls.-> RT[("Cobol.Net.Runtime\nCobolNum · CobolString · files · intrinsics")]
```

## The 6 phases (table)

| # | Phase | Input → Output | Project |
|---|---|---|---|
| 1 | Preprocess | text → normalized text | `Cobol.Net.Frontend/Preprocessor` |
| 2 | Lex | text → token stream | `Cobol.Net.Frontend` (ANTLR lexer) |
| 3 | Parse | tokens → CST | `Cobol.Net.Frontend` (superset parser) |
| 4 | Bind | CST → **bound tree** | `Cobol.Net.Compiler/Binding` |
| 5 | VersionConformancePass | bound tree → gated (HALT on error) | `Cobol.Net.Compiler/Validation` |
| 6 | Emit | bound tree → C# → assembly | `Cobol.Net.Compiler/CodeGen/Roslyn` |

## Invariants shown
- **No lowered IR** — one bound tree flows straight to the backend.
- **Edition gate is its own pass** and **halts before emit** on any error.
- **Emitters only render** — semantics are fixed at bind.
- Selectable backend (`roslyn` default; `cil` future) over the same bound tree.


## Annotated pipeline (ISO constructs · rules · IR)

Each phase, annotated with the ISO clause it centres on, the rule it enforces, and the IR it touches. The full
per-phase detail is in [[kb/Compiler/Pipeline-to-ISO-Mapping]].

```mermaid
flowchart TD
    PRE["0 · PREPROCESS<br/>ISO: §6 ref-format · §7 COPY/REPLACE/>>directives<br/>Rule: col-7 indicator gates (VCR 2/94)<br/>IR: none (text + Turn/Flag/Words carriers)"]
    LEX["1 · LEX<br/>ISO: §8.3 words/literals/PICTURE<br/>Rule: §8.3.2 word-formation (63-char 2023)<br/>IR: token stream"]
    PARSE["2 · PARSE (superset)<br/>ISO: §11–§16 syntax formats<br/>Rule: context-free only (edition deferred)<br/>IR: CST"]
    BIND["3 · BIND (AST+IR)<br/>ISO: §8.4 refs · §13 data · §14.9 verbs<br/>Rule: §14.9.25.3 SR1 MOVE · §8.8.4 precedence<br/>IR: bound tree + Place"]
    VAL["4 · VALIDATE (edition gate)<br/>ISO: Annex E/F · §8.9 · §4 conformance<br/>Rule: 0900 intro / 0902 removed / 0901 reserved<br/>IR: gated bound tree — HALT on error"]
    OPT["5 · OPTIMIZE<br/>delegated → C# compiler + JIT<br/>Rule: preserve observable behavior<br/>IR: (unchanged)"]
    GEN["6 · CODEGEN (Roslyn)<br/>ISO: §14 verb semantics → C#<br/>Rule: PC dispatch · loud-failure (D8)<br/>IR: bound tree → .g.cs → assembly"]
    RUN["7 · RUNTIME<br/>ISO: §8.8 · §9.1.13 · §14.6–.9 · §15<br/>Rule: ROUNDED §14.7.4 · FILE STATUS · EC Table 13<br/>IR: node → runtime call"]
    PRE --> LEX --> PARSE --> BIND --> VAL --> OPT --> GEN --> RUN
    VAL -. "0900/0902/0901" .-> STOP["diagnostics → HALT if error"]
```

### Phase → construct · rule · IR node (quick table)

| Phase | ISO constructs | Rules enforced | IR nodes / links |
|---|---|---|---|
| Preprocess | COPY/REPLACE, `>>` directives (§7) | ref-format §6 | — → [[kb/Spec/Lookup/Constraints]] |
| Lex | words, literals, PICTURE (§8.3) | word-formation §8.3.2 | tokens → [[kb/Spec/Lookup/Keywords]] |
| Parse | divisions/statements/expr (§11–§16) | syntax formats | CST → [[kb/Spec/Lookup/Grammar]] |
| Bind | data items, verbs, refs (§13/§14/§8.4) | MOVE SR1, precedence, category | `Bound*`, `Place` → [[kb/Spec/Lookup/IR Mapping]] |
| Validate | edition-varying constructs (Annex E/F) | 0900/0901/0902/0903 | gate → [[kb/Spec/Lookup/Semantic Rules]] |
| Codegen | all verbs (§14) | PC dispatch, loud-failure | render → [[kb/Spec/Lookup/IR Mapping]] |
| Runtime | numerics/files/intrinsics/EC (§8.8/§9/§14/§15) | ROUNDED, FILE STATUS, EC | calls → [[kb/Spec/Lookup/Runtime Mapping]] |

**Full mapping:** [[kb/Compiler/Pipeline-to-ISO-Mapping]].

## See also
- [[kb/Compiler/Pipeline]] · [[kb/Compiler/Phases]]
- [[kb/Diagrams/IR Graph Overview]] · [[kb/Diagrams/Semantic Validation Flow]]

## Backlinks
- [[kb/Diagrams/MOC]] · [[kb/Index]] — link here.
