## A numeric literal sent to a group stores its digits, and a CALL that cannot see its formal sends both alphabets (PB3570, PB3587)

Two notes, one root: the channels that send a value into a plain group each decided the sender's characters for themselves, and
the answers had drifted from `OperandText.NonElementaryMoveSender`, the one codec ISO 14.9.25.4 GR4 ("no conversion of data from one
form of internal representation to another") needs.

**PB3570 (owner decision 2026-10-23, implementor latitude).** `MOVE -456 TO G` stored `-45`, `+456` stored `+45` and `-4.56` stored
`-4.`: PB1113's determination was that a numeric literal sent to a group stores its characters, sign included, while an elementary
`PIC X` receiver already dropped the sign by GR6 a). The standard gives a literal no representation to preserve; rule 1's precedence
(ISO, then GnuCOBOL) picks GnuCOBOL's literal data, the digits without sign or decimal point. Fix: one arm in `NonElementaryMoveSender`
for `BoundNumericLiteral { FunctionValue: false }` (`LiteralDigits`; a floating-point literal sends the digits of its significand as written, `1.5E+3` sends 15 as cobc 3.2.0 does. The first landing expanded the exponent through `NumericLiteral.TryUnscaledParts` and sent 1500; branch review R1-1 measured the divergence from GnuCOBOL, and the fix drops that call, which also ends the text expansion of `1.0E+6144`, with `pb3570_floating_literal_group_move` and a `1.5E+3` leg in each lane of `pb3570_numeric_literal_group_formal`).
The CALL lane's known-group-formal literal arm (it spelled `CsLiteral(n.Text)` itself) and the INVOKE lane (whose binder handed a numeric
literal to a group formal through the string channel) now read the same arm; the INVOKE binder uses the numeric channel for every formal.
D-B4 in `docs/CONFORMANCE.md` records the determination. A numeric FUNCTION value sent to a group keeps its signed rendering (separate
question, a lead in the report); a function folded to a literal keeps it too, so the answer never depends on the fold.

**PB3587.** The re-probe showed what the note said: `CALL "SUBG" USING BY CONTENT E8` (a `PIC 1(8) USAGE BIT` item) into a plain group of
bit items arrived as `0011 0000` (the text '1' as byte 0x31) where BY REFERENCE was right, a national item as mojibake, and an INVOKE BY
CONTENT national literal as `??`; the run-time-located CALL (a program defined later in the compilation group, with a program-specifier)
failed alike for items, literals and the values of boolean and national functions. Mechanism: the argument cell is built in the elementary
alphabet and the group formal reads it as a storage image. The activating element of such a CALL cannot see the formal, so the fix is not
at the producer: each BY CONTENT argument's `MoveSending` now carries `StorageImage` (the representation from the same codec, for a
sender of usage national or bit) beside its characters, and `CobolArgAdapt.Text` gives a plain-group formal the image (BY CONTENT only,
the caller's argument array copied, never written). `RuntimeApi.MoveSendingNew` and `ArgWithValueSending` REQUIRE the second alphabet
(null is an answer), so a new producer must decide. `BoundInvokeArg.StringLiteral` is now the bound literal (its category kept), the
INVOKE group arm sends it through the codec, and a literal into a bit or national group formal takes `MoveEmitter.RecordValue` as the
CALL lane does. The codec by usage is written once (`RepresentationOfText`), read by the sender and by the run-time converter. The stale
D-B3 sentence (review lead R1-1 of PB3562) is corrected.

**Goldens.** `wiseowl/cases/14.9.25-MOVE/pb3570_numeric_literal_group_move` (all four editions), `wiseowl/cases/14.2-.../pb3570_numeric_literal_group_formal`
and its `_below_2002` negative, `cases/14.2-.../pb3587_content_group_formal_usage_values` (ten legs) and its `_below_2002` negative. With the
one-line group-adapter read disabled, the Format-1 and specifier legs of the PB3587 golden print `F:G F`, `F:N ??`, `S:G F`, `S:N ??`
(measured). Unit: `GroupMoveSenderChannelDriftTests` (the shape), `MoveSendingStorageImageTests` (the adapters).

**Version pins.** `MoveSending` and `CobolArgAdapt.WithValueSending` changed shape (the shipped surface, no shim left behind), so the
runtime major is 15; a new caller states `StorageImage` and an old callee would ignore it, so the call ABI is 2 (it is also a constant in
the shipped surface list, which is why the surface hash moved with it). `RuntimeAbiPinDriftTests` re-pinned both pairs; the first gate
was red on exactly those two tests and nothing else.

**Sweep.** Reference-modified national slices, a numeric item of usage national, a national group, the function values and the specifier
lane all answer correctly; a universal-receiver INVOKE is BY REFERENCE only (COBOLNET0866); Format-1 CALL admits identifiers only BY
CONTENT. Rows: none claimed, none closed (GR-14.9.25.4-4 and GR-14.2.3-9 were already CONFORMS).

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 7,899 · Unit 1,042 · Characterization 31 … Conformance 10,418/18,317 · Unit 34,618/35,660 · Characterization 5/36 cases ran (skipped 0)` (run 000002-20261023T214754Z; the first, 000001, was red on the two `RuntimeAbiPinDriftTests` only). The twelve conformance rows of the new goldens passed in leg 1 (`leg-1-Conformance.trx`); semgrep `verify.py` PASS with no count raised. Self-review: the throw in `LiteralDigits` and the INVOKE group arm are unreachable for legal source (an arithmetic expression into a group formal is refused COBOLNET0828 at bind); no comment lies about a fold the probes did not measure.
