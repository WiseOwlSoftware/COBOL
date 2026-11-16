**2026-11-16, two more register sentences are owed, both 1985 determinations (the Mythos lane's rulings on the ten open readings of the 1985 code reads; tracked copy `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, sections 7 and 8).** Each paragraph below is copied unchanged from the section that makes it; each closes the note named with it when the entry is in `docs/conformance/`.

**Row `1985-SR-IV.4.3.8.1-L2.9`, kb/Work PB5800 (section 7).**

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

**Row `1985-GR-XIII.3.16.4-3`, kb/Work PB5824 (section 8).**

**Register sentence owed (1985, `docs/conformance/`):** "At TERMINATE the control break is at the highest level of
the control hierarchy: FINAL when FINAL is written in the CONTROL clause, otherwise the major control. The NEXT
GROUP clause of a control footing below that level is ignored (`§1985:XIII.3.16.4 GR3`). The 1985 text also
supports taking the level from the most major control data-name (`§1985:XIII.4.7.4 GR1`); WiseOwl does not."
PB5824 closes when the entry is in the register.
