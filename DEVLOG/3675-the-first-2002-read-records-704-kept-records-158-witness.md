## Entry 3675 — 2026-11-15 03:47 PST — The first 2002 read records: 704 KEPT records, 158 witness bands widened to 2002, the 2002 GAP 3,284 to 2,582

**What was read.** Readers decided 1,720 of the 3,298 rules of the 2002 inventory on the 2002 text at `--std 2002`, and
a refuter attacked every SAME (op-20261114 read-2002). An answer pass then re-read 644 flagged rows flag by flag.
`audit_batch.py import-reads --edition 2002` imported the merged reads, 69 slices, as the tracked records
`tests/version-matrix/lineage-reads-2002/` (kb/Work PB5597). It kept 863 rows before the evidence check and refused 13
for a lineage-screen flag the read did not answer. It sent 844 to work items: needs-own-witness 408, defect 240,
undecided 140, own-verdict-from-pointer 55 and uninterpretable 1. The first import attempt was refused, 19 rows in three
shapes the reads used and the tool rejects. Each shape came from the scratch merge of the answer pass: maps and lists
written where the brief asks for one string, and a refuter record kept for a row the answer pass moved off SAME. The
merge was fixed in scratch and the import re-run. The tool was not changed.

**Band widening.** `lineage_reads.py widen --edition 2002` planned 184 rows as widenable and 137 as needing a witness of
their own. `--apply` proved each candidate at 2002 before writing it: the generator's `check_cases`, then the suite's
runner. It wrote 158 widenings: 140 accepted cases gain 2002 in `accept`, and 18 refusal entries gain it in `refuse-at`,
each refused with its expected diagnostic. It wrote none of the 26 it could not prove. Of those, 25 cases FAIL at 2002
and one refusal, `pb345_record_less_fd_area_85`, is accepted at 2002. `git diff` held exactly the 158 proven paths, and
the widening is its own commit.

**Records.** `change-batch --edition 2002` wrote 704 KEPT change records and refused 172: 13 for an unanswered flag and
159 for their evidence. Of the 159, 124 have no case that runs and passes at 2002, and 35 are rules that grant a form and
are witnessed only by a refusal. None was refused for its licence. `record_verdicts.py --edition 2002` wrote the 704 and
reported `GAP: 3284 → 2582`. The 2002 GAP was 3,297 when the reads were planned, and PB5617/PB5604's 13 records
(129df50689) moved it to 3,284 before this branch. Two of the 704 records do not close their rows: their 2023 targets'
band is `2023` only. `lineage_reads.py --check` accepts them and the view does not, which is filed as F4 of kb/Work PB5649.

**What remains**, by kind: needs-own-witness 408, defect 240, undecided 140, refused-evidence 124,
own-verdict-from-pointer 55, refused-needs-accepting-case 35, refused-flag-unanswered 13, recorded-still-gap 2 and
uninterpretable 1. kb/Work PB5649 files PB5597's three round-3 review leads on the evidence check: the "need not" grant,
the "may ... only" prohibition, and the design doc's reason for trusting a committed band.

