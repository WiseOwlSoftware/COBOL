---
title: Diagram — Semantic Rule Flow
area: diagrams
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - diagram
---

# Diagram — Semantic Rule Flow

How a semantic rule is enforced from bind to diagnostic. Companion to
[[kb/Diagrams/Semantic Validation Flow]]; feeds [[kb/Spec/Lookup/Semantic Rules]].

## Rule classification & routing

```mermaid
flowchart TD
    RULE["a semantic rule"] --> Q{"edition-dependent?"}
    Q -->|no — invariant| BIND["enforced in the BINDER\n(MoveBinder, data binder, IO binder)"]
    Q -->|yes — conditional| PASS["enforced in VersionConformancePass\n(the ONE edition gate)"]
    BIND --> D1["Error/Warning → IDiagnosticSink\n(e.g. 0809 MOVE class, 0801 digits)"]
    PASS --> ARM{"syntactic or semantic?"}
    ARM -->|syntactic recognition| PARSEARM["ParseArm\nintro/removal/phrase + §8.9 words"]
    ARM -->|resolved bound fact| BOUNDARM["GateStatement/GateMove/GateData"]
    PARSEARM --> CHECK["ConstructRegistry.Check(edition, id)"]
    BOUNDARM --> CHECK
    CHECK --> SEV["EditionSeverityPolicy.For(verdict, edition)"]
    SEV --> D2["0900 intro / 0902 removed / 0901 reserved / 0903 obsolete"]
    D1 --> SINK[("IDiagnosticSink")]
    D2 --> SINK
    SINK --> HALT{"HasErrors?"}
    HALT -->|yes| STOP["HALT — no emit"]
    HALT -->|no| EMIT["EMIT → runtime behavior"]
```

## Invariant vs conditional rules (examples)
| Rule | Kind | Enforced by |
|---|---|---|
| MOVE class legality (SR1) | invariant | `MoveBinder` (0809) |
| MOVE figurative category (SR5) | conditional | `VersionConformancePass.GateMove` |
| Digit capacity | conditional | `CheckDigitCapacity` (0801/0802) |
| EXIT format availability | conditional | `VersionConformancePass` (0900) |
| Condition precedence | invariant | binder |
| Reserved-word §8.9 | conditional | ParseArm funnel (0901) |

## See also
- [[kb/Semantics/Passes]] · [[kb/Semantics/Validation Rules]]
- [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Diagrams/Semantic Validation Flow]]

## Backlinks
- [[kb/Diagrams/MOC]] · [[kb/Spec/Lookup/Semantic Rules]] — link here.
