## Entry 3722 — 2026-11-15 20:30 PST — records-1985-reread: the re-read of the 12 open PARTIAL 1985 rows; five re-recorded CONFORMS, seven restated, four defects filed (GAP 844 to 839)

A registrar landing (no compiler source changes) of the operator's re-read of the twelve 1985 rows that stood PARTIAL:
two adjudicators and their refuters read them on tree 5e49d46678 (`op-20261115/open-rows-1985`). Every probe of theirs
was re-run on this branch's own build (main 2722570af0, `dotnet build WiseOwl.Cobol.sln` 0 warnings, 0 errors) at
`--std 85` through `gl1985/run_case.py`: the 79 format probes of the second refuter agree with its log one for one, and
every other probe (the sort ranges, uniqueness, GLOBAL, EXTERNAL and CORRESPONDING sets) printed what the inputs record.
Nothing re-ran differently.

**Five rows re-recorded CONFORMS** (`record_verdicts.py --edition 1985`: rows changed 12, witnesses +23 test-ref and
+10 code-location, 0 retired, GAP 844 to 839):
- `1985-SR-IV.4.3.8.1-3`, after PB5566 (RECORD KEY ambiguity) landed. Two new fixed-form cases witness the statements no
  case did: the redefinition exception (an unqualified REDEFINES of a non-unique name, accepted; §1985:VI.5.10.3 SR7)
  and a paragraph referenced from another program (refused COBOL1639).
- `1985-FMT-VI.6.11.2`, `VI.6.20.2`, `VI.6.26.2` and `1985-FMT-VII.4.7.2`, after PB5108 (a function-identifier operand)
  and PB5158 (phrase order) landed. Ten new cases: for DIVIDE, MULTIPLY and SUBTRACT the reversed SIZE ERROR pair and a
  FUNCTION INTEGER operand, refused COBOL0900; for WRITE an accepted END-OF-PAGE pair on a LINAGE IS 3 file, with its
  expected lines (NOEOP 1, NOEOP 2, EOP 3, NOEOP 4) derived from §1985:VII.3.7.4 GR1 and GR9 and §1985:VII.4.7.4 GR18,
  plus the reversed pair and a function ADVANCING operand, refused.
A re-adjudication replaces a row's code-location (`inventory_schema.resites`), so each record restates the row's sites
before it adds the new ones; the first dry run showed 60 sites about to be dropped, which is why.

**Seven rows stay PARTIAL**, each record rewritten to say what is gone (and which landed note removed it), what is still
wrong (the repro, the checked 1985 citation, expected and actual) and the open note that owns it:
`1985-SR-IV.4.2.2.2.3-L3.2` and `1985-GR-VI.6.3.1.1.2-1` (PB5569, a SIGN SEPARATE position missing from the comparison
temporary; dated file `PB5569/2026-11-15-two-1985-rows-wait-on-it.md`, PB5569 itself untouched as another branch holds
it), `1985-GR-VII.4.7.4-16` (PB5766), `1985-SR-IV.4.3.8.1-4` (PB5629, an open adjudication: do a GLOBAL entry's
subordinates possess the global attribute; the row joins its `inventory_rows`), `1985-GR-VI.6.19.4-1` (PB5767),
`1985-SR-XI.4.3.3-3` (PB5768) and `1985-SR-XI.4.2.3-2` (PB5768, and PB5628 for the contained-program half, whose 1985
reading of §1985:X.1.3.7 is open: `PB5628/2026-11-15-the-1985-reading-is-open.md`).

**Notes filed:**
- PB5766: an EXTERNAL file whose ADVANCING describer is compiled apart. The opener's plain WRITEs made before that
  program runs are bare records, while the same pair compiled together advances. It behaves the same at `--std 2023`, so
  the CONFORMS 2023 row GR-14.9.51.4-25 is also contradicted (named in the note, not re-recorded).
- PB5767: at 85, MOVE CORRESPONDING freezes a subscript whose data-name lies in a redefined record. The position route
  hoists it into a statement temporary, so `m-send-two` prints 22 where separate MOVEs give 29 or 12. 2002 (GR6) and 2023
  (GR11) require the freeze, so the defect is in the 85 arm only.
- PB5768: the declarative exemption at `VersionConformancePass.cs:196` is too wide. A declarative RETURN or RELEASE that
  no statement of the range can trigger compiles, and at run time it takes AT END silently (refuter probes t13 and t14).
- PB5770: `ApplyPostReadLockActions` discards `LockRecord`'s '51' for a shared sequential file. This one is filed from
  the code; the two-process witness is the fix's job.
- PB5769 was released unused. PB4950 is the open pattern note for a diagnostic that cites 2023 rules at 85. Its dated file
  `2026-11-15-1985-reread-instances.md` takes COBOL0900, which cites RETURN's §2023:14.9.34.3 SR4 for DIVIDE, MULTIPLY,
  SUBTRACT and WRITE, plus COBOL1639 (already PB5627), COBOL2121 and COBOL5997.

Branch `adopt-2002-s1` is recorded ABANDONED beside PB5574 (`prune_worktrees.py` finds the record; nothing was applied).

**Part 5: five 2023 rows that the 1985 code reads contradict.** These are the operator's addition. The rulings
(`my-20261115/contradictions-b1/rulings.json`, "cause b", wrong at every edition) and notes PB5773, PB5775, PB5776 and
PB5777 (from pull request 1745, merged in here) name five 2023 rows stored CONFORMS. Each note's repro was re-run on this
build at `--std 2023`, and each reproduced:
- `m2srch` prints HIT 2 (FMT-14.9.37.2, PB5773).
- `m4gen` prints RAN and `m4use` prints BEFORE, RAN (FMT-14.9.16.2 and FMT-14.9.49.2, PB5775).
- `m5str` prints OVF, H=[5a    ] PTR=03 (GR-13.18.38.4-8, PB5776).
- `m5uns` prints OH=[AB] R1=[FGH] P=09 (GR-14.9.48.4-11, PB5777).

The rows were re-recorded PARTIAL, following DEVLOG 3709: part of each rule holds. Each record keeps the prior prose as the
account of that part, keeps its sites, adds the defect's site and names the note. The 2023 GAP went from 9 to 14. Each
note now claims its rows in `inventory_rows`. The landed notes that claimed those rows in `closes_rows` gave them up, each
with a reason naming the open note: PB351, PB332, PB365, PB80 and PB979. Eight 1985 KEPT rows chain into these targets,
so they reopen, and the 1985 GAP rose by 8 (775 to 783 on the merged tree): 1985-FMT-VI.6.22.2, X.5.6.2, XIII.4.3.2,
XIII.4.9.2, 1985-GR-VI.5.8.4-3, VI.6.19.4-5, VI.6.22.4-9 and VI.6.27.4-13. PB5730 had claimed `1985-GR-VI.6.22.4-9` in
`closes_rows`, and that claim was withdrawn with a reason.

**Checks.** `generate.py --check`, `case_collisions.py --check`, `work.py check`, `inventory_view.py --check`,
`lineage_reads.py --check` and `audit_witness_loss.py --check` all GREEN. Every citation written was run through
`cite.py --check`.

