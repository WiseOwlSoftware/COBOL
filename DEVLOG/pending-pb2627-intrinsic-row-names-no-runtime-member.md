## PB2627: an intrinsic catalog row names no runtime member; the renderer keys on the function and names each body through RuntimeApi

**What was wrong.** `IntrinsicSig.RuntimeMethod` looked like the runtime body a function runs, but the renderer used it as
a dispatch key. The MAX row said `"MaxScaled"`, yet a MAX call emits `MaxAt`, `MaxDec`, `MaxExact`, `MaxWide` or `MaxReal`,
never `MaxScaled`. Because the key matched a method name, two dead public bodies stayed in the reachability census as
emitted text: `CobolIntrinsics.OrdMax` and `OrdMin`. The binder also picked a body name at bind time
(`sig with { RuntimeMethod = "MaxString" }`), so the bound tree carried a renderer key. The bound layer's
`FunctionValueLane` sets used that same key space.

**Re-probe (ccb23d13e4).** The column was still the key: 98 lines of `src` named it, 73 of them in the renderer, the rest in the lane, `FunctionDeviceText` and
the binder. A grep of `src` and `tests` found no emitter of `OrdMax` or `OrdMin` (now `params ReadOnlySpan<Int128>`). Their
only callers were in `SelectionRuleCarrierAgreementTests`. `MaxScaled` and `MinScaled` were called only by `RangeScaled`
and `MidrangeScaled`.

**Fix.** The catalog row has no member-name column. The function's catalog name is the one key, as kb/Work PB4370 already made
it for `IntrinsicCarrierRules`. That covers every renderer switch (`RenderNum`, `RenderDec`, `RenderExactUnary`, `RenderWide`,
`RenderWholeAligned`, the string channel), `FunctionDeviceText`, `FunctionValueLane`'s sets, `StandardValueFixedByRule` and
`EquivalentExpressionUnaryArms`. Each body an arm emits is a `nameof`-anchored constant in `RuntimeApi` (`IntrinsicBody`,
`DateBody`, `LocaleBody`, `EcBody`: 184 names), the façade `RuntimeApiGuardTests` holds every runtime name to. The `XxxScaled → XxxReal` name convention
(`RealMethod`) is now one function-keyed table, `IntrinsicRenderer.Binary64Bodies`. The binder records its string-comparison
choice as a fact on the call, `BoundIntrinsicCall.ComparesStrings`. Each arm that used to exclude the string body by its key
now asks that fact, at the same point in the flow. `OrdMax` and `OrdMin` are deleted. `MaxScaled` and `MinScaled` are private
helpers of RANGE and MIDRANGE. They were Unshipped API, so the ABI pin does not move.

**Drift tests.** `IntrinsicCarrierRuleKeyDriftTests.EveryBackendFunctionKey_IsACatalogFunctionName` checks every function-keyed
table and every string arm label against the catalog. `IntrinsicRealArgDriftTests` checks that `Binary64Bodies` covers every
function whose argument run can carry a binary64, and fails on any `…Real` body that no row names.
`IntrinsicCarrierAgreementDriftTests` now names an arm by the bodies it emits, not by its label. Eleven source-reading tests
moved from the old key space to the function names.

**Self-review: the arm the oracle could not see.** Under the old key the string form was excluded from the numeric bodies
by its spelling (`"OrdMaxString"` matched none of them). Under the function key, each arm has to say so. The first cut missed
`RenderDec`: under ARITHMETIC IS STANDARD (2002) every call reaches its switch, and `FUNCTION ORD-MAX` over `"BBB" "CCC" "AAA"`
answered 001 from `OrdMaxDec` over the strings. A probe on the build showed it (`ORD-MAX=001`), and the guard
`if (ic.ComparesStrings) return null;` restored 002. The oracle corpus has no such call, so the golden
`15.58@2002-ORD-MAX-FUNCTION/g2-ord-max-strings-arithmetic-standard` now pins it (§2002:15.58.3 RV1-RV3: COMPUTE and MOVE
002, a tie 002, a space-padded tie 001). Every other numeric arm keyed on MAX, MIN, ORD-MAX or ORD-MIN was swept: `RenderNum`'s
selection arms, `SelectionPastReceiverHeadroom`, `RenderWide`, `FunctionDeviceText.CallField` and `FunctionValueLane`. Each
asks `ComparesStrings` at the point where the old key fell through.

**Neutrality.** `python scripts/arch/compare_oracle.py --base ccb23d13e4`:
`=== ARCH-ORACLE: IDENTICAL — 23499 cases (baseline ccb23d13e470, new ccb23d13e470-dirty) ===` before the golden, and after it
and the `RenderDec` guard `=== ARCH-ORACLE: DIFFERENT — 1 of 23500 cases differ ===`, the one line being
`ADDED suite/2002/g2-ord-max-strings-arithmetic-standard` (no CSHARP, DIAGNOSTICS or REMOVED line).

**Rows.** Eleven inventory rows cited a deleted or retired member (`RealMethod`, `OrdMax`, `OrdMin`, `MaxScaled`, `MinScaled`).
They were re-sited by `record_verdicts` batches: `RV-15.22.4-1`, `RV-15.47.4-1`, `RV-15.59.4-2`, `RV-15.63.4-1`, `RV-15.71.4-3` and
`RV-15.72.4-3` at 2023 (GAP 1 → 1); `1989-GR-A.2.32.1-p1` and `1989-GR-A.2.33.1-p1` at 1985 (GAP 0 → 0); `2002-RV-15.37.3-1`,
`2002-RV-15.58.3-3` and `2002-RV-15.59.3-3` at 2002 (GAP 1150 → 1150). The first batches missed six of them: the grep looked for
`OrdMax` but not `OrdMin`, and not in the 2002 store. Gate 3's `EveryCodeLocation_ResolvesInTheTree` named five, and the sweep
that followed found the sixth. The landing closes no row (`closes_rows: []`, a behaviour-neutral R2 wave).

**Gate reds on the way.** Run 1: CITATIONS, two stale baseline entries for the deleted `CannotLeaveTheCarrier` lines
(`--write-baseline`). Run 2: `RuntimeApiGuardTests` counted the renderer's `nameof(CobolIntrinsics.X)` operands as bare
runtime names. The ratchet is right: names are anchored in the `RuntimeApi` façade, so they moved there. Run 3:
`DocCommentShapeDriftTests` (a doc summary above the first anchor class), the six inventory rows, `NameTableOwnershipDriftTests`
(five new function-name comparisons classified KEYWORD like their siblings, three stale entries deleted, the renamed CONCAT site
re-keyed) and `OperandStringChannelDriftTests` (its arm labels are function names now).

**Docs.** `docs/INTRINSICS_DESIGN.md` (the catalog sketch, the ORD-MAX paragraph) and `DESIGN-architecture-review.md` §8.3 (5),
which says the `intrinsics.json` row carries no member name.

**Group.** PB2622, PB2724 and PB3231 were dispatched in the same group. Each is a separate mechanism and is left for a
successor.
