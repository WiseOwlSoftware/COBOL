## PB5706: a prototype's SPECIAL-NAMES holds only LOCALE, CURRENCY and DECIMAL-POINT at 2002 and 2014

`PrototypeUnitRules.ScreenBody` applied the COBOL-2023 list of five clauses (§2023:10.6.2 SR4c) at every edition. COBOL-2002 and COBOL-2014 list three: "the LOCALE clause, the CURRENCY clause, and the DECIMAL-POINT clause" (§2002:10.5.2 SR5c; §2014:10.6.2 SR4c). A program prototype with `ALPHABET AL IS NATIVE.` compiled clean at `--std 2002` (re-probed on this build: exit 0, "OK").

Fix: `DialectBehavior.PrototypeSpecialNamesAdmitAlphabetAndSymbolic` (VCR row 7.125, from 2023) is the one predicate; `AdmittedInPrototype` asks it for the ALPHABET and SYMBOLIC CHARACTERS arms, and COBOL2272's message names the clause list of the edition. Method prototypes share the screen and the three editions' rule text covers all three prototype kinds.

Goldens: `pb5706-prototype-special-names-alphabet-2023` (accepted at 2023, output OK), refusals `pb5706-prototype-special-names-alphabet` and `-symbolic` at 2002 and 2014 (COBOL2272). Row `2002-SR-10.5.2-5` GAP to CONFORMS (GAP 1136 to 1135). Sibling sweep: SR4 a, b, d, e, f read identically in all three editions.
