## The COBOL-85 Segmentation module: its flow restrictions and an independent segment's initial state

Wave 20261983 group A finished the binder-driver group. It replayed the fifteen unlanded commits of `worktree-wf_82a7f4cd-626-1`
(wave 20261979 A) onto main. Main had meanwhile split `tests/version-matrix/constructs.json` into one file per construct
(kb/Work PB5110), so the two commits that changed construct rows were ported row by row onto the new store and the registry was
regenerated; the other conflicts were append-only (three kb/Work notes, the code list of `compiler-facts.json`, VCR row 7.52 beside
main's new 7.53). The replay gated GREEN at ce1090a2f9 (run 000001-20261110T185145Z: Conformance 23,023, Unit 40,709,
Characterization 37).

Then it finished PB5084, the remainder of the Segmentation module (§1985:XVI), which VCR row 7.18 had recorded as "deliberately NOT
implemented". On the replayed build the note's repro printed INITIAL then ALTERED, the refuter's SORT variant printed FIRST then
LATER, and each flow-rule shape compiled. The module now has one model, `Segmentation`: each section carries its segment-number
(`SectionInfo.Segment`), and

- the restrictions on program flow (§1985:XVI.3.3.1 ALTER, XVI.3.3.2 PERFORM, XVI.3.3.3 MERGE, XVI.3.3.4 SORT) are COBOL6877, asked
  where each statement binds. The first version closed each range over the procedures it performs (the range of §1985:VI.6.21.4
  GR11); it refused the CCVS-85 programs SG102A and SG103A, which perform the fixed-portion report paragraphs from inside an
  independent segment's range, so the rule is asked of the procedures a statement names (docs/CONFORMANCE.md §3.8);
- an independent segment's initial state (§1985:XVI.1.4.3; §1985:VI.6.7.4 GR1) is the dispatcher's: `__SegOf` holds each pc's
  segment, the loop head calls `__EnterSegment` whenever control enters a pc of another segment (which resets that segment's
  alterable GO TOs to their written targets), and `__Dispatch` restores the caller's segment in a `finally`, so the return from a
  PERFORM, SORT or MERGE procedure or declarative leaves the segment in its last-used state (CONTROL_FLOW_DESIGN D4). The
  scaffolding is emitted only for a program with an alterable GO TO in an independent segment;
- under --permissive at a later edition the segment-number keeps its run-time meaning, as ALTER does, without the 1985
  restrictions.

Witnesses: `14.2-Procedure-division-structure/pb5084_independent_segment_initial_state_85` (fall-through, PERFORM, GO TO and SORT
entries, a fixed segment's ALTER kept, an ALTER kept across a PERFORM return; main's compiler printed ALTERED on four re-entries and lost a
SORT record), `pb5084_independent_segment_called_last_used_85` (a second CALL of a program whose first section is independent finds it last-used, §1985:X.5.2.4 GR5, which an earlier draft that started `__seg` at -1 broke) and six COBOL6877 refusals, one per arm. The CCVS-85 segmentation programs SG101A-SG106A and SG201A-SG204A become
asserted NIST rows with their reports as goldens: on the replayed build SG102A, SG103A, SG201A and SG203A failed 1, 3, 23 and 4 tests,
and now none fails (SG201A and SG203A also pass at `--std 2023 --permissive`). One diagnostic code was used (COBOL6877); COBOL6878
and COBOL6879 are returned. PB5084 lands with `closes_rows: []` (a lead that claimed no inventory row).
