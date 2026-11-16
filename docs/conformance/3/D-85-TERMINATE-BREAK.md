- **D-85-TERMINATE-BREAK — the control break at TERMINATE at COBOL-85 (kb/Work PB5824).** This entry speaks for COBOL-85. At
  TERMINATE the control break is at the highest level of the control hierarchy: FINAL when FINAL is written in the CONTROL
  clause, otherwise the major control. The NEXT GROUP clause of a control footing below that level is ignored
  (`§1985:XIII.3.16.4 GR3`). The 1985 text also supports taking the level from the most major control data-name
  (`§1985:XIII.4.7.4 GR1`); WiseOwl does not. Decided by the lane (a recorded determination): the text admits both readings,
  and GnuCOBOL does not model the rule (`libcob/reportio.c` prints every control footing at terminate with no level test),
  so its agreement with either reading would be an accident. The compiler's reading keeps the NEXT GROUP of the last footing
  of a sequence, the one at the highest level, which is FINAL's when FINAL is a control. Source:
  `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, section 8. Witnesses: no tracked conformance case pins the choice
  yet; `conformance:cases/XIII.3.16@1985-NEXT-GROUP-CLAUSE/g85_bf36_03` is the row's own case for the clause.
