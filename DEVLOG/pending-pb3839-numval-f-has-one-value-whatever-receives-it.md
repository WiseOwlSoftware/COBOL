## PB3839: NUMVAL-F has one value whatever receives it, and MAX and MIN no longer truncate before the receiver rounds

`ROUNDED MODE AWAY-FROM-ZERO` of `FUNCTION NUMVAL-F("0.12340000000001")` into a `PIC 9V9(4)` stored 0.1234 where NUMVAL, and
GnuCOBOL's NUMVAL-F, store 0.1235 (kb/Work PB3839, owner decision 2026-10-26: "We should always be consistent with ourself barring an
explicit requirement differently"). Wave 20261711, group B.

**Re-probe on the wave's build (base `1b6af36e5`).** Every row of the note's table reproduced, and the cause was wider than the table:
one reference had four values. DISPLAY, a relation and a COMP-2 got a binary64 (`5E+30`, and `NUMVAL-F("0.12345678901234567891") =
NUMVAL-F("0.12345678901234567892")` was TRUE); a fixed-point receiver got an `Int128` parse truncated at `max(receiver scale, 9)` places
and then rounded the truncation again (the 0.1234, and 12.0000000001 AWAY-FROM-ZERO into `PIC 9(3)` stored 12, a trailing-P receiver
1200 for 1200.00000000001); and a MOVE of a small value into a floating-point numeric-edited item stored `+0.000E+00` where the
multi-receiver MOVE stored `+1.500E-12`.

**Determination.** §15.69.4 2) makes the native value "an approximation of the numeric value represented by argument-1", 3) gives
standard-decimal arithmetic the value itself, and §15.4.1 leaves a native value's representation to the implementor; nothing says which
approximation, so the precedence is the ISO text (silent), GnuCOBOL (rounds an exact decimal once at the receiver) and self-consistency.
The approximation WiseOwl COBOL chooses is r3's value, the exact parse as an SDIDI, for every mode, channel and receiver, exactly as
NUMVAL's is. All citations `cite.py --check` OK; the determination replaces the float-family one in `docs/CONFORMANCE.md` item 92.

**The fix is a deletion, not a branch.** `IntrinsicCarrierRules.ValueFixedByDefinition` (two functions) became `ReturnsExactParse` over
`ExactParseFunctions` (three), still asked by `RenderNum`'s first arm ahead of the mode dispatch and by `FunctionValueLane`. The
`RenderNum` arm that tested `Receiver.Real || FloatEdited || Receiverless && !MoveSender` is gone (PB624 closed with it: its coupling is now
asserted), and so is everything that existed only for it: the scaled-`Int128` runtime bodies `CobolIntrinsics.NumvalF` and `Rescaled`
(PublicAPI `*REMOVED*`), `ReceiverContext.MoveSender` (and its one setter in `MoveEmitter.SenderContext`), and `FloatWorkingScale` /
`FloatScaleFloor` (no reader left: the float family has landed at the receiver's own scale since PB3829). `NumvalFDouble` /
`NumvalFSingle` stay for the ACCEPT device transfer. Consequences, all recorded in item 92: DISPLAY of the function prints the exact digits
(`5000000000000000000000000000000`, `0.0000000000015`); `COMPUTE F = FUNCTION NUMVAL-F("1.5E-12")` into a COMP-2 now stores what
`COMPUTE F = 0.0000000000015` stores, the truncated neighbour 1.4999999999999999E-12, because a COMPUTE with no ROUNDED phrase truncates
(§14.7.4.3 2)) and a decimal is a decimal; a native argument past decimal128's range (`1E+7000`) is the §14.7.5 case 5 size error
condition (ON SIZE ERROR catches it, the receiver stays unchanged) instead of a binary64 Infinity or a saturated value, as for every other
SDIDI-carried native value and as under standard-decimal.

**The sibling sweep.** The note asked for every function whose value is chosen by its receiver's shape. `IntrinsicRenderer` reads
`num.Receiver.` in five places. FloatLanding is the float family's one decision (PB647/PB653/PB3829). **MAX and MIN** had the same
defect: a receiver-bound selection was landed at the receiver's own scale (`RescaleStoreCap`, truncating), so `MAX(0.12345 0.1)` into
`PIC 9V99` ROUNDED MODE AWAY-FROM-ZERO stored 0.12 where §14.7.4.3 3) owes 0.13 (§15.59.4 1): the content of an argument). Fixed: the
selection lands at the common argument scale widened to the receiver's and capped at its `Int128` headroom
(`ReceiverContext.WorkingScale(s)`). **MEAN** still has it (`max(receiver scale, sum scale + 1, 6)` for its quotient, so
`MEAN(0.00000001 0 ... 0)` over 20 arguments is 0 in a DISPLAY, 5E-10 in a 12-place receiver and 0.00 where AWAY-FROM-ZERO owes 0.01): the
cure is the SDIDI quotient for every mode, a restructuring of the cross-aligning native arm beyond this slice, filed as a lead.
`ReceiverShapeReaderDriftTests` makes the set of receiver members the renderer reads closed and justified (MEAN's two reads are named as
open in it, to be removed when it lands), and asserts that each exact-parse function has no `RenderNum` arm and an SDIDI arm.

**Goldens** (each run on a build of the base commit and failing on it for the reason its comment names): `pb3839_numval_f_one_value`
(28 legs: every rounding mode at scale 4 in both signs, scales 0, 9 and 12, a trailing-P receiver, an operand, DISPLAY, the 10**-20
relation, the COMP-2 landings against the decimal literal), `pb3839_numval_f_beyond_the_sdidi`, `pb3839_max_min_round_once`, the negative
`pb3839_numval_f_below_2002` (COBOL1502 at 85); `pb60_numvalf_native_channels` and `pb251_numval_native_value_fixed` re-derived, their old
expectations being the retired determination. Expected values derived with Python's `decimal` from the exact value and the cited rule, never
from a run.

**Register.** Fourteen inventory rows keep CONFORMS and were restated by batch `pb3839-batch.json` (RV-15.69.4-2 carries the new
determination; the deleted `NumvalF` / `Rescaled` / `MoveSender` witnesses were retired with reasons and the prose of the rows that named
them rewritten; RV-15.59.4-1 and RV-15.63.4-1 gained the MAX/MIN golden); GAP 31 -> 31, so `closes_rows` is empty with its reason.

**Self-review.** Comments: the stale claims about NUMVAL-F's float lane in `IntrinsicRenderer`, `ReceiverContext`, `CobolIntrinsics.cs`
(`FromDouble`'s "scale must be FloatWorkingScale", now the receiver's own scale), `NumericRenderer`, `CobolFloat` and the three design
documents were rewritten to the present state; no history narrative was added in code. Silent failures: the one new loud path (a native
argument past decimal128) raises a size error rather than defaulting; the reject projection still returns the section 15.3 default under
EC-ARGUMENT-FUNCTION with checking off, unchanged. Tests: the unit tests that pinned the deleted `Int128` projection were rewritten to pin the
SDIDI value (not deleted), `CarrierLandingFormTests` now asserts the NUMVAL family has no `case` in `RenderNum` rather than a checked-landing flag.

**Review round 1 (R1-1, blocking).** The MAX/MIN fix left the receiver's headroom cap standing: `WorkingScale(s)` caps at 38 minus the
receiver's integer digits, so `MAX(0.000000000000001 0)` into `PIC 9(24)V9` ROUNDED MODE AWAY-FROM-ZERO (cap 14 below s 15) and
`MAX(0.000000001 0)` into `PIC 9(30)V9` (cap 8 below 9) still truncated the selection before the receiver's rounding and stored 0.0 where
NUMVAL-F of the same text stored 0.1 (owed 0.1: §15.59.4 1) the content of an argument, §14.7.4.3 the one rounding). Fixed at the root: a
MAX/MIN whose common argument scale exceeds the receiver's headroom (`IntrinsicRenderer.SelectionPastReceiverHeadroom`, asked of the same
`RoundsAtItsScale` / `WorkingScale` the landing uses) takes the SDIDI selection (`MaxDec` / `MinDec`) in every mode, which holds the selected
argument at its own digits and rounds once at the store; `RenderDec`'s early-out asks the same predicate so the two cannot disagree. Golden
legs D1-D7 were added to `pb3839_max_min_round_once` (both caps, MIN, TOWARD-GREATER, truncation, a negative TOWARD-LESSER, a nested `* 1`
form); D1-D4, D6 and D7 fail on the reviewed head (0.0 and +0.0) and pass now. Leads R1-2 (the drift test grounded in the standard-mode
sentence of §15.4.1; it now states the owner determination for native) and R1-3 (a theory name and remark naming the deleted float floor)
were fixed in files this change touches.
