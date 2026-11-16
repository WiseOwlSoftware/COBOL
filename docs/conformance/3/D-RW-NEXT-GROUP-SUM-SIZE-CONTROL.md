- **D-RW-NEXT-GROUP-SUM-SIZE-CONTROL — Report Writer: the group after a saved NEXT GROUP integer, the time EC-REPORT-SUM-SIZE is raised, and the break test of a numeric CONTROL item (kb/Work PB5823, PB5643, PB5782)**.
  **Pinned choice: PER EDITION, each edition's own text, except (c), which is one rule at every edition.** (a) *the group after a saved NEXT GROUP (PB5823, VCR row 7.81)*: from 2002 the
  integer an absolute NEXT GROUP saved forces a page advance before the next non-dummy body group (§2023:13.18.37.4 GR4a); at `--std 85` that group meets the
  ordinary fit test first (§1985:XIII.3.10.8 GR3b: "If a body group has been presented on the page to which the report is currently positioned, the RWCS computes a trial
  sum"), so a control footing of LINE PLUS 0 at LINE-COUNTER = FOOTING stays on the page, and only a group that fails the test advances the page and reads the saved integer
  (`CobolReport.PresentBody`, `PlaceAfterSavedNextGroup`; `DialectBehavior.SavedNextGroupForcesPageAdvance`). (b) *when a SUM size error is raised (PB5643, VCR row 7.82)*:
  at the addition from 2014 (§2014:13.18.54.3 GR3, §2023:13.18.54.4 GR3: "the EC-REPORT-SUM-SIZE exception condition is set to exist"); at `--std 2002` the addition sets only
  the size error indicator (§2002:13.16.52.3 GR3) and the condition is raised when the entry is presented (GR4), as it is again from 2014 (`CobolReport.Accumulate`,
  `SumPresentable`; `DialectBehavior.SumSizeErrorRaisedAtAddition`). (c) *a numeric CONTROL item (PB5782, every edition)*: the break test is the comparison of two numeric operands
  (§1985:XIII.3.7.4 GR3 a; §2023:8.8.4.2.4: "Zero is considered a unique value regardless of the sign"), so a signed zero that changes sign is no break; an image-carried numeric
  control (zoned, packed, a REDEFINES view) is decoded through its profile before the comparison (`CobolReport.NumericControlEqual`). Witnesses
  `conformance:cases/13.18.37-NEXT-GROUP/pb5823_saved_next_group_fit_test` (`.85.out` and `.out`), `conformance:cases/13.18.54-SUM/pb5643_sum_size_error_raise_time` (`.2002.out` and `.out`) and
  `conformance:cases/13.18.16-CONTROL/pb5782_numeric_control_signed_zero`.
