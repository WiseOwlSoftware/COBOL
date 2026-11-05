## Entry 3137 — 2026-11-04 22:33 PST — PB4713: USE FOR DEBUGGING ON a declared cd-name is the staged subject, and a cd-name written as a data item is told what it is

**Re-probe.** `cobol --nist DB205A tests/nist/programs/DB205A.cob` on a fresh build gave `COBOL1639: 'CM-INQUE' is not defined` and the same for `CM-OUTQUE`, as the note said. A probe program with an input CD showed the same text for `ON CM-IN`, `ON ALL CM-IN` and `MOVE CM-IN TO T`, while a file-name in the same positions was already "declared as a file-name" (`ALL SF`) or the staged COBOL1571 (`ON SF`).

**Mechanism.** Two places did not know a cd-name. `ProcedureTableBuilder.FinalizeDebug`'s bare-name arm asked `NamesDataItem` and `NamesFile`, so a declared cd-name fell through to `ReportUnresolvedDebugOperand` and the reference resolver, which says "not defined". And the resolver's one non-data-name query (`DataBinder.NonDataDeclarationOf`, kb/Work PB1906) had no cd-name class, so every other data-item position (`ALL CM-IN`, `MOVE CM-IN TO T`) said the same.

**Fix.** `FinalizeDebug` asks the new `NamesCommunicationDescription` beside `NamesFile`, so a declared cd-name reaches the staged COBOL1571 (the program is right; the cd-name trigger of X3.23-1985's debug module is not modeled). `NonDataDeclarationKind.CdName` joins the one list `NonConditionDeclarationsOf` (depth 0: a cd-name is its source element's alone, ISO/IEC 1989:2002 8.4.5), and `DeclaredAs`/`ClassPhrase` word it, so `ALL CM-IN` and `MOVE CM-IN TO T` are COBOL1639 "declared as a cd-name (a communication description entry), not as a data item". The record's own data items (`ALL IN-Q`) stay the staged data-item subject.

**Not fixed, still staged.** The cd-name, file-name and data-name debug triggers are not modeled (docs/CONFORMANCE.md D-DEBUG-USE), so DB205A is still refused, now with a true message. Modeling them is the DEBUG-ITEM/DEBUG-CONTENTS instrumentation of the ENABLE, DISABLE, PURGE, RECEIVE and SEND statements plus the file and data triggers: a feature of its own, reported as a lead.

**Goldens.** `pb4713-debug-cd-name` (COBOL1571 at 85), `pb4713-debug-all-cd-name` (COBOL1639 at 85), `pb4713-move-cd-name` (COBOL1639 at 85 and 2002), and `DebugFacilityTests` rows for the bare, mixed and ALL operands. No inventory row is claimed or closed (the debug module has no ISO 2023 rule).

