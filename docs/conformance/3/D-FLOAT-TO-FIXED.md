- **D-FLOAT-TO-FIXED — Floating-point value → fixed-point receiver: the conversion manner (§2023:14.6.8.2 r1/r2/r4; kb/Work PB77,
  2026-08-18).** §2023:14.6.8.2 r2 leaves "the manner in which the value is converted to a fixed-point value" to the
  implementor for a FLOAT-SHORT/-LONG/-EXTENDED sending item, and r1 treats an intermediate or standard-float
  sender "as if it had been converted to a fixed-point value"; WiseOwl COBOL's FLOAT-LONG and FLOAT-BINARY-64 are the
  same `double` (item 22), and a §2023:15.4.1 float-family returned value IS a binary64 (item 92), so ONE conversion
  serves all three: **the binary64's EXACT algebraic value, aligned by decimal point and truncated (or rounded,
  under a ROUNDED phrase) at the receiver's scale — at every magnitude** (`CobolFloat.TryExactScaled`; kb/Work
  PB623, 2026-09-05, which ANSWERED kb/Work PB90's three-way choice). A double is ±man·2^exp exactly and therefore
  always a terminating decimal, so "the value" is never in doubt: a COMP-2 holding 8.2 holds
  8.199999999999999289457264239899814128875732421875 and moves **8.1** into PIC 9V9; one holding 1.15 moves 1.14
  into PIC 9V99; one holding 0.1 moves .1000000000000000055 into PIC V9(19); **past the carrier the same expansion
  keeps supplying the low-order digits** (`CobolFloat.LowOrderDigits` — a COMP-2 holding 1.0E+40, whose exact value
  is 10000000000000000303786028427003666890752, moves 90752 into PIC 9(5); an in-carrier 1.0E+25 moves its exact
  69664) — never a saturation sentinel, which has no capacity check downstream in a MOVE and used to store ITS low
  digits (884105727 / 03715). A non-finite value reaching a fixed-point landing with checking off (NaN, ±Infinity —
  EC-DATA-NOT-FINITE at the sending read under checking, §2023:14.6.13.2 item 3) lands ZERO. The CHECKED landing (an
  arithmetic store under ON SIZE ERROR / EC-SIZE checking) RAISES the size error past the carrier
  (`CobolFloat.ToScaled` and the checked `FromDouble` landing throw `CobolSizeError`
  EC-SIZE-TRUNCATION, item 179, kb/Work PB2967): it used to saturate and leave the receiver's capacity check to
  notice, which a 16-byte COMP-5 receiver cannot — its capacity is the whole carrier, so `COMPUTE R = FUNCTION
  VARIANCE(A, 1)` (about 2.5E+57) stored 2^127 - 1 and took NOT ON SIZE ERROR.
  **Why the exact expansion and not the shortest-round-trip decimal (PB90's option b) — the choice is not free.**
  r2's latitude covers a FLOAT-SHORT/-LONG/-EXTENDED sending ITEM only; r1 governs a standard-float item and an
  intermediate with no latitude at all ("the value is treated as if it had been converted to a fixed-point value"),
  and `COMPUTE r = <any float>` lands through an intermediate. So a different manner for COMP-2 would make
  `MOVE c2 TO r` disagree with `COMPUTE r = c2` for one value — the split PB623 exists to remove. The same
  expansion answers the ROUNDED MODE PROHIBITED question (§2023:14.7.4.3 item 7): the double nearest 0.1 is NOT
  representable in one fraction digit, so `COMPUTE r ROUNDED MODE IS PROHIBITED = <COMP-2 0.1>` raises
  EC-SIZE-TRUNCATION and leaves the receiver unchanged, where a binary64 product test called it exact
  (`0.1 * 10.0` is exactly 1.0). Pinned by `2002|2014|2023/pb623_float_landing_exact`,
  `2023/pb77_move_past_the_carrier` and `CarrierLandingFormTests`.
