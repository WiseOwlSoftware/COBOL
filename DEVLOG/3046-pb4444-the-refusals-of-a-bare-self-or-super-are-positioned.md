## Entry 3046 — 2026-11-03 13:50 PST — PB4444 - the refusals of a bare SELF or SUPER are positioned at the word

PB4153 made COBOL1639 ("'SELF' is not defined") the verdict for SELF and SUPER at an edition whose section 8.9 leaves them
free, and said every reader reports at the word. Two readers did not. `OoBinder.OoPredefinedSearchRoot`, which selects the
method through an INVOKE receiver and the object of an object property, passed its `qualifier` as the position, and
`qualifier` is the class name of `object-class-name-1 OF SUPER`: null for a bare SELF or SUPER. The diagnostic then landed on
the enclosing statement (`INVOKE SELF "M".` reported at the INVOKE, column 12, with the word at 19) or on the data reference
(`MOVE P OF SELF TO X.` at the `P`, column 17, with SELF at 22).

Re-probe on the build before the fix reproduced both. The same function reports its other refusals through the cursor the
caller left standing, so SR1 outside a method (COBOL6002, section 8.4.3.8.3), SR4 (COBOL2777) and a root class's SUPER had the
same habit; `INVOKE SELF "M"` in a program at 2002 drew COBOL6002 at column 12.

The fix is at the function that owns the position. `OoPredefinedSearchRoot` now takes the SELF or SUPER token itself (the
`selfAndSuper` term's first token for a bare word, the SUPER that ends `object-class-name-1 OF SUPER`, or the property object's
`SELF()` / `SUPER()` symbol) and holds the diagnostic cursor on it for every refusal it makes. `RefuseUnreservedPredefinedObject`
takes a token rather than a parse node, so each of its other readers (the OO identifier door, SET's sender classifier, both
`arg.Self` readers, the object view's identifier-1, RAISE) names the word by construction; none changed its answer.

Goldens: the two PB4153 negatives that were the defect (`pb4153-invoke-receiver-super-undeclared-at-85`,
`pb4153-property-of-self-undeclared-at-85`) now pin `(line,column)` in their `expect`, and the new
`pb4444-invoke-self-outside-method-position` pins COBOL6002 at the word at 2002, 2014 and 2023.

