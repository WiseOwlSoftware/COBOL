# Dialect & Strictness Model

> **STATUS:** Dialect infrastructure design / in progress (DEVLOG 254–269, 2026-05-31). See docs/MASTER_PLAN.md §3 Phase A for current implementation status of leniency gating. Stack: .NET 10 / C# 14.

**Status:** design / in progress (2026-05-31, DEVLOG 254)
**Audience:** anyone touching the grammar, the binder, or the NIST guard.

## Why this document exists

CobolSharp's long-term goal is to support multiple COBOL standards selectable from the
command line (`--standard cobol85|cobol2002|cobol2014|cobol2023`). At the same time, the
NIST CCVS85 conformance suite — our primary regression corpus — contains a small number of
**non-conformant constructs** (e.g. `REWRITE rec INVALID GO TO …`, with the required `KEY`
keyword dropped). Accepting those constructs to pass the suite, and enforcing a strict modern
standard, look like opposing requirements. They are not — *if* leniencies are modelled as a
**separate axis** from standard version. This document defines that model so the two goals stay
compatible and leniencies never silently leak into strict-conformance mode.

## Two orthogonal axes

| Axis | Question it answers | CLI surface | Direction over time |
|------|---------------------|-------------|---------------------|
| **Version / features** | *Which language features are legal?* | `--standard <ver>` → `CompilationOptions.Dialect` | Additive: newer standards add features, delete a few (e.g. ALTER in 2002+). |
| **Strictness / leniency** | *How tolerant of non-conformant / vendor syntax?* | `Default` (permissive) vs named-strict modes; future per-feature flags | Tightening: as the compiler matures, fewer relaxations are on by default. |

The trap to avoid: treating "support the latest spec" as "accept more." It is the **opposite** —
`--standard cobol2023` means *enforce exactly the 2023 feature set*, which makes the compiler
**stricter** about CCVS-era deviations, not looser. The CCVS deviations live entirely on the
strictness axis and must not be conflated with the version axis.

## What already exists (the seam)

The version axis is already scaffolded and wired:

- `Semantics/CompilationOptions.cs` — `DialectMode` enum, **ordered** so numeric comparison works:
  `Default(0) < StrictCobol85(85) < Cobol2002 < Cobol2014 < Cobol2023`. Plus `WarnNonStandard`,
  `IsCobol2002OrLater`, and `DialectName` (for diagnostics).
- `CLI/Program.cs` — `--standard <ver>` parses to a `DialectMode` (CLI default string: `cobol85`
  → `StrictCobol85`).
- `CompilationOptions` is threaded through `Binder`, `BindingContext`, `BoundTreeBuilder`,
  `LoweringContext`, and `Compilation`.

`Default` mode is documented as "permissive mode, accepts vendor extensions." **That is the home
for CCVS leniencies.** The named modes (`StrictCobol85` … `Cobol2023`) are strict.

## The validator pattern (how a leniency is implemented)

Do **not** fork the grammar per dialect, and do **not** bake an unconditional relaxation into the
grammar. Instead, for every non-conformant form we choose to tolerate:

1. **Grammar parses the permissive superset.** Make the missing keyword optional in the rule
   (e.g. `INVALID KEY? imperativeStatement`), and arrange for the parse tree to record whether
   the keyword was actually present (optional-token presence).
2. **A post-parse / binding validation consults `Dialect`:**
   - `Default` (permissive): accept. (Optionally an info note when `WarnNonStandard`.)
   - `>= StrictCobol85`: emit a diagnostic — **error**, or **warning** if `WarnNonStandard` —
     citing the construct and the active `DialectName`.
3. **Run the NIST/CCVS suite under `Default`** so the relaxations have a home that does not
   contaminate strict conformance.

This mirrors how GnuCOBOL works: parse a permissive superset, then a per-`-std` configuration maps
each construct to ok / warning / error.

### The discipline rule (non-negotiable)

> **Every leniency is dialect-gated from the moment it is added** — accepted only in `Default`,
> diagnosed under named-strict modes. Never an unconditional grammar relaxation.

If even one relaxation is baked in unconditionally, a future `--standard cobol2023` will silently
accept non-conformant code, and we will be hunting for it later. The cost of doing it right is one
validator check per leniency; the `Dialect` plumbing already makes that cheap.

## Risk classes of leniency

Not all relaxations carry equal parse-ambiguity risk; this affects how readily each should be
adopted:

- **Reserved-word-anchored (LOW risk).** The relaxation only drops a *noise word* after a token
  that is already an unambiguous reserved word. Example: `INVALID KEY?` — `INVALID` is reserved
  and cannot be a user data-name, so making `KEY` optional after it changes nothing else.
- **Data-name-anchored (HIGHER risk).** The relaxation drops `KEY` where a *data-name* follows,
  so the lenient form can collide with an unrelated construct. Examples: `RELATIVE data-name`
  (vs the `RELATIVE` organization keyword) and `RECORD data-name` (vs other `RECORD` clauses).
  Adopt these only with care, and prefer the strict diagnostic to be an error by default.

## Registry of known CCVS non-conformant constructs

Counts are occurrences in `tests/nist/extracted/newcob.val` (the upstream CCVS master). Each is a
genuine authoring deviation, not an extraction artifact (verified in the master). For scale: the
conformant `INVALID KEY` appears **1,490** times — the deviations are a ~0.7% errata rate that the
1993-era validating compilers happened to tolerate (they treated the dropped keyword as optional
noise), which is why the suite still "passed."

| # | Construct | Conformant form | Spec basis | Risk | Count | Programs |
|---|-----------|-----------------|-----------|------|-------|----------|
| L1 | `INVALID` / `NOT INVALID` without `KEY` (READ/WRITE/REWRITE/START/DELETE) | `INVALID KEY` | KEY unbracketed in the statement formats → required (ISO §14.9.x; ANSI-85) | LOW (reserved-word-anchored) | 10 | IX108A, RL109A, RL118A, RL207A |
| L2 | `RELATIVE data-name` without `KEY` (FILE-CONTROL) | `RELATIVE KEY IS data-name` | ISO §12.4.5.13 | HIGHER (data-name-anchored) | 14 | RL104A, RL109A, RL112A–119A, RL204A |
| L3 | `RECORD data-name` without `KEY` (indexed FILE-CONTROL) | `RECORD KEY IS data-name` | ISO §12.4.5.12 | HIGHER (data-name-anchored) | 7 | IX103A, IX104A, IX108A, IX203A, IX204A, IX216A |
| L4 | `USE … AFTER … ERROR` without `STANDARD` | `USE … AFTER STANDARD … ERROR PROCEDURE` | ISO §14.9.49 | LOW–MED | many | DB104A, IC233A/234A, IX204A/208A, RL113A–119A, … |

Notes:
- **L1** is the first to be implemented (DEVLOG 254): lowest risk, and it gates the RL update/delete
  producer chains (RL109A→RL110A, RL206A→RL207A→RL208A) at the *compile* step.
- **L2** is implemented (DEVLOG 255): `relativeKeyClause : RELATIVE KEY? IS? dataReference`, gated by
  CBL3613/3614 — unblocked the relative DYNAMIC delete/read chains.
- **L3** is implemented (DEVLOG 269): `recordKeyClause : RECORD KEY? IS? dataReference` and
  `alternateKeyClause : ALTERNATE RECORD? KEY? IS? …`, gated by CBL3615/3616
  (`SemanticBuilder.CheckRecordKeyNoiseWord`). Disambiguation from `recordDelimiterClause` / FD
  `RECORD CONTAINS|VARYING` holds because those second-words are reserved tokens (can't match the key
  clause's dataReference). Paired with indexed READ-NEXT runtime correctness, it yielded 12 IX baselines.
- **L4** is partially in place already (the grammar currently treats `STANDARD` as optional in USE);
  it should be re-audited and routed through the same dialect gate rather than left unconditional.

## How this maps onto the "latest spec" goal

With the pattern above:

- `--standard cobol2023` → CCVS `INVALID GO TO` is **rejected**; conformance is clean.
- `--standard default` (NIST runs) → the same line is **accepted** as a documented relaxation.
- Same source, two answers, no contradiction — which is the entire point of a multi-standard compiler.

A useful side effect: implementing each leniency as a dialect-gated relaxation **forces the
strict-mode diagnostics into existence**, which is itself progress toward strict modern conformance.

The heavy lifting for "latest spec support" remains on the **version axis** — implementing
2002/2014/2023 syntax + semantics and gating each on `Dialect >= …`, plus the standard's deletions.
The leniency axis described here is small and orthogonal; this document exists mainly to keep it
*from* polluting that work.

## Open decisions (not blocking L1)

- **Default mode for normal users.** GnuCOBOL ships a permissive default; CobolSharp's CLI currently
  defaults to `cobol85` → `StrictCobol85`. Whether the friendly default is permissive (opt-in
  strictness) or strict (opt-in leniency) is a product call, deferred. NIST runs use `Default`
  regardless.
- **Per-feature flags.** If the leniency set grows, add GnuCOBOL-style per-construct flags
  (`-frelax-…`) or a dialect-config table mapping each construct to ok/warning/error, rather than
  overloading the single `Dialect` enum.
- **`--nist` ⇒ `Default`.** Simplest wiring: have the existing `--nist` flag imply `Default` mode
  so the guard need not also pass `--standard default`. (Chosen for the L1 implementation.)
