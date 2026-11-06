## CONCAT refuses a numeric literal beside a national argument

`FUNCTION CONCAT(7 NN)` with NN usage national compiled and printed `7AB`, although ISO 15.18.3 r2 requires every argument to be national when any is.
The numeric-literal arm of `IntrinsicBinder.CheckConcatArgs` always continued before the r2 usage test (the sibling of PB4766 review R1-3, which fixed the
numeric-item arm). An unsigned integer literal now falls through to r2 with its implied usage DISPLAY (9.3.6 SR3) and is refused COBOL1627.
Witness: `conformance-suite/wiseowl/cases/15.18.3-Argument-rules/pb4801-concat-numeric-literal-beside-national.cob`. kb/Work PB4801.
