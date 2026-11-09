## Entry 3365 — 2026-11-08 23:09 PST — PB4995: the refused drafts are drafted again under the shape that ties them to their rule; the recheck canary is measured

Wave 20261939, group A (PB4995, PB4996), the 1985 lineage campaign (PB4991). PB4996 stays blocked on PB4995: the band
drift closes only by the fleet's adjudications, and nothing in this change moves it.

**The refused drafts.** 291 of the drafts pass's refused drafts were written before its prompt asked for the violating
line and a conforming control, so `run_drafts.py` judged them in a second, weaker arm (REFUSED UNCONFIRMED: a diagnostic
anywhere), and the harness carried a `Run.flag` helper extracted only for that arm. The pass's answer shape
(`audit_batch.parse_answer`) already refused such a draft, so it is now the one rule of what an answer is: the new
`audit_batch.py withdraw --pass P` moves each stored answer the shape refuses to `raw/` (`<custom id>.withdrawn.json`,
never over an earlier one) and its item is unanswered again; `retry` drafted the 291 again with the plan's 24 newly grown
items ($5.12, account1). The REFUSED UNCONFIRMED arm is deleted from `run_drafts.py` and `Run.flagged` is whole again
(its blob is the one before the extraction). The self-tests plant a withdrawn draft, a second withdraw, and a refused
draft the compiler translates.

The re-run of all 989 drafts at 85 (cobol 25.0.0, assemblies bf3c3ca4796b): MATCH 769 (624 output, 145 refusals with
the diagnostic at the violating line), MISMATCH 219 (72 output; 117 refused drafts that drew no diagnostic, 30 whose
control the compiler refused), NO VERDICT 1 (`1985-GR-VII.4.7.4-13`, the 600 s hang guard again). The 117 are the
fleet's first triage: each is a possible under-rejection at 85 or a suspect draft.

**The recheck canary** (PB5008's pass). Collected: 16 answers; 4 replies stopped at the 32,000-token limit; $4.82, so
$0.24 a request against the $0.094 estimated, about $250 for the 1,040 left. The limit is now 64,000 and the pre-canary
estimate the measured mean (22,300 output tokens). The recheck upholds 7 of the 16 and disputes 9; 8 of the 9 keep the
kind and only add 2023 rules. At that rate the full pass contests about 590 decisions beside the 158 contested now, so
the full submit waits for the owner: the spend, and whether a same-kind record that only adds rules is a dispute.

Self-review: one rule of an answer's shape, read by the pass and by `withdraw`; no reader keeps an arm for an older
shape; no new list. Gate: see the branch's gate record.

