## Review round 2 (2026-11-11): a module loaded by exec_module, and the re-derived key

The reviewer (w20261997b, head 029397d649, same findings file, probe `hook_probe.py`) confirmed that R1-1 and R1-2 are
fixed for the cases they raised. It found one blocking gap and one lead.

**R2-1 (medium): a module loaded by `spec_from_file_location` + `exec_module` was unjudged with a warm cache.**
- *Scenario* (`hook_probe.py`): that loader never puts the module in `sys.modules`, so round 1's end-of-run module list
  omitted it. With a warm `__pycache__` only the .pyc is opened, and the static closure does not see this load form. Run 2
  was recorded, and an edit to the undeclared `lib/helper.py` left the target "up to date" on stale output. This is the form
  `conformance-suite/harness/config.py` `_load_hook` uses for adapter hooks. There is no false green today only because the
  real hooks sit under the declared `conformance-suite/adapters/`.
- *Fix at the root:* the TRACER sets `sys.pycache_prefix` to a fresh empty directory of the run's own before anything is
  imported, and removes that directory at the end. With no cache to read, every Python source the run executes is OPENED,
  whatever loader runs it, and the open hook judges it. A warm `__pycache__` in the worktree is never read.
- *One mechanism per job:* round 1's `sys.modules` listing judged sources, and the open event now does that job, so that
  part is deleted. What remains of the listing covers the one thing the open event cannot see: a module that is not a
  Python source, such as a compiled extension, whose loading raises no open event. Such a module loaded from inside the
  repository is a failure naming it. One rule was added to `undeclared`: compiled code (`.pyc`, `.pyd`, `.so`) opened inside
  the worktree is a failure, because the run's own cache is outside the worktree, so such a file is code with no tracked
  source.
- *Proof:*
  - `buildgraph --self-test` PASS, 23 arms. The new arm: a module loaded by `spec_from_file_location` + `exec_module`, with
    a warm cache in the default location, is refused ("opened lib/helper.py").
  - With the tracer's cache line removed, both warm-cache arms FAIL.
  - `hook_probe.py`: runs 1, 2 and 3 are all `UndeclaredRead … opened lib/helper.py`, so nothing is recorded.
  - `pyc_probe.py`: runs 1, 2 and 3 are all `UndeclaredRead … opened lib/helper.py`.
  - `audit_probe.py`: `GeneratorCheckTests.cs` is still undeclared.

**R2-2 (low): `--rederive` keyed Conformance on the digest the store holds now, not the digest the run was handed.**
- *Fix* (`Gate._rederived_keys`): a dependent assembly's key uses the output digest the RUN recorded (`generated`). If the
  store's record at that generated key now holds another digest, the run is refused with a message naming both digests. A
  run from before the hand-off recorded no generation, and its host generated its own suite. Its Conformance is now REFUSED:
  its level is `refused: …`, it is left out of the re-derived keys, and it is never stored or carried. That replaces the
  residue round 1 named. Unit and Characterization are still re-derived. `buildgraph.keys(refused=)` and
  `Host.target_keys(refused=)` leave an unresolvable key out with its reason instead of raising.
- *Proof:* new `gate --self-test` arm, over a real gate record (g7 of the generated-target arms):
  - the keys equal the run's own recorded keys;
  - with the store's suite record rewritten to another digest, the run is refused, naming both digests;
  - with `generated` removed (a run before the hand-off), Conformance is refused and Unit keeps its recorded key.

**Determinism.** Three traced generations at key `db25f0f1c393` (tree `ae55ec71582d`), across two worktree paths and three
hash seeds, all wrote `c399e25b9a6ee4002690df5d475f3b3b000cf305e6e836c467e541e76ce5a9c3`. That is round 1's digest, as it
should be: the generator's inputs did not change. Every module source the real generator executes is now opened and
judged, and none lies outside its declared inputs.

The merge of origin/main after this round brought only `tests/version-matrix/inventory-1985/` records (a live
records-only change). The generator's key moved to `c3638872b296`, at tree `fc2c9c18cca6`. Three traced generations there
(two paths, three seeds) again wrote `c399e25b9a6e…`, so Conformance's key is unchanged and Conformance is up to date: the
cut-off this note exists for. With `_rederived_keys` replaced by the old store-keyed lookup, the new rederive arm FAILS.
