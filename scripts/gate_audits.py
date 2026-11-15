#!/usr/bin/env python3
"""gate_audits.py - THE GATE'S AUDITS (kb/Work PB5433, PB4328; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.5): the list
(`AUDITS`), which lane runs which, what they need first, the CI invariant over them (`ci_audits_not_in_gate`) and the ONE SHAPE audit
(`gate.py --audit-shape`)."""
from __future__ import annotations

import re
import subprocess
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]


#: ⛔ THE AUDITS, in order — THE list for the gate (build-local.ps1 and .sh used to carry a copy each); battery.sh PHASE
#: -1 and CI's `audits` job run the same scripts. A wrong § is the one defect no test can catch, and each costs about a
#: second. None needs a build or a test result, so they start FIRST and run BESIDE the build and the legs (kb/Work
#: PB4326, step 3 of PB4323: they were a serial valley ahead of the slot): a red one STOPS the test hosts and ends the gate
#: RED, and a full lane that finds one before the slot, the build or a leg has started never starts it (kb/Work PB2523 —
#: four of wave 1034's eight first-run reds were audit-only, each found after a whole ~6-min population). They run
#: OUTSIDE the gate slot (kb/Work PB2524): the slot rations builds and test hosts, not reading the tree.
#: They run CONCURRENTLY, each its own process with its output captured and printed whole (kb/Work PB2563), so the
#: phase costs its slowest audit, not their sum.
#: ⛔ AND THE FIRST RED STOPS THE REST (kb/Work PB3061): the moment one audit exits non-zero, every audit still running
#: is stopped — its whole process tree, so the self-test runner's children go with it — and reported STOPPED, never
#: RED. Wave 1062's agents A and B each had CITATIONS RED at 23 s and their verdict 5–7 min later, after the
#: self-tests. A stopped self-test run loses nothing: every self-test that finished has recorded its PASS, which the
#: re-gate reuses; the rest run then.
#: ⛔ SELF-TESTS is EVERY script self-test, found by discovery (scripts/self_tests.py; kb/Work PB2563) — never a self-test
#: named here by hand: this tuple once named five while eleven orchestrator self-tests ran in no gate at all. The few
#: that read the BUILT assemblies run beside the test hosts of a gate that builds (`self_tests.py --built`,
#: `Host.built_self_tests`). Both halves run ONLY WHEN THE CHANGE TOUCHES A SCRIPT (`TOOLING_ONLY`, `Lane.tooling`; a
#: compiler data file a self-test reads sets only `Lane.reads_tooling`, CI's flag, owner 2026-11-01, kb/Work PB4409).
AUDITS = (
    ("CITATIONS", ["scripts/spec/audit_code_citations.py", "--check"]),
    ("DOC CITATIONS", ["scripts/spec/audit_doc_citations.py", "--check"]),
    ("EVIDENCE SUPERSESSION", ["scripts/spec/audit_evidence_supersession.py", "--check"]),
    ("RENDERED VIEWS", ["scripts/rendered_views.py", "--check"]),
    ("WITNESS LOSS", ["scripts/spec/audit_witness_loss.py", "--check"]),
    # kb/Work PB4886, PB5256: every HELD edition's catalog (inventory_schema.HELD, the schema's list, so a newly held
    # edition needs no line here) is this run's, and a reconciled edition's is complete (no finding), so a repair of a
    # transcription that is not regenerated cannot leave a stale denominator; an unreconciled edition's findings are its
    # catalog's, and a change of them is stale too.
    ("RULE CATALOGS", ["scripts/spec/extract_rule_catalog.py", "--held", "--check"]),
    # kb/Work PB5256: every older held edition's candidate pairing with its next edition is this run's, so the
    # adjudication of its change records starts from the catalogs as they are.
    ("LINEAGE CANDIDATES", ["scripts/spec/lineage.py", "--candidates", "--check"]),
    # kb/Work PB5589: every held edition with a lineage-screen trigger table has its screen current (the pending-clause
    # diff, the definitions, each trigger's flags) and the regression fixture's caught DIFFERENT pairs are all flagged.
    # What the table owes, and each stored record a flag now asks a new question (kb/Work PB5597), is printed, never red.
    ("LINEAGE SCREEN", ["scripts/spec/lineage_screen.py", "--check"]),
    # kb/Work PB5597: every read record of an older edition's rules interprets (the one normalization), every stored
    # change record whose source names one is that read's decision (a SAME its refuter upheld), and every stored KEPT
    # record of an edition that closes on own-edition evidence (2002) still has a suite witness that runs there.
    ("LINEAGE READS", ["scripts/spec/lineage_reads.py", "--check"]),
    # kb/Work PB689, PB5256: the independent count critic, every held ISO edition: each rule block holds exactly the
    # ordinals its transcription prints, but where the catalog records a finding that explains the difference.
    ("CATALOG COVERAGE", ["scripts/spec/audit_catalog_coverage.py", "--held", "--check"]),
    # kb/Work PB4992: each held edition's inventory is a VIEW (its catalog joined with its store of adjudications), and
    # what the view refuses (an adjudication of a dropped rule, a copied catalog field or state, an adjudication decided
    # against other text) and an older edition's illegal change record are refused here, in every lane.
    ("INVENTORY VIEW", ["scripts/spec/inventory_view.py", "--check"]),
    # kb/Work PB4996: every edition gate of the compiler (a construct catalogue window, a grammar version predicate) rests on
    # an edge the rule lineage proves: a start on a NEW or CHANGED rule, an end on a REMOVED or CHANGED one.
    ("EDITION GATES", ["scripts/spec/edition_gates.py", "--check"]),
    ("SPEC CORRECTIONS", ["scripts/spec/verify_publishable.py"]),
    # kb/Work PB4850: the 1985 transcription is publishable and agrees with its reconciliation ledger (LEDGER-1985.json).
    ("SPEC CORRECTIONS 1985", ["scripts/spec/verify_publishable.py", "--edition", "1985"]),
    ("LINE ENDINGS", ["scripts/line_endings.py", "--check"]),
    # kb/Work PB735, PB2878: a byte-level fact about the committed tree, an audit so that no test reads every path.
    ("CONFLICT MARKERS", ["scripts/conflict_markers.py", "--check"]),
    # kb/Work PB5396, PB3694: the public-API baselines list no symbol twice (RS0025 fails the Release build on one) and every
    # Unshipped one is in ordinal order, the one order that lets plain git merge two branches' new symbols; the module's `--write`
    # is the fixer. A byte-level fact about the committed tree, an audit like CONFLICT MARKERS.
    ("PUBLIC API BASELINES", ["scripts/public_api_baselines.py", "--check"]),
    # kb/Work PB4453: no compiler test reads a Markdown file (owner 2026-11-02), so what a document says about the compiler
    # or about another document is checked HERE, in every lane: the documentation audits (scripts/doc_audits.py), and the two
    # register audits that were Unit tests running these scripts to read docs/CONFORMANCE.md.
    ("DOC AUDITS", ["scripts/doc_audits.py", "--check"]),
    ("ARCH SUBSYSTEMS", ["scripts/arch/r2_subsystems.py", "--check"]),
    ("ANNEX A.1 REGISTER", ["scripts/spec/audit_annex_a1.py", "--check"]),
    ("DERIVATION REGISTER", ["scripts/spec/audit_derivations.py", "--check"]),
    # kb/Work PB5052: every 1985 figure verdict names the block of the transcription its page and ordinal say; it reads no
    # PDF, so the gate runs it where `sweep_figures.py --edition 1985 --check` (which measures the scan) cannot.
    ("FIGURE VERDICTS 1985", ["scripts/spec/sweep_figures.py", "--edition", "1985", "--identity"]),
    # kb/Work PB2878, PB4395: THE lanes' classification against the tree (every entry names a tracked path; every script
    # the build or a compiler test is seen to read is CODE_READS), a question over the committed tree, so an audit.
    ("INERT LANE", ["scripts/inert_lane.py", "check"]),
    # kb/Work PB5218: every repository read a test assembly's sources spell lies in its target's declared inputs
    # (scripts/build_targets.json), so a target found up to date at its key cannot have read a path its key left out.
    ("BUILD TARGETS", ["scripts/buildgraph.py", "check"]),
    # kb/Work PB3022, PB4395: the gate has ONE shape, so no live line names a mode; a question over the committed tree.
    ("ONE SHAPE", ["scripts/gate.py", "--audit-shape"]),
    # kb/Work PB3575: every diagnostic code is COBOL####; the rename's own rules, asked as a question over the committed
    # tree, an audit for the reason CONFLICT MARKERS is one (PB2878).
    ("DIAGNOSTIC PREFIX", ["scripts/rename_diagnostic_prefix.py", "--check"]),
    # kb/Work PB3837: the code carries the product's name; the rename's own audit over the committed tree, for the same reason.
    ("RETIRED NAMES", ["scripts/rename_retired_names.py", "--check"]),
    ("SELF-TESTS", ["scripts/self_tests.py"]),
    # kb/Work PB1957 row 44, the CI invariant: CI's `audits` job ran these two and the gate did not, so train 1067's
    # PR 9 went RED in CI on a `blocked: true` the work register refuses, behind a GREEN gate.
    ("WORK REGISTER", ["scripts/spec/work.py", "check"]),
    ("FLEET PRACTICES", [".claude/skills/workstream/check_practices.py"]),
    # DESIGN-conformance-suite.md §9 (kb/Work PB3174): the conformance suite's generator refuses a malformed test
    # (its header, its rules, its control, its expected outputs, the portability lint) reading only the tree, so a
    # refused test is red here in a fraction of a second, long before the test hosts end.
    ("CONFORMANCE SUITE", ["conformance-suite/harness/generate.py", "--check"]),
)


#: ⛔ THE AUDITS THAT RUN ONLY WHEN THE CHANGE TOUCHES A SCRIPT (kb/Work PB4326, PB4409; scripts/lane_classification.py
#: `Lane.tooling`): a script, a workflow, a tool, an agent definition, a root manifest the self-tests read or any
#: .py/.ps1/.sh file. A compiler data file a self-test reads (lane_classification.py's TOOLING_READS) starts none here (owner
#: 2026-11-01: "I see no reason why a compiler change should need to run script self-tests"); its first run is CI's.
#: The rest of the population is CI's: the merge group's `audits` job runs every Linux self-test whatever the change (a
#: declared population reader and the DEVLOG/kb corpora are the named residue, lane_classification.py) and `windows-self-tests`
#: the Windows ones whenever the change touches a script or that data (`Lane.reads_tooling`). The recorded-inputs
#: reuse (PB2914) still lets a script change re-run exactly the self-tests that read it.
TOOLING_ONLY = ("SELF-TESTS",)


#: ⛔ THE AUDITS THAT READ ONLY WHAT THE CHANGE CHANGES (kb/Work PB5225): each is handed the change's cut point and its
#: measured tree (`--base <base> --tree <tree>`), the pair the lane is classified from. LINE ENDINGS read all 35,241
#: tracked files in every gate (13.2 s) although a file already on main was read when it landed; the full sweep is CI's
#: Linux `audits` job's, once per merge group, the backstop for a commit made outside git's attributes. A gate that has
#: no base or no measured tree (the full lane's fallback, `Host.lane`) runs them over everything.
CHANGE_SCOPED = ("LINE ENDINGS",)


#: ⛔ WHAT AN AUDIT NEEDS IN THE TREE, made ready before any audit runs, exactly as CI's `audits` job makes it ready:
#: FLEET PRACTICES reads the pinned public skills submodule, which a fresh worktree does not have. A prerequisite that
#: fails is a RED and no audit runs. The self-test maps every command of CI's `audits` job to an AUDITS entry, to one of
#: these, or to its recorded reason for running elsewhere (`ci_audits_not_in_gate`), so the next audit CI gains reaches the gate.
AUDIT_PREREQUISITES = (
    ("PUBLIC SKILLS SUBMODULE", ["git", "submodule", "update", "--init", "tools/claude-skills"]),
)


#: ⛔ A CI AUDIT THAT IS NOT A TREE AUDIT, AND WHERE IT RUNS INSTEAD (kb/Work PB1957 row 44, the CI invariant): train 1067's
#: PR 9 went RED in CI's `audits` job on `work.py check` behind a GREEN gate, because CI ran it and the gate did not and
#: nothing compared the two lists.
CI_AUDITS_ELSEWHERE = {
    "scripts/devlog.py": "a LANDING check, not a tree audit: an implementer's entry stays DEVLOG/pending-*.md until "
                         "enqueue.py stamps it, and `devlog.py check` refuses a pending entry, so it runs where every "
                         "landing passes (`enqueue.py checks`, exit 6) and in CI on a pull request or merge group",
}


#: CI's audits job runs at least this many `python3 <script>` commands, and makes this submodule ready: a reading that
#: finds fewer, or not it, read nothing.
CI_AUDITS_FLOOR, CI_AUDITS_SUBMODULE = 10, "tools/claude-skills"


CI_PYTHON_RE = re.compile(r"\bpython3?\s+(\S+\.py)\b")


CI_SUBMODULE_RE = re.compile(r"\bgit submodule update --init (\S+)")


def ci_audits_not_in_gate(workflow: str, audits=AUDITS, prerequisites=AUDIT_PREREQUISITES,
                          elsewhere: dict[str, str] = CI_AUDITS_ELSEWHERE, floor: int = CI_AUDITS_FLOOR,
                          submodule: str | None = CI_AUDITS_SUBMODULE) -> list[str]:
    """THE CI INVARIANT FOR THE AUDITS (kb/Work PB1957 row 44): every `python3 <script>` a code line of CI's `audits` job
    runs is a script of `audits` or recorded in `elsewhere`, every `git submodule update --init <path>` it runs is one of
    `prerequisites`, and every `elsewhere` entry is still run by the job. A reading under `floor` scripts, or one that
    does not see the job make `submodule` ready, read nothing, and is a finding."""
    from ci_workflow import job  # noqa: PLC0415 — this directory's: the one reader of a CI workflow's shape
    lines = [x.strip() for x in job(workflow, "audits").code_lines]
    scripts = list(dict.fromkeys(m.group(1) for x in lines for m in CI_PYTHON_RE.finditer(x)))
    submodules = list(dict.fromkeys(m.group(1) for x in lines for m in CI_SUBMODULE_RE.finditer(x)))
    gated = {c for _, cmd in audits for c in cmd[:1]}
    prepared = {cmd[-1] for _, cmd in prerequisites if cmd[:4] == ["git", "submodule", "update", "--init"]}
    bad = [f"CI's audits job runs only {len(scripts)} `python3 <script>` commands: the reading is broken"] \
        if len(scripts) < floor else []
    if submodule and submodule not in submodules:
        bad.append(f"CI's audits job no longer makes {submodule} ready: the reading is broken")
    bad += [f"CI's audits job runs {s} and the gate's AUDITS do not" for s in scripts if s not in gated | set(elsewhere)]
    bad += [f"CI's audits job makes the submodule {p} ready and the gate's AUDIT_PREREQUISITES do not"
            for p in submodules if p not in prepared]
    bad += [f"{s} is recorded as running elsewhere (CI_AUDITS_ELSEWHERE) but CI's audits job no longer runs it"
            for s in elsewhere if s not in scripts]
    return bad


#: ⛔ EVERY GATE REUSES A SELF-TEST'S RECORDED PASS (kb/Work PB2914, generalized to the one gate by PB3022;
#: DESIGN-test-build-ci.md §3.14.10, §3.17.3): the gate's audits add this to SELF-TESTS, so a self-test whose inputs
#: are unchanged since a recorded PASS is REUSED, not run, and one that does run records its PASS for the next gate.
#: CI's `audits` job, the Linux gate and a hand run never pass it: they run every self-test.
SELF_TESTS_REUSE = "--reuse"


#: The post-build half of the self-tests: the ones declaring `SELF-TEST-NEEDS: build` (scripts/self_tests.py), run only
#: when the change touches tooling (`TOOLING_ONLY`'s rule) by a gate that builds.
BUILT_SELF_TESTS = ["scripts/self_tests.py", "--built"]


#: ⛔ ONE SHAPE (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; DESIGN-test-build-ci.md §3.17.3): the gate takes no mode,
#: so no brief, skill, script, doc or test may tell anyone to run one: a caller left naming the deleted lander mode would
#: fail at the parameter binder, and one naming the implementer mode would teach a distinction that no longer exists.
#: History (the DEVLOG, the kb/Work notes, frozen evidence) keeps its words. The `--audit-shape` audit (AUDITS' ONE SHAPE)
#: reads the tracked files of these trees and the root's Markdown files; until kb/Work PB4395 it was a compiler test
#: (GateLegDriftTests) that walked them all, which made every script, skill and workflow a path a compiler test reads.
SHAPE_MODE_RE = re.compile(r"(?:-Mode|--mode)\s+(?:" + "implementer" + "|" + "lander" + r")\b")


SHAPE_TREES = (".claude/", ".github/", "scripts/", "docs/", "tests/")


SHAPE_EXTENSIONS = (".md", ".py", ".ps1", ".sh", ".js", ".cs", ".yml", ".json")


SHAPE_SKIPPED = ("bin", "obj", "TestResults", "claude-skills", "evidence", "Work", "worktrees")


def audit_shape(root: Path, paths: list[str]) -> list[str]:
    """ONE SHAPE's offenders among the tracked `paths` (`SHAPE_MODE_RE`): `path:line: text` for each line that names a
    gate mode, in the shape trees and the root's Markdown files, history and frozen evidence left out."""
    out: list[str] = []
    for rel in paths:
        parts = rel.split("/")
        in_tree = rel.startswith(SHAPE_TREES) and rel.endswith(SHAPE_EXTENSIONS)
        if not (in_tree and not any(p in SHAPE_SKIPPED for p in parts[:-1])) and not (len(parts) == 1 and rel.endswith(".md")):
            continue
        try:
            text = (root / rel).read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        out += [f"{rel}:{i}: {line.strip()}" for i, line in enumerate(text.splitlines(), 1) if SHAPE_MODE_RE.search(line)]
    return out


def audit_shape_main(root: Path = REPO) -> int:
    paths = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    tracked = [p for p in paths.decode("utf-8").split("\0") if p]
    if ".claude/skills/workstream/templates/MANDATORY-PRACTICES.md" not in tracked:
        print("=== ONE SHAPE: NOT RUN — the tracked tree has no MANDATORY-PRACTICES.md: the walk is broken ===")
        return 2
    bad = audit_shape(root, tracked)
    for b in bad:
        print(f"    {b}")
    print("=== ONE SHAPE: " + (f"RED — {len(bad)} line(s) tell someone to run a gate mode; the gate has ONE shape, "
                                "`pwsh scripts/build-local.ps1 -Priority BelowNormal` (kb/Work PB3022) ===" if bad
                                else "GREEN — no live line names a gate mode ==="))
    return 1 if bad else 0
