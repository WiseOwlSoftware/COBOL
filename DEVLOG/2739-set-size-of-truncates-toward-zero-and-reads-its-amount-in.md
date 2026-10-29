## Entry 2739 — 2026-10-29 14:59 PDT — SET SIZE OF truncates toward zero, and reads its amount in its own domain (PB464)

**Mechanism.** ISO §14.9.39.4 GR37's second sentence says a non-integer amount "is truncated to the nearest integer". The
phrase is the standard's only use of those words and reads two ways (toward zero, or to the closer integer). The compiler
chose toward zero by a C# cast and nothing recorded the choice or pinned it; the one golden that reaches the line drives -0.5,
which GR37's first sentence catches before the truncation.

**Re-probe.** On the landing build a `PIC X DYNAMIC LENGTH LIMIT IS 9` item set to 2.7, 2.2 and 2.5 had length 2 each time;
0.9 had length 0 with no condition. The note's measurement held.

**Determination.** `docs/CONFORMANCE.md` §7 now records it beside the GR38 one: toward-zero truncation, calibrated by the standard's
own TRUNCATION rounding mode (§14.7.4.3 GR2 and GR10, §11.9.11.2 GR2 e; each `cite.py --check` OK), with the order of the tests:
GR37's sign test and GR38's "greater than the maximum size" read the evaluated value (so 9.5 against a maximum of 9 clamps and
sets EC-STORAGE-NOT-AVAIL), and the truncation only picks the stored length.

**Found on the way, fixed here (I9).** `SetEmitter.EmitSetSize` rendered every amount as a binary64. A `PIC 9V9(17)` item holding
2.99999999999999999 therefore set the length to 3 (the double is 3.0) where the exact value truncates to 2. The fix is the shape
CONTINUE AFTER already has (`StatementEmitter.ContinueAfterCall`, PB1529): `CobolDynString.SetSize` has one overload per
evaluation carrier (binary64, `CobolDec`, scaled `Int128`, unsigned-wide `UInt128`), the three legs live once in
`Classify` / `RaiseLeg` / `Resize`, and `SetEmitter.SetSizeCall` maps each `NumXCarrier` onto its own overload and throws on a
carrier it does not map. `CobolDynStringTests.SetSizeCall_IsTotalOverTheNumXCarriers_AndNarrowsNothing` walks the enumeration, so a
fifth carrier fails the build of that test. The exception detail is formatted only when a leg raises (no allocation on the
in-range path). The three new overloads are in `PublicAPI.Shipped.txt`.

**Goldens.** `2023/pb464_set_size_fraction_truncates` (2.7, 2.2, 2.5, 0.9, 8.9, the grow case, the 17-digit near-integer, and 9.5
against the maximum; expected output derived from the rules, then matched by the run) and `pb464-set-size-fraction-below-2023`
(COBOL0900 at 85, 2002 and 2014). Unit: the toward-zero theory, the exact scaled amount, the SDIDI and unsigned-wide legs.

**Rows.** `GR-14.9.39.4-37` PARTIAL to CONFORMS (batch `w20261754a-pb464`; GAP 30 to 29). PB464 is landed with `closes_rows`.

**Sibling sweep.** No other `Real(...Render(...))` site exists in `CodeGen`; the PB459 `SetEmitter` narrowings of the SET capacity and
index formats are a different rule (an exception condition, not a truncation) and stay with that note.

**ABI pin.** The three overloads add to the shipped surface and remove nothing, so `RuntimeAbi.pin` was re-measured with `abi_pin.py repin` (no major bump).

**Review round 1 (R1-1).** The carrier split dropped PB463's once-per-statement guard: `Resize`'s `OutOfMemoryException` catch raised
EC-STORAGE-NOT-AVAIL again after the clamp leg had raised it, so one SET SIZE ran the USE declarative twice. `Resize` now raises on
the third leg only when the request was in range (`leg == Leg.InRange`); regression test
`CobolDynStringTests.SetSize_ClampThenStorageRefused_RaisesOncePerStatement` counts dispatches for the clamp-then-refused and the
in-range-refused requests (one each).

