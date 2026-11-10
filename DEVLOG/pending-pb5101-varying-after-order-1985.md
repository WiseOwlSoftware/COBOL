## PERFORM VARYING ... AFTER runs the 1985 order at --std 85 (PB5101)

PB436 (landed earlier) pinned ONE order for the two operations of an AFTER step under TEST BEFORE, the 2023 order of
GR13 e) 2 a-c (reset the inner variable, then augment the outer), at every edition, because the archive held no 1985
text. The 1985 confirmation pass (PB4995) found the 1985 text states the other order: §1985:VI.6.21.4 GR10 d) 1
augments the outer variable and THEN sets the inner one from its from-value. Re-probe on the build: the repro printed
`I=2 J=1` / `END 3 2` at --std 85 where the 1985 text gives `I=2 J=2` / `END 3 3`. The 2002 transcription words
GR13 e) 2 as 2023 does, so the edge is 1985 only.

Fix shape: the binder decides the edition's order (`PerformVarying.AugmentOuterFirst`, edition year below 2002) so the
emitter asks the rule's question and not the edition; `ControlFlowEmitter.EmitVarying`'s TEST BEFORE arm emits the two
operations in that order. TEST AFTER is the same at every edition and is untouched. NIST NC201A PFM-TEST-F4-23, which
PB436 recorded as a CCVS defect (the golden carried the FAIL), asserts exactly the 1985 order and now passes at 85: its
golden is all-PASS, its corpus row is `green`, and the pin `SpecPinnedNistTests` reads the PASS line and the footer.
The `CCVS-DEFECT` allowance and its set-equality audit stay, with no member.

Goldens: `14.9.28-PERFORM/pb5101_varying_after_from_outer_1985` (accept 85; 6 bodies, A=4 B=4, plus the TEST AFTER
contrast, derived from the rule). No refusal golden: nothing is refused, and no edition is below 1985. Rows: the
2023 row GR-14.9.28.4-13 stays CONFORMS and gains the new witnesses (a `record_verdicts` batch); no 1985 inventory rows
exist yet (PB5094). Docs: the CONFORMANCE.md section 3 determination now reads per edition, and VERSION_CHANGE_REFERENCE
row 7.53 records the difference.

Review round 1 (R1-1): the 1985 edge is now a DialectBehaviors member, PerformVaryingInitInnerBeforeAugmentOuter (ChangedIn 2002, VCR row 7.53 carrying its behavior anchor), read by BindVarying through IsActive instead of an inline year test; the stale NC201A sentence in scripts/guard-verdict.sh (R1-2) is corrected.
