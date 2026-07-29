---
title: Key Concepts (Semantic Index)
area: search
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - search
---

# Key Concepts (Semantic Index)

A concept-first index into the vault. Start from an idea, jump to the note that develops it.

## The defining ideas
1. **Typed-native, no byte substrate** — record→`record struct`, elementary→native field, program→class. →
   [[kb/Architecture/High-Level Design]], [[kb/IR/Data Flow]]
2. **Bound tree is the only IR** — no lowered basic-block/SSA IR; emitters only render. →
   [[kb/IR/Node Types]], [[kb/Compiler/Pipeline]]
3. **Native numerics** — unscaled `long`/`Int128`, scale as compile-time metadata; no `decimal`. →
   [[kb/Runtime/Execution Model]]
4. **PC-dispatcher control flow** — paragraphs are program-counter cases, not methods. →
   [[kb/IR/Control Flow]]
5. **Idiomatic, readable C# output** — correctness over prettiness for irregular flow. →
   [[kb/Architecture/High-Level Design]]
6. **Four compilers in one** — `--std 85|2002|2014|2023`, one two-arm edition gate. →
   [[kb/Spec/Version Targeting]], [[kb/Semantics/Passes]]
7. **Spec is the sole authority** — cite the ISO § before implementing. →
   [[kb/Context/Goals]], [[kb/Spec/Overview]]
8. **Singular pattern** — one best mechanism per job (`Place`, PC dispatcher, `VersionConformancePass`). →
   [[kb/Context/Doctrine & Anti-Patterns]]

## Index by question
| I want to understand… | Go to |
|---|---|
| How a `.cob` file becomes a `.dll` | [[kb/Compiler/Pipeline]] |
| Where semantics live | [[kb/IR/Node Types]] |
| How COBOL types map to .NET | [[kb/IR/Data Flow]] |
| How GO TO / PERFORM / ALTER work | [[kb/IR/Control Flow]] |
| How editions are enforced | [[kb/Semantics/Passes]] |
| What diagnostics exist & how they fire | [[kb/Semantics/Validation Rules]] |
| How numbers/strings/files/CALL run | [[kb/Runtime/Execution Model]] |
| What the assemblies are | [[kb/Architecture/Module Overview]] |
| The ISO spec structure | [[kb/Spec/Overview]] |
| The implemented language surface | [[kb/Spec/Language Features]] |
| Where the project is going | [[kb/Modernization/Tasks]] |
| How we got here | [[kb/Context/Project History]] |
| The build & test machinery | [[kb/Compiler/Build System]] |

## Index by subsystem keyword
- **Preprocessor / COPY / `>>directives`** → [[kb/Compiler/Phases]]
- **ANTLR / grammar fragments / lexer modes** → [[kb/Compiler/Phases]]
- **`Place` / `ReferenceResolver` / REDEFINES** → [[kb/IR/Data Flow]]
- **`CobolNum` / rounding / ON SIZE ERROR** → [[kb/Runtime/Execution Model]]
- **INSPECT / STRING / UNSTRING / ref-mod** → [[kb/Runtime/Execution Model]]
- **FILE STATUS / connectors / SORT** → [[kb/Runtime/Execution Model]], [[kb/Semantics/Validation Rules]]
- **Intrinsics (§15)** → [[kb/Runtime/Execution Model]], [[kb/Spec/Language Features]]
- **CALL / linkage / `ManagedPointer`** → [[kb/Runtime/Execution Model]]
- **OO / INVOKE / `CobolObject`** → [[kb/Runtime/Execution Model]]
- **EC / declaratives / `>>TURN`** → [[kb/Runtime/Execution Model]]
- **Report Writer / `CobolReport`** → [[kb/Runtime/Execution Model]]
- **Reserved words / §8.9 funnel** → [[kb/Semantics/Validation Rules]]
- **VCR / version matrix / edition gating** → [[kb/Spec/Version Targeting]]
- **Audits / CA*/V* / fix queue** → [[kb/Modernization/Audit Artifacts]]


## Index by ISO lookup
Start from a language element and cross-reference every layer via the lookup tables:
- **A keyword** → [[kb/Spec/Lookup/Keywords]]
- **A grammar construct** (division/section/statement/expression/data type) → [[kb/Spec/Lookup/Grammar]]
- **A semantic rule** → [[kb/Spec/Lookup/Semantic Rules]]
- **An IR node** (`Bound*`) → [[kb/Spec/Lookup/IR Mapping]]
- **A runtime behavior** → [[kb/Spec/Lookup/Runtime Mapping]]
- **A constraint / limit** → [[kb/Spec/Lookup/Constraints]]
- **The master index + cross-domain map** → [[kb/Spec/Lookup/Index]]

## See also
- [[kb/Search/Glossary]] · [[kb/Search/Frequently Asked Questions]]

## Backlinks
- [[kb/Search/MOC]] · [[kb/Index]] — link here.
