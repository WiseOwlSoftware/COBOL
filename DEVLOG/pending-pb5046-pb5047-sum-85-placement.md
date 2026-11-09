## A COBOL-85 SUM clause is refused outside a control footing and over a SOURCE or VALUE entry

PB5046: 1985:XIII.3.19.3 SR3 admits a SUM clause only in a control footing report group; at --std 85 a SUM in a detail compiled. ScreenReportEntryClausePresence now refuses it with COBOL6763 below 2002 (2002 and later admit it, 2023:13.18.54.3 SR4 f).

PB5047: 1985:XIII.3.19.3 SR1 requires an identifier of the Report Section to reference a sum counter; at 85 a SUM naming a SOURCE or VALUE entry compiled. ScreenRolledAddend now refuses it with COBOL6764 below 2002 (2023:13.18.54.4 GR6 adds such an operand from 2002).

Both re-probed on the build before the fix and now refuse at 85 only. Goldens: pb5046_report_sum_in_detail and pb5047_report_sum_names_source_entry (accept 2002+, outputs derived from GR7 c, GR2, GR7 a) and the negatives pb5046-sum-not-in-control-footing and pb5047-sum-addend-source-entry. No inventory row closed. Siblings swept: the 2002 arms of both checks already agree with the 2002 text. Gate verdict: see the commit that follows.
