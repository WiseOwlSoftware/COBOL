## PB5566: RECORD KEY and ALTERNATE RECORD KEY refuse a name declared twice, even when one copy lies in the file's records

`FileControlBinder.ResolveFiles` resolved the two key clauses with a local `InRecords` that preferred the candidate inside the file's records and counted only those. `RECORD KEY IS K` therefore compiled and ran when K was also declared in working storage, where every other reference (FLD, DEPENDING ON CNT, ASCENDING KEY IS FLAG) draws COBOL1639. §2023:12.4.5.12.3 SR2 and §1985:IX.2.7.3 SR2 put the key inside a record of the file; that constrains the item found and does not choose between two items. §2023:8.4.2.2.3 SR1 requires qualification, and §1985:IX.2.7.3 SR1 allows it for data-name-1.

Re-probe on the base: both clauses, `--std 85` and `--std 2023`, printed `RK`. Fix: the candidates from `QualifiedCandidates` go whole to `UniqueOrReportAmbiguous`; the key screen's SR2 row still judges the one item. The fix is edition-neutral (the clause is refused at 85, 2002, 2014 and 2023); `K OF FR` still compiles. SOURCE-phrase operands share the arm. RELATIVE KEY, PADDING and RECORD DEPENDING already count survivors through `ResolveClauseOperand`.

Goldens: `pb5566-record-key-ambiguous` and `pb5566-alternate-key-ambiguous`, each refused at every edition. The note's closes_rows stay empty: the 1985 inventory rows are bridges that work.py refuses (PB5094).
