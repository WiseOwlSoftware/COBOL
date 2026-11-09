## PB5022: a COBOL-85 same-group SUM crossfoot compiles at --std 85

**Re-probe.** A control footing group whose entry TOT writes `SUM SA SB`, over the sum counters SA and SB of its own
group, was refused at `--std 85` with COBOL2730 quoting 2023:13.18.54.3 SR4 b) ("shall be a repeating item"). That rule is
COBOL-2002's (2002:13.16.52.2 SR3 b)). COBOL-85's SUM clause has no repetition: 1985:XIII.3.19.3 SR1 admits a sum counter
"defined ... in the same report group that contains this SUM clause", and 1985:XIII.3.19.4 GR9 a) names the addition a
crossfoot. A rejects-legal defect at 85.

**Fix.** `ReportSectionBinder.RolledRepetitionFault` (now an instance method) returns no fault for the same-group arm when
the compilation's edition is below 2002; the other arms (different group, SR4 c) and d)) are unchanged, since 85 has no
OCCURS in a report group to make them differ. The program prints `04  06  010` at 85 and is still refused at 2002, 2014
and 2023.

**Goldens.** conformance `13.18.54-SUM/pb5022_report_sum_crossfoot_same_group` (accept 85; two details of 2 and 3, SA = 4,
SB = 6, TOT = 10) and the negative `pb5022-sum-crossfoot-same-group` (refused at 2002, 2014, 2023). The older negative
`pb1294-sum-data-name-same-group-not-repeating` was refused at 85 only through this defect (it puts SUM in a detail, which
1985:XIII.3.19.3 SR3 refuses for its own reason, a separate lead); its `refuse-at` drops 85.

**Rows.** None closed: 1985-SR-XIII.3.19.3-1 has more sentences than this change witnesses and stays with the 1985 audit.
