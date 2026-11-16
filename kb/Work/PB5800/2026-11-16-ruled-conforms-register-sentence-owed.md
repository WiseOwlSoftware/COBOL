**2026-11-16, ruled CONFORMS as a recorded determination, register sentence owed (the Mythos lane's rulings on the ten open readings of the 1985 code reads; row `1985-SR-IV.4.3.8.1-L2.9`; tracked copy `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, section 7).** The section follows unchanged. The note stays open until the sentence is in the register and the store's record is adjudicated.

## 7. An unqualified LINE-COUNTER counts the report descriptions of the program it is written in (PB5800): a recorded determination

**Ruling: CONFORMS. The text does not decide it; GnuCOBOL does; the choice is recorded.**

`§1985:IV.4.3.8.1 SRL2.9` requires qualification "if more than one report description entry is specified in the
source program", and `§1985:XIII.3.5.6 SR3` "If more than one LINE-COUNTER exists in a program". Neither says
whether a program's count includes the report descriptions of a program it contains, or a containing program's
GLOBAL report (`§1985:X.4.4.4 GR1`: "the special registers LINE-COUNTER and PAGE-COUNTER are global names"). Both
readings are honest, as the earlier ruling found.

GnuCOBOL counts per program: `cobc/parser.y` keeps `report_count` (line 216), clears it in
`clear_initial_values` (line 1267), which `setup_program` calls for every program after the first, contained or
not (line 1462), and refuses an unqualified LINE-COUNTER only when that count is above one (lines 18998 to 19012).
So `lc1.cob` (one report description in the container, a second in the contained program, the container's
reference unqualified) and `lc2.cob` (a GLOBAL report in the container, the contained program's own report, the
contained program's reference unqualified) both compile there, and the second means the contained program's own
report. The compiler does the same in both (`OUTER 01`; `IN1 03`): `CounterResolver.ReportOf` takes the one
report of the nearest program that describes a report, and refuses (`COBOL2729`) when that program describes more
than one.

**Register sentence owed (1985, `docs/conformance/`):** "An unqualified LINE-COUNTER or PAGE-COUNTER in the
Procedure Division is resolved among the reports of the nearest program that describes a report visible at the
reference: the program the reference is written in or, when that program describes none, the nearest containing
program with a GLOBAL report. It refers to that program's report when there is exactly one and must be qualified
when there is more than one. The report descriptions of a contained program are not counted against its
container, and a container's GLOBAL report is not counted against a contained program that describes a report of
its own. The 1985 text leaves the count open (`§1985:IV.4.3.8.1 SRL2.9`, `§1985:XIII.3.5.6 SR3`); GnuCOBOL counts
per program." The sentence is what `CounterResolver.ReportOf` does (read on main `c96e8fc9b0`: the enclosing
report description first, then `Reports.NearestInScope` over the visible reports, exactly one or `COBOL2729`).
The GLOBAL case stands on `§1985:X.4.4.4 GR1` alone: GnuCOBOL gives a contained program no counter of a
container's report. PB5800 closes when the entry is in the register.
