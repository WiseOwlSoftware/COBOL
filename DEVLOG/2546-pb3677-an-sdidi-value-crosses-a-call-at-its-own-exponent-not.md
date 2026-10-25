## Entry 2546 — 2026-10-25 11:39 PDT — PB3677: an SDIDI value crosses a CALL at its own exponent, not at the 6-digit receiver-less floor

**What the re-probe showed.** The note's repro (`CALL "W" USING BY CONTENT FUNCTION MIN(F 3)`, F COMP-2 0.0001, sends
`0000100` where AS NESTED and a MOVE send the value's 24 digits) reproduced on a fresh build, and was one branch of a wider
defect. The value 2 (`MIN(H 3)`, H COMP-2 2) crossed as `2000000`, and with a REPOSITORY program-specifier a numeric formal
`PIC 9V9(12)` took `0.333333000000` of 0.3333333333333333 where the AS NESTED call gave `0.333333333333`. The cause is
one place: a CALL whose formal the activating element cannot see (Format-1, a program-prototype name) rendered the argument
receiver-less, and `NumericRenderer.Landed` landed every SDIDI intermediate at `SdidiLandingScaleFloor` (6 fraction digits)
BEFORE any formal could cut it.

**The rule.** ISO 14.2.3 GR9 moves the argument "without conversion" into a record of the argument's length (the first
regime) and fills a numeric formal by "a COMPUTE statement without the ROUNDED phrase" (the second regime; GR10 likewise), so
only the formal may cut the value. For a plain-group formal, D-B4 gives the digits of the function's value. All three
citations were run through `cite.py --check`.

**The fix.** `CobolArgAdapt.SdidiValue(mode, CobolDec, SignEncoding)` builds the argument at the value's own exponent: an
Int128 cell at `-Exp` fraction digits, capped at the carrier's 38 (the widest scale difference `CobolNum.Rescale` can cross),
a positive exponent as its integer, and the size error of every intermediate landing past the carrier. Its `Sending` is stated
from the value itself (`CobolDec.ToFunctionText`, `CobolNum.GroupDigits`), so a value past the cap still reaches a character
formal exactly; `WithValueSending` keeps a sending operand already stated. `NumericRenderer.SdidiOf` is the one question "is
this rendered intermediate an SDIDI value" that `Landed` and the CALL arm now both ask (`Landed` is behavior-neutral).
Swept: the other `Landed` consumers (SET pointer UP/DOWN BY, ALLOCATE, a keyed-I/O START length, an intrinsic argument) are
value-semantics scalars with no formal to defer to, so the floor stays theirs; the INVOKE lane always knows the formal.

**Witnesses.** `pb3677_format1_sdidi_function_value` (NUMVAL values with no float conversion: the M, C and D lanes agree on
17 / 1 / 2 digits, and the REPOSITORY formal gets `0.333333333333`; it prints `0000123`, `2000000`, `1500000` and
`0333333000000` before the fix) with its `_below_2002` negative; `pb3677_format1_sdidi_expression` (STANDARD-DECIMAL `(2 / 3)` and
`(1 / 8)` at a prototype-name CALL) with its `_below_2014` negative; and `SdidiValueArgumentTests` (9 facts, including the
cap, the positive exponent and the overflow). Docs: CONFORMANCE D-B4, COBOLNET_INTERPROGRAM_DESIGN. No inventory row closes.

