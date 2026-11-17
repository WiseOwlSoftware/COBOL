- **D-85-TERMINATE-BREAK — the control break at TERMINATE at COBOL-85 (kb/Work PB5824).** This entry speaks for COBOL-85. At
  TERMINATE the control break is at the highest level of the control hierarchy: FINAL when FINAL is written in the CONTROL
  clause, otherwise the major control. The NEXT GROUP clause of a control footing below that level is ignored
  (`§1985:XIII.3.16.4 GR3`). The 1985 text also supports taking the level from the most major control data-name
  (`§1985:XIII.4.7.4 GR1`), and GnuCOBOL does not model the rule; the choice follows IBM's COBOL Report Writer (Programmer's
  Manual SC26-4301-04, section 3.13.3 rule 5: 'If the level of the break is higher than the level of this group, the NEXT
  GROUP clause is ignored', with its end-of-report example). Source: `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`,
  section 8, and `kb/Work/PB5824/2026-11-16-the-vendor-step.md`. Witnesses:
  `conformance:cases/XIII.3.16@1985-NEXT-GROUP-CLAUSE/pb5824_terminate_break_at_final_85` (kb/Work PB5824's probe
  `m3tfin.cob`, made a case) pins the choice; `conformance:cases/XIII.3.16@1985-NEXT-GROUP-CLAUSE/g85_bf36_03` is the
  row's own case for the clause.
