**2026-11-11, review round 3 of `spec-2002-reconcile-tools` at f53c0a1688 (1772d2cf71 plus the coordinator's merge of
origin/main): the content key and the re-run rule accepted; one blocking finding, fixed (review record
`E:\COBOL-coord\scratch\review-w20261997-f.jsonl`, probes `r3.py`, `probe3.py`).**

**R3-1 (medium).** The workflow computed a verifier's `claim_key` from the compare agent's RETURN VALUE, and the merge
recomputes it from the compare FILE. An agent writes those two separately and can retype one differently (`r3.py`: 60 of
2023's 346 verify files differ from their claim only in whitespace). A file and a return one space apart would have every
verdict of the batch refused at the merge, after its verifiers had spent their quota, and the workflow's own check could
not see it. The key itself stays whitespace-sensitive: a re-worded claim must get another key (`r3.py`: a whitespace or
case variant gets a different key).

**The fix: one source, the compare FILE.**
- `merge_reconciliation.py --claim-key <compare file> <claim id>` validates the file by the merge's own rules and prints the
  claim with its `claim_key` as one JSON line. A file the merge would refuse, or an id the file does not hold, exits 1
  naming why (`<file> holds no claim p2-9 (its claims: [...])`). Each verify agent runs it, judges the claim AS THE FILE
  HOLDS IT, and copies that key. The workflow computes no key: `claimKey` is deleted, and `claim_key` exists once, in
  Python.
- The sibling: the workflow counted `pages_checked` and `pages_unread`, and dispatched claim ids, from the compare agent's
  retyped result, and validated it with a JS copy of the merge's rules (`compareProblems`). Now each compare agent runs
  `merge_reconciliation.py --compare-summary <its file>` after writing it, corrects the file until the command passes,
  and returns that command's line: what the FILE records, judged by the merge's rules. The script counts and dispatches
  from that summary, and refuses a summary of another file or of another batch. `compareProblems` is deleted, so the
  merge's rules are written once.
- What a workflow script still cannot see: a script cannot read files, so a summary an agent shortened (a claim id
  dropped) goes unseen by the run. Every claim id it does dispatch is checked against the file by the verifier's
  command, and the merge, which reads the files, names every claim with no verdict. README step 3 says so.

**Proof.** `merge_reconciliation.py --self-test` PASS, new arms: `--claim-key` gives the file's key when the return differs
by one space; an id the file does not hold fails naming the file, the id and its claims (also through the command's exit
code); `--compare-summary` reports what the file records, refuses an invalid file naming its problems, and names a file
never written. `test_wf_spec_reconcile.py` PASS (10 arms). Its stub agents now do what the prompts say: they write their
files into a temporary edition directory and run the real `--compare-summary` and `--claim-key`, and each run's directory
is merged by the real merge. Two of its arms: a return retyped with one more space changes nothing (both verdicts attach,
the merge reports 2 refuted), and a summary naming an id its file lacks leaves that claim unverified with the command's
failure. probe2, probe3 and merge_probe are refused as before. The 2023 ledger is CURRENT under `--check` (210 confirmed,
136 refuted).
