## PB4995: the 1985 recheck's 712 contested change records decided by cobol-adjudicators in two rounds and recorded

The max-effort 1985 recheck (account2, $229.66) upheld 1,538 change records, every one already the stored record, and
contested 712. The owner chose a `cobol-adjudicator` decision for each (2026-11-09: not the recheck's own record for the
473 that only added 2023 rules). Round 1: 36 adjudicators (workflow `wf_a8422424-a02`) decided 711, every citation
through `cite.py --check`, each decision checkpointed.

Round 1's brief, written by the coordinating session, paraphrased KEPT as "a conforming 1985 program means the same
under 2023", dropping "the same programs are accepted or refused" that DESIGN-spec-conformance-review.md section 10.2
and the prompts state, so 2023 additions only 1985-absent syntax reaches were recorded KEPT (PB5021); the gate's EDITION
GATES audit refused 8. The owner then had the fleet's 426 KEPT decisions rechecked at max effort on account2
(`audit_batch.py seed`, run `state-adj712-kept`, $112.05): 166 upheld, 259 disputed, 1 cut off. Round 2: 15 adjudicators
(`wf_bfda95f7-cbb`) re-decided those 260 with the definitions quoted verbatim: 50 KEPT became CHANGED (six of them the
records the edition gates refused), 205 stayed KEPT (about 113 with a corrected `to` list), 5 were withdrawn. The
branch review then made the six WRITE and REWRITE FROM-equivalence records CHANGED (2023 adds FROM literal-1, as their
RELEASE sibling already recorded), and `procedure-returning-2002` now cites §2023:14.2.2 SR4, the rule that names
RETURNING in a program definition: the EDITION GATES audit is GREEN with no gate held open for this.

691 records are recorded here through `record_verdicts.py --edition 1985` (KEPT 365, CHANGED 310, REMOVED 16; source
`adjudicate-712/<slice>` or `adjudicate-712-r2/<slice>`); the 1985 GAP is 1,034 (from 1,089: each KEPT made CHANGED
needs a verdict of its own at 85). 21 are held: their 2023 carrier is unnumbered prose or a table the 2023 catalog has
no row for, listed in PB479, which owns the class. Filed from the fleet's findings: PB5021 (the KEPT definition and its cause), PB5022 (a 2023
SUM refusal of 1985-legal source to probe at `--std=85`) and PB5023 (catalog rows cut at 4,000 characters, a merged
errata row, an ambiguous citation).
