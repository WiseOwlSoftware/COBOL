- **D-STR4 — STRING: a FUNCTION operand is evaluated ONCE, when the statement's identifiers are identified (kb/Work PB4973, PB1908;
  row `GR-14.9.43.4-10`).** A function-identifier "references a temporary data item whose value is determined when the function is referenced
  at runtime" (§2023:8.4.3.2.4 GR1; `cite.py --check "§2023:8.4.3.2.4 GR1"` OK), and §2023:14.6.4 7) evaluates the identifiers of a statement
  *"in left to right order as the first operation of the execution of that statement"* (`cite.py --check "§2023:14.6.4 GR7"` OK). No transfer
  of the STRING can store into a temporary item, so GR10's overlap (identifier-1 or identifier-2 occupying identifier-3's storage)
  never reaches a function, whatever its arguments are, and the standard DEFINES the result: `STRING "xy" FUNCTION UPPER-CASE(F)
  INTO F` over `F = "abcdefgh"` gives `xyABCDEF`, and the delimiter `FUNCTION UPPER-CASE(X(1:1))` after `"z"` filled `X(1:1)` is `Q`, the
  original, so `"12Z34Q56"` is cut at `Q` (`z12Z34....`). Every function of every sending, the first included, is therefore
  identified and evaluated once, up front, in statement order (its arguments and reference modifiers range-tested then, so an
  EC-BOUND-REF-MOD in the sender precedes an EC-BOUND-SUBSCRIPT in identifier-3 as §2023:14.6.4 7) and §2023:14.9.49.4 GR11
  require, kb/Work PB3261; `conformance:cases/14.9.43-STRING/pb1908_string_function_order`); FUNCTION RANDOM and a user-defined
  function run once like the rest. **This reverses the determination this entry held until PB4973**, which followed GnuCOBOL (cobc
  3.2.0 evaluates a function in the argument list of `cob_string_append`, when its transfer begins, giving `xyXYCDEF` and `z12.......`):
  the standard controls (CLAUDE.md rule 1), and GnuCOBOL's behaviour for this case is a divergence, as for INSPECT (D-INS1, PB4115).
  Only a DATA-ITEM sender or delimiter that can share identifier-3's storage (D-STR1, D-STR2) is read again at its transfer.
  Pinned by `conformance:cases/14.9.43-STRING/pb1908_string_overlap_function` and held by `unit:StringLiveReadDriftTests`
  (`FunctionOverTheReceiver_IsEvaluatedOnceUpFront_AndNeverStoresBetween`, `RandomOverTheReceiver_IsEvaluatedOnce`).
