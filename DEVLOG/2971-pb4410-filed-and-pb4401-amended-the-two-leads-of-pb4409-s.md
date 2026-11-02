## Entry 2971 — 2026-11-01 18:36 PST — PB4410 filed and PB4401 amended: the two leads of PB4409's review

The operator filed the two low-severity leads that PB4409's round-2 review (R2-1, R2-2) left. Each site was re-read on
main after #999.

- **PB4410:** three descriptions predate PB4409's split of the tooling flag. `land_lane.py`'s THE RULE comment,
  `self_tests.py`'s unclassified-read message, and CLAUDE.md Testing plus PB4409 itself all still state the old rule. The
  last two say the Linux audits job runs every self-test, though it reports the Windows-only ones NOT RUN.
- **PB4401, amended:** a merge-queue red in a self-test job leaves no trx, so `classify_red` calls it INFRA and
  re-queues it. PB4409 makes that more frequent, because a compiler change's self-tests now first run in the merge
  group. The job and step facts PB4401 reads settle this case too.

