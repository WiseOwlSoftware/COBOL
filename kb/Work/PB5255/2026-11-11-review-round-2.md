**2026-11-11, review round 2 of `spec-2002-reconcile-tools` at ccf2f76542: R1-1 to R1-9 confirmed; one blocking finding
and one lead, both fixed (review record `E:\COBOL-coord\scratch\review-w20261997-f.jsonl`, probe `probe2.py`).**

**R2-1 (high).** A verdict attached by `claim_id`, page and kind, but an id is not an identity: a re-run batch numbers its
claims afresh, so `p5-1` can name another claim. With the batch re-run, its old files not deleted and the new verifier
dead, the old `verify-p5-1.json` (real=false) attached to the new p5-1 and `--write` decided it REFUTED for the old
claim's reason (probe2 case 2 on ccf2f76542: rc 0, `('p5-1', '{| ADVANCING |} lost its choice bars', 'REFUTED',
'REFUTED', 'OLD claim: present, reflowed')`).

**R2-2 (low).** A kept decision was held to its claim by `pdf_says` alone, so a re-run claim with the same quote and
another kind kept ALREADY while UNVERIFIED (probe2 case 3: rc 0, `('missing-heading', 'UNVERIFIED', 'ALREADY')`).

**The fix: a claim's identity is its content.** `merge_reconciliation.claim_key(claim)` fingerprints the page and every
text field (`kind`, `pdf_says`, `markdown_says`, `why_it_matters`): two 32-bit FNV-1a hashes over the UTF-16 code units,
16 hex digits. The workflow computes the same function (`claimKey`) and hands it to the verifier, which copies it into
its file as `claim_key`. A verdict attaches only to the claim whose id AND key it carries. A verify file whose key differs
from the claim holding its id is refused by name, never attached and never ignored. A kept decision is held to its claim
by the same function. The key, rather than a field-by-field comparison of the quoted text, is the measured choice: 203 of
2023's 346 verify files quote their claim inexactly (collapsed whitespace, re-escaped newlines, truncated
`why_it_matters`), so comparing retyped text would refuse honest verdicts. A copied 16-character key does not depend on an
agent retyping a long quote. `test_wf_spec_reconcile.py` holds the JS and Python halves equal over ASCII, non-ASCII,
astral and control characters. The 346 committed 2023 verify files took the key of the claim their `claim_id` already
named (the identity proven in round 1), checked file by file; `--check` keeps LEDGER.json CURRENT, 210 confirmed and 136
refuted as before.

**The re-run rule (README "Re-running a batch"; the workflow's argument comment).** A re-run batch replaces its earlier
files as a unit, and the run on disk wins: its compare file replaces the earlier one under the same name, and the
operator deletes the batch's earlier verify files first. The merge REFUSES a leftover verdict whose key the new claim
does not share. It never deletes, and the workflow never deletes: a workflow script cannot see the disk, and an agent told
to delete files could delete a valid verdict, whereas a refusal loses nothing. A leftover verdict whose claim the new run
found again, identical in every keyed field, is a verdict on exactly that claim, and it attaches.

**Proof.** `merge_reconciliation.py --self-test` PASS, new arms: a stale verdict after a re-run refused by name, a verdict
with no key refused, a verdict differing only in kind refused, an identical re-found claim keeps its verdict, a kept
decision on a re-kinded claim refused, and the key covers page and every text field but not severity or id.
`test_wf_spec_reconcile.py` PASS (10 arms; on ccf2f76542's workflow the wrong-key arm and the key-equality arm FAIL). probe2
on this merge: case 2's first run is refused, because the probe's verify file carries no key. A copy whose `ver()` writes
the key shows case 2's re-run refused (rc 1, ledger unchanged) and case 3 refused (rc 1). `merge_probe.py` is still
refused (8 problems in 4 files).
