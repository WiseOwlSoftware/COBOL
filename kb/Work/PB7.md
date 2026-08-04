---
title: "PB7 — LANDED (DEVLOG 1129) — every ZERO-ARGUMENT intrinsic was unreachable i"
id: PB7
kind: defect
status: landed
severity: BLOCKER
area: intrinsics
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: false
blocked: false
blocked_by: []
spec_refs: [12.3.8.1, 15.21.2, 8.4.3.2.3]
tags: [cobolsharp, work, defect]
---

# PB7 — LANDED (DEVLOG 1129) — every ZERO-ARGUMENT intrinsic was unreachable in the keyword-omitted form, and it compiled clean

> **Silent compile-then-crash, the worst failure mode this review has produced.**
> ```cobol
> REPOSITORY. FUNCTION ALL INTRINSIC.
>     MOVE CURRENT-DATE TO WS-CD      *> compiles with zero diagnostics
> ```
> then at RUN TIME: `NotImplementedCobolFeatureException: reference 'CURRENT-DATE'`. `PI` and `E` failed
> identically — the whole zero-argument family.
>
> §15.21.2's general format is `FUNCTION CURRENT-DATE` with NO parentheses, so with the keyword omitted
> (§12.3.8.1 + §8.4.3.2.3 SR2) the reference is a **bare name — ZERO suffixes, not one**.
> `IntrinsicBinder.KeywordOmittedFunction` opened `if (suffixes.Length != 1 …) return null;`, so it fell through
> to a data reference, resolved to nothing, and reached the runtime's not-implemented stage. The standard writes
> the form itself at §D.14.3.6: `MOVE FUNCTION LOCALE-DATE (CURRENT-DATE (1:8))`.
>
> **Fixed narrowly.** A bare name becomes a function reference ONLY when the catalog says the function admits
> zero arguments (`MinArgs == 0`), so a declared data item still wins and no other bare word is re-routed for
> merely sharing a name with a function. Verified both directions.
> **GOLDEN** `conformance:2023/pb7_keyword_omitted_zero_arg`.
