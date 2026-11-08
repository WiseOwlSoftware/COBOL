## Entry 3302 — 2026-11-07 22:00 PST — PB4623 discharged: a function's out-of-range value raises one condition alone and beside an operator

Wave 20261921 group C. Re-probed PB4623 on a fresh Debug build: `COMPUTE R = FUNCTION EXP10 (400)` and `COMPUTE R = 0 + FUNCTION EXP10 (400)` both end with EC-SIZE-OVERFLOW unchecked, both raise EC-SIZE-OVERFLOW under `>>TURN EC-SIZE CHECKING ON`, and both keep R under ON SIZE ERROR; EXP (1000) and EXP10 (-400, EC-SIZE-UNDERFLOW) agree the same way. The intrinsic checks the binary64 range itself (§2023:14.7.5 item 5, DOC-A.1-179, landed by PB1196/PB1147/PB1581), so no infinity reaches `CobolFloat.ToScaledApproximation` or `CobolDec.RejectNonFinite` from a function. The 2026-11-07 audit had read the code only. No compiler change; the note is retired as discharged and claims no inventory row.

Pinned: conformance case `pb4623_function_overflow_alone_and_beside_operator` (accept 2002) and refused twin `pb4623_function_overflow_below_2002` (COBOL1502 at 85). Codes COBOL6550-6552 unused.

