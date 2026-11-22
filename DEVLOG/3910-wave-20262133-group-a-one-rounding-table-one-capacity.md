## Entry 3910 — 2026-11-22 00:23 PST — Wave 20262133 group A: one rounding table, one capacity discipline, one selection kernel, one date-form type, and the runtime's exception layer cleaned of upward names

Seven R2 restructuring notes of the PB1754 cluster, each fixed at its root in its own checkpoint; four notes of the group
(PB2636, PB2637, PB2747, PB2723) and the driver half of PB2619 are left for a successor.

**PB2644 — the receiver's capacity discipline and the §2023:14.7.4.3 mode table, each written once.** Re-read on the pin:
`CobolNum.Store`/`TryStore` over Int128, over `CobolDec` and `StoreU`/`TryStoreU` over UInt128 each carried the
BinaryCapacity / 10^Digits / unsigned-magnitude tail, and `InPictureRange` a fourth copy. Now every lane does only its own
alignment and ends in `Fits` / `Commit` / `TryCommit` (with `CommitU` / `TryCommitU` reducing a value past Int128 first), and
the class test's range half asks `Fits`. The rounding decision was written four times (`CobolNum.BumpsMagnitude`,
`CobolDec.RoundFromRemainder`'s switch, `CobolDec.Round34Wide`'s switch, `FloatResultant.DirOf` and its tie handling); it is
now `RoundingRule.StepOf` / `Bumps` in CobolRounding.cs, asked by the divide kernels, the standard-decimal digit walk, the
256-bit final transfer and both floating-point landings. The copies had drifted: `Round34Wide` rounded AWAY-FROM-ZERO,
TOWARD-GREATER, TOWARD-LESSER and NEAREST-TOWARD-ZERO as NEAREST-AWAY-FROM-ZERO; its callers pass the INTERMEDIATE ROUNDING
mode, whose four values it already rounded right, so no program's answer changes. `RoundingRuleLaneAgreementTests` pins the
lanes to the divide kernel over every mode (it fails on the old walk). The inventory row GR-14.7.4.3-7 was re-sited from
`CobolNum.cs#BumpsMagnitude` to `CobolRounding.cs#StepOf` by a record_verdicts batch.

**PB2774 — CA1305 in CallAbi.** `StoreReturn`'s two `FitToReceiver(value.ToString(), …)` legs and the character-result
`Int128.TryParse` now name the invariant culture; the code sites were unchanged on the pin.

**PB2619 (CLI half) — the CLI reads the edition model.** The validator had been derived from `EditionInfo.All` by PB4126;
the help text's and resolver's 2023 default, check-batch's `int std = 2023` and the `--listing` default and help ("on at
85") now read `EditionInfo.Latest`, `EditionInfo.All` and a new `EditionInfo.RequiresSourceListing`. `cobol --help`
prints the same text. The `CompilerDriver.Options` EditionInfo half (189 construction sites) is the note's next step,
recorded in its addendum.

**PB2634 — the §2023:15.5 date forms owned by `IntegerDateForm`.** The 3,067,671 literal (four sites), the epoch arithmetic
(five), `DateTime.DaysInMonth` / `IsLeapYear` (four) and `CobolLocale.TryDate`'s own validity test now ask one type, and
the calendar arithmetic moved there from CobolDate. `IntegerDateFormDriftTests` fails on a source that spells the rule again.

**PB2635 — one MAX / MIN / ORD-MAX / ORD-MIN selection kernel.** `ExtremeIndex<TOrder>` owns the empty-list assertion and
the leftmost strict-update rule; the aligned Int128, unaligned pair, SDIDI, binary64 and string bodies supply only an
`ISelectionOrder`. The binary64 MAX / MIN / RANGE / MIDRANGE no longer call `Enumerable.Max`/`Min`, whose NaN rule made MAX
and ORD-MAX name different arguments (§2023:15.59.4 RV1, §2023:15.71.4); `SelectionRuleCarrierAgreementTests` pins every
carrier, and `EmptyArgumentListDriftTests` accepts a body that selects through the kernel, whose own first statement it
asserts.

**PB2756 — the census matches a constructor.** `Reachability.Exclusion` and `MemberIndex` compared `.ctor` with the emitted
identifiers; `Reachability.EmittedName` maps a constructor to its type's simple name, the identifier `new TypeName(` text holds.

**PB2743 — Runtime.Exceptions names nothing above it.** `EcFunctions` moved to Intrinsics, `INonfatalSelector` and
`NonfatalSelectorFn` beside `DispatchResult`, `ExternalChecks` beside `CheckingFlags`, and `ExceptionEngine` takes an
`IActivationIdentity` that `ModuleStack` implements. The emitted text is unchanged (the generated file imports both
namespaces wherever it names these types). The public surface moved, so the runtime major was bumped
(`abi_pin.py bump --runtime-major`), and the six 2023 rows and one 2002 row whose code-location named
`Exceptions/EcFunctions.cs` were re-sited by record_verdicts batches (witness-loss audit GREEN: 8 retired, 0 unexcused).

**Self-review.** Architecture: each rule now has one home (RoundingRule, Fits/Commit, IntegerDateForm, ExtremeIndex) and the
exception layer has no upward type edge left but the RunUnit/CobolObject ones the note assigns to PB2349 and the §8.2 move.
Performance: the selection orders are structs passed by `in`, so the collation overloads no longer allocate a delegate
per call. Duplication: `WideningFitsU` replaced three copies of the UInt128 widening test.

**Review round 1 (review-a), answered by the finisher.** R1-1: `docs/NUMERIC_DESIGN.md` still named the deleted
`CobolNum.BumpsMagnitude`, "two" rounding tables and the deleted `InPictureRange`; its three passages now describe
`RoundingRule` (`StepOf`, `Bumps`, `BumpsOnDigit`, every lane) and `CobolNum.Fits`, and the sweep of docs/ for every name
the branch deleted or moved also fixed the DOC-A.1-70 conformance determination (which named `BumpsMagnitude`) and the
runtime survey's path of `EcFunctions`. R1-2: the `Round34Wide` comment said its callers pass a statement's ROUNDED MODE;
every caller passes the INTERMEDIATE ROUNDING mode or TRUNCATION with the round-to-odd jam, so its directed-mode arms
were latent. R1-3: the capacity banner in `CobolNum.cs` named the deleted `InPictureRange`; it names `IsNumericImage`.
R1-4: PB2635 was not behavior-neutral for a NaN: with X a binary64 NaN and Y = 1, `MIN (Y X)` selected the NaN and now
selects Y, and `MAX (X Y)` selected Y and now the NaN, each as ORD-MIN / ORD-MAX already named (§2023:15.63.4 RV1,
§2023:15.72.4 RV1). The golden `15.63.4-Returned-value-rules/pb2635_nan_selection_agrees_with_ordinal` (2014, 2023) pins
the finite selections and the ordinals, PB2635's `closes_rows_reason` says so, and the older defect the probe exposed, a
selected NaN landing as 0 through `RealResult`'s EC-ARGUMENT-FUNCTION screen with no EC-DATA-NOT-FINITE
(§2023:14.6.13.2 GR3), is filed as kb/Work PB6042.

**Gate.** Run 000005 at 49370e1fa7: `=== BUILD-LOCAL GATE: GREEN — Conformance 28,998/28,998 ran · Unit 42,652/42,652 ran ·
RepoDrift 217/217 ran · Characterization 37/37 ran (skipped 0) ===`, after four reds each fixed at its cause (the Unshipped
API order, the new drift test's Sweep trait, a non-canonical designator with the cite-resolve baseline shrinking 6029 to
6026, and EXCEPTION-FILE's r1b two spaces named as the rule's result once EcFunctions joined the Intrinsics folder that
ArgumentSubstituteDriftTests reads). After review-a round 1, run 000007 at 7f712021f8: `=== BUILD-LOCAL GATE: GREEN — Conformance 29,000/29,000 ran ·
Unit 42,652/42,652 ran · RepoDrift 217/217 ran · Characterization 37/37 ran (skipped 0) ===` (the two new rows are the
2014 and 2023 runs of the NaN-selection golden).

**Merging main (18fa71b14b: #1936 PB1754 group B, #1935 train-1985-e).** Nine conflicts, one mechanism: main's PB4976/PB6000 canonicalizer rewrote citation designators on lines this branch rewrote; the branch's code was kept and the canonicalizer re-run (`audit_code_citations.py --canonicalize`), the cite-resolve baseline regenerated (1953 to 1952 entries). The merged gate (run 000009) was RED in RepoDrift only: main's new PB6031 ratchet (`InlineEditionComparisonDriftTests`) counted the CLI's source-listing edge where this branch moved it, from `Program.cs` (`std == 85`) into `EditionInfo.RequiresSourceListing` beside the record's other nineteen compiler-side edition facts; the ratchet's two entries follow the move (Program.cs 1 to none, EditionInfo.cs 19 to 20), the total unchanged.

