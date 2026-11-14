- **D-RW-PAGE-ADVANCE-CONTROLS — Report Writer — which control values a page group sees during a control footing's page advance (§2023:13.18.16.4
  GR4 a) from 2002; §1985:XIII.3.20.4 GR13 at `--std 85`)**: §2023:13.18.16.4 GR4 a), and the 2002 text, give "any
  reference to a control data item" the PRIOR value while the footings print, the page heading and page footing a
  footing's page advance reaches included. §1985:XIII.3.20.4 GR13 gives the prior values only to a control footing's own
  USE procedure and SOURCE clauses (a), and at TERMINATE to the control footings and the report footing (b); "All other
  data item references within report groups and their USE procedures access the current values" (c). **Pinned choice:
  PER EDITION, each edition's own text (kb/Work PB5128).** At `--std 85` the page footing, page heading and OR PAGE
  control headings of the advance, and the TERMINATE page footing, see the CURRENT values (`CobolReport.WithPageGroupControlValues`,
  `DialectBehavior.ControlFootingPageGroupsSeePriorControlValues`, VCR row 7.54); from 2002 they see the prior ones.
  Witnesses: `conformance:cases/13.18.16-CONTROL/pb5128_footing_page_groups_control_values` and `.../pb5128_terminate_page_footing_control_values` (each `.85.out` and `.out`).
  The sum-counter side of the same 1985 rule (GR10 a)–c): crossfoot and roll forward BEFORE the USE procedure) is
  every edition's order, §2023:14.9.49.4 GR9 b) saying the same. A group that owes such additions takes the presence
  snapshot §2023:13.18.54.4 GR11 reads for them BEFORE the USE procedure, and takes the snapshot its printing reads
  AFTER it (the determination of kb/Work PB367b stands: a declarative decides its own group's presence).
  Witness `conformance:cases/14.9.49-USE/pb5127_use_before_reporting_sees_crossfoot` (85 only: a same-group addend is
  §2023:13.18.54.3 SR4 b) 's repeating-item case from 2002) (kb/Work PB5127).
