---
title: "PB5 — LANDED (DEVLOG 1124) — the float→fixed quantizer saturated at an ORDIN"
id: PB5
kind: defect
status: landed
severity: BLOCKER
area: numerics
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: false
blocked: false
blocked_by: []
spec_refs: [14.6.13.1.1, 14.6.13.1.6, 14.7.4, 15.4.1, 15.9.4]
tags: [cobolsharp, work, defect]
---

# PB5 — LANDED (DEVLOG 1124) — the float→fixed quantizer saturated at an ORDINARY COBOL magnitude

> **Silent wrong arithmetic in ordinary business ranges — the worst defect this review has surfaced.**
> `CobolIntrinsics.FromDouble` returned a `long` and clamped at `long.MaxValue`; its caller quantizes at
> `ws = max(Receiver.Scale, 9)`, so the clamp bit at **|value| ≈ 9.2 × 10⁹**. Every float-family result at or
> above that magnitude was replaced by the constant **9223372036.85**.
>
> ```cobol
> 01 R PIC 9(12)V99.
>     COMPUTE R = FUNCTION ANNUITY(10000000000 1)      *> §15.9.4 r1b ⇒ exactly 1 + argument-1 = 10000000001.00
>       ON SIZE ERROR ... NOT ON SIZE ERROR ...        *> printed NO SIZE ERROR
>     R = 00922337203685                                *> wrong by 8%
> ```
> `SQRT(1e20)`, `EXP(23.3)`, `ABS` and `MAX` over a COMP-2 all produced that same constant. **A twelve-digit
> money field is routine COBOL**, so this was not an edge case; and there was no diagnostic, because §14.7.4
> never saw an overflow — the value had already been clamped to something that fits.
>
> ⛔ §15.4.1 licenses an implementor-defined **approximation** of the equivalent arithmetic expression under
> native arithmetic. **9223372036.85 is not an approximation of 10000000001.**
>
> **THE FIX IS A TYPE, AND IT IS A CORRECTNESS FIX RATHER THAN A WIDENING FOR COMFORT.** The scaled domain of
> this compiler already IS `Int128` — every `…Scaled` body takes one — and `FromDouble` was the one member of
> that pipeline still returning `long`. At scale 9 the Int128 ceiling is ≈1.7 × 10²⁹, past the 10¹⁸ any PICTURE
> can describe, so the saturation is now unreachable from a declarable receiver rather than merely further away.
> One emit site (`IntrinsicRenderer.RenderFloat`), so the change is contained.
>
> ⚠ **FOUND BY THE PHASE-B REFUTE STAGE, NOT BY THE ADJUDICATOR** — the ANNUITY reviewer overturned a PARTIAL to
> DIVERGES and produced the repro. The adjudicator had checked only small receivers (`V9(4)`, `V9(7)`), where
> the clamp never bites. That is the second time this session the adversarial pass paid for itself on a defect
> the first pass had looked straight at.
>
> **AND A CITATION DEFECT AT THE SAME SITE:** the `FromDouble` comment cited **§14.6.13.1.1 Table 13** for
> EC-ARGUMENT-FUNCTION being fatal. `cite.py --check` FAILS on it (that clause is titled "General"); the real
> one is **§14.6.13.1.6**. It had propagated from the source comment into an agent's adjudication — the
> inherited-citation shape again, and one the quoted-fragment audit could not see because it carries no quote.
>
> **GOLDEN** `conformance:2023/pb5_float_quantize_range` — pins the saturation, deliberately NOT the binary64
> cent that the §15.4.1 approximation legitimately produces on ANNUITY's division.
