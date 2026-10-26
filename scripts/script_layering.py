#!/usr/bin/env python3
"""script_layering.py — THE PIPELINE SCRIPTS' IMPORT DAG (kb/Work PB3726, PB3728; docs/rearchitecture/DESIGN-orchestrator-loop.md
section 16.10).

    python scripts/script_layering.py --check       # every declared module's imports against the DAG below
    python scripts/script_layering.py --self-test   # the planted cases, then --check over this tree

⛔ WHY. The five pipeline scripts each did several jobs (plan_wave.py, prune_worktrees.py, run_gate_legs.py, enqueue.py,
land_lane.py), and every fix appended to them; PB3726 splits each into library modules with ONE job each. A split is
kept only if the modules stay layered: a report reader that imports the planner, or a survey module that imports the
CLI built on it, is the tangle coming back. This audit holds the layering by structure, not by a list of forbidden
pairs: the DAG below is the one declaration, read from each module's own syntax tree (every `import` and `from ...
import`, at module level, inside a function, or under `if TYPE_CHECKING:`), and the C# sweep
tests/Cobol.Net.Tests.Unit/ScriptLayeringDriftTests.cs runs `--check` in every gate's leg 1.

THE RULES (--check):
  1. DECLARED   a declared module may import another declared module only where the DAG lets it: a module of an
                EARLIER tier of a line both are on (LINES), or, from scripts/orchestrator/, a declared module of
                scripts/ itself (the gate's modules: "or in scripts/", section 16.10). Modules that are not declared (the
                standard library, work.py, git_batch.py, alloc.py, ...) are not constrained here.
  2. ACYCLIC    the DAG itself has no cycle: the union of every line's tier order is a partial order.
  3. ABSENT     a declared module whose file does not exist yet (a later step creates it: NODES names the step) is
                skipped: it imports nothing and nothing imports it.
  4. UNSPLIT    a module whose split has not landed yet (SPLITS: none of the modules its split creates exists) STANDS FOR
                those modules: its own imports are its split's to check, and an import OF it is allowed wherever an
                import of the module itself or of any module of its split is. Its split's landing removes the stand-in,
                and from then on the module is checked like any other (kb/Work PB3731, PB3733, PB3735, PB3737).
  5. TESTS      an import inside a function named `self_test` or `_self_test` is test code, not a layer: a script's cases
                move to scripts/selftests/ (section 16.2), which the DAG does not constrain.
"""
from __future__ import annotations

import argparse
import ast
import pathlib
import sys
from typing import Iterable, Mapping, Sequence

from standard_streams import ensure_utf8  # (scripts/, this file's own directory)

REPO = pathlib.Path(__file__).resolve().parents[1]

#: THE DAG: each LINE is a sequence of TIERS (tuples of module names). A module may import every module of an EARLIER
#: tier of any line it is on; modules of one tier never import each other. Section 16.10 of the design draws these lines.
LINES: tuple[tuple[tuple[str, ...], ...], ...] = (
    # scripts/: the gate
    (("gate_verdict",), ("gate_runs",), ("gate_host",), ("run_gate_legs",)),
    # scripts/orchestrator/: the branch survey, its CLI last
    (("coord",), ("reports",), ("branch_classify",), ("review_coverage",), ("branch_lifecycle",), ("branch_survey",),
     ("prune_worktrees",)),
    # the planner, its CLI last
    (("coord",), ("reports",), ("branch_classify",), ("inflight",), ("clusters",), ("groups",), ("dispatch_ledger",),
     ("plan_wave",)),
    # the survey reads the planner's in-flight rule and the notes' weights; the planner reads the survey
    (("inflight", "clusters"), ("branch_survey",)),
    (("review_coverage", "branch_lifecycle", "branch_survey", "prune_worktrees"), ("plan_wave",)),
    # the landing step, its CLI last
    (("coord",), ("landing_verdict",), ("land_checks", "land_tree", "land_evidence", "land_queue"), ("enqueue",)),
    # the landing lane, its CLI last
    (("coord",), ("landing_verdict", "lane_state"), ("conflict_brief", "lane_decide", "lane_watch"), ("lane_status",),
     ("land_lane",)),
)

#: Every declared module: its repo-relative file, and the kb/Work step that creates it (None: it exists already).
NODES: dict[str, tuple[str, str | None]] = {
    "gate_verdict": ("scripts/gate_verdict.py", "PB3733"),
    "gate_runs": ("scripts/gate_runs.py", "PB3733"),
    "gate_host": ("scripts/gate_host.py", "PB3733"),
    "run_gate_legs": ("scripts/run_gate_legs.py", None),
    "coord": ("scripts/orchestrator/coord.py", None),
    "reports": ("scripts/orchestrator/reports.py", "PB3728"),
    "branch_classify": ("scripts/orchestrator/branch_classify.py", "PB3731"),
    "review_coverage": ("scripts/orchestrator/review_coverage.py", "PB3731"),
    "branch_lifecycle": ("scripts/orchestrator/branch_lifecycle.py", "PB3731"),
    "branch_survey": ("scripts/orchestrator/branch_survey.py", "PB3731"),
    "prune_worktrees": ("scripts/prune_worktrees.py", None),
    "inflight": ("scripts/orchestrator/inflight.py", "PB3728"),
    "clusters": ("scripts/orchestrator/clusters.py", "PB3728"),
    "groups": ("scripts/orchestrator/groups.py", "PB3728"),
    "dispatch_ledger": ("scripts/orchestrator/dispatch_ledger.py", "PB3728"),
    "plan_wave": ("scripts/orchestrator/plan_wave.py", None),
    "landing_verdict": ("scripts/orchestrator/landing_verdict.py", "PB3735"),
    "land_checks": ("scripts/orchestrator/land_checks.py", "PB3735"),
    "land_tree": ("scripts/orchestrator/land_tree.py", "PB3735"),
    "land_evidence": ("scripts/orchestrator/land_evidence.py", "PB3735"),
    "land_queue": ("scripts/orchestrator/land_queue.py", "PB3735"),
    "enqueue": ("scripts/orchestrator/enqueue.py", None),
    "lane_state": ("scripts/orchestrator/lane_state.py", "PB3737"),
    "conflict_brief": ("scripts/orchestrator/conflict_brief.py", "PB3737"),
    "lane_decide": ("scripts/orchestrator/lane_decide.py", "PB3737"),
    "lane_watch": ("scripts/orchestrator/lane_watch.py", "PB3737"),
    "lane_status": ("scripts/orchestrator/lane_status.py", "PB3737"),
    "land_lane": ("scripts/orchestrator/land_lane.py", None),
}

#: A module whose split has not landed: the step that splits it and the modules that step creates out of it (rule 4).
SPLITS: dict[str, tuple[str, tuple[str, ...]]] = {
    "prune_worktrees": ("PB3731", ("branch_classify", "review_coverage", "branch_lifecycle", "branch_survey")),
    "run_gate_legs": ("PB3733", ("gate_verdict", "gate_runs", "gate_host")),
    "enqueue": ("PB3735", ("landing_verdict", "land_checks", "land_tree", "land_evidence", "land_queue")),
    "land_lane": ("PB3737", ("lane_state", "conflict_brief", "lane_decide", "lane_watch", "lane_status")),
}

TEST_FUNCTIONS = ("self_test", "_self_test")


def allowed_edges(lines: Sequence[Sequence[Sequence[str]]]) -> dict[str, set[str]]:
    """{module: every module the DAG lets it import} from the lines' tier order (rule 1, without the scripts/ clause)."""
    out: dict[str, set[str]] = {}
    for line in lines:
        earlier: set[str] = set()
        for tier in line:
            for m in tier:
                out.setdefault(m, set()).update(earlier)
            earlier |= set(tier)
    return out


def cycle(edges: Mapping[str, set[str]]) -> list[str] | None:
    """A cycle of the declared DAG (rule 2) as the modules along it, or None."""
    state: dict[str, int] = {}   # 1 on the path, 2 done
    path: list[str] = []

    def visit(m: str) -> list[str] | None:
        state[m] = 1
        path.append(m)
        for n in sorted(edges.get(m, ())):
            if state.get(n) == 1:
                return path[path.index(n):] + [n]
            if n not in state and (found := visit(n)):
                return found
        path.pop()
        state[m] = 2
        return None

    for m in sorted(edges):
        if m not in state and (found := visit(m)):
            return found
    return None


def imports_of(source: str) -> list[tuple[str, int]]:
    """[(the top-level module name an import names, its line)] for every import of the module, wherever it stands,
    except inside a test function (rule 5). Relative imports name no module of the DAG and are skipped."""
    found: list[tuple[str, int]] = []

    def walk(node: ast.AST) -> None:
        for child in ast.iter_child_nodes(node):
            if isinstance(child, (ast.FunctionDef, ast.AsyncFunctionDef)) and child.name in TEST_FUNCTIONS:
                continue
            if isinstance(child, ast.Import):
                found.extend((a.name.split(".")[0], child.lineno) for a in child.names)
            elif isinstance(child, ast.ImportFrom) and child.module and not child.level:
                found.append((child.module.split(".")[0], child.lineno))
            walk(child)

    walk(ast.parse(source))
    return found


def check(root: pathlib.Path, lines: Sequence[Sequence[Sequence[str]]] = LINES,
          nodes: Mapping[str, tuple[str, str | None]] = NODES,
          splits: Mapping[str, tuple[str, tuple[str, ...]]] = SPLITS) -> tuple[list[str], dict[str, int]]:
    """(the findings, the counts) of the rules above over the tree at `root`."""
    edges = allowed_edges(lines)
    findings: list[str] = []
    undeclared = sorted({m for line in lines for tier in line for m in tier} - set(nodes))
    if undeclared:
        findings.append(f"DAG: {', '.join(undeclared)} stand(s) on a line but no NODES entry names its file")
    if loop := cycle(edges):
        findings.append(f"DAG: the declared lines make a cycle: {' -> '.join(loop)}")
    present = {m for m, (rel, _) in nodes.items() if (root / rel).is_file()}
    unsplit = {m for m, (_, made) in splits.items() if m in present and not any(x in present for x in made)}
    # "or in scripts/" (section 16.10): the modules of a line that holds scripts/ modules only (the gate's), which every
    # module of scripts/orchestrator/ may import; prune_worktrees.py lives in scripts/ too, but stands on the survey's line
    in_scripts = {m for m, (rel, _) in nodes.items() if rel.count("/") == 1}
    gate = {m for line in lines if all(x in in_scripts for tier in line for x in tier) for tier in line for m in tier}

    def may_import(importer: str, target: str) -> bool:
        return target in edges.get(importer, set()) or (
            nodes[importer][0].startswith("scripts/orchestrator/") and target in gate)

    checked = 0
    for m in sorted(present - unsplit):
        rel = nodes[m][0]
        checked += 1
        for target, line in imports_of((root / rel).read_text(encoding="utf-8")):
            if target == m or target not in nodes or target not in present:
                continue
            stands_for = (target, *splits[target][1]) if target in unsplit else (target,)
            if not any(may_import(m, t) for t in stands_for):
                via = (f" (it stands for {', '.join(splits[target][1])} until {splits[target][0]} splits it)"
                       if target in unsplit else "")
                findings.append(f"{rel}:{line}: {m} imports {target}{via}, which the DAG does not let it import "
                                f"(DESIGN-orchestrator-loop.md section 16.10; scripts/script_layering.py LINES)")
    return findings, {"checked": checked, "absent": len(set(nodes) - present), "unsplit": len(unsplit)}


def verdict(findings: list[str], counts: Mapping[str, int]) -> str:
    return (f"=== SCRIPT LAYERING: {'FAIL' if findings else 'PASS'} — {counts['checked']} declared modules checked, "
            f"{len(findings)} finding(s) ({counts['absent']} declared modules not created yet, {counts['unsplit']} "
            f"standing for their split) ===")


def run_check(root: pathlib.Path = REPO) -> int:
    findings, counts = check(root)
    for f in findings:
        print(f"FAIL     {f}")
    print(verdict(findings, counts))
    return 1 if findings else 0


def self_test() -> int:
    fails: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"{'PASS' if ok else 'FAIL'}     {label}" + (f": {detail}" if detail and not ok else ""))
        if not ok:
            fails.append(label)

    lines = ((("coord",), ("reports",), ("branch_classify",), ("inflight",), ("plan_wave",)),
             (("coord",), ("reports",), ("branch_classify",), ("branch_survey",), ("prune_worktrees",)),
             (("inflight",), ("branch_survey",)),
             (("gate_verdict",), ("run_gate_legs",)))
    nodes = {"coord": ("scripts/orchestrator/coord.py", None), "reports": ("scripts/orchestrator/reports.py", "X1"),
             "branch_classify": ("scripts/orchestrator/branch_classify.py", "X2"),
             "inflight": ("scripts/orchestrator/inflight.py", "X1"), "plan_wave": ("scripts/orchestrator/plan_wave.py", None),
             "branch_survey": ("scripts/orchestrator/branch_survey.py", "X2"),
             "prune_worktrees": ("scripts/prune_worktrees.py", None),
             "gate_verdict": ("scripts/gate_verdict.py", "X3"), "run_gate_legs": ("scripts/run_gate_legs.py", None)}
    splits = {"prune_worktrees": ("X2", ("branch_classify", "branch_survey")),
              "run_gate_legs": ("X3", ("gate_verdict",))}
    import scratch_repo  # (scripts/: THE way a self-test makes its scratch, removed on every way out, kb/Work PB3194)
    root = scratch_repo.scratch_dir("script-layering-")

    def plant(files: Mapping[str, str]) -> tuple[list[str], dict[str, int]]:
        for rel, _ in nodes.values():
            (root / rel).unlink(missing_ok=True)
        for m, text in files.items():
            p = root / nodes[m][0]
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(text, encoding="utf-8", newline="\n")
        return check(root, lines, nodes, splits)

    base = {"coord": "import json\n", "reports": "import coord\n", "inflight": "import coord\nfrom reports import X\n",
            "plan_wave": "import reports\nimport inflight\n", "prune_worktrees": "import coord\nimport inflight\n"}
    f, c = plant(base)
    expect("the declared edges pass (an earlier tier, the standard library, an unsplit module's own imports)",
           f == [] and c == {"checked": 4, "absent": 4, "unsplit": 1}, f"{f} {c}")
    f, _ = plant({**base, "reports": "import coord\nimport inflight\n"})
    expect("an import of a LATER tier is a finding (the report reader importing the in-flight module)",
           len(f) == 1 and "reports.py:2: reports imports inflight" in f[0], str(f))
    f, _ = plant({**base, "reports": "import coord\ndef read():\n    import plan_wave\n"})
    expect("a lazy import inside a function is read too", len(f) == 1 and "imports plan_wave" in f[0], str(f))
    f, _ = plant({**base, "reports": "from typing import TYPE_CHECKING\nif TYPE_CHECKING:\n    import inflight\n"})
    expect("an import under TYPE_CHECKING is read too", len(f) == 1 and "imports inflight" in f[0], str(f))
    f, _ = plant({**base, "reports": "import coord\ndef self_test():\n    import plan_wave\n"})
    expect("an import inside self_test is test code, never a layer (rule 5)", f == [], str(f))
    f, _ = plant({**base, "inflight": "import coord\ndef git():\n    import prune_worktrees\n"})
    expect("while its split is absent, prune_worktrees stands for branch_classify, which inflight may import (rule 4)",
           f == [], str(f))
    f, c = plant({**base, "inflight": "import coord\ndef git():\n    import prune_worktrees\n",
                  "branch_classify": "import reports\n", "branch_survey": "import inflight\n",
                  "prune_worktrees": "import branch_survey\n"})
    expect("once its split exists, an import of the CLI prune_worktrees from inflight is a finding",
           len(f) == 1 and "inflight imports prune_worktrees, which" in f[0] and c["unsplit"] == 0, f"{f} {c}")
    f, _ = plant({**base, "branch_classify": "import reports\n", "branch_survey": "import inflight\n",
                  "prune_worktrees": "import plan_wave\n"})
    expect("once its split exists, prune_worktrees' own imports are checked (it may not import the planner)",
           len(f) == 1 and "prune_worktrees imports plan_wave" in f[0], str(f))
    f, _ = plant({**base, "prune_worktrees": "import plan_wave\n"})
    expect("while unsplit, prune_worktrees' own imports are its split's to check", f == [], str(f))
    f, _ = plant({**base, "reports": "import coord\ndef gate():\n    import run_gate_legs\n",
                  "run_gate_legs": "import json\n"})
    expect("an orchestrator module may import a module of scripts/ (the gate), unsplit or not", f == [], str(f))
    f, _ = plant({**base, "run_gate_legs": "import json\n", "gate_verdict": "import coord\n"})
    expect("a module of scripts/ may not import an orchestrator module", len(f) == 1 and "gate_verdict imports coord" in f[0],
           str(f))
    f, _ = check(root, ((("coord",), ("reports",)), (("reports",), ("coord",))), nodes, splits)
    expect("a cyclic declaration is refused (rule 2)", any("make a cycle" in x for x in f), str(f))
    f, _ = check(root, ((("coord",), ("nowhere",)),), nodes, splits)
    expect("a line naming a module NODES does not declare is refused", any("nowhere" in x for x in f), str(f))
    expect("the real DAG is acyclic and every module on a line is declared",
           cycle(allowed_edges(LINES)) is None and {m for l in LINES for t in l for m in t} <= set(NODES))
    rc = run_check(REPO)
    expect("--check over this tree", rc == 0)
    print(f"=== script_layering self-test: {'FAIL' if fails else 'PASS'} ({len(fails)} failed) ===")
    return 1 if fails else 0


def main(argv: Iterable[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--check", action="store_true", help="hold every declared module's imports to the DAG")
    ap.add_argument("--self-test", action="store_true", help="the planted cases, then --check over this tree")
    a = ap.parse_args(list(argv) if argv is not None else None)
    if a.self_test:
        return self_test()
    if a.check:
        return run_check()
    ap.error("one of --check or --self-test is required")
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
