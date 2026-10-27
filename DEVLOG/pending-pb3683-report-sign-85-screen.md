## A report group entry's SIGN clause gets its COBOL-85 screen (PB3683)

PB1319 gave the data description entry's SIGN clause a COBOL-85 subject screen and left the report arm to a note.
PB3683 asked for the 1985 report-writer wording, which the repository did not hold. The archived post-ballot text of
ISO/IEC 1989:2002 (v200112.txt, the P2 draft in the `archived-cobol-drafts` reference) holds the 2002 change list, and
its Annex F.2 item 123 states the edge in the standard's own words: "SIGN clause in a report description entry. The
SEPARATE phrase is no longer required in a report description entry and the SIGN clause may be specified at the group
level." (item 115 repeats the first half). Item 124 also quotes PB1319's first edge: "A sign clause may be specified on
any group item. Formerly, the group had to contain at least one numeric item."

**Re-probe.** On this build, before the fix, all of these compiled clean at --std 85: an elementary report entry
`SIGN LEADING` without SEPARATE (printed the over-punch `}05`), a level-1 group `SIGN TRAILING` without SEPARATE (`00N`),
a level-1 group and a LINE group each with `SIGN ... SEPARATE` (`-005`, `005-`). The elementary SEPARATE control stayed
legal.

**Fix.** Two construct rows introduced at 2002, `report-sign-without-separate-2002` and `report-group-sign-2002`
(COBOL0900), asked by one screen, `DataBinder.ScreenReportSignClauses`, once per written entry from `BindReportGroups`.
The note suggested the elementary subject test inside `BindReportEntry`; that site is per replay and runs only for entries
with a COLUMN clause, so a SIGN on a LINE or level-1 group never reached it. The group-versus-elementary question the
flat report screens ask is now one reader, `IsReportGroupEntry`, which `ScreenReportEntryClausePresence` also uses.
VCR row 7.40 and CONFORMANCE.md D-SIGN2 carry the quoted edge.

**Goldens.** `2002/pb3683_report_sign_2002` (expected `}05  00G` and `00N  00G` from GR5 and the DOC-A.1-177 over-punch
table, computed before the run), negatives `pb3683-report-sign-without-separate-85` and `pb3683-report-group-sign-85`.

**Not fixed, registered.** Annex F.2 item 115 also lists FILLER in the report section, the `+`/`-` symbols for PLUS/MINUS
and arithmetic-expression/ROUNDED SOURCE as 2002 introductions; all three still compile clean at --std 85.
