# PB6022 - the three reads follow PB6020's moves; the gate's audit now refuses an unmapped one (wave 20262132 group h)

**Re-probe (branch train85e-e3-cat at 0ffdede4cc, train D's head).** The note's repro, over a scratch copy of the tracked
record directory with an empty reads directory and an empty state:

    python scripts/spec/audit_batch.py import-reads --edition 1985 --reads <empty> --asked <empty> --record <copy of
      tests/version-matrix/lineage-reads-1985> --state <new>
    import-reads: 34 slice(s) (0 written or rewritten ...); 853 rule(s): ... -> <state>      (exit 0)

So the refusal no longer fires: PB6020 (train B) recorded the five moves of PB6001's relabel as
`docs/rearchitecture/spec-moves-1985/moves-pb6001.json`, each naming the slices that read its old id, and
`lineage_reads.load` reads s-18's and s-99's reads under the new ids (`follow_moves`). The same records loaded with NO
moves name exactly the three orphans of this note (`s-18: 1985-GR-XIV.2.2.6-T1`, `s-99: 1985-GR-XIII.3.10.7-p5`,
`s-99: 1985-GR-XIII.3.10.9-p5`), so the moves are what covers them.

**What PB6020 left of the owed fix: the gate did not see an orphan.** `lineage_reads.py --check` held no test that a read
names a rule of the catalog; only `audit_batch.py import-reads` did, AFTER `import_records` had written the new slices.
A relabel with no recorded move would again have been GREEN at the gate and refused at the next import. Fixed:

- `lineage_reads.unknown_rule_errors(year, rules, slices)`: a read (after every recorded move) of an id the catalog does
  not hold, naming slice and id. The one test, called by `check` (the gate's LINEAGE READS audit) and by
  `audit_batch.import_reads` BEFORE anything is written (the inline post-write test there is deleted).
- Self-test `scripts/selftests/spec/lineage_reads/test_reads.py` case 18: a mapped read is no orphan; an unmapped one is
  refused with its slice and id; a move naming another slice maps nothing. RED on the base (`AttributeError: module
  'lineage_reads' has no attribute 'unknown_rule_errors'`, 27 cases, 1 failed), GREEN on the head.
- `python scripts/spec/lineage_reads.py --check 1985`: `=== LINEAGE READS: GREEN ===` on the head (the tracked moves map
  all three).

The note's "relabel map the catalog extraction writes" is the move record PB6020 made; a second map would be a second
mechanism for one question (rule 4), so none is added.
