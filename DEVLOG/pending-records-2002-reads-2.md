## records-2002-reads-2: the unpaired 2002 reads imported; 398 own verdicts from pointers; GAP 2169 -> 1771

The second records landing of the 2002 reads (kb/Work PB5597). The first imported the paired reads and wrote the KEPT
change records. This one imports the reads of the 1,528 unpaired 2002 rules (slices `u-01`..`u-56`). It adds the
missing writer step: the own verdict from a pointer.

**The tool.** A 2002 rule with no candidate pair is never KEPT, but a SAME its refuter upheld says the 2002 text states
what the pointer states. Nothing wrote that record, so the first landing sent its 55 such rows to witness writers by
hand. `python scripts/spec/audit_batch.py own-verdict-batch --edition YEAR --state RUN --out BATCH`, beside
`change-batch`, now writes it from an imported run. The record carries the pointers' one verdict, their code, and ONLY
their cases that witness the rule at the edition. Its notes open with the read record. `lineage_reads.
own_verdict_problems` is the one check, and `decide`, the writer, `widen` and the audit share it. It requires every
pointer witnessed at the edition by the KEPT record's own evidence check (`Witnesses.judged`, which the record's test-ref
also reads), one resolving verdict, a declined facility's 2002 licence, a 2002 citation, a register entry of the row's
own where its kind owes one, and a code location where the verdict requires one. `lineage_reads.py --check` holds each
stored own verdict whose notes name its read to the KEPT standing. `widen` plans the own verdicts that only their
evidence blocks, through the same `--apply`. Design section 10.6 states all of it.

**A defect found and fixed (kb/Work PB5699).** `decide` asked the pairing before the refuter. So every unpaired SAME was counted
`own-verdict-from-pointer` whatever its refuter said. In the operator's dry run that was 133 OVERTURNED or DOUBTFUL reads
(and 63 with no refute yet). The refuter is now read first, as for a paired rule.

**The run.** Every u- slice had its refute by 12:56 PST, so none was left out. `import-reads`: 125 slices, 3,098 rules;
kept before the evidence check 807, refused `flag unanswered` 69, work `defect` 435, `needs-own-witness` 977,
`own-verdict-from-pointer` 571, `undecided` 224, `uninterpretable` 15. `widen --apply`: proven and written 75, not proven
25. The 25 are 24 accepted cases that FAIL at 2002 and one refusal test that is accepted there; each is a lead in the
branch report. `own-verdict-batch`: 398 records (CONFORMS 334, DOCUMENTED-NON-SUPPORT 64), refused 148, recorded already
25. `record_verdicts.py --edition 2002`: GAP 2169 -> 1771. A 15-record seeded spot check ran each first cited case at
`--std 2002`: 10 PASS, 5 refused with their expected diagnostic, none dropped. `change-batch` adds nothing new. Of the 69
rows a flag leaves unanswered, 35 are KEPT records the first landing stored before the screen gained a trigger. They
need an answer pass.
