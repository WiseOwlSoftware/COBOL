---
title: Spec Constraints — Conformance, Reference Format, Directives
area: spec
status: draft
last_updated: 2026-07-23
related_files:
  - specs/ISO_COBOL.md
  - docs/CONFORMANCE.md
  - CONSTRAINTS.md
tags:
  - cobolsharp
  - spec
---

# Spec Constraints — Conformance, Reference Format, Directives

The rules a *conforming* COBOL program (and a conforming processor) must obey. These are the spec-side constraints
distinct from the project's engineering doctrine (which lives in [[kb/Context/Doctrine & Anti-Patterns]]).

## The conformance clause (§4)

§4 defines what "conforming" means: a conforming processor accepts and correctly processes the mandatory language of
its claimed edition, documents every processor-dependent choice (§4.2.16 → [[docs/CONFORMANCE]]), and rejects or
warns on unsupported syntactically-detectable elements. The owner target (**D13**) is 100% conformance per §4.2.16
across editions 1985/2002/2014/2023. See [[kb/Spec/Overview]].

## Reference format (§6)

Programs are written in **fixed** or **free** form. Fixed form is column-sensitive: indicator column 7 (`*` comment,
`/` page-eject, `-` continuation, `D` debugging), area A (8–11) for divisions/sections/paragraphs/level indicators,
area B (12–72) for everything else, columns 73+ ignored. Free form drops the column rules. The compiler's
**preprocessor normalizes fixed↔free before lexing**; only the column-aware pass can see the indicator, so
indicator-dependent gates (VCR rows 2/94) fire there. See [[kb/Compiler/Phases]].

## The compiler-directing facility (§7)

- **COPY** (with `REPLACING`) and **REPLACE** — text manipulation before the grammar sees the source.
- **`>>` directives** — `>>DEFINE`, `>>IF`/`>>ELSE`/`>>END-IF`, `>>EVALUATE` (conditional compilation, §7.2.1;
  processed *inside* copybooks), `>>SET`, `>>TURN` (EC checking), `>>COBOL-WORDS` (§7.3.10 reserved-word table
  modification), `>>FLAG-02`/`>>FLAG-14` (§7.3.14/.15 migration flagging).
- Compile-time expression evaluation (§7.3.6 arithmetic / §7.3.7 boolean / §7.3.8 constant-conditional) is shared by
  the conditional-compilation stage and the CONSTANT-entry binder.

## Documented non-support facilities

Five whole facilities are deliberately not implemented (a *documented* non-support, part of the deliverable, not a
waiver): **MCS asynchronous messaging**, **COMMIT/ROLLBACK**, **VALIDATE**, **SCREEN handling**, and the **Locale
facility**. Syntactically-detectable ones are recognized-and-warned (COBOLNET1560 band); the locale module is rejected
at bind (COBOLNET1518 — retired at PB64 T6 with the A.4.9 claim). See [[kb/Semantics/Validation Rules]].

## Engineering constraints (project doctrine)

Separate from the ISO constraints, [[CONSTRAINTS]] catalogs the compiler-development anti-patterns (god objects,
layer violations, scattered dialect flags, primitive obsession, exception-as-control-flow) and the migration phases /
session rituals. These are captured in [[kb/Context/Doctrine & Anti-Patterns]].

## Key concepts
- §4 conformance; §4.2.16 user-documentation obligation.
- Fixed vs free reference format; preprocessor normalizes before lex.
- §7 compiler-directing facility: COPY/REPLACE + the `>>` directive family.
- Five documented non-support facilities (warn vs reject).

## See also
- [[kb/Spec/Overview]] · [[kb/Spec/Version Targeting]]
- [[kb/Compiler/Phases]] — the preprocessor & directive handling.
- [[kb/Context/Doctrine & Anti-Patterns]] — the engineering constraint catalog.

## Backlinks
- [[kb/Spec/MOC]] — indexes this note.
- Lookup: [[kb/Spec/Lookup/Constraints]] — the constraint lookup table.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Preprocess phase (reference format, directives) maps here.
