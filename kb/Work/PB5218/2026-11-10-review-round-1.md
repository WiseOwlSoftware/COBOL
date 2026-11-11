## Review round 1 (w20261991a-reviewer at 82b71db804): FINDINGS 1, both answered at the root

- **R1-1 (medium, performance).** The BUILD TARGETS audit took 62 s on the real tree, because `unjudged_roots`
  recomputed `_top_dirs` (a `git ls-tree` process) once per C# source file, about 1,100 times, and read every target's
  sources a second time only to print a count. Every gate runs this audit, including the inert and tooling lanes, whose
  whole budget is about 40 s. Fix: `undeclared` and `unjudged_roots` are replaced by `scan`, which reads each target's
  sources once and returns both halves (`Scan.undeclared`, `Scan.roots`). `check` computes the top directories and the
  tracked paths once for all targets. Measured: `buildgraph.py check` took 1m02s before and 4.8 s after, with the same
  verdict (GREEN; roots unit 107, conformance 5, characterization 4).
- **R1-2 (low, lead).** `Target.matches` used `fnmatch.fnmatchcase`, which is case-sensitive, while the classification
  (`inert_lane._under`) and the .NET listing that a test reads both match case-insensitively. A branch that added
  `scripts/Guard-legacy.sh` would have left the conformance key unchanged, so Conformance would have shown as up to date
  and the gate would have been green: a false green. Fix: one matcher. `inert_lane._under` becomes the public
  `inert_lane.under`, every caller is changed, and `buildgraph._matches` delegates to it. A new self-test arm checks that
  `scripts/guard*.sh` matches `scripts/Guard-legacy.sh` and `scripts/guard-x.SH` but not `scripts/sub/guard-a.sh` or
  `scripts/other.sh`, and that adding the cased file changes the key. Under the old `fnmatchcase` this arm fails, because
  `fnmatchcase("scripts/Guard-legacy.sh", "scripts/guard*.sh")` is False.

Proof at the fix commit: `buildgraph.py --self-test` PASS (8 arms), `buildgraph.py check` GREEN in 4.8 s,
`inert_lane.py --self-test` PASS, `inert_lane.py check` GREEN, `gate.py --self-test` PASS (109 arms).
