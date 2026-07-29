---
title: Diagram — Semantic Validation Flow
area: diagrams
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - diagram
---

# Diagram — Semantic Validation Flow

Visualizes [[kb/Semantics/Passes]] and [[kb/Semantics/Validation Rules]] — the edition-gating and
diagnostics path.

## The two-arm edition gate (Mermaid)

```mermaid
flowchart TD
    BIND["BIND (edition-agnostic)\nBoundProgram + raw parse tree"] --> VCP
    subgraph VCP["VersionConformancePass — the SOLE edition gate"]
        A1["Parse-tree arm (ParseArm)\nsyntactic intro/removal/phrase gates\n+ §8.9 reserved-word funnel"]
        A2["Bound-tree arm (GateStatement/GateMove/GateData)\nresolved semantic facts:\nMOVE category · USAGE · file-org · pointer"]
    end
    A1 --> CHK
    A2 --> CHK
    CHK{{"ConstructRegistry.Check(edition, id)\nStatusAt(year) verdict"}}
    CHK --> POL
    POL{"EditionSeverityPolicy.For(verdict, edition)"}
    POL -->|NotYetIntroduced| E0900["Error 0900"]
    POL -->|"Removed (strict)"| E0902["Error 0902"]
    POL -->|"Removed (permissive)"| W0902["Warning 0902"]
    POL -->|Obsolete| W0903["Warning 0903"]
    POL -->|reserved word §8.9| E0901["Error 0901"]
    E0900 --> SINK
    E0902 --> SINK
    W0902 --> SINK
    W0903 --> SINK
    E0901 --> SINK
    SINK[("IDiagnosticSink")] --> GATE
    GATE{"HasErrors?"}
    GATE -->|yes| HALT["HALT — no codegen"]
    GATE -->|no| FLAG["FlagConformancePass\n>>FLAG-02/14 → Warning 1620/1621"]
    FLAG --> EMIT["EMIT"]
```

## The reserved-word funnel (§8.9)

```
every user word ──► cobolWord rule ──► ParseArm.VisitCobolWord
                                          │
              position-blind (CheckedTokenTypes)  ── OR ──  position-aware (IsProvableUserWordPosition)
                                          │
                     ReservedWordSet  (4 sources: ISO §8.9 · VCR row32/Annex E.2 · GnuCOBOL 85/2002/2014)
                       + >>COBOL-WORDS overlay
                                          │
                   Confidence == "high"  ─► reject (0901)     (no-false-reject policy)
```

## Diagnostic descriptor anatomy

| Field | Meaning | Example |
|---|---|---|
| `Code` | emitted, may repeat | `COBOLNET0900` |
| `Id` | stable kebab-case, survives renumber | `construct-requires-edition` |
| `Severity` | Error / Warning / Info | Error |
| `IsoSection` | citation | §8.9 |
| `SuppressKey?` | suppress family | `recognized-not-implemented` |

## See also
- [[kb/Semantics/Passes]] · [[kb/Semantics/Validation Rules]]
- [[kb/Spec/Version Targeting]] · [[kb/Diagrams/Compiler Pipeline Diagram]]

## Backlinks
- [[kb/Diagrams/MOC]] · [[kb/Index]] — link here.
