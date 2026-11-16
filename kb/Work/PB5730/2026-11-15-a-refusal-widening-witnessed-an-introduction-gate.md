## Finding: a refusal widening proved by an introduction gate (records-2002-reads-2 review R1-1, 2026-11-15)

**What was wrong.** `lineage_reads.refused_at`, the widening's proof of a refusal test at an older edition, asked only
that the compilation fail and name the entry's `expect`. A case built on a construct the older edition does not have is
refused there FIRST for that (the COBOL0900 introduction gate, or the edition's own limit such as COBOL1567's
31-character word), so its refusal passes whatever the rule under test does, and the widening wrote it as a witness of
the older rule. The review of records-2002-reads-2 found one at 2002 (message-tag-refused-strong-group-ordering:
COBOL1535 with COBOL1567 and COBOL0900 at `--std 2002`, USAGE MESSAGE-TAG being new in 2023).

**The fix (records-2002-reads-2, 9f783f7622).** Every error code a refusal draws at the edition must be one its entry's
`expect` names or one it also draws at its newest refusing edition, where it witnesses its rule; anything else refuses
the widening with the codes named. Self-test `case_07b` (a mutation that drops the check turns it RED). Design 10.6
states it.

**The sweep.** The new check over every refusal entry a widening wrote below its band
(`E:/COBOL-coord/scratch/finish-records-2002-reads-2/sweep.py`, results `sweep-merged-2/sweep.json`): of 57, 35 refused.
At 2002, 6 (records-2002-reads-1's f6d61eaf51: 5; this branch's 184936d043: 1). At 85, 27 of this note's 38 refusal
widenings (1d9a10a7e2), and 2 more (pb1562, DYNAMIC LENGTH) taken back to [2014, 2023] at the merge. Each band is
reverted. No 1985 row cited any of the 85 ones. Two 2002 KEPT rows stood on two of the 2002 ones and now have 2002-legal
witnesses of their own: 2002-SR-14.8.31.2-2 (pb1160-resume-in-global-declarative, COBOL0713) and 2002-SR-7.2.2-10
(pb807-define-directive-concatenation, COBOL1619), the old witness retired through `record_verdicts.py`.

**Not swept.** A refusal entry WRITTEN at an older edition by a golden lane (not widened) is not re-proved by this
check; five entries also draw at 2023 an error their entry does not name (constant-record-level, pb1087 x2, pb486,
pb495), which the check reads as their own cascade.
