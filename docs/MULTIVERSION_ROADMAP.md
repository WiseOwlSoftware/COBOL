# CobolSharp — Multi-Version Roadmap (ISO COBOL 1985 → 2023)

Status: 2026-06-04. **This is the overarching mission document.** The COBOL-85 work
(`docs/COBOL85_COMPLIANCE_PLAN.md`) is **Milestone 1** of this roadmap, not a separate goal.

---

## 0. Mission

Build **one compiler that supports every ISO COBOL standard from 1985 to 2023**, with the target version
**selected on the command line** and gated end-to-end through a single version/dialect engine. A user compiles
legacy '85 code with `--standard cobol85` and modern code with `--standard cobol2023`, from the same binary,
with the compiler accepting exactly that version's language, applying that version's obsolete/removed rules, and
emitting that version's diagnostics.

COBOL-85 is Milestone 1 **because NIST CCVS85 is the only external conformance suite that exists for any COBOL
version.** It anchors correctness. Versions 2002/2014/2023 have no NIST equivalent, so each ships with a
**custom conformance corpus** authored from the ISO spec (the repo carries ISO/IEC 1989:2023 at
`specs/ISO_COBOL.md`, the authority for the forward deltas).

---

## 1. The version option — what exists today (the spine)

Already wired (verified 2026-06-04):

- **CLI:** `--standard <version>` — values `default | cobol85 | cobol2002 | cobol2014 | cobol2023`
  (default `cobol85`; `--nist` implies `default`). `src/CobolSharp.CLI/Program.cs`.
- **Model:** `enum DialectMode { Default=0, StrictCobol85=85, Cobol2002=2002, Cobol2014=2014, Cobol2023=2023 }`
  — **ordered for numeric comparison** so checks read `Dialect >= Cobol2002`. `CompilationOptions.cs`
  (`Dialect`, `IsCobol2002OrLater`, `DialectName`).
- **Enforcement:** `DialectStrictnessChecks.cs` — a handful of obsolete/non-conforming gates (the L1–L5 CCVS
  leniency registry, `docs/dialect-strictness.md`).

**Gap:** the engine is *thin*. `DialectMode` gates a few checks but does not yet model each version's full
feature surface — source format, reserved-word set, intrinsic-function set, grammar acceptance, runtime feature
availability. Deepening it into a real per-version feature model is the foundational forward workstream below.

---

## 2. Milestones

| # | Milestone | Validation | Status |
|---|---|---|---|
| **M0** | **Version engine** — deepen `DialectMode` into a full per-version feature model | unit tests on the config matrix | foundation (thin today) |
| **M1** | **COBOL-85 to 100%** | NIST CCVS85 (3 axes — see `COBOL85_COMPLIANCE_PLAN.md`) | in progress (~87% baseline, Wave 1 running) |
| **M2** | **COBOL-2002** | custom corpus | not started |
| **M3** | **COBOL-2014** | custom corpus | not started (Report Writer already done) |
| **M4** | **COBOL-2023** | custom corpus (vs in-repo spec) | not started |

M0 underpins all of M2–M4 and is partly reusable by M1's WS-DIALECT (the removed-feature flagging is the same
engine viewed from the '85 side). **M1 continues uninterrupted; M0 is the bridge to the forward milestones.**

---

## 3. M0 — Version engine (WS-VERSION-ENGINE)

Elevate the thin enum into a canonical, single-source-of-truth **`DialectConfig`** resolved from `DialectMode`,
exposing per version:

- **Source format** — default + allowed reference format. Fixed-only (`85`); fixed **and free-form** (`2002+`),
  selectable via `>>SOURCE FORMAT` directive and/or CLI.
- **Reserved-word set** — each version adds reserved words (e.g. `OBJECT`, `INVOKE`, `FUNCTION-ID`, `BIT`,
  `DYNAMIC`, `VALIDATE`). The lexer/parser consult the active set so a 2014 keyword is a valid identifier under
  `--standard cobol85`.
- **Intrinsic-function set** — the functions legal in that version (IF module grows 85→2023).
- **Feature flags** — OO, user-defined functions, national data, bit/boolean, dynamic-capacity tables, pointers,
  conditional compilation, VALIDATE, TYPEDEF, recursion, standard floating point, … Each consumed by the binder.
- **Obsolete/removed policy** — the existing `DialectStrictnessChecks` rules, reorganized as data on the config:
  per construct, per version, one of {accept · accept+flag-obsolete · flag-removed · reject}.

**Design rule (project doctrine):** one canonical dispatch — never `if (Dialect >= …)` scattered across call
sites. Callers ask the config (`config.SupportsFreeForm`, `config.IsReserved(word)`, `config.AllowsObject`).
This is the refactor-first/centralized-logic principle applied to versioning.

**Done:** every version-conditional behavior in the compiler reads from `DialectConfig`; a unit-test matrix
pins each version's feature set; `--standard` selects it end-to-end (lexer → parser → binder → diagnostics).

---

## 4. M2 — COBOL-2002 (the largest delta)

ISO/IEC 1989:2002 was the big modernization. Workstreams (each grammar + binder + runtime + corpus, integrated
sequentially guard-gated). Ordered by foundational value and contained-ness:

1. **WS-2002-FORMAT** — free-form source format; inline comments `*>`; `>>` compiler directives
   (`>>SOURCE FORMAT`, `>>DEFINE`, `>>IF/>>ELSE/>>END-IF` conditional compilation, `>>CALL-CONVENTION`).
   Foundational and self-contained — do first.
2. **WS-2002-UDF** — user-defined functions (`FUNCTION-ID`, function prototypes, `FUNCTION` invocation of
   user functions). Extends the existing intrinsic-function pipeline.
3. **WS-2002-DATA** — national character data (`USAGE NATIONAL`, `N"…"` literals, UTF-16); boolean & bit data;
   standard floating point (`FLOAT-SHORT/LONG/EXTENDED`); pointer data + based addressing (`ADDRESS OF`,
   `SET … ADDRESS OF`).
4. **WS-2002-PROC** — procedural additions: full `EXIT PARAGRAPH/SECTION/PERFORM/PERFORM CYCLE`, recursion
   (`RECURSIVE`), enhanced `EVALUATE`/`INITIALIZE`, `ARITHMETIC IS STANDARD`, in-line method/standard-call.
5. **WS-2002-VALIDATE** — the data-validation facility (`VALIDATE` statement, validation clauses).
6. **WS-2002-SCREEN** — standardized SCREEN SECTION + `ACCEPT/DISPLAY` screen forms (some screen support exists).
7. **WS-2002-OO** — **Object-Oriented COBOL**: `CLASS-ID`, `METHOD-ID`, `INTERFACE-ID`, `FACTORY`/`OBJECT`,
   inheritance, polymorphism, `INVOKE`, object references. **The single largest sub-project** — a whole object
   model mapped onto .NET types. Scope/sequencing is an owner decision (see §7).

## 5. M3 — COBOL-2014

- **WS-2014-DYNTABLE** — dynamic-capacity tables (`OCCURS DYNAMIC CAPACITY`).
- **WS-2014-TYPEDEF** — type declarations (`TYPEDEF`, `TYPE TO`, `SAME AS`, `USAGE <typedef>`).
- **WS-2014-SHARING** — file sharing & record locking (`SHARING`, `LOCK MODE`, `RETRY`).
- **WS-2014-RW** — Report Writer as the standardized optional module — **already implemented (M1 RW work)**;
  here it is just re-pointed under the 2014 dialect flag.
- **WS-2014-MISC** — function/method pointers, IEEE-754 alignment, increased limits, conditional-expression
  enhancements.

## 6. M4 — COBOL-2023

Audited directly against the in-repo `specs/ISO_COBOL.md` (the precise 2023 delta is itself a Wave-0 audit task,
mirroring M1's WS-SPEC). Known buckets:

- **WS-2023-FUNC** — new/enhanced intrinsic functions (e.g. `CONVERT`, `FIND-STRING`, `SUBSTITUTE` growth, …).
- **WS-2023-BIT** — bit-data manipulation & boolean operations.
- **WS-2023-MISC** — dynamic-table finalization + assorted clarifications/refinements the spec introduces.

## 7. M2–M4 cross-cutting — WS-CORPUS-FWD

No NIST suite exists past '85. Each forward feature ships with a **custom conformance test** under
`tests/conformance/<version>/`, run under the matching `--standard`, asserting spec-defined output. The corpus
is authored from `specs/ISO_COBOL.md` per feature (the forward analogue of M1's WS-SPEC), and feeds the
compliance dashboard's forward axis. This is the forward equivalent of the NIST backbone.

---

## 8. Owner decisions — RESOLVED (2026-06-04)

- **Sequencing → "Engine now, features after M1".** Build M0 (version engine) now, in parallel with M1 — it is
  the shared spine and M1's own WS-DIALECT already needs it to flag removed features under `cobol2002+`. Hold
  COBOL-2002+ *feature* work (M2 WS-2002-* …) until M1 reaches 100% across all three axes.
- **OO scope → "Non-OO first, OO later".** When forward features begin, implement the procedural/non-OO 2002
  surface first (free-form, directives, UDFs, national/bit/float/pointer data, VALIDATE, SCREEN); WS-2002-OO is a
  dedicated later sub-milestone (task #11 is blocked on #10).

---

## 9. Execution model (unchanged across milestones)

Parallel design/audit → worktree-isolated parallel implementation → sequential, guard-gated integration onto
`main`. Every version-conditional behavior reads from `DialectConfig` (one canonical dispatch). Guard stays ALL
GREEN; every commit ≥1 DEVLOG entry. The compliance dashboard grows a per-version column so "% to 100%" is
measurable for each standard, not just '85.
