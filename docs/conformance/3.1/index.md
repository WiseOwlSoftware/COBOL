---
heading: "### 3.1 Implementor behavior in Annex A.2 undefined cases (kb/Work PB1907)"
---
Annex A.2 lists situations whose results the standard leaves undefined, and §2023:4.4 2) says that "A COBOL run unit that
allows these situations to happen is a conforming run unit, although the resultant execution is not defined by
standard COBOL" (`cite.py --check 4.4 "A COBOL run unit that allows these situations to happen is a conforming run
unit"` → OK 2)). This is NOT a §8 "no observable obligation" derivation: for each case below the owner decided
(2026-10-01) to DOCUMENT what WiseOwl COBOL does, align it with GnuCOBOL where GnuCOBOL measurably differs (CLAUDE.md
rule 1: the standard where it controls, otherwise GnuCOBOL; IBM Enterprise COBOL and Micro Focus are consulted only
where GnuCOBOL has nothing, and here GnuCOBOL 3.2 was built and run on every repro), and pin the documented behavior
with a golden so it cannot drift. The documented behavior is a promise about THIS compiler, never a statement about
what the standard requires. For the overlapping-operand cases IBM Enterprise COBOL 6.4 calls the result
"unpredictable" (Language Reference, "Overlapping operands") and Micro Focus calls it "undefined"; neither documents
a result, so GnuCOBOL 3.2 (libcob, measured on every case below) is the comparator.
