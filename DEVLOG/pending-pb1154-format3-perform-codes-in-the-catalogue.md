## The exception-checking PERFORM's syntax-rule codes move into the diagnostic catalogue, and every ban reports at its own statement

kb/Work PB1154 named five raw codes (COBOLNET1605, 1606, 1607, 1612, 1613) and three defects: absent from
`DiagnosticCatalog`, untested, and positioned at the PERFORM rather than the offending statement. The re-probe on this
tree found the first and third true and the second stale, and found the problem wider than the note: the whole
Format-3 PERFORM family was written as bare `ctx.Edition.Error("COBOLNETnnnn", ...)` literals, 21 sites for
COBOLNET1597, 1599, 1600, 1601, 1604 and 1605 to 1617. A probe with a multi-file CLOSE, a DELETE FILE, a VALIDATE and a
WHEN-phrase GO TO on lines 20 to 24 printed all four errors at `(18,12)`, the PERFORM's own position.

Why the family stayed raw is the root. `DiagnosticRegistryDriftTests.EveryEmittedCode_IsACatalogDescriptor` scans for
emit calls with a look-behind that skips a dotted call, so `ctx.Edition.Error("COBOLNET16nn", ...)` was invisible to
it; the semgrep `cobolnet-raw-diagnostic-code-literal` baseline (244 findings) was the only count, and nothing pinned
this family. The positions were wrong for a structural reason: the bans run after imperative-statement-1 has bound,
when the diagnostic cursor is back at the PERFORM, and each of eleven hand-written loops reported without moving it.

The fix is one shape. Seventeen descriptors join `DiagnosticCatalog`, one per emitted code (1598, 1602, 1603 and 1609
are reserved numbers no rule emits), each quoting its syntax rule; every citation went through `cite.py --check`. One
reporter, `EcBinder.Ban<TStmt>`, replaces the eleven loops and positions each report at the offending statement, so the
next ban is one more call and positions itself. The SR14 and SR15 repeat reports at the repeat, SR16 at the WHEN item.
Two consequences are measured by tests: two offending statements in one PERFORM are now two reports (before, both
carried the PERFORM's position and text, and the second was dropped as a repeat), and an offender inside a nested
Format-3 PERFORM is still reported once. COBOLNET1604 and COBOLNET1610, which the verb binders report at the statement
already, moved to their descriptors too.

A sibling in the same function: the GR16 FINALLY transfer-out report (COBOLNET2927) was positioned at `Column + 1`, one
column right of every other statement diagnostic, and the MESSAGE-TAG exclusivity report in `DataBinder` (COBOLNET2158)
did the same. Both now use `Edition.At(context)`. The semgrep count fell from 244 to 228 and its baseline was updated.
`DiagnosticRegistryDriftTests.ExceptionCheckingPerformCodes_AreCatalogDescriptors` pins the family: no source file
outside the catalogue writes one of its codes as a quoted literal, and each emitted code has an error descriptor.

Witnesses. Ten new refusal goldens in `conformance-suite/cases/`, one for each code that had none (1597, 1600, 1604,
1607, 1608, 1610, 1612, 1613, 1614, 1617), each registered in `diagnostics.json`; the harness check is green.
`ExceptionPerformBanPositionTests` asserts the line and column of all 17 codes against the statement text, with the
two-offender, nested and legal-twin cases. `PerformFormat3IoStatementTests` covers the abnormal I-O paths the
campaign requires for the legal single-operand OPEN, CLOSE and DELETE FILE inside the PERFORM, with statuses taken from
the standard: OPEN INPUT of an absent file is 35 (an EC-I-O-PERMANENT-ERROR, ISO 9.1.13.6 item 5), CLOSE of an unopened
file is 42 and OPEN or DELETE FILE of an open file is 41 (EC-I-O-LOGIC-ERROR, 9.1.13.7), and DELETE FILE of an absent
file is the successful 05 (9.1.13.2 item 4 b, 14.9.10.4 GR20), raised only as EC-I-O-WARNING. Each is run with and
without FILE STATUS, with a matching WHEN, with none (fatal), with checking off, with the GR14 implicit enable, and
with a USE declarative for an unmatched WHEN (resumed, and left to the fatal action). Writing the checking-off test
first caught my own mistake, not a compiler defect: a WHEN naming EC-I-O enables checking over imperative-statement-1
(GR14), so the handler ran, correctly.

The register: PB1154 is landed with no inventory row to close (`closes_rows_reason` says why). Self-review against the
test, silent-failure and comment lenses found nothing further; the one thing left open is the root hole itself, the
look-behind in `EveryEmittedCode_IsACatalogDescriptor`, which would flag the remaining 228 raw sites at once and is
a campaign of its own.

Branch review, round 1 (two findings, both fixed). First, the one-column-late cursor fix was incomplete: the expression
formation pass positioned COBOLNET1719 (a stacked unary sign, a stacked B-NOT, a compound shift count) at
`token.Column + 1` in three places, the same defect fixed for COBOLNET2927 and COBOLNET2158. The pass now uses
`Sink.At(token)`. `ExpressionFormationPositionTests` asserts the three at the column of their offending token, with
legal twins (the test fails against the old code: it printed column 20 for a sign at column 19), and
`DiagnosticCursorColumnDriftTests` forbids adding 1 to a `Column` anywhere under `src/` (the one exemption is
`SourceLocation.ToString`, which renders a 0-based record and is not a cursor). Second, the abnormal I-O paths the
campaign requires were not all covered. `PerformFormat3IoConflictAndSortTests` adds 42 cases: lock contention under a
RETRY phrase (OPEN OUTPUT answers 61, DELETE FILE answers 62, ISO 9.1.13.9) delivered to a WHEN, to the file-paired WHEN,
to the USE declarative, to nothing (EC-I-O-FILE-SHARING is nonfatal, the run goes on) and with checking off; OPEN 39 (a
fixed-attribute conflict) and 37 (a read-only file, with the host's ability to refuse measured through
`HostCapability`), both fatal; an OPTIONAL file's successful 05 (EC-I-O-WARNING, only when enabled); CLOSE 42 and DELETE
FILE 41 with a USE declarative and with no FILE STATUS; AT END and INVALID KEY with and without their own phrase (the
phrase wins, ISO 9.1.13.1); and the SORT and MERGE statements around the PERFORM: a table SORT in imperative-statement-1,
a file SORT or MERGE in a WHEN phrase with the USING file absent (35, the SORT terminated, the run unit goes on, no GIVING
file created), absent with no FILE STATUS and no USE (fatal), a USING file open (EC-SORT-MERGE-FILE-OPEN) and a USING
file out of key order (EC-SORT-MERGE-SEQUENCE). Two things the tests showed that I had guessed wrong, both the
compiler being right: a USE declarative for a file still runs with checking off (ISO 14.9.49.4 GR6 does not condition it
on a TURN directive), and a WHEN body binds under TURN OFF ALL (14.9.28.4 GR14), so the SORT-MERGE conditions are raised
inside it only after a `>>TURN EC-SORT-MERGE CHECKING ON` written in the body. No compiler defect turned up.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 11,257 of 17,595, Unit 33,777 of
34,093, Characterization 5 of 36 ran; the 47 added Conformance cases and the new Unit drift test passed in leg 1).
