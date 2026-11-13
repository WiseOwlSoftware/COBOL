#!/usr/bin/env python3
"""selftest_layout.py — THE CASE LAYOUT RULE: where a script's tests live, and what a tests directory may hold
(docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.2; kb/Work PB5428, PB3726).

`scripts/self_tests.py` calls `check_repo` first on every run: this module has no entry point of its own, and imports no
runner (scratch_repo.py and inert_lane.py import it for the path rule, and a runner import would put the whole runner in
the build targets' import closure, scripts/build_targets.json).

WHERE A TEST LIVES. The tests of a script `<root>/<path>/<name>.<ext>`, for each live-tools root (`live_tools.ROOTS`),
live in the directory `<root>/selftests/<path>/<name>/`; `subject_of` is the ONE function of that rule (a test file's
repository path to its subject's), and `selftest_cases.subject`, the refusals below and the static readers
(`inert_lane._imports`, `scratch_repo._self_test_scopes`) all call it. A test whose subject is no file under a root lives
in `<root>/selftests/_tree/<name>/`, the one directory whose subject the rule does not look up.

THE REFUSALS (`check` returns each as a line; scripts/self_tests.py turns the first into the run's RED verdict):
  - a directory under `<root>/selftests/` whose subject does not exist, or whose Python subject still handles
    `--self-test` or defines a `self_test`;
  - a file there that is neither `fixtures.py` nor `test_*.py`;
  - a `test_*.py` that does not end in the `selftest_cases.run` call, defines no `case_` function, keeps its `case_`
    functions out of name order, defines two top-level functions of one name, defines a top-level function that is
    neither `case_*` nor `_`-prefixed, or calls `expect` outside a `case_` function;
  - a `fixtures.py` that defines a `case_` function or calls `expect`;
  - a module outside `<root>/selftests/` that imports `selftest_cases` or a module of a `selftests/` tree;
  - THE RATCHET: the set of Python files outside `selftests/` that still hold a self-test differs from `REMAINING`.
    A file holds a self-test when it handles `--self-test`, is a `test_*.py` the runner runs bare, or defines a
    top-level `self_test*` or `*_arms` function (the `scripts/docchecks/*.py` delegates, reached only through
    `doc_audits.py`). A path in the tree and not in the set is a new in-file self-test; a path in the set and not in the
    tree is a conversion or a deletion that did not remove its line. Each conversion removes exactly its own subjects'
    lines in the landing that converts them; the set is one path per line, sorted, so two branches that convert
    different subjects merge with plain git (section 16.2: why a set and not a count).
"""
from __future__ import annotations

import ast
from collections.abc import Callable, Iterable, Mapping
from pathlib import Path

import live_tools

REPO = Path(__file__).resolve().parents[1]
SELFTESTS = "selftests"
UNDER_TREE = "_tree"
FIXTURES = "fixtures.py"
COLLECTOR = "selftest_cases"
#: The extensions a subject may have, in the order a directory name is resolved against them.
SUBJECT_SUFFIXES = (".py", ".sh", ".ps1", ".js", ".mjs", ".cmd")

#: THE RATCHET'S SET: every Python file under the live roots and outside `selftests/` that still holds a self-test,
#: one repository path per line in sorted order. A conversion deletes its own lines; nothing else edits this.
REMAINING: tuple[str, ...] = (
    "conformance-suite/harness/generate.py",
    "conformance-suite/harness/run.py",
    "scripts/abi_pin.py",
    "scripts/api_key.py",
    "scripts/arch/census.py",
    "scripts/arch/emit_fraction.py",
    "scripts/arch/file_census_notes.py",
    "scripts/arch/member_index.py",
    "scripts/arch/perf_baseline.py",
    "scripts/arch/r2_collect.py",
    "scripts/arch/r2_cost.py",
    "scripts/arch/r2_inputs.py",
    "scripts/arch/r2_subsystems.py",
    "scripts/arch/source_units.py",
    "scripts/buildgraph.py",
    "scripts/carry_retired_registers.py",
    "scripts/ci_bounds.py",
    "scripts/ci_plan.py",
    "scripts/ci_workflow.py",
    "scripts/conflict_markers.py",
    "scripts/corpus_sweep.py",
    "scripts/detached.py",
    "scripts/devlog.py",
    "scripts/differential_fuzz.py",
    "scripts/doc_audits.py",
    "scripts/docchecks/common.py",
    "scripts/docchecks/design.py",
    "scripts/docchecks/facts.py",
    "scripts/docchecks/inventory.py",
    "scripts/docchecks/manual.py",
    "scripts/docchecks/register.py",
    "scripts/docchecks/tree.py",
    "scripts/docchecks/vcr.py",
    "scripts/external_corpus.py",
    "scripts/filter_population.py",
    "scripts/fuzz_batch.py",
    "scripts/fuzz_claude.py",
    "scripts/fuzz_ollama.py",
    "scripts/gate.py",
    "scripts/gate_growth.py",
    "scripts/gate_slot.py",
    "scripts/git_batch.py",
    "scripts/git_merge.py",
    "scripts/history_sha.py",
    "scripts/hooks/inbox_guard.py",
    "scripts/hooks/session_start.py",
    "scripts/hooks/test_dispatch_guard.py",
    "scripts/hooks/test_forbidden_commands.py",
    "scripts/hooks/test_own_scratch.py",
    "scripts/hooks/test_readonly_repo.py",
    "scripts/hooks/test_shell_location.py",
    "scripts/hooks/test_worktree_rm_allow.py",
    "scripts/hooks/tooling_check.py",
    "scripts/inert_lane.py",
    "scripts/line_endings.py",
    "scripts/live_tools.py",
    "scripts/models/kept_liveness_model.py",
    "scripts/models/sequential_journal_model.py",
    "scripts/orchestrator/account.py",
    "scripts/orchestrator/cancel_superseded.py",
    "scripts/orchestrator/compact_notes.py",
    "scripts/orchestrator/coord.py",
    "scripts/orchestrator/enqueue.py",
    "scripts/orchestrator/group_sizes.py",
    "scripts/orchestrator/land_lane.py",
    "scripts/orchestrator/landing_check.py",
    "scripts/orchestrator/landing_measure.py",
    "scripts/orchestrator/landing_rules.py",
    "scripts/orchestrator/mailbox.py",
    "scripts/orchestrator/progress.py",
    "scripts/orchestrator/queue_facts.py",
    "scripts/orchestrator/review_findings.py",
    "scripts/orchestrator/review_lane.py",
    "scripts/orchestrator/ruleset.py",
    "scripts/orchestrator/test_adopt_main.py",
    "scripts/orchestrator/test_alloc.py",
    "scripts/orchestrator/test_budget.py",
    "scripts/orchestrator/test_checkpoint.py",
    "scripts/orchestrator/test_inventory_ratchet.py",
    "scripts/orchestrator/test_ledger_state.py",
    "scripts/orchestrator/test_next_unit.py",
    "scripts/orchestrator/test_rolling_wave.py",
    "scripts/orchestrator/test_watch_agent.py",
    "scripts/orchestrator/turns_by_group_size.py",
    "scripts/process_job.py",
    "scripts/prune_worktrees.py",
    "scripts/rename_diagnostic_prefix.py",
    "scripts/rename_retired_names.py",
    "scripts/rendered_views.py",
    "scripts/scratch_repo.py",
    "scripts/script_layering.py",
    "scripts/self_test_arms.py",
    "scripts/self_test_clock.py",
    "scripts/self_test_inputs.py",
    "scripts/self_tests.py",
    "scripts/semgrep/verify.py",
    "scripts/shard_weights.py",
    "scripts/sharedfile.py",
    "scripts/sparse_checkout.py",
    "scripts/spec/audit_annex_a1.py",
    "scripts/spec/audit_code_citations.py",
    "scripts/spec/audit_derivations.py",
    "scripts/spec/audit_doc_citations.py",
    "scripts/spec/audit_evidence_supersession.py",
    "scripts/spec/audit_grammar_optional_words.py",
    "scripts/spec/audit_witness_loss.py",
    "scripts/spec/backfill_closes_rows.py",
    "scripts/spec/citation.py",
    "scripts/spec/cite.py",
    "scripts/spec/construct_store.py",
    "scripts/spec/derive_verdict_batch.py",
    "scripts/spec/drift_rules.py",
    "scripts/spec/edition_gates.py",
    "scripts/spec/element_standing.py",
    "scripts/spec/extract_annex_a2.py",
    "scripts/spec/extract_rule_catalog.py",
    "scripts/spec/figure_options.py",
    "scripts/spec/figure_reading.py",
    "scripts/spec/gen_ledger.py",
    "scripts/spec/generated_output.py",
    "scripts/spec/grammar_findings_to_batch.py",
    "scripts/spec/grammar_source.py",
    "scripts/spec/inventory_schema.py",
    "scripts/spec/inventory_store.py",
    "scripts/spec/inventory_view.py",
    "scripts/spec/ledger_plan.py",
    "scripts/spec/lineage.py",
    "scripts/spec/merge_reconciliation.py",
    "scripts/spec/page_workunit.py",
    "scripts/spec/phase_b_batch.py",
    "scripts/spec/place_witnesses.py",
    "scripts/spec/reconcile_ranges.py",
    "scripts/spec/reserved_word_order.py",
    "scripts/spec/rule_catalog_1985.py",
    "scripts/spec/run_drafts.py",
    "scripts/spec/spec_headings.py",
    "scripts/spec/spec_pdf.py",
    "scripts/spec/sweep_figures.py",
    "scripts/spec/test_audit_batch.py",
    "scripts/spec/test_figure_geometry.py",
    "scripts/spec/test_figure_raster.py",
    "scripts/spec/test_render_figure.py",
    "scripts/spec/test_text_layer.py",
    "scripts/spec/test_transcribe_pdf.py",
    "scripts/spec/test_wf_spec_reconcile.py",
    "scripts/spec/verify_acknowledgment.py",
    "scripts/spec/verify_publishable.py",
    "scripts/spec/work.py",
    "scripts/spec/work_inventory.py",
    "scripts/standard_streams.py",
    "scripts/statusline_tap.py",
    "scripts/temp_names.py",
    "scripts/test_fuzz_history.py",
    "scripts/test_fuzz_triage.py",
    "scripts/test_fuzz_undefined.py",
    "scripts/test_held_analyzer_output.py",
    "scripts/test_leg_report.py",
    "scripts/test_linux_gate.py",
    "scripts/test_population.py",
    "scripts/toolchain.py",
    "scripts/toolchain_audit.py",
    "scripts/verdict_line.py",
)


def tree_parts(rel: str) -> tuple[str, list[str]] | None:
    """(the root, the path components below its `selftests/`) of a repository path under a `<root>selftests/` tree, or
    None for any other path."""
    for root in live_tools.ROOTS:
        prefix = f"{root}{SELFTESTS}/"
        if rel.startswith(prefix):
            return root, rel[len(prefix):].split("/")
    return None


def in_tree(rel: str) -> bool:
    """True for a repository path under a `<root>selftests/` tree (a case file, a fixtures file or a stray one)."""
    return tree_parts(rel) is not None


def subject_dir(rel: str) -> tuple[str, str] | None:
    """(the subject's path without its extension, the test directory's name) for a file or directory path under a
    `selftests/` tree whose directory is a subject's, else None (not under a tree, directly in it, or under `_tree`)."""
    parts = tree_parts(rel)
    if parts is None:
        return None
    root, below = parts
    dirs = below[:-1] if "." in below[-1] else below
    if not dirs or dirs[0] == UNDER_TREE:
        return None
    # the subject's directory is the test directory's path; the files of a test directory sit directly in it
    return f"{root}{'/'.join(dirs)}", dirs[-1]


def tests_dir_of(rel: str) -> str | None:
    """THE PATH RULE, inverted: the directory the tests of the script `rel` live in (`scripts/gate.py` ->
    `scripts/selftests/gate`), or None when `rel` is under no live root or is itself a test."""
    if in_tree(rel):
        return None
    for root in live_tools.ROOTS:
        if rel.startswith(root):
            stem = rel[len(root):].rpartition(".")[0]
            return f"{root}{SELFTESTS}/{stem}" if stem else None
    return None


def subject_of(rel: str, repo: Path = REPO) -> str | None:
    """THE PATH RULE (module docstring): the repository path of the script a test file `rel` tests, or None when `rel`
    is not under a `selftests/` tree, is a `_tree` test, or names a subject that does not exist."""
    found = subject_dir(rel)
    if found is None:
        return None
    stem, _ = found
    for suffix in SUBJECT_SUFFIXES:
        if (repo / f"{stem}{suffix}").is_file():
            return f"{stem}{suffix}"
    return None


# ── the refusals ─────────────────────────────────────────────────────────────────────────────────────────────────────


def _top_functions(tree: ast.Module) -> list[ast.FunctionDef | ast.AsyncFunctionDef]:
    return [n for n in tree.body if isinstance(n, (ast.FunctionDef, ast.AsyncFunctionDef))]


def _calls_expect(node: ast.AST) -> list[ast.Call]:
    """Every `expect(...)` and `expect.<method>(...)` call under `node`."""
    out = []
    for n in ast.walk(node):
        if isinstance(n, ast.Call):
            f = n.func
            if (isinstance(f, ast.Name) and f.id == "expect") or (
                    isinstance(f, ast.Attribute) and isinstance(f.value, ast.Name) and f.value.id == "expect"):
                out.append(n)
    return out


def _ends_in_run(tree: ast.Module) -> bool:
    """The module's last statement runs the collector: `if __name__ == "__main__": sys.exit(selftest_cases.run(...))`."""
    if not tree.body:
        return False
    return any(isinstance(n, ast.Call) and isinstance(n.func, ast.Attribute) and n.func.attr == "run"
               and isinstance(n.func.value, ast.Name) and n.func.value.id == COLLECTOR
               for n in ast.walk(tree.body[-1]))


def refusals_of_case_file(rel: str, source: str) -> list[str]:
    """The refusals for ONE `test_*.py` of a tests directory (module docstring)."""
    try:
        tree = ast.parse(source, filename=rel)
    except SyntaxError as e:
        return [f"{rel}: does not parse: {e}"]
    out: list[str] = []
    funcs = _top_functions(tree)
    cases = [f.name for f in funcs if f.name.startswith("case_")]
    if not _ends_in_run(tree):
        out.append(f"{rel}: does not end in the `{COLLECTOR}.run` call")
    if not cases:
        out.append(f"{rel}: defines no `case_` function (an empty file is not a pass)")
    elif cases != sorted(cases):
        out.append(f"{rel}: keeps its `case_` functions out of name order (a new case goes where its name sorts)")
    seen: set[str] = set()
    for f in funcs:
        if f.name in seen:
            out.append(f"{rel}:{f.lineno}: defines `{f.name}` twice (Python keeps the second silently)")
        seen.add(f.name)
        if not f.name.startswith(("case_", "_")):
            out.append(f"{rel}:{f.lineno}: top-level function `{f.name}` is neither `case_*` nor `_`-prefixed "
                       f"(the collector would never call it)")
    inside = {id(c) for f in funcs if f.name.startswith("case_") for c in _calls_expect(f)}
    for call in _calls_expect(tree):
        if id(call) not in inside:
            out.append(f"{rel}:{call.lineno}: `expect` is called outside a `case_` function (a module-level "
                       f"expectation is a case list again)")
    return out


def refusals_of_fixtures(rel: str, source: str) -> list[str]:
    """The refusals for ONE `fixtures.py`: it holds worlds, never a case or an expectation."""
    try:
        tree = ast.parse(source, filename=rel)
    except SyntaxError as e:
        return [f"{rel}: does not parse: {e}"]
    out = [f"{rel}:{f.lineno}: a fixtures file defines the case `{f.name}`" for f in _top_functions(tree)
           if f.name.startswith("case_")]
    out += [f"{rel}:{c.lineno}: a fixtures file calls `expect`" for c in _calls_expect(tree)]
    return out


def imported_modules(source: str) -> set[str]:
    """The top-level names a Python module imports, wherever the import stands; a module that does not parse, none."""
    try:
        tree = ast.parse(source)
    except (SyntaxError, ValueError):
        return set()
    out: set[str] = set()
    for n in ast.walk(tree):
        if isinstance(n, ast.Import):
            out.update(a.name.split(".")[0] for a in n.names)
        elif isinstance(n, ast.ImportFrom) and n.module and not n.level:
            out.add(n.module.split(".")[0])
    return out


def holders(sources: Mapping[str, str], handles_flag: Callable[[str], bool]) -> set[str]:
    """The ratchet's measured set (module docstring): the Python files of `sources` (repository path -> text) outside
    `selftests/` that hold a self-test."""
    out: set[str] = set()
    for rel, text in sources.items():
        if in_tree(rel):
            continue
        name = rel.rsplit("/", 1)[-1]
        try:
            tree = ast.parse(text, filename=rel)
        except SyntaxError:
            continue  # discovery (scripts/self_tests.py) fails a script that does not parse; this rule counts the rest
        defines = any(f.name.startswith("self_test") or f.name.endswith("_arms") for f in _top_functions(tree))
        if handles_flag(text) or name.startswith("test_") or defines:
            out.add(rel)
    return out


def check(sources: Mapping[str, str], files: Iterable[str], exists: Callable[[str], bool],
          handles_flag: Callable[[str], bool], remaining: Iterable[str] | None = None) -> list[str]:
    """The refusals (module docstring) over a tree, as lines; empty when the tree keeps the layout.

    `sources` maps each tracked Python file's repository path to its text, `files` is every tracked file under the live
    roots (any extension), `exists` says whether a repository path is a file, and `handles_flag` is
    `self_tests.py_handles_flag` (this module cannot import that one, which imports this)."""
    out: list[str] = []
    listed = sorted(files)
    tree_files = [f for f in listed if in_tree(f)]
    dirs: set[str] = set()
    for rel in tree_files:
        found = subject_dir(rel)
        name = rel.rsplit("/", 1)[-1]
        if found is not None:
            dirs.add(found[0])
        if name != FIXTURES and not (name.startswith("test_") and name.endswith(".py")):
            out.append(f"{rel}: a file under selftests/ that is neither `fixtures.py` nor `test_*.py`")
            continue
        if found is None and not (tree_parts(rel) or ("", []))[1][0] == UNDER_TREE:
            out.append(f"{rel}: a test file directly in selftests/ (it belongs in `selftests/<path>/<name>/`)")
        text = sources.get(rel, "")
        out += refusals_of_fixtures(rel, text) if name == FIXTURES else refusals_of_case_file(rel, text)
    for stem in sorted(dirs):
        subject = next((f"{stem}{s}" for s in SUBJECT_SUFFIXES if exists(f"{stem}{s}")), None)
        shown = stem.replace("/selftests/", "/")
        if subject is None:
            out.append(f"{shown}: the tests directory has no subject (no `{shown}` with one of "
                       f"{', '.join(SUBJECT_SUFFIXES)})")
        elif subject.endswith(".py") and handles_flag(sources.get(subject, "")):
            out.append(f"{subject}: still handles `--self-test` although its cases live in selftests/")
        elif subject.endswith(".py") and any(f.name == "self_test" for f in _top_functions(
                ast.parse(sources.get(subject, "")))):
            out.append(f"{subject}: still defines `self_test` although its cases live in selftests/")
    trees = {Path(f).stem for f in tree_files if f.endswith(".py")}
    outside = {Path(f).stem for f in sources if not in_tree(f)}
    for rel, text in sorted(sources.items()):
        if in_tree(rel):
            continue
        for module in sorted(imported_modules(text)):
            if module == COLLECTOR or (module in trees and module not in outside):
                out.append(f"{rel}: a module outside selftests/ imports `{module}`, which lives in a selftests/ tree")
    want = set(REMAINING if remaining is None else remaining)
    have = holders(sources, handles_flag)
    out += [f"{p}: a new in-file self-test (write its cases under selftests/; the ratchet, REMAINING)"
            for p in sorted(have - want)]
    out += [f"{p}: listed in REMAINING but holds no self-test or is gone (a conversion removes its own line)"
            for p in sorted(want - have)]
    return out


def check_repo(handles_flag: Callable[[str], bool], repo: Path = REPO, env: Mapping[str, str] | None = None) -> list[str]:
    """`check` over the tracked tree at `repo`; `env` is the git child's environment (live_tools.tracked)."""
    files = live_tools.tracked(repo, None, env)
    sources = {f: (repo / f).read_text(encoding="utf-8") for f in files if f.endswith(".py")}
    return check(sources, files, lambda p: (repo / p).is_file(), handles_flag)
