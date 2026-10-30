## Entry 2812 — 2026-10-30 14:38 PDT — PB4124: a function argument held for left-to-right order keeps the description the unheld argument has

**What was wrong.** `FUNCTION LENGTH (FUNCTION MAX (A * B, 3))` answered 4 and `FUNCTION LENGTH (FUNCTION MAX (A * B, FUNCTION UDF (A)))` answered
8; the nested `MAX (MAX (C, 5), UDF (A))` answered 38 against 8 (C PIC 9(8)). DISPLAY of the result differed the same way (`+000000000000000000012.50000000000000000`
where the unheld call shows `+012.50`). One call, two answers, and only the later argument's activation differed. ISO §8.4.3.2.4 GR2 orders the
arguments' evaluation and nothing else, so the held call must answer what the unheld one does; the unheld answers were the right ones, being
the temporary descriptions of docs/CONFORMANCE.md DOC-A.1-92 (a copy of the selected argument's item, or an item the value describes).

**Re-probe.** Reproduced on the base build with the note's p6.cob (4 / 8 / 38). A matrix of 22 argument shapes x MAX, MIN, MEDIAN x DISPLAY, LENGTH,
BYTE-LENGTH (198 pairs, p7.cob) showed 141 held answers differing from the unheld ones: every expression argument, every nested selecting function,
DATE-OF-INTEGER, and the `Value`-form functions (INTEGER, NUMVAL). A plain data item, a literal and a binary64 function already agreed.

**Root.** `ArgumentOrder.Window.Settle` holds an earlier argument in a compiler temporary, and the temporary is a data item: `FunctionDeviceText.Field`
described it by its own digits (8 for A * B, 38 for a nested function's value). Holding an argument kept its value and lost its description.

**Fix.** Two mechanisms, one root. (1) A held expression or `Value`-form function is in a `FunctionValueSeparator`-flagged temporary, and `Field`
describes that temporary by its value, exactly as it describes the argument it replaced. (2) A nested function whose result copies an argument's item
(`FunctionDeviceForm.Argument`: ABS, MAX, MIN, MEDIAN, MEAN) chooses its description at run time, by which argument it
copies, so no value temporary can stand for it: `ArgumentOrder.Hold` holds the nested call's own arguments, recursively, at the place the nested
argument finished binding, and the call is read over them. `NeedsItsArguments` names every `FunctionDeviceForm`, so a new form is a compile error until it
says which kind it is. A user-defined function's argument is read for its value alone and is still held as a value (`Freeze`'s `describedValue` is false
there). A nested call that names a table(ALL) argument is held as a value as before, because an ALL operand is enumerated where the call renders and
has no recorded operand to hold; its description stays the value's (reported as a lead, same root as the direct `MAX (T (ALL), UDF (X))` reading the table
after the activation).

**Review round 1 (R1-1).** The first fix also took DATE-OF-INTEGER / DAY-OF-INTEGER (`FixedDigits`) into the hold-its-arguments arm, but a call whose arguments are all
stable holds nothing, so the call itself was read AFTER a later argument's activation and its fatal EC-ARGUMENT-FUNCTION came late (`MAX (DATE-OF-INTEGER (0), UDF (X))`
ran the UDF first), against GR2's left-to-right order. The fixed digits are no argument's description, so `FixedDigits` is held as a value at its place again and the
temporary carries the digits (`DataItem.FunctionDeviceDigits`, set in `SendingValueTemp.OfComputed`, read by `FunctionDeviceText.Field`); `ReadsArgumentDescriptions`
is true for `Argument` only. `HeldFunctionArgumentDescriptionTests.AFatalExceptionOfAnEarlierArgumentIsRaisedBeforeALaterActivation` pins the order for DATE-OF-INTEGER
(field and literal) and DAY-OF-INTEGER.

**Goldens and tests.** `pb4124_held_function_argument_keeps_description` (cases/15.4.1-Numeric-and-integer-functions: six measurements of one call held and
unheld, and GR2's own order, `MIN (MAX (O, 1), activation that adds 1 to O)` = 4) with its negative `pb4124_held_function_argument_below_2002`
(COBOL0900, the FUNCTION-ID introduction gate). `HeldFunctionArgumentDescriptionTests` pins the held/unheld agreement over the 22 shapes. The values
are not asserted: §15.4.1 leaves the description to the implementor, so the assertion is agreement.

**Self-review.** Architecture: the rule lives in two named places (the binder decides what is held, `Field` decides how a held value is described), no C# text
in a bound node. Full code: the replaced nested node keeps `ResultType` (PB4036) and `FunctionValue` through `with`. Performance: the extra recursion runs
only for a held argument of a nested selecting call. Duplication: none added; `IsStable` and `RootOf` moved up one level to serve `Hold`.

