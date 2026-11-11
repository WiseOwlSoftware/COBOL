## The import key: closing the false green the dropped import listings left

**The false green.** Item 7 made the tracer drop the `os.listdir` of the import system's directory cache
(`FileFinder._fill_cache`), so a Python self-test that imports from `scripts/` was no longer keyed on every name in
`scripts/` and the repository root. The cost was a reused PASS where a new file changes what an import resolves to: a new
`scripts/json.py` shadowing the standard library's `json`, a new `scripts/foo.py` answering a `try: import foo` /
`except ImportError` fallback, a new package directory `scripts/foo/` ahead of a module. None was opened by the recorded
run, so no file key could see it.

**Decision.** Key the import, not the directory: record the module NAMES each import looked for and keep, per searched
directory inside the repository, only the entries that could answer one of them. Bringing back the whole listing was
rejected (it re-ran 30 of 42 self-tests in one gate on unrelated new files).

**Mechanism** (`scripts/self_test_inputs.py`). The tracer handles the audit event `import`, which CPython raises in
`import_find_and_load` before the search, so a failed import is recorded too. A top-level name is recorded for each
`sys.path` entry inside the repository; a dotted name's last part for each entry of its parent package's `__path__`
(trace record `I <dir> <name>`). `Trace.imports` carries the pairs; `Deps.imports` holds
`{dir: {"names": [...], "found": Tree.importable(dir, names)}}`, where `Tree.importable` gives the directory's entries named
`name` (package or namespace directory) or `name<suffix>` for each `importlib.machinery.all_suffixes()` (exact case).
`Deps.changed` names each entry that appeared or went: `imports:<dir>/<entry>`. A record without `imports` raises
KeyError in `Deps.from_json`, which `self_tests.py` `lookup` already counts unusable, so old PASSes re-run once; no
PASS_SCHEMA bump is needed.

Probe on this host (Python 3.15.2): `import nosuchmod` → event `nosuchmod`; `from pkg import sub` → `pkg`, `pkg.sub`; a
relative `from . import rel` / `from .rel2 import x` inside `pkg` → `pkg.rel`, `pkg.rel2`; `from pkg import missing` →
`pkg.missing`. A module already in `sys.modules` raises nothing, and is not searched for either.

**What remains** (module docstring): `importlib.util.find_spec` and other `os.stat`-style probes; a finder, path hook,
zip importer or `.pth` a self-test installs; `sys.path` directories outside the repository; an answer the tree does not
hold (an empty namespace directory, a git-ignored file). CI never reuses a PASS.

**Proof.** `python scripts/self_test_inputs.py --self-test`: ALL GREEN, with arms (a) an unrelated new file: no change;
(b) a new `colorsys.py` shadowing the stdlib: `imports:scripts/o/colorsys.py`; (c) a new module and a new submodule
answering failed imports; (d) a new package directory `colorsys/`; (e) the imported module removed: its file key and
its import key; and the record round trip. Real tree, `scripts/devlog.py --self-test` under the tracer: PASS (49/49);
no directory key; import key `scripts` 73 names (json among them), 7 entries found; `scripts/orchestrator` 9 names,
2 found. A new `scripts/zz_unrelated_probe.py`: `[]`. A new `scripts/json.py`: `['imports:scripts/json.py']`.
