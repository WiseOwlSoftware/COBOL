**2026-11-16, ruled CONFORMS as a recorded determination, register sentence owed (the Mythos lane's rulings on the ten open readings of the 1985 code reads; row `1985-GR-XIII.3.16.4-3`; tracked copy `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, section 8).** The section follows unchanged. The note stays open until the sentence is in the register and the store's record is adjudicated.

## 8. TERMINATE's control break is at FINAL when FINAL is a control (PB5824): a recorded determination

**Ruling: CONFORMS. The text admits two readings, GnuCOBOL does not model the rule, the lane decides and records it.**

`§1985:XIII.3.16.4 GR3` ignores NEXT GROUP on a control footing "that is at a level other than the highest level
at which a control break is detected". At TERMINATE no break is detected; `§1985:XIII.4.7.4 GR1` has the report
writer "produce all the control footing report groups beginning with the minor control footing report group". The
compiler takes that as the break at the top of the hierarchy, which is FINAL when FINAL is written
(`§1985:XIII.3.7.4 GR1`: "FINAL, if specified, is the highest control, data-name-1 is the major control"), so with
`CONTROLS ARE FINAL MAJ` the footing of MAJ is below the highest level and its NEXT GROUP is ignored. The other
reading takes the level from the same rule's last clause, "as though a control break has been sensed in the most
major control data-name", so that the footing of MAJ keeps its NEXT GROUP whether or not FINAL is written.

Reasons for the compiler's reading. That clause is the end of the sentence about the values SOURCE clauses and USE
procedures see ("makes the prior set of control data item values available"), and FINAL has no value to make
available; it does not say at which level the footings are produced. GR3 keeps the NEXT GROUP of the LAST footing
of a sequence, the one at the highest level; at TERMINATE with FINAL a control the last footing is FINAL's, and
under the other reading a NEXT GROUP written on the FINAL footing could never take effect. Neither reason makes
the other reading impossible, so this is a determination and not a reading the text forces.

GnuCOBOL cannot arbitrate: `libcob/reportio.c` saves a footing line's NEXT GROUP (`set_next_info`, line 247) and
applies it only when a later line is printed (`report_line`, lines 958 to 988), and `cob_report_terminate` (line
1419) prints every control footing with no level test. It has no rule 3 at any break, so its agreement with
either reading on one program would be an accident.

**Register sentence owed (1985, `docs/conformance/`):** "At TERMINATE the control break is at the highest level of
the control hierarchy: FINAL when FINAL is written in the CONTROL clause, otherwise the major control. The NEXT
GROUP clause of a control footing below that level is ignored (`§1985:XIII.3.16.4 GR3`). The 1985 text also
supports taking the level from the most major control data-name (`§1985:XIII.4.7.4 GR1`); WiseOwl does not."
PB5824 closes when the entry is in the register.
