## PB4949: 37 unflagged analyses audited, 8 hidden defects surfaced, and work.py check now holds analyses to a harm flag

The owner asked for the 37 analyses to be investigated for hidden defects. They were the open `kind: analysis` notes that set
no harm flag and not `process_only`: `work.py next` ranks on harm flags alone, and `register_problems` refused that state only
for `kind: defect`, so none of them was ever ranked.

Four read-only adjudicators took one batch each (evidence and probes in E:/COBOL-coord/scratch/analyses-audit/). Eight
describe a user-visible defect. Five keep their note and gain the flag: PB4115, PB4623 and PB4641 (wrong answer), PB4765
(under-rejects) and PB4119 (misleading diagnostic). Three are filed as their own defects: PB4946 (the ambient run unit never
ends at process exit, so a host's committed changes are rolled back; DOC-A.1-167 pointed that divergence at the landed PB1253
and now points at PB4946), PB4947 (`IF LINAGE-COUNTER` compiles and aborts at run time) and PB4948
(`VALIDATE X OF G (1:3)` escapes §2023:14.9.50.3 SR1). PB3631 is flagged as a suspected wrong answer because its probe could not
build. PB4123, PB4417, PB4618 and PB4628 are retired as stale or determined. The other 27 are `process_only: true` with the
reason each audit gave.

`HARM_CHECKED_KINDS = ("defect", "analysis")` now drives the check, so an analysis declares a harm flag or `process_only` the
day it is filed; `unflagged_kind_findings` is its self-test. PB4900 gains its second witness (wave 20261915 group B wrote no
report; the operator wrote it by hand).
