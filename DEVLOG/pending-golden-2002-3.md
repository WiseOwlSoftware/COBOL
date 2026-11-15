## The 2002 golden lane, third landing: 105 witnesses at --std 2002 for 14 more files, 2002 GAP 2,258 to 2,170

The gl2002 lane (kb/Work PB5597) gives each 2002 row a witness of its own at `--std 2002`. This landing integrates 14 of
the lane's 85 input files: 7.2 compiler directives, 7.2.9 DEFINE, CLOSE, CANCEL (second part), UNSTRING, SORT, SEARCH,
REWRITE, RELEASE, READ, RAISE, INVOKE, INSPECT and GENERATE.

**The shape.** The 14 files hold 98 rows by work kind: 46 needs-own-witness, 35 refused-evidence, 11
own-verdict-from-pointer and 5 refused-needs-accepting-case. The writers reported 97 of them: 83 new-golden, 4
existing-golden, 1 widen-existing, 2 defect-suspected and 7 not-closable. They deferred one row, 2002-SR-14.8.47.2-3,
because their pinned build predated PB5579's WRITE FILE. The refuters overturned 18 verdicts: expected-value 7,
does-not-exercise-rule 4, convention 3, citation 3 and nondeterministic 1. The fixers applied all 18.

**What landed.** 105 cases, each validated on this branch's build because no validate stage ran for these files:
- 61 new cases are accepted at [2002], and one existing case,
  `14.9.35-REWRITE/l1m8_rewrite_unsuccessful_no_update`, now also accepts 2002. run.py at 2002 on a manifest of
  exactly these 62 gave PASS 62, FAIL 0.
- 43 refusals have `refuse-at: ["2002"]`. Each was refused at `--std 2002` with its expected code. One of them is the
  deferred row's refusal, `g2_write_file_advancing_indexed_neg` (COBOL1720), which this build can observe.
- Two PROGRAM-IDs collided inside the drafts and were renumbered: G2RD01 became G2RD02 and G2SRT01 became G2SRT03.

**Records.** `record_verdicts.py --edition 2002` wrote 92 store files: CONFORMS 88, DOCUMENTED-NON-SUPPORT 1 and
DIVERGES 3, and the 2002 GAP went from 2,258 to 2,170.
- The non-support record is 2002-GR-14.8.10.3-17, the DISPLAY screen format's ON EXCEPTION. Its 2002 licence is
  `§2002:B.3` with `§2002:3.1.5`, and its witness `a42-display-screen-exception` is refused COBOL1707 at 2002.
- 2002-GR-14.8.29.3-18 was already CONFORMS (PB5639). The writer's record widens its witnesses, and the earlier notes
  are kept after the new ones.
- Every test-ref was checked to run at 2002 on this tree. A refusal-only witness was accepted only for an SR or FMT rule
  whose text grants no form, or for that one licensed non-support.

**Defects (each re-run on this build at `--std 2002`; the rows are recorded DIVERGES naming the note).**
- PB5685 (new): CANCEL's implicit CLOSE of an active report's file raises no EC-REPORT-NOT-TERMINATED. The same happens
  at 2023. The note blocks 2002-GR-14.8.5.3-9.
- PB5686 (new): a universal INVOKE resolves by 2023's signature match at 2002, where `§2002:9.3.5` binds by name. The
  existing 2002 case `pb480_universal_match_relations` encodes the 2023 answer. The note blocks 2002-GR-14.8.22.3-7.
- PB5612 (dated finding): a SORT in a SORT's input procedure, with EC-SORT-MERGE-ACTIVE checking on and no declarative,
  is skipped, and the run unit continues. This adds 2002-GR-14.8.36.3-9.

**Held back.** `g2can06_implicit_close_open_files` and `g2inv09-ec-oo-method-declarative` both belong to
defect-suspected rows.

**Not closable (7 rows), each needing a 2002 derivation cell or the registrar's decision.**
- 2002-GR-7.2.3-3: no IMP directive is defined.
- 2002-GR-7.2.8.2-3: only one call convention exists.
- 2002-GR-14.8.5.3-10: no non-COBOL program can be canceled.
- 2002-GR-14.8.5.3-11: the result is undefined.
- 2002-GR-14.8.36.3-4: the order of equal keys is undefined.
- 2002-GR-14.8.29.3-17 and -20: there is no reel/unit medium. The writers wrote DERIVATION records for these two, and
  they were dropped, since no 2002 derivation cell exists.
