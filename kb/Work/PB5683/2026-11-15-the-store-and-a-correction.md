**2026-11-15, wave 20262055 group A (kb/Work PB5759): the store of a code read, and a correction of this note.**

**The store.** A read that adjudicates a KEPT row on the code at its own edition is kept as a tracked record of the
read: `tests/version-matrix/code-reads-<year>/<rule-id>.json`, one file per rule, written only by
`scripts/spec/code_reads.py` (design section 10.6.1). The commands:

- `python scripts/spec/code_reads.py import --edition YEAR --reads DIR --pin COMMIT [--record DIR] [--replace]`: the
  pipeline's `<slice>.json` and `<slice>-refute.json` as they are; every slice or none;
- `python scripts/spec/code_reads.py adjudicate --edition YEAR RULE --verdict V --reason TEXT --ref PATH [--work PBnnnn]`
  and `work --edition YEAR RULE PBnnnn`;
- `python scripts/spec/code_reads.py plan --edition YEAR [--witnessed] [--json]`: the KEPT rows still owed a read, the
  population this note's campaign builds its slices from (1,338 for 1985 after the first import, 535 witnessed);
- `python scripts/spec/code_reads.py --check`: the gate's CODE READS audit.

A read that confirms a KEPT row is a `confirmed` record there; one that contradicts it is `contradicted` and names its
mechanism's note. No row's state changes until the switch (kb/Work PB5682).

**A correction.** This note says the pairing check "takes the stored record's own targets". It does not:
`lineage_reads.pairing_problems` reads only the candidate pairing (`want = lin.pairs.get(rid)`) and refuses a rule with
no candidate pair, and 1985 pairs none of its 2,274 rules. The sentence describes a docstring, not the code, and no 1985
code read can become a KEPT record through `import-reads`; that is why the code read has a store of its own.
