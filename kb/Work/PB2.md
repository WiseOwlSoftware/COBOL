---
title: "PB2 — LANDED — the ARGUMENT path (DEVLOG 1118); the RECEIVER residue stays o"
id: PB2
kind: defect
status: landed
severity: MAJOR
area: intrinsics
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: false
blocked: false
blocked_by: []
spec_refs: [15.17.4, 15.3, 15.6, 15.71.3, 15.75.4]
tags: [cobolsharp, work, defect]
---

# PB2 — LANDED — the ARGUMENT path (DEVLOG 1118); the RECEIVER residue stays open

> **The renderer now routes on the ARGUMENT's type, not only on the function's family** — one line in
> `IntrinsicRenderer.RenderNum`, plus a floating-point body per exact-family function in
> `CobolIntrinsics.RealArgs.cs`. Five rows close (ABS, ORD-MAX, ORD-MIN, RANGE, REM); GAP 3779 → 3774.
>
> **⛔ IT WAS WORSE THAN THIS ENTRY SAID.** Not "no value, a CS1503, or a silent requantization" as three
> possibilities — for the exact family it was reliably a **raw Roslyn error escaping the compiler on legal
> COBOL**: `CS1503: cannot convert from 'double' to 'System.Int128'`, quoting generated C# the user never asked
> to see. Ten of eleven functions probed did it. That is an INTERNAL failure surfaced as a diagnostic, which is
> worse than a wrong answer.
>
> **⚠ THE FIRST FIX WAS WRONG AND THE CORPUS CAUGHT IT.** The elegant form — give the real bodies the SAME names
> as the exact ones, since `Int128` has no implicit conversion from `double`, and let one dispatch line do
> everything — does not compile. An integer LITERAL converts implicitly to BOTH, so `FUNCTION MAX(5 7)` emitted
> `MaxScaled(5, 7)` and C# reported `CS0121: ambiguous call`, **breaking six previously-green corpus programs
> that never touched a float**. The real bodies therefore carry a `…Real` name, by a CONVENTION rather than a
> table (`XxxScaled` → `XxxReal`, else a `Real` suffix — one string transform in `IntrinsicRenderer.RealMethod`).
>
> **AND THE DRIFT TEST FOUND A GAP THE PROBE MISSED**: `COMBINED-DATETIME`, whose argument-2 §15.6 types `Num2`
> and which may therefore legitimately be a float. Its body is `argument-1 + (argument-2 / 100000)` exactly as
> §15.17.4 r1 writes it (the exact twin encodes the same expression as a scale shift, so the two agree by
> construction).
>
> **RESIDUE — the rows this did NOT fix, each still recorded PARTIAL:** `RV-15.75.4-1` RANDOM, whose defect is
> the fixed-point RECEIVER path (`FromDouble(call, ws)` re-rounding a value §15.75.4 r1 already places in
> `[0,1)`), not the argument path; the standard-arithmetic legs `RV-15.73.3-2` / `-3` (PI under
> standard-binary/standard-decimal) and `RV-15.74.4-1`; and the EC-ARGUMENT-FUNCTION **value** rules
> (`AR-15.74.3-2`, `AR-15.75.3-2`, `AR-15.77.3-2`), which §15.3 makes a RUN-TIME condition and which this
> compile-time change deliberately does not touch.
>
> **GOLDEN:** `conformance:2023/pb2_float_argument_exact_family` — 17 lines, every expected value derived from
> the spec, built around the pairs that distinguish a correct body from a plausible one (MOD floors where REM
> truncates; INTEGER floors where INTEGER-PART truncates). It matched on the first run.
>
> The original finding follows, for provenance.

### PB2 (as found) · a FLOATING-POINT argument falls off the end of the intrinsic result path

> **19 of the batch's 42 open rows cluster here** — the second pattern behind the same 11-function sample, and
> independent of PB1. Where PB1 is "no argument-class rule is enforced", this is "an argument of a class the rule
> ALLOWS is not handled".
>
> The intrinsic result path is written for fixed-point operands. A float argument — legal for every one of these
> functions, since §15.71.3 r1 and its siblings bar only boolean/message-tag/object/pointer/strongly-typed-group —
> variously produces no value at all, a Roslyn `CS1503`, or a silent requantization. Reported instances span
> ORD-MAX/ORD-MIN (`RenderNum`'s `OrdMax or OrdMin` arm calls the scale-aligning path), RANGE, REM, ABS with a
> COMP-2 operand, and RANDOM's fixed-point RECEIVER leg (`FromDouble(call, ws)` re-rounds a value the spec says is
> already in `[0,1)`). PI's `standard-binary` / `standard-decimal` rows (`RV-15.73.3-2`, `-3`) are the same seam
> seen from the arithmetic-mode side, as is PRESENT-VALUE's `RV-15.74.4-1`.
>
> **Do not fix these one function at a time.** The shape of the defect is a missing branch in ONE renderer seam,
> so the fix belongs there, with the per-function rows as its verification set. Rows: `RV-15.7.4-1`,
> `RV-15.71.4-1`/`-2`, `RV-15.72.4-1`/`-2`/`-3`, `RV-15.73.3-1`/`-2`/`-3`, `AR-15.74.3-2`, `RV-15.74.4-1`,
> `AR-15.75.3-2`, `AR-15.75.3-4`, `RV-15.75.4-1`/`-3`, `RV-15.76.4-1`, `AR-15.77.3-2`, `RV-15.77.4-1`,
> `RV-15.78.4-1`.
>
> ⚠ Agent-surfaced, adversarially re-verified, NOT hand-confirmed. Verify before it drives a code change.
