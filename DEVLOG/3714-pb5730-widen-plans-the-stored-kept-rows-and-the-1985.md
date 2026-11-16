## Entry 3714 — 2026-11-15 18:36 PST — PB5730: widen plans the stored KEPT rows, and the 1985 widening batch closes 64 rows (1985 GAP 844 to 780 on main)

**The mechanism.** `lineage_reads.py widen` planned only from read decisions, and 1985 has no read records: its 1,373
KEPT records are stored records of its comparison audit, so the 803 without a witness of their own had no widening path.
`unwitnessed` is now the one population (stored KEPT rows with no own test-ref, through `bare_kept`, which
`witness-batch` shares; and read decisions refused only for their evidence), classified by the unchanged `widen_plan`.
Each `needs-own-witness` row says why per lacking target from a closed set (`REASON_OF`). `--state` is needed only
where read records exist. The 2002 plan from the same state is identical before and after (138 rows); the 1985 plan
matches the prototype's 803 rows (483 widenable, 320 needs-own-witness).

**A sibling found and fixed.** The proof checks a band widened in memory with `generate.check_cases`, but the
free-form and run-time-expectation refusals ran only when a file was read. The first 1985 run wrote 33 free-form cases
at 85 that `generate.py --check` refused. Both refusals now live in `generate.check_editions`, called from
`check_cases`, with a generator self-test; the batch was run again from an untouched tree.

**The batch.** 74 of 365 candidate cases proven at 85 and written; `witness-batch` wrote 64 records; 1985 from 835 to
771 GAP at the branch's base, and from 844 to 780 on the tree merged with main at landing (main's registrar
landing records-sample-pb5692 had raised it 835 to 844; `inventory_view.py --summary`); a second witness-batch writes nothing; LINEAGE READS green. The 291 not proven: 258 later-construct, 33
harness (run-time expectation at 85), 0 suspected defects. The 419 rows whose widening did not hold go to the witness
lane.

**Stopped.** A predicate that a KEPT row's own witness cites the row or a target fails 25 stored references (10 at
1985, 15 at 2002); it was committed and reverted, and the list goes to the deciding session.

