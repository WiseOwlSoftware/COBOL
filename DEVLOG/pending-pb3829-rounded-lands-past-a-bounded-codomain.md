## PB3829: a ROUNDED store of ACOS, ASIN, ATAN, RANDOM or FRACTION-PART rounds like any other value; the codomain clamp is gone

**The defect (fifth fuzz night, owner-approved fix).** `COMPUTE R ROUNDED = FUNCTION ACOS(-1)` into `PIC 9V9999`
stored 3.1415 where §14.7.4.3 r4 gives 3.1416, while `FUNCTION ACOS(-1) + 0` stored 3.1416. Re-probed on this branch's
own build before any change: the repro printed `A 3.1415` / `B 3.1416`, and a 216-line probe (eight bounded function
references x nine rounding phrases x receivers at scales 4, 2 and -1) differed from the same program under
`ARITHMETIC IS STANDARD-DECIMAL` on 96 lines, every one a native bare reference held inside the bound.

**The determination (spec first, every citation `cite.py --check` OK).** The §15.x.4 rules bound the RETURNED value,
§15.4's "temporary elementary data item" (§15.8.4 r1, §15.10.4 r1, §15.11.4 r1, §15.75.4 r1); the stored value is
§14.9.8.4 r2's, "stored, in conformance with the specifications in 14.6.8 ..., 14.7.4, ROUNDED phrase, and 14.7.5,
SIZE ERROR phrase and size error condition". So a rounded value may lie past the bound, and one the receiver cannot
hold is a size error (§14.7.5 3)): RANDOM ROUNDED into `PIC SV9999` is ON SIZE ERROR, where the clamp stored a silent
.9999. PB65's premise (landed 2026-08-09) is overturned and its note says so.

**The fix, at the root.** `CobolIntrinsics.FromDoubleBounded`, its three scale-37 constants, the catalog's
`IntrinsicCodomain` column and `RuntimeApi.CodomainConst` are deleted; every function of the §15.4.1 float family
lands through the one `FromDouble`, rounding once at the resultant identifier with the statement's mode. The runtime's
public surface lost four members, so the runtime major is bumped (`abi_pin.py bump --runtime-major`). After the fix
the native probe equals the standard-decimal one on all 216 lines, and both equal an independent Decimal derivation of
§14.7.4.3 rules 2-10 from the exact binary64 returned values.

**Goldens.** `14.7.4-ROUNDED/pb3829_rounded_bounded_function_modes` (ACOS(-1), ASIN(+-1), ATAN(+-10^18) under all nine
rounding phrases at scales 4, 2 and the trailing-P -1, plus PROHIBITED under ON SIZE ERROR);
`wiseowl/cases/15.75.4-Returned-value-rules/pb3829_random_fraction_rounded_store` (RANDOM(97090) and FRACTION-PART of a
FLOAT-LONG +-5.9999999996, whose returned values are implementor-defined and DISPLAYed first, plus the V4 size error
with and without ON SIZE ERROR); `pb65_codomain_clamp` renamed `pb65_bounded_returned_value_landing` with its four
ROUNDED legs re-derived (RASIN1 1.570796327, RASINM -1.570796327, RACOSM 3.141592654, RFRACP 1.000000000). Every
expected value is computed from §14.7.4.3 by a Decimal derivation over the exact binary64 values, never copied from a
run. Inventory: RV-15.10.4-1, RV-15.11.4-1, RV-15.75.4-1, FMT-15.10.2, FMT-15.11.2 retire the clamp's witnesses and
restate their notes; the GR-14.7.4.3 rows, RV-15.8.4-1, RV-15.42.4-1 and GR-14.7.5-3 gain the new goldens.

**The sweep the owner asked for ("not just fixes that one example").** Every path that reaches a receiver was probed:
COMPUTE of FLOAT-SHORT/LONG/EXTENDED and COMP-2 items, float sub-expressions inside decimal expressions, ADD /
SUBTRACT / MULTIPLY / DIVIDE in their GIVING, TO and BY forms with float and function senders, PROHIBITED, MOVE and
the standard-decimal arm all round once. Two more places shortened a value before the rounding decision:

- **Native DIVIDE ... GIVING ... REMAINDER** landed a binary64 or SDIDI sender TRUNCATED at the float working scale
  before dividing (`NumericRenderer.FixedLane`, its one consumer): `ROUNDED MODE AWAY-FROM-ZERO` of FL =
  0.12340000000001 over 1 stored 0.1234 with REMAINDER and 0.1235 without, a NUMVAL dividend the same, and 0.09 / FB
  (0.30000000000001) stored Q 0.3000 / R 0 where §14.9.12.4 1) and 6) c) give 0.2999 / 0.000029999. Such a sender now
  takes `ArithmeticEmitter.CarriedSubsidiaryQuotient`: the quotient is the very `Combine(dividend "/" divisor)` final
  transfer DIVIDE GIVING performs, at the receiver's mode for the stored quotient and at TRUNCATION for the subsidiary
  quotient; two scaled senders keep the exact Int128 kernel; `FixedLane` is deleted. Golden
  `wiseowl/cases/14.9.12-DIVIDE/pb3829_remainder_float_sender`; CONFORMANCE.md DOC-A.1-179 says so.
- **A floating-point numeric-edited receiver** took a bare float function's value or a native power at the >= 9
  working scale with truncation before the mask normalized it (`ReceiverContext.FloatLanding`'s third arm):
  `SQRT(1.5241383936E-24)` into `+9.999E+99` displayed +0.000E+00 where `... + 0` displayed +1.235E-12. The value keeps
  its binary64 now (`FloatLanding` has two answers; `FloatLandingModeDriftTests` invariant 4 says so), and NUMVAL-F's
  arm treats a float-edited receiver as it treats a float one. Golden
  `14.6.8.4-Floating-point-numeric-edited-receiving-data/pb3829_float_edited_small_magnitude`.

**Left as a lead:** native NUMVAL-F into a fixed-point receiver lands its exact parse at `max(scale, 9)` with
truncation before the ROUNDED decision (AWAY-FROM-ZERO of NUMVAL-F("0.12340000000001") into four places stores 0.1234,
NUMVAL 0.1235); that is CONFORMANCE.md DOC-A.1-92's documented NUMVAL-F determination and open PB624's arm.
