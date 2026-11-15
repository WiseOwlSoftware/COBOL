## PB5619: a SET-family amount under standard arithmetic keeps its fraction and its magnitude

**The defect.** `SET pointer UP BY` under `ARITHMETIC IS STANDARD` (and every standard-decimal mode) rendered the amount as a
standard intermediate (SDIDI) and then lowered it through `NumericRenderer.Landed`, which lands an SDIDI at a fixed working
scale of 6 in an `Int128`. Re-probed on this build: `SET P UP BY ONE + ONE / 100000000000000000000` moved the pointer with no
condition (the 1E-20 fraction was truncated before the integrality test, §2002:14.8.35.3 GR16), and `SET P UP BY 10 ** 40`
ended the run with EC-SIZE-OVERFLOW instead of EC-RANGE-PTR (GR17).

**The fix, at the root.** A SET-family amount needs the fraction and the full magnitude, and no fixed scale gives both.
`NumericRenderer.LandedAmount` now leaves an SDIDI value an SDIDI (the unsigned-wide read and the float-under-standard
conversion are unchanged), and each consumer dispatches on `NumX.Carrier` into a new runtime overload that lands on the
SDIDI's own exponent: `SetAmount.Land(CobolDec, out Int128)` (integer / not an integer / beyond the carrier), reached by
`CobolPtr.UpByAmountDec`, `CobolIndex.TryAmountDec` and `StartKeyLength.OfDec`. The sibling arms swept and fixed the same
way: `SetEmitter.LandAmount` (SET index TO / UP / DOWN BY, PERFORM VARYING index FROM, SET capacity), `PtrEmitter`'s pointer
UP/DOWN BY, and `KeyedIoEmitter`'s START WITH LENGTH. ALLOCATE CHARACTERS keeps `Landed` (its rule is a round-up, not
integrality; not changed). Probe `SET IX UP BY` the same fraction raises EC-BOUND-SUBSCRIPT with the index unchanged, and
`DOWN BY 10 ** 40` raises EC-RANGE-INDEX.

**Goldens.** `conformance-suite/cases/14.8.35@2002-SET-STATEMENT/`: `set-pointer-by-standard-sdidi-amount` (GR16, GR17 and a
control, accepted at 2002), `set-index-by-standard-sdidi-amount` (the index sibling, GR3a), and
`set-pointer-by-standard-below-2002` (refused at 85, COBOL0900). `SetAmountTests` pins the three landings of the SDIDI lane.
2023 removed ARITHMETIC IS STANDARD, so the goldens stop at 2002.

**Register.** Rows `2002-GR-14.8.35.3-16` and `-17` re-verdicted CONFORMS (batch pb5619-set-ptr-std-2002; GAP 2258 to 2256).
