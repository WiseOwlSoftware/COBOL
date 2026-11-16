**2026-11-16, ruled CONFORMS by the 1989 text (the Mythos lane's rulings on the ten open readings of the 1985 code reads; row `1989-FMT-A.2.34.2`; tracked copy `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, section 5).** The section follows unchanged. The note stays open until the store's records are adjudicated on the next records branch.

## 5. One ALL table may supply argument-1 and the argument-2 series of PRESENT-VALUE (PB5834)

**Ruling: CONFORMS. The 1989 text controls.**

`§1989:A.2.2 AR4` gates the ALL subscript on the function, not on a position ("When the definition of a function
permits an argument to be repeated a variable number of times"), and it defines what the reference means: "the
effect is as if each table element associated with that subscript position were specified". A reference is
therefore legal when the reference it stands for is legal. `PRESENT-VALUE (WS-E (ALL))` over four elements stands
for `PRESENT-VALUE (WS-E (1) WS-E (2) WS-E (3) WS-E (4))`, which the general format admits and which the compiler
evaluates to the same 60. The reading that ALL may stand only where the repeated argument stands needs words the
rule does not have. GnuCOBOL has nothing to say: its parser has no ALL subscript. No register entry is owed.
PB5834 closes.
