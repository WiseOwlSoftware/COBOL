## A strongly-typed group is no longer an alphanumeric CALL / CANCEL / CODE operand, and the EVALUATE range asks the same-class identity (PB3355)

PB3355 reported that a strongly-typed group passes alphanumeric-class screens outside a function argument, because
`IntrinsicArgumentRules.ClassOfItem` answers ALPHANUMERIC for it. That answer is ISO §15.3 item 2's reading of a function
ARGUMENT ("Strongly-typed group items are treated as though they were of class and category alphanumeric, unless they are
prohibited as arguments of a function"), kept by PB1901; every other position falls under §8.5.2.1 ("Both the class and the
category of a strongly-typed group item are the type-name specified in the TYPE clause"). The note asked for that adjudication
before any fix; it is in `kb/Work/PB3355.md`. All citations were run through `cite.py --check`.

**What the re-probe showed.** `CALL V` and `CANCEL V` over a strongly-typed group compiled clean (CALL then read the group's
characters as a program-name at run time), and so did a report `RD ... CODE IS V`, a third arm of the same shape that the sibling
sweep found. The EVALUATE range `WHEN V THRU Y` did not under-reject: the relation the range lowers to refused it (COBOLNET1533),
so the gap there was the missing §14.9.13.3 SR4 diagnostic.

**Fix.** The three identifier screens are CATEGORY rules (§8.5.2.1: a class or category named in the rules "refers to the
category unless class is specifically indicated"), so they ask the reader every such rule already asks (ASSIGN USING, FILE STATUS,
the record keys, SET ... ADDRESS OF): `ItemCategory.IsAlphanumericOrNational(Place)` / `IsAlphanumeric(DataItem)`, whose
alphanumeric-group definition excludes a strongly-typed and a variable-length group. `CallBinder.NameTargetRefusal` is the one
CALL / CANCEL screen; the refusal prints the new `ItemCategory.Face(Place)`, which replaces `SetBinder.AddressNameFace`. The range
is a CLASS pair rule, so `EvaluateBinder.ScreenRangeOperands` asks the new `IntrinsicArgumentRules.SameClass`, which takes a
strongly-typed group to be of another operand's class only when that operand is a strongly-typed group of the same type.
Consequence of reading the rules as categories, recorded as a determination: an alphanumeric-edited, numeric-edited,
national-edited or variable-length-group CALL / CANCEL / CODE operand is refused too (before, accepted through the class).

**Goldens.** Refusals `pb3355-call-target-strong-group` (COBOLNET1681), `pb3355-cancel-target-strong-group` (COBOLNET1696),
`pb3355-code-identifier-strong-group` (COBOLNET2713), `pb3355-evaluate-range-strong-group-and-alphanumeric` and
`pb3355-evaluate-range-strong-groups-of-two-types` (COBOLNET2107), `pb3355-call-target-alphanumeric-edited` (COBOLNET1681);
accepted `pb3355_call_cancel_group_targets` (`COUNT=1` twice, §14.9.5.4 GR3) and `pb3355_evaluate_range_same_type_strong_groups`.
`StrongGroupClassScreenDriftTests` keeps `CobolClass.Alphanumeric` / `National` out of every Binding source that is not a
function-argument position or a listed lattice consumer. START's generic-key SR6 b) 2 reads the same lattice but is unreachable
for a strongly-typed operand (a second record description sharing the key's bytes is refused, COBOLNET1532).

**Rows.** None claimed or closed (`closes_rows: []`, reason in the note).

**Gate.** First gate RED on `HostWitnessTraitDriftTests` only: the drift test carried `[Trait("Sweep", "tree")]` yet scans
`src/Cobol.Net.Compiler/Binding`, a specific path (drift_rules.py SPECIFIC_DEPTH), so the trait was removed. Re-gate:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261018T164851Z-074753); all 22 pb3355 conformance
cases in `leg-1-Conformance.trx` Passed. `semgrep/verify.py` PASS, `cobolnet-raw-diagnostic-code-literal` 244 -> 243 (baseline updated).
