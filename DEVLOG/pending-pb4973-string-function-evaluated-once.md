## PB4973: a STRING evaluates a function-identifier once (D-STR4 reversed to the standard)

`STRING "xy" FUNCTION UPPER-CASE(F) INTO F` over `F = "abcdefgh"` gave `xyXYCDEF`: `StringEmitter.ReadOperand`/`ReachOf` evaluated a later
sending's function a second time at its transfer, from the storage the earlier transfers had stored into (D-STR4, following GnuCOBOL's
`cob_string_append`). The standard defines the result. A function-identifier references a temporary data item (§2023:8.4.3.2.4 GR1), so no transfer
can store into it and GR10's overlap never reaches it, and §2023:14.6.4 7) evaluates the identifiers of a statement left to right as the first
operation. One evaluation at identification gives `xyABCDEF`. Rule 1 puts the standard before GnuCOBOL; the divergence is recorded in D-STR4, as PB4115
did for INSPECT.

Re-probe on this build before the fix: the golden printed `F1 [xyXYCDEF]`, `F2 [z12.......]`. Fix: `ReadOperand` reads every operand once in statement order;
`ReadsStorageOf` answers only for a data-item reference (D-STR1, D-STR2 keep their in-place re-read); the `Reach` record and its three `ReachOf` overloads are
deleted, so FUNCTION RANDOM and a user-defined function need no special case. The golden `pb1908_string_overlap_function` now expects `xyABCDEF` and
`z12Z34....`; `StringLiveReadDriftTests` pins one evaluation and one store. Row GR-14.9.43.4-10 stays CONFORMS with restated notes and witnesses.
