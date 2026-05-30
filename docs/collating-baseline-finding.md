# Collating Gap 2 — root cause found; 8 NIST baselines are contaminated by a pre-existing bug

_2026-05-30. main = 3a80e58 (clean, green). Fix preserved in `/e/tmp/repro/collating-fix.patch`
and reflog `b1626f1`. Repro: `/e/tmp/repro/SPCMP.cob`, `GRP.cob`._

## The figurative-SPACE-vs-PCS symptom is fully diagnosed

It is **not** a comparison-semantics bug. It is the pre-existing **all-255 alphabet-table bug**:
`ALPHABET x IS STANDARD-2` (and STANDARD-1 / NATIVE) are dedicated grammar tokens, not cobolWords, so
`SemanticBuilder.BuildAlphabetCollatingSequence` saw zero entries and built a table where every
character has weight 255 → **every string compares equal to every other string.**

Proven with `SPCMP.cob` (`ALPHABET x IS STANDARD-2`, `IF "ABCD" = SPACE`): at main this wrongly
prints `ABCD-EQ-SPACE TRUE`. With the fix (treat STANDARD-1/2/NATIVE as the identity sequence, and
normalize an identity program collating sequence to null so the native comparison path is used) it
correctly prints `ABCD-EQ-SPACE FALSE`.

## The fix (small, correct, preserved as a patch)

1. `SemanticBuilder.BuildAlphabetCollatingSequence` — detect `alphaDef.NATIVE()/STANDARD_1()/
   STANDARD_2()` first → return `AlphabetDefinition.NativeCollatingSequence()` (identity).
2. `Compilation.BuildSemanticModel` — only `SetProgramCollatingSequence` when the table is **not**
   identity (`IsIdentityCollation` helper). An identity sequence is indistinguishable from "none", so
   STANDARD-* programs use the proven, trailing-space-insensitive native comparison path.

`SPCMP` and a group-vs-SPACE repro (`GRP.cob`) both behave correctly with the fix.

## Why this nonetheless makes the guard RED — and the real decision

With the fix, **8 baselined NIST tests regress**: NC114M, NC214M, IF105A, IF119A, IF123A, IF127A,
IF128A, IF129A. The diff in every case is the **same**: the baseline contains
`*** INFORMATION ***NO FURTHER INFORMATION, SEE PROGRAM.` lines after **passing** subtests; the
corrected output omits them. All actual PASS/FAIL result lines match.

**Those INFORMATION-after-PASS lines are artifacts of the all-255 bug.** Mechanism, from the CCVS
boilerplate these programs share:
- `PRINT-DETAIL` (IF119A line 249): `IF P-OR-F EQUAL TO "FAIL*" … PERFORM FAIL-ROUTINE ELSE PERFORM
  BAIL-OUT`. For a pass, `P-OR-F = "PASS "`.
- Under the all-255 table, `"PASS " = "FAIL*"` is **vacuously TRUE**, so `FAIL-ROUTINE` runs even for
  passes. `FAIL-ROUTINE` (line 318-324) finds `COMPUTED-X`/`CORRECT-X` = SPACE (also vacuously true)
  and writes `MOVE "NO FURTHER INFORMATION, SEE PROGRAM." … PERFORM WRITE-LINE`.
- With correct collation, `"PASS " ≠ "FAIL*"` → `BAIL-OUT` runs → with spaces in `COMPUTED-A`/
  `CORRECT-A` it takes `BAIL-OUT-EX` and writes nothing. So a correct compiler prints **no**
  INFORMATION line after a clean pass — which is the spec-correct CCVS behavior.

These baselines were self-captured from our own compiler (commit 46d9b37 "IF suite baselined") while
the all-255 bug was already present, so they encode the bug. **The corrected output is more correct
than the baseline.**

## Decision required: regenerate the 8 contaminated baselines

There is **no fix that both makes collation correct AND leaves these 8 baselines unchanged** — the
INFORMATION-after-PASS lines exist only because of the bug. To land the figurative-SPACE fix (and
therefore CHAR/ORD, which needs correct collation), the 8 baselines
(`tests/nist/valid/{NC114M,NC214M,IF105A,IF119A,IF123A,IF127A,IF128A,IF129A}.txt`) must be
regenerated from the corrected compiler output.

Recommended sequence once approved (and with a healthy tool channel — it was corrupting output during
this analysis, so baseline regeneration must be done carefully and verified):
1. Apply `/e/tmp/repro/collating-fix.patch` (the SemanticBuilder + Compilation fix). DEVLOG 226.
2. For each of the 8: regenerate `tests/nist/valid/<T>.txt` from corrected output **after eyeballing**
   that the only change is the removal of INFORMATION-after-PASS lines (no result-line changes).
3. Guard ALL GREEN (149) — kill stray dotnet first; require `=== ALL GREEN ===` at exit 0.
4. Re-apply CHAR/ORD + dead-code from reflog `fcaab53`; add `IntrinsicCollatingTests` using a
   **reordered** alphabet (`"B","A"`) so the collating CHAR/ORD path is actually exercised (STANDARD-2
   is now identity→null, so it would NOT exercise it). DEVLOG 227. Guard green. Commit, push.

## Why I paused instead of doing it

Overwriting 8 NIST baselines is a change to the test suite's source of truth and a judgment call;
combined with an actively-corrupting tool channel and this session's earlier red-build/false-green
mistakes, I restored clean green main and surfaced the decision rather than rewrite baselines on
unreliable tooling.
