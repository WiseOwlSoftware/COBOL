## PB5622: UPPER-CASE, LOWER-CASE and REVERSE raise EC-ARGUMENT-FUNCTION on an argument that is zero-length at run time

A group whose only content is an `OCCURS 0 TO n DEPENDING ON` table at 0 occurrences is a zero-length item (§2023:8.5.4 item 2).
§2002:15.65.2 AR1, §2002:15.78.2 AR1 and their 2023 twins (§2023:15.78.3, §2023:15.97.3, §2023:15.57.3 AR1) require at least one
character position, and §2002:15.2 AR13 / §2023:15.3 AR14 make a violating argument set EC-ARGUMENT-FUNCTION. The static MinWidth
screen cannot see a run-time width, and `CobolIntrinsics.UpperCase`, `LowerCase`, `Reverse` and the two `CobolLocale` carriers
(LOCALE phrase, CHARACTER CLASSIFICATION) had no empty arm, so a reference returned an empty string and set nothing. The re-probe on
the Debug build at `--std 2002` printed `AFTER R1=      EXC=` and exited 0.

The fix is one guard, `CobolIntrinsics.CaseArgumentZeroLength`, which raises through `ExceptionState.ArgumentErrorZeroLength`, so
checking-off still answers the zero-length value (row DOC-A.1-90's class: the result's length is the argument's). `CobolLocale.LowerCase`
had also served as the ANYCASE fold of FIND-STRING, SUBSTITUTE and NUMVAL-C, whose arguments may be legally zero length, so the fold
became `CobolLocale.FoldLower` and the function entry keeps the guard. Sweep: CONVERT, ORD and SUBSTITUTE already guard; FIND-STRING
returns zero by its own rule; DISPLAY-OF and NATIONAL-OF state no minimum.

Three goldens, each with a checking-off leg (zero-length value), a checking-on leg (declarative fires) and a legal-argument leg:
`15.65@2002-REVERSE-FUNCTION/pb5622-reverse-zero-length-2002`, `15.78@2002-UPPER-CASE-FUNCTION/pb5622-upper-lower-zero-length-2002`
and `15.78.3-Argument-rule/pb5622-case-reverse-zero-length` (2014, 2023). Rows `2002-AR-15.65.2-1` and `2002-AR-15.78.2-1` closed
(GAP 2258 to 2256 at 2002); the three 2023 rows gained the witness. No negative below 2002: 1985 refuses the `>>TURN` directive, not
this rule.
