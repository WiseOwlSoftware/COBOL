- **D-85-COUNTER-QUAL — an unqualified LINE-COUNTER or PAGE-COUNTER in the Procedure Division at COBOL-85 (kb/Work
  PB5800).** This entry speaks for COBOL-85. An unqualified LINE-COUNTER or PAGE-COUNTER in the Procedure Division is
  resolved among the reports of the nearest program that describes a report visible at the reference: the program the
  reference is written in or, when that program describes none, the nearest containing program with a GLOBAL report. It
  refers to that program's report when there is exactly one and must be qualified when there is more than one. The report
  descriptions of a contained program are not counted against its container, and a container's GLOBAL report is not counted
  against a contained program that describes a report of its own. The 1985 text leaves the count open (`§1985:IV.4.3.8.1
  SRL2.9`, `§1985:XIII.3.5.6 SR3`); GnuCOBOL counts per program. Decided by the precedence of CLAUDE.md rule 1: the text
  leaves the count open, and GnuCOBOL decides (`cobc/parser.y` keeps `report_count` per program and refuses an unqualified
  LINE-COUNTER only when it is above one). The contained program's own report and the container's GLOBAL report
  (`§1985:X.4.4.4 GR1`: "the special registers LINE-COUNTER and PAGE-COUNTER are global names" in the report description
  entry that contains the GLOBAL clause) are the two readings the sentence settles; the compiler does what it says
  (`CounterResolver.ReportOf`: the enclosing report description first, then the nearest visible report, exactly one or
  COBOL2729). Source: `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, section 7. Witnesses (the rulings' probes `lc1.cob`
  and `lc2.cob`, made cases): `conformance:cases/IV.4.3.8@1985-Uniqueness-of-Reference/pb5800_line_counter_contained_report_85`
  (one report description in the container and a second in the contained program) and
  `conformance:cases/IV.4.3.8@1985-Uniqueness-of-Reference/pb5800_line_counter_own_report_over_global_85` (a GLOBAL report
  in the container and the contained program's own).
