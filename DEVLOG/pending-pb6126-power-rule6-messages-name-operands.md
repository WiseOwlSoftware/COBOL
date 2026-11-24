## PB6126 the rule-6 exponentiation refusals name the base and the exponent

Re-probe on the build: `COMPUTE R = B ** E` with B=0, E=-1 reported "a zero base requires an exponent greater than zero (ISO §2023:8.8.1.2 GR6a)" with no operand. `CobolPower.ScreenRule6(bool, bool, bool, bool)` could not name them.

Fix: the four-boolean screen is gone. `CobolPower.Classify` decides GR6a or GR6c once; `ScreenRule6(double, double)` and `ScreenRule6(CobolDec, CobolDec, bool)` call it and, only on a refusal, build the text through `Rule6Error` ("a zero base requires an exponent greater than zero, not {e}", "a negative base ({b}) requires an integer exponent, not {e}"). `CobolExact.Pow` keeps its binary64 overload (its exponent is an integer, so only the zero base can refuse). The COBOL-85 GR5c message now formats invariantly. After: the same probe says "... not -1".

Goldens: none; the text is run-time diagnostics, covered by `PowerRule6MessageTests` (five facts over the three carriers). No inventory row. Siblings swept: every `EC-SIZE-EXPONENTIATION` throw is in CobolPower (ExponentiationOwnerDriftTests green).
