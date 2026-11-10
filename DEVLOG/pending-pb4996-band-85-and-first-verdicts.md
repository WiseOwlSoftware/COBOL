## PB4996, PB5021: the 85 band of 113 rules of 2023 adjudicated, and the first refuted code-read verdicts (1985 GAP 1,046 to 882)

**What was wrong.** 148 rows of the 1985 inventory were KEPT (2023 states the same requirement) and still open, because
the 2023 rule each names stored an edition band that began after 85. Those bands were written before the 1985 text was
held. The other open rows, CHANGED and REMOVED, had tests (placed by the previous entry) and no verdict.

**The decision this rests on (kb/Work PB5021, decided this day).** Two rules of adjacent editions are compared on the
programs the older edition admits. A case the newer edition adds that no such program can write does not make the older
rule CHANGED; the general format or syntax rule that excludes the syntax carries the CHANGED record and owes the refusal.
A lifted refusal is a change. It is written into DESIGN-spec-conformance-review.md section 10.2 and into the adjudicate and
recheck prompts of `scripts/spec/audit_batch.py`.

**The band.** Fifteen Mythos 5.2 adjudicators decided the 113 rules of 2023 from the two standards: 95 are in force at 85
(their `editions` gain 85, each with the 1985 citation in its notes), 18 are not (the 1985 records lose the target; two
become CHANGED). A refuter attacked a seeded sample of 20 and overturned one (GROUP INDICATE, `GR-13.18.28.4-1`: 1985
supplies spaces, 2023 omits the entry and its line), which is corrected here.

**The code read.** 350 of the 699 witnessed rows were read against the compiler at `--std 85`: 239 closing verdicts, 110
non-conforming, one decision to take. Refuters attacked a seeded sample of 26 closing verdicts and overturned 2. At that
rate the unsampled verdicts are not recorded: this entry records the 110 non-conforming verdicts and the 19 the refuters
upheld. 213 closing verdicts wait for their refuter.

**Tooling fixed on the way (review leads of the placement entry).** `place_witnesses.py` refuses to place a rule whose
case exists and differs (`--replace` removes the old shape's files first), and its self-test now fails when the re-run
check or the DRAFTED kind's check is removed; `audit_witness_loss.py`'s self-test covers a witness-only record at edition
1985.

**Counts.** 1985 inventory: 1,046 GAP before, 882 after; no KEPT row is open. 2023 inventory: 0 GAP before and after.
