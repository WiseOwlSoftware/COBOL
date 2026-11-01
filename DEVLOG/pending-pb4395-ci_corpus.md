## PB4395 item 1, the CI and corpus family: five pipeline classes leave the compiler's test assembly

kb/Work PB4395 item 1, family CI_CORPUS (wave 20261810 group D, owner direction 2026-11-01: parallelize by script
family). Five C# classes of the Unit assembly checked pipeline tooling, not the compiler, so a workflow or script change
was a compiler change that only a build could check. Each moved to the script that owns its rule, reading .cs, workflow
and doc files as text, and each C# class was deleted in the same change (no forwarders).

**Re-probe.** On this tree the five classes held 29 test methods, 40 cases; 38 passed and the two
`ExternalCorpusPopulationDriftTests` facts that need the GPL corpus were red until `external_corpus.py ensure` fetched
it (the worktree shape PB2611 names).

**Where each rule went.**
- `CiBoundsDriftTests` (8 methods) became `scripts/ci_bounds.py` (new: `--check`, `--self-test` of 14 arms): the six
  fixture arms of the one C# fixture method, the design-section reader, each of the six audits over the real tree with its
  population floor, and the PB4349 pointer arm, which now reads this module's own docstring. DESIGN-test-build-ci.md §1.4
  names the script.
- `SparseCheckoutDriftTests` (13 methods, 23 cases) and its reader `SparseCheckoutAudit` became
  `scripts/sparse_checkout.py` (new: a port of the reader over `scripts/ci_workflow.py`, `--check`, `--self-test` of 14
  arms; the theory's 11 rows are one arm).
- `HostWitnessTraitDriftTests` (2 methods) became two rules of `scripts/spec/drift_rules.py --check`, the RENDERED VIEWS
  audit that runs in every lane, with three planted arms in its `--self-test`. They read sources, not compiled
  attributes, so the sweep-trait equality now covers every test project (the C# class saw only the Unit assembly: the gap
  PB4328's report named). The fixture found a defect in the first draft: a one-line partition declaration
  (`class P0 : Base<S0>;`) passed its Host trait to the next class's preamble.
- `ExternalCorpusFetchTests` (4 methods) became three arms of `scripts/external_corpus.py --self-test` (the fetch's own
  `-SelfTest` and its named checks, only the rule names the fetch script, no `--force-local` tar call). Its declared-arms
  list sits in `scripts/corpus_sweep.py`'s self-test, the corpus family's other script, because
  `scripts/self_test_arms.py` (the register it belongs in) is another group's file in this wave.
- `ExternalCorpusPopulationDriftTests` (3 methods) became `scripts/corpus_sweep.py --self-test` (6 arms): the invariant on
  the real corpus, obtained through `external_corpus.ensure`, the programs-not-files count, the emptied-extraction reds
  and the census, which now runs on a planted tree, so the self-test reads the compiler's test corpora only through the
  external corpus and the committed baseline.

**What the move needed beside it.** `scripts/inert_lane.py` TOOLING_READS gained the two data paths these self-tests read
(`docs/rearchitecture/DESIGN-test-build-ci.md` for ci_bounds.py, `tests/external/gnucobol-verdict-baseline.tsv` for
corpus_sweep.py): without them `self_tests.py`'s population proof refuses the PASS. CI's `audits` and
`windows-self-tests` jobs restore main's GnuCOBOL tarball, so the population self-test fetches over the network only on a
cold cache. `scripts/ci_workflow.py` and `scripts/self_test_arms.py` now call `ensure_utf8()` first (the STANDARD STREAMS
rule was red on both at the base). No compiler test reads the external corpus any more.

**Self-review.** One mechanism per job: the two workflow audits share the one reader and are two scripts; the trait rules
live in the index whose sweep classification they compare. No second copy of a rule: the C# reader is deleted with its
last caller here. Docs follow the code: DESIGN-test-build-ci.md §1.4, the corpus instrument rows, the supersede step,
§3.17.3 and §3.17.8; DOC_INDEX.md; the workstream briefs and landing reference; the comments of the scripts and workflows that named
the deleted classes.

**Gate.** See the branch report (E:\COBOL-coord\scratch\reports\w20261810d-PB4395-report.md).
