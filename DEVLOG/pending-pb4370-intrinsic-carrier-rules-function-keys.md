## PB4370: the intrinsic carrier rules key on the function, and one wide argument no longer escapes the carrier

**The mechanism (kb/Work PB4370, cluster PB2293).** `IntrinsicCarrierRules` lives in `compiler.bound`, the layer a second
code-generation backend would consume, and keyed its three sets and `AlignmentMayLeaveCarrier` on the Roslyn renderer's runtime
methods (`"ModScaled"`, `"MeanScaled"`, `"Numval"`). DESIGN-backend-abstraction.md section 1.2 bullet 2 forbids runtime spellings on
the bound layer, and the PB4266 contract test carried the three sets as an L9 leak row. The re-probe found them unchanged at
51a9ba52f.

**The fix.** Every key is now the intrinsic-function-name as its catalog row spells it (`IntrinsicSig.Name`: `"MOD"`, `"MEAN"`,
`"NUMVAL-C"`), and `RenderNum`, `RenderDec`, the renderer's `AlignmentMayLeaveCarrier` and `FunctionValueLane.Of` pass `sig.Name`.
The runtime body stays the Roslyn renderer's own spelling, in its switch and in its own `StandardValueFixedByRule`.
`CrossAlignedNativeArms` names functions now, so it is renamed `CrossAligningFunctions`, with every caller changed in the same
commit. `AlignmentMayLeaveCarrier` names MEDIAN and MIDRANGE in its switch instead of a `_` default, and a function outside the set
throws `UnreachableException`. The contract table moves the three sets from the L9 leak row to the COBOL-word row. A new drift
test, `IntrinsicCarrierRuleKeyDriftTests`, holds every key to a catalog row spelt exactly as the key, because the sets compare
ordinally and the catalog lookup does not, and it is the one place a test maps a key to its runtime body. The four drift tests
that compare a set with the renderer's switch labels map through it. A probe program covering MOD, REM, SUM, RANGE, MIDRANGE,
MEAN and the NUMVAL family printed the same output before and after.

**Found on the way, in the same file.** `AlignmentMayLeaveCarrier` ordered the aligned bounds with `OrderByDescending` and took
the two largest for RANGE, MEDIAN and MIDRANGE. A bound past 38 digits is null, and the default comparer sorts null last, so with
one wide argument (`PIC 9(31)` beside a scale-18 item: 49 aligned digits) and two others it took the two bounded terms, proved a
fit and kept the native arm. That arm then raised EC-SIZE-OVERFLOW for `RANGE`, `MEDIAN` and `MIDRANGE` values a `PIC S9(31)`
receiver holds. PB621's witness missed it because each of its legs had two wide arguments. The predicate now answers true as
soon as any argument's aligned bound is unbounded. Witness: `wiseowl/cases/15.76.4-Returned-value-rule/pb4370_one_wide_argument_leaves_the_carrier`,
accepted from 2002 (the first edition with a 31-digit item), with expected values computed by exact arithmetic.

**Rows.** None closed: the re-key is process-only, and the found defect's rows (RV-15.76.4-1, RV-15.61.4-1/2, RV-15.62.4-1)
already read CONFORMS for the cases their witnesses exercise. One witness was re-sited: the first gate went RED on
`SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`, because RV-15.4.1-L2.1 still cited
`IntrinsicRenderer.cs#CrossAlignedNativeArms`. That witness had been stale since PB3268 moved the set and resolved only through a
renderer comment that named it. A `record_verdicts` batch retired it for `IntrinsicCarrierRules.cs#CrossAligningFunctions` and
restated the row's notes (GAP 19 to 19).

**Not changed, and reported as leads:** `FunctionValueLane`'s own sets, the binder's restatement of the renderer's switch labels,
still key on runtime bodies, and `IntrinsicSig.RuntimeMethod` crosses into the bound tree on `BoundIntrinsicCall.Sig`. That
field is chosen at bind time (`MaxString` for MAX over strings) and is reached through a tolerated bound-to-binding edge.

**Self-review** (architecture, full code, performance, duplication): the fix adds one allocation-free early return on a
compile-time path. The key-to-runtime-body map lives in one test helper (`IntrinsicCarrierRuleKeyDriftTests.RuntimeMethodOf`),
shared by the four drift tests that compare a set with the switch. The `CobolIntrinsics.Exact` summary still named MEAN as
cross-aligning and `IntrinsicRenderer` as the set's home; it is corrected. **Gate:** `=== BUILD-LOCAL GATE: GREEN — Conformance
20,541/20,541 · Unit 39,150/39,150 · Characterization 37/37 cases ran (skipped 0) ... run 000002-20261102T040002Z ===`.
