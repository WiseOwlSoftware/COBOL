#!/usr/bin/env python3
"""ci_plan.py — THE MERGE GROUP'S PLAN: each build target's key at the merged tree on each leg of the test matrix, the
passes the store ref holds for them, and the generated targets each leg makes once and hands its shards (kb/Work PB5235,
stage 6 of PB5207; PB5231; DESIGN-test-build-ci.md §3.14.14).

    python scripts/ci_plan.py plan --remote origin [--tree T] [--github-output F] [--summary F]   # a merge group
    python scripts/ci_plan.py plan --no-records [...]                                             # any other event
    python scripts/ci_plan.py generate --leg L --expect JSON [--github-output F]   # a leg's generated targets, once
    python scripts/ci_plan.py list --targets JSON --configuration C --generated DIR --out-dir D   # the populations
    python scripts/ci_plan.py handed --targets JSON --generated DIR --digests JSON --results DIR   # the hosts ran them
    python scripts/ci_plan.py --self-test

A LEG (`CI_LEGS`) is one platform and configuration the merge group tests on. For each leg, the plan computes every
target of scripts/build_targets.json's key at the merged tree with `buildgraph.keys` (the function the local gate uses),
in the graph's topological order, under the leg's facets: its platform, its configuration, and the toolchain
toolchain.env names for its OS, exactly as the gate on that OS names it. A dependent's key takes its generated
dependency's OUTPUT DIGEST (kb/Work PB5231): a leg that ACCEPTS RECORDS takes the digest the store ref records at that
dependency's key, and a test target's pass the ref holds at its key whose facets are the leg's
(`buildgraph.Published.lookup`, the store's one rule), and does not run that target; a key that cannot be resolved (a
leg that accepts no record, a generator with no record at its key) runs its target. Every other target runs.

A leg's GENERATED targets are made ONCE, by its `<leg>-generate` job (`generate`: the same traced
`buildgraph.bring_up_to_date` the gate runs, of the generated targets the leg's running targets depend on), and handed
to every shard: listed (`list`) and run with each one's variable naming its directory
(`buildgraph.Graph.handed`), so no host generates its own. A digest the plan took from the ref must be reproduced, or the
generation is `Nondeterministic`. After the hosts, `handed` checks what the gate's `_check_generated` checks: each output
still has its digest and no host recorded its own generation (`GeneratedTarget.recorded_own`).

The `changes` job of .github/workflows/build-and-test.yml runs `plan`, and its outputs (per leg, the configuration, the
JSON list of target assemblies to run and the generated targets to make) are the ONLY thing the generate and shard jobs
decide on: `ci_wiring` holds the workflow to that, for every target the targets file declares and every leg here.

The records travel on `buildgraph.STORE_REF` (buildgraph.py's transport: `publish`, `fetch_published`).
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import buildgraph  # noqa: E402  — the targets, their keys and the store's transport
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
#: Where a shard downloads its leg's generated outputs: `<HANDED>/generated-<leg>/<generated target>/`. The shard
#: downloads the leg's ONE artifact by NAME into `<HANDED>/generated-<leg>` (`Leg.handed_dir`), which
#: actions/download-artifact extracts directly into that path; a PATTERN download's layout depends on how many artifacts
#: it matched (one: extracted flat into the path; several: each in `<path>/<name>/`), so it never fetches a known one.
HANDED = "TestResults/handed"


# ── the legs ─────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class Leg:
    """One platform and configuration the merge group tests on. `accepts_records`: whether a pass recorded elsewhere at
    this leg's key stands for the leg's run; `why` says what the leg proves that no record can."""
    name: str
    platform: str
    configuration: str
    system: str          # toolchain.py's name for the OS (its `gate` scope's tools differ by OS: git-linux, git-windows)
    accepts_records: bool
    why: str

    @property
    def artifact(self) -> str:
        """The artifact the leg's `<leg>-generate` job uploads its generated outputs as."""
        return f"generated-{self.name}"

    @property
    def handed_dir(self) -> str:
        """The repository-relative directory a shard of this leg downloads its artifact into, by name."""
        return f"{HANDED}/{self.artifact}"

    @property
    def handed_root(self) -> str:
        """The workflow's spelling of the directory a shard of this leg downloads them into."""
        return f"${{{{ github.workspace }}}}/{self.handed_dir}"


#: ⛔ THE ONE STATEMENT OF WHAT EACH MERGE-GROUP LEG RUNS. The workflow's shard jobs read the configuration and the
#: target set from `plan`'s outputs (`ci_wiring` holds them to it). Windows runs Debug, the local gate's configuration,
#: so a pass the owner's machine recorded at the merged tree stands for it (owner 2026-11-10); Linux runs Release and
#: accepts no record: it is the merge group's own proof of Linux, of Release, of the inert watch and of INV-1-strong.
CI_LEGS = {
    "linux": Leg("linux", "linux", "Release", "Linux", False,
                 "Linux and Release are proven only here; the inert watch and INV-1-strong run only in this leg"),
    "windows": Leg("windows", "windows", "Debug", "Windows", True,
                   "the local gate's own facets (windows, Debug, toolchain.env's tools): a pass recorded at the merged "
                   "tree's key stands for this leg; with none, the leg runs"),
}


def leg_facets(leg: Leg, manifest: dict[str, str]) -> dict[str, str]:
    """The facets of `leg`'s keys: its platform and configuration and the toolchain toolchain.env names for its OS,
    exactly as the gate on that OS names it (`toolchain.summary(…, "gate", …)`, the gate's `Host.toolchain`)."""
    import toolchain  # noqa: PLC0415  — THE reader of toolchain.env
    return {"platform": leg.platform, "configuration": leg.configuration,
            "toolchain": toolchain.summary(manifest, "gate", leg.system)}


def _graph(repo: Path) -> buildgraph.Graph:
    return buildgraph.load_targets(repo / "scripts" / "build_targets.json")


def _manifest(repo: Path) -> dict[str, str]:
    import toolchain  # noqa: PLC0415
    return toolchain.load(repo / "toolchain.env")


# ── the plan ─────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class Planned:
    leg: str
    target: str
    kind: str                # buildgraph's KIND: "test" or "generated"
    assembly: str | None     # a test target's assembly
    key: str | None          # None: not resolvable on this leg (`why` says why); the target runs
    record: dict | None      # a test target: the accepted pass (None: the leg runs it). A generated target: the record
                             # whose output digest the leg's dependent keys took (None: none was taken)
    why: str = ""

    def line(self) -> str:
        where = f"{self.leg}/{self.target}" + (f" ({self.assembly})" if self.assembly else "")
        r = self.record
        if self.kind == buildgraph.GeneratedTarget.KIND:
            took = (f"its dependents are keyed on output {str(r.get('outputs'))[:12]} ({r.get('files')} file(s), "
                    f"recorded {r.get('when')}), which this leg's generation must reproduce") if r else \
                "no record taken: its dependents' keys are not resolved here"
            return f"plan: {where} (generated) key {self.key}: {took}"
        if self.key is None:
            return f"plan: {where}: RUN (its key is not resolved on this leg: {self.why})"
        if r is None:
            return f"plan: {where} key {self.key}: RUN (no pass at this key)"
        return (f"plan: {where} key {self.key}: PASSED, not run: the record of run {r.get('run')} at "
                f"{str(r.get('head'))[:12]} (tree {str(r.get('tree'))[:12]}, {r.get('when')}, counts "
                f"{json.dumps(r.get('counts'), sort_keys=True)})")


def plan(repo: Path, tree: str, records: buildgraph.Published, graph: buildgraph.Graph | None = None,
         manifest: dict[str, str] | None = None) -> list[Planned]:
    """THE MERGE GROUP'S PLAN: each target's key at `tree` on each leg, in topological order. A leg that accepts records
    keys a dependent on the output digest the ref records at its generated dependency's key, and takes a pass of a test
    target at its key whose facets are the leg's (toolchain.env's tools at `tree`); it runs every other test target. A
    leg that accepts none resolves no dependent's key, and runs every test target."""
    graph = graph if graph is not None else _graph(repo)
    manifest = manifest if manifest is not None else _manifest(repo)
    entries = buildgraph.tree_entries(repo, tree)
    out: list[Planned] = []
    for leg in CI_LEGS.values():
        facets = leg_facets(leg, manifest)
        held = records if leg.accepts_records else buildgraph.Published({}, None)
        refused: dict[str, str] = {}
        got = buildgraph.keys(graph, entries, facets, buildgraph.stored_outputs(held), refused)
        for name in graph.order:
            t = graph.targets[name]
            k = got.get(name)
            rec = held.lookup(name, k) if k else None
            if isinstance(t, buildgraph.TestTarget):
                if rec is not None and rec.get("facets") != facets:
                    rec = None
                out.append(Planned(leg.name, name, t.KIND, t.assembly, k, rec, refused.get(name, "")))
            else:
                out.append(Planned(leg.name, name, t.KIND, None, k, rec if rec and rec.get("outputs") else None,
                                   refused.get(name, "")))
    return out


def plan_outputs(rows: list[Planned], graph: buildgraph.Graph) -> dict[str, str]:
    """The `changes` job's outputs, per leg: its configuration; the JSON list of the assemblies it runs; and the JSON
    object of the generated targets those depend on (`<leg>_generate`: each one's name, and the output digest its
    dependents were keyed on, "" when none was), which the leg's `<leg>-generate` job makes; `{}` when none."""
    out: dict[str, str] = {}
    for leg in CI_LEGS.values():
        mine = [r for r in rows if r.leg == leg.name]
        runs = [r for r in mine if r.kind == buildgraph.TestTarget.KIND and r.record is None]
        needed = graph.needs_generated(r.target for r in runs)
        out[f"{leg.name}_configuration"] = leg.configuration
        out[f"{leg.name}_targets"] = json.dumps(sorted(r.assembly for r in runs))
        out[f"{leg.name}_generate"] = json.dumps(
            {r.target: (r.record or {}).get("outputs", "") for r in mine if r.target in needed}, sort_keys=True)
    return out


# ── the wiring ───────────────────────────────────────────────────────────────────────────────────────────────────

def ci_wiring(repo: Path) -> list[str]:
    """⛔ THE WORKFLOW RUNS WHAT THE PLAN SAYS, AND NOTHING DECIDES IT BESIDE THE PLAN (kb/Work PB5235): the drift test
    that makes a new target, generated target or leg automatic. For each leg of CI_LEGS: its `<leg>-generate` job runs
    only when the plan gives it a generated target, makes them with `generate` and uploads them as the leg's artifact;
    its `<leg>-shard` job needs it, runs only when the plan gives it a target (and no needed job failed); builds and
    tests in the plan's configuration; downloads the leg's generated outputs; lists its populations from the plan's set,
    handed them; runs a `dotnet test` step for every test target, each gated on that target's assembly being in the set
    and, for a target with generated dependencies, handed each one's variable naming its directory; checks after the
    hosts that they ran what they were handed (`handed`); and `ci-gate` checks the population of exactly that set.
    Returns the findings."""
    import ci_workflow  # noqa: PLC0415
    text = (repo / ".github" / "workflows" / "build-and-test.yml").read_text(encoding="utf-8").replace("\r\n", "\n")
    jobs = {j.id: j for j in ci_workflow.jobs(text)}
    graph = _graph(repo)
    tests = graph.tests()
    bad: list[str] = []
    changes = jobs.get("changes")
    if changes is None or not all(w in changes.code for w in (
            'if [ "$EVENT" = "merge_group" ]; then records=(--remote origin); else records=(--no-records); fi',
            'python3 scripts/ci_plan.py plan "${records[@]}"')):
        bad.append("the `changes` job no longer runs `ci_plan.py plan`, reading the store ref in a merge group only")
    gate = jobs.get("ci-gate")
    for leg in CI_LEGS.values():
        out = f"needs.changes.outputs.{leg.name}_targets"
        gen_out = f"needs.changes.outputs.{leg.name}_generate"
        conf = f"--configuration ${{{{ needs.changes.outputs.{leg.name}_configuration }}}}"
        for o in ("targets", "configuration", "generate"):
            line = f"{leg.name}_{o}: ${{{{ steps.plan.outputs.{leg.name}_{o} }}}}"
            if changes is not None and line not in changes.code:
                bad.append(f"the `changes` job does not output `{line}`")
        gen = jobs.get(f"{leg.name}-generate")
        if gen is None:
            bad.append(f"no `{leg.name}-generate` job for the {leg.name} leg of CI_LEGS")
        else:
            if f"{gen_out} != '{{}}'" not in (gen.value("if") or ""):
                bad.append(f"{gen.id}: its `if` does not skip the job when the plan gives it no generated target "
                           f"({gen_out} != '{{}}')")
            if f"scripts/ci_plan.py generate --leg {leg.name}" not in gen.code or gen_out not in gen.code:
                bad.append(f"{gen.id}: it does not make the plan's generated targets "
                           f"(`ci_plan.py generate --leg {leg.name}` from {gen_out})")
            if not any(s.input("name") == leg.artifact and s.input("path") == buildgraph.GENERATED_ROOT.as_posix() + "/"
                       for s in gen.steps):
                bad.append(f"{gen.id}: it does not upload {buildgraph.GENERATED_ROOT.as_posix()}/ as {leg.artifact}")
            if not re.search(r"\bdigests: \$\{\{ steps\.\w+\.outputs\.digests \}\}", gen.code):
                bad.append(f"{gen.id}: it does not output the generations' `digests`")
        job = jobs.get(f"{leg.name}-shard")
        if job is None:
            bad.append(f"no `{leg.name}-shard` job for the {leg.name} leg of CI_LEGS")
            continue
        cond = job.value("if") or ""
        if f"{out} != '[]'" not in cond or "!failure()" not in cond:
            bad.append(f"{job.id}: its `if` does not skip the job when the plan gives it no target ({out} != '[]') or a "
                       "needed job failed (!failure())")
        if f"{leg.name}-generate" not in (job.value("needs") or ""):
            bad.append(f"{job.id}: it does not need `{leg.name}-generate`, whose outputs it is handed")
        steps = job.steps
        builds = [s for s in steps if "dotnet build WiseOwl.Cobol.sln" in s.code]
        if not builds or any(conf not in s.code for s in builds):
            bad.append(f"{job.id}: the solution is not built in the plan's configuration ({conf})")
        if not any(s.input("name") == leg.artifact and s.input("path") == leg.handed_dir and s.input("pattern") is None
                   and gen_out in (s.value("if") or "") for s in steps):
            bad.append(f"{job.id}: it does not download {leg.artifact} by name into {leg.handed_dir} when the plan "
                       "generates for it (a pattern's layout depends on how many artifacts it matched)")
        lists = [s for s in steps if "scripts/ci_plan.py list" in s.code]
        if not lists or any(out not in s.code or conf not in s.code
                            or f'--generated "{leg.handed_root}"' not in s.code for s in lists):
            bad.append(f"{job.id}: the populations are not listed from the plan's set in its configuration, handed the "
                       f"leg's generated outputs (--generated \"{leg.handed_root}\")")
        test_steps = [s for s in steps if re.search(r"\bdotnet test tests/", s.code)]
        results_dirs: set[str] = set()
        for s in test_steps:
            m = re.search(r"\bdotnet test (tests/[^/\s]+/)", s.code)
            gated = re.search(rf"contains\(fromJSON\({re.escape(out)}\), '(\w+)'\)", s.value("if") or "")
            if conf not in s.code:
                bad.append(f"{job.id}: `{s.label}` does not test in the plan's configuration")
            if gated is None:
                bad.append(f"{job.id}: `{s.label}` runs whatever the plan says (no contains(fromJSON({out}), …))")
                continue
            owner = next((t for t in tests.values() if m and t.project == m.group(1) and t.assembly == gated.group(1)),
                         None)
            if m and owner is None:
                bad.append(f"{job.id}: `{s.label}` is gated on {gated.group(1)!r}, not on its own project's target")
            # ⛔ ONE HOST, ONE OBSERVATION LOG (buildgraph.observation_log; the bounce of #1530): a log the shard's hosts
            # share turns the Unit host's own generation of the suite into a Conformance red at `handed`.
            results = ci_workflow.RESULTS_DIRECTORY.search(s.code)
            log = next((m for m in map(ci_workflow.HARNESS_LOG.search, s.code.splitlines()) if m), None)
            if owner is not None and results is not None:
                results_dirs.add(results.group(1))
                want_log = buildgraph.observation_log(Path(results.group(1)), owner.assembly).as_posix()
                if log is None or log.group(1) != want_log:
                    bad.append(f"{job.id}: `{s.label}` does not write its harness log to {want_log}, its host's own "
                               f"(buildgraph.observation_log): another host's own generation would read as this one's")
            for var, directory in (graph.handed(owner.name, Path("@")) if owner else {}).items():
                want = f"{var}: {leg.handed_root}/{Path(directory).name}"
                if want not in s.code:
                    bad.append(f"{job.id}: `{s.label}` is not handed `{want}`: its host would generate its own")
        tested = {m.group(1) for s in test_steps if (m := re.search(r"\bdotnet test (tests/[^/\s]+/)", s.code))}
        for t in tests.values():
            if t.project not in tested:
                bad.append(f"{job.id}: no test step of {t.project} for target {t.name}: the leg would never run it")
        if len(results_dirs) != 1:
            bad.append(f"{job.id}: its test steps write into {sorted(results_dirs) or 'no'} results directory, not one, "
                       "so `ci_plan.py handed` cannot read each host's own log")
        if any(t.depends for t in tests.values()) and not any(
                "scripts/ci_plan.py handed" in s.code and f"needs.{leg.name}-generate.outputs.digests" in s.code
                and f'--generated "{leg.handed_root}"' in s.code
                and any(f"--results {d}" in s.code for d in results_dirs) for s in steps):
            bad.append(f"{job.id}: no step checks after the hosts that they ran the outputs they were handed "
                       f"(`ci_plan.py handed`, from needs.{leg.name}-generate.outputs.digests, reading the hosts' own "
                       f"logs in --results {' '.join(sorted(results_dirs))})")
        if gate is None or out not in gate.code:
            bad.append(f"ci-gate's population check does not read the {leg.name} leg's plan ({out})")
    return bad


# ── the plan step ────────────────────────────────────────────────────────────────────────────────────────────────

def read_records(repo: Path, remote: str, say=print) -> tuple[buildgraph.Published, str]:
    """THE STORE REF, READ LOUDLY (kb/Work PB5235 review R1-2): the records, and one line saying what was read. Every
    case in which the plan can accept nothing is a named `::warning::` in the job log, and every target then runs: the
    remote has no ref; the remote or the ref cannot be read; the ref names no commit (a blob, a tree); the commit holds no
    record. Each entry the commit holds that is no pass (`Published.refused`) is named too."""
    ref = f"{remote} {buildgraph.STORE_REF}"
    try:
        records = buildgraph.fetch_published(repo, remote)
    except subprocess.CalledProcessError as e:
        why = (e.stderr or b"").decode(errors="replace").strip() or f"git exit {e.returncode}"
        say(f"::warning::build store: {ref} could not be read ({why}); every target runs")
        return buildgraph.Published({}, None), f"{ref} could not be read"
    except buildgraph.StoreRefError as e:
        say(f"::warning::build store: {e}; every target runs")
        return buildgraph.Published({}, None), f"{ref} names no commit of records"
    if records.commit is None:
        say(f"::warning::build store: {remote} has no {buildgraph.STORE_REF} (no landing has published a pass); every "
            "target runs")
        return records, f"{remote} has no {buildgraph.STORE_REF}"
    for r in records.refused:
        say(f"::warning::build store: {ref} at {records.commit[:12]} holds {r}: ignored")
    if not records.records:
        say(f"::warning::build store: {ref} at {records.commit[:12]} holds no pass; every target runs")
    return records, (f"{ref} at {records.commit[:12]}, {len(records.records)} record(s)"
                     + (f", {len(records.refused)} refused" if records.refused else ""))


def _write_outputs(path: Path | None, outputs: dict[str, str]) -> None:
    nl = chr(10)
    if path is not None:
        with path.open("a", encoding="utf-8", newline=nl) as fh:
            fh.write("".join(f"{k}={v}{nl}" for k, v in outputs.items()))


def plan_main(repo: Path, tree: str, remote: str | None, github_output: Path | None, summary: Path | None,
              say=print) -> int:
    """`plan`: read the remote's store ref (a ref that cannot be read is said, and every target runs: a missing record
    costs minutes, never a verdict), print one line per leg and target, and write the outputs. `remote` None
    (`--no-records`: a dispatch run or a push to main, which run the whole matrix) reads no record."""
    if remote is None:
        records, source = buildgraph.Published({}, None), "not read (--no-records: only a merge group accepts a recorded pass)"
    else:
        records, source = read_records(repo, remote, say)
    graph = _graph(repo)
    rows = plan(repo, tree, records, graph)
    say(f"plan: the store: {source}")
    for r in rows:
        say(r.line())
    outputs = plan_outputs(rows, graph)
    for k, v in outputs.items():
        say(f"plan: {k}={v}")
    _write_outputs(github_output, outputs)
    nl = chr(10)
    if summary is not None:
        def cell(r: Planned) -> str:
            if r.kind == buildgraph.GeneratedTarget.KIND:
                return "generated" + (f": keyed on `{str(r.record.get('outputs'))[:12]}`" if r.record else "")
            if r.record is None:
                return "run"
            return f"passed: run {r.record.get('run')} at `{str(r.record.get('head'))[:12]}`"
        with summary.open("a", encoding="utf-8", newline=nl) as fh:
            fh.write(f"### plan (scripts/ci_plan.py; kb/Work PB5235){nl}{nl}The store: {source}{nl}{nl}"
                     f"| leg | target | key | |{nl}|---|---|---|---|{nl}"
                     + "".join(f"| {r.leg} | {r.target} | `{(r.key or 'unresolved')[:16]}` | {cell(r)} |{nl}"
                               for r in rows))
    return 0


# ── the generate step and the after-host check ───────────────────────────────────────────────────────────────────

def generate_main(repo: Path, leg_name: str, expect: str, github_output: Path | None, say=print,
                  graph: buildgraph.Graph | None = None, manifest: dict[str, str] | None = None) -> int:
    """`generate`: bring the generated targets the plan names for `leg_name` (`expect`: `<leg>_generate`, each name and
    the output digest its dependents were keyed on, or "") up to date at HEAD, ONCE for every shard of the leg: the
    gate's own `buildgraph.bring_up_to_date`, traced, into `GENERATED_ROOT/<target>`. A digest the plan took is recorded
    at its key in a private store first, so a generation that does not reproduce it is `Nondeterministic`. Writes the
    `digests` output (each target's output digest, which the shards' `handed` check compares). A BuildError is a named
    `::error::` and exit 1."""
    graph = graph if graph is not None else _graph(repo)
    facets = leg_facets(CI_LEGS[leg_name], manifest if manifest is not None else _manifest(repo))
    wanted: dict[str, str] = json.loads(expect)
    if unknown := sorted(set(wanted) - set(graph.generated())):
        say(f"::error::GENERATED TARGET RED — the plan names {', '.join(unknown)}, which the targets file does not "
            "declare as generated")
        return 1
    entries = buildgraph.tree_entries(repo, "HEAD^{tree}")
    with tempfile.TemporaryDirectory(prefix="ci-generate-") as tmp:
        store = buildgraph.Store(Path(tmp) / "store")
        taken = {n: d for n, d in wanted.items() if d}
        refused: dict[str, str] = {}
        keyed = buildgraph.keys(graph, entries, facets, taken, refused)
        for n, d in taken.items():
            if n in keyed:
                store.record(n, keyed[n], {"outputs": d, "when": "the plan (the store ref's record)"})
        try:
            done = buildgraph.bring_up_to_date(repo, graph, entries, facets, store, only=wanted)
        except buildgraph.BuildError as e:
            say(f"::error::GENERATED TARGET RED — {type(e).__name__}: {e}")
            return 1
    for g in done.values():
        say(f"generate: {leg_name}/{g.describe()} into {g.directory} (key {g.key[:12]}; {g.why})")
    _write_outputs(github_output, {"digests": json.dumps({n: g.outputs for n, g in done.items()}, sort_keys=True)})
    return 0


def list_main(repo: Path, targets: str, configuration: str, generated: Path, out_dir: Path, say=print,
              graph: buildgraph.Graph | None = None, lister=None) -> int:
    """`list`: THE POPULATIONS a leg's shards are checked against (`ci-gate`), listed once, from shard 1's build: for
    each assembly of `targets` (the plan's `<leg>_targets`), its project's scrubbed `--list-tests`
    (`test_population.list_population`) written to `<out_dir>/<assembly>.list.txt`, HANDED what its target depends on
    (`Graph.handed`, under `generated`), as the gate hands its listing, so discovery reads the suite the hosts run."""
    import test_population  # noqa: PLC0415
    graph = graph if graph is not None else _graph(repo)
    lister = lister or test_population.list_population
    by_asm = graph.by_assembly()
    out_dir.mkdir(parents=True, exist_ok=True)
    for asm in json.loads(targets):
        t = by_asm[asm]
        handed = graph.handed(t.name, generated.resolve())
        names, raw = lister(repo / t.project, configuration, cwd=repo, handed=handed)
        (out_dir / f"{asm}.list.txt").write_text(raw, encoding="utf-8", newline="\n")
        say(f"list: {sum(names.values()):,} cases listed for {asm} ({t.project}) -> {out_dir / f'{asm}.list.txt'}"
            + (f", handed {', '.join(f'{k}={v}' for k, v in handed.items())}" if handed else ""))
    return 0


def handed_main(repo: Path, targets: str, generated: Path, digests: str, results: Path, say=print,
                graph: buildgraph.Graph | None = None) -> int:
    """`handed`: THE HOSTS RAN THE OUTPUTS THEY WERE HANDED (gate.py `_check_generated`, in CI): for each assembly of
    `targets` the shard ran, each generated target its target depends on still has, in `generated/<target>`, the digest
    its generation wrote (`digests`), and THAT assembly's own harness observation log in the shard's results directory
    (`buildgraph.observation_log(results, asm)`) does not record that its host generated its own
    (`GeneratedTarget.recorded_own`). Another host's log is never read: the Unit host generates the suite it reads
    without depending on it, and one log shared by the shard's hosts turned that into a Conformance red (the bounce of
    #1530, runs 43130044782 and 43132487649). Each failure is a named `::error::`; exit 1 when any."""
    graph = graph if graph is not None else _graph(repo)
    made: dict[str, str] = json.loads(digests or "{}")
    by_asm = graph.by_assembly()
    bad: list[str] = []
    checked: list[str] = []
    for asm in json.loads(targets):
        t = by_asm.get(asm)
        for d in (t.depends if t is not None else ()):
            g = graph.targets[d]
            if not isinstance(g, buildgraph.GeneratedTarget):
                continue
            try:
                now = buildgraph.output_digest(generated / d, g)[0]
            except buildgraph.Unresolved as e:
                now = f"none ({e})"
            if now != made.get(d):
                bad.append(f"GENERATED OUTPUT CHANGED: {d}'s output in {generated / d} is {now[:60]} after the {asm} "
                           f"host, not the {str(made.get(d))[:12]} its generation wrote")
            if g.recorded_own(log := buildgraph.observation_log(results, asm)):
                bad.append(f"GENERATED BY THE HOST: the {asm} host generated its own {d} instead of the one handed it "
                           f"({log}), so it ran a suite its key does not name")
            checked.append(f"{asm} <- {d} {str(made.get(d))[:12]}")
    for b in bad:
        say(f"::error::{b}")
    say(f"=== HANDED OUTPUTS: {'RED' if bad else 'GREEN'} — {'; '.join(checked) or 'no target the shard ran is handed one'} ===")
    return 1 if bad else 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    isolate()
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    root = scratch_dir("ci-plan-selftest-")
    try:
        repo = ScratchRepo.init(root / "repo")
        repo.commit("base", {".gitignore": "TestResults/\n__pycache__/\n", "src/A.cs": "class A {}\n",
                             "conformance-suite/c.cob": "c\n", "docs/n.md": "n\n",
                             "gen/make.py": buildgraph.PLANTED_GENERATOR, "inv/r1.json": '{"id": "r1"}\n'})
        every = ("platform", "configuration", "toolchain")
        suite = buildgraph.GeneratedTarget(name="suite", inputs=("gen/", "inv/"), exclude=("@inert", "@tooling-lane"),
                                           facets=("toolchain",), command=("gen/make.py", "--out", buildgraph.OUT),
                                           outputs=("suite/manifest.json",), env="WISEOWL_COBOL_GATE_SUITE")
        graph = buildgraph.Graph.of({
            "suite": suite,
            "unit": buildgraph.TestTarget(name="unit", inputs=("src/",), exclude=(), facets=every, assembly="Unit",
                                          project="tests/U/"),
            "conformance": buildgraph.TestTarget(name="conformance", inputs=("src/", "conformance-suite/"), exclude=(),
                                                 facets=every, depends=("suite",), assembly="Conformance",
                                                 project="tests/C/")}, every)
        manifest = {"DOTNET_SDK_VERSION": "10.0.404", "PYTHON_VERSION": "3.15.2", "JDK_VERSION": "27.0.1",
                    "PWSH_VERSION": "7.7.0", "GIT_LINUX_VERSION": "2.56.0", "GIT_WINDOWS_VERSION": "2.56.0",
                    "GH_VERSION": "2.103.3", "JQ_VERSION": "1.8.2"}
        facets = leg_facets(CI_LEGS["windows"], manifest)
        tree = repo.git("rev-parse", "HEAD^{tree}")
        entries = buildgraph.tree_entries(repo.path, tree)
        digest = "d" * 64
        ks = buildgraph.keys(graph, entries, facets, {"suite": digest})
        gen_rec = {"target": "suite", "key": ks["suite"], "result": "pass", "outputs": digest, "files": 1,
                   "when": "2026-11-10T12:00:00Z"}
        rec = {"target": "conformance", "key": ks["conformance"], "result": "pass", "assembly": "Conformance",
               "facets": facets, "run": "000007", "head": "f" * 40, "tree": tree, "when": "2026-11-10T12:00:00Z",
               "counts": {"discovered": 9, "ran": 9, "skipped": 0, "carried": 0}}
        held = buildgraph.Published({("suite", ks["suite"]): gen_rec, ("conformance", ks["conformance"]): rec}, "c" * 40)
        suite_only = buildgraph.Published({("suite", ks["suite"]): gen_rec}, "c" * 40)

        def rows(records: buildgraph.Published, at: str = tree, m: dict[str, str] = manifest) -> dict:
            return {(r.leg, r.target): r for r in plan(repo.path, at, records, graph, m)}

        def outs(r: dict) -> dict[str, str]:
            return plan_outputs(list(r.values()), graph)

        same = rows(held)
        arm("the Windows leg keys Conformance on the generator's output digest the ref records at the generator's key, "
            "accepts the pass at that key and names the record, and runs the target with no pass; the Linux leg accepts "
            "no record, so Conformance's key is unresolved there and it runs; the outputs list what each leg runs and "
            "the generated targets it makes (none on Windows, whose Conformance passed)",
            same[("windows", "conformance")].record is not None and same[("windows", "unit")].record is None
            and same[("windows", "conformance")].key == ks["conformance"]
            and same[("windows", "suite")].record == gen_rec
            and same[("linux", "conformance")].record is None and same[("linux", "conformance")].key is None
            and "suite" in same[("linux", "conformance")].why
            and "PASSED, not run: the record of run 000007" in same[("windows", "conformance")].line()
            and outs(same) == {
                "linux_configuration": "Release", "linux_targets": '["Conformance", "Unit"]',
                "linux_generate": '{"suite": ""}',
                "windows_configuration": "Debug", "windows_targets": '["Unit"]', "windows_generate": "{}"},
            str([r.line() for r in same.values()]) + str(outs(same)))
        only_gen = rows(suite_only)
        repo.commit("an inventory edit", {"inv/r1.json": '{"id": "r1", "verdict": "OK"}\n'})
        inv = rows(held, repo.git("rev-parse", "HEAD^{tree}"))
        arm("with the generator's record and no Conformance pass, Windows runs Conformance and generates the suite, "
            "expecting the recorded digest; a change to the GENERATOR's inputs (inv/) moves its key off its record, so "
            "Conformance's key is unresolved and it runs, its generation expecting nothing",
            only_gen[("windows", "conformance")].record is None
            and outs(only_gen)["windows_generate"] == json.dumps({"suite": digest})
            and inv[("windows", "conformance")].key is None and inv[("windows", "conformance")].record is None
            and outs(inv)["windows_generate"] == '{"suite": ""}'
            and json.loads(outs(inv)["windows_targets"]) == ["Conformance", "Unit"],
            f"{outs(only_gen)} {inv[('windows', 'conformance')].line()} {outs(inv)}")
        repo.commit("edit", {"inv/r1.json": '{"id": "r1"}\n', "conformance-suite/c.cob": "c2\n"})
        moved = rows(held, repo.git("rev-parse", "HEAD^{tree}"))
        tools = rows(held, tree, {**manifest, "DOTNET_SDK_VERSION": "10.0.999"})
        repo.commit("docs only, against the recorded tree", {"conformance-suite/c.cob": "c\n", "docs/n.md": "n2\n"})
        docs_tree = repo.git("rev-parse", "HEAD^{tree}")
        docs = rows(held, docs_tree)
        arm("a record is accepted only at the key CI computes: a change to one of the target's own inputs (the merged "
            "tree moved), or another toolchain named by toolchain.env, runs the target; a tree that differs from the "
            "recorded one outside the target's inputs (docs/) still takes the pass",
            moved[("windows", "conformance")].record is None and tools[("windows", "conformance")].record is None
            and docs_tree != tree and docs[("windows", "conformance")].record is not None,
            f"{moved[('windows', 'conformance')].line()} {tools[('windows', 'conformance')].line()} "
            f"{docs[('windows', 'conformance')].line()}")
        wrong = rows(buildgraph.Published({("suite", ks["suite"]): gen_rec, ("conformance", ks["conformance"]):
                                           {**rec, "facets": {**facets, "configuration": "Release"}}}, "x"))
        forged = rows(buildgraph.Published({("suite", ks["suite"]): gen_rec,
                                            ("conformance", ks["conformance"]): {**rec, "key": ks["unit"]}}, "x"))
        arm("a pass whose facets are not the leg's, and a record filed under a key it does not name, are refused",
            wrong[("windows", "conformance")].record is None and forged[("windows", "conformance")].record is None)
        said: list[str] = []
        out = root / "out.txt"
        plan_main(REPO, "HEAD^{tree}", None, out, None, said.append)
        written = dict(line.split("=", 1) for line in out.read_text(encoding="utf-8").splitlines())
        real = _graph(REPO)
        arm("--no-records reads no ref and runs every test target of the real targets file on every leg, generating "
            "each generated target they depend on and expecting no digest, writing each leg's configuration, target "
            "list and generated targets as GitHub outputs",
            any("not read (--no-records" in s for s in said)
            and set(written) == {f"{leg}_{o}" for leg in CI_LEGS for o in ("configuration", "targets", "generate")}
            and all(json.loads(written[f"{leg}_targets"]) == sorted(t.assembly for t in real.tests().values())
                    and json.loads(written[f"{leg}_generate"]) == {n: "" for n in real.needs_generated(real.tests())}
                    and json.loads(written[f"{leg}_generate"])
                    for leg in CI_LEGS), f"{said} {written}")
        # the generate step and the after-host check, on the planted generator (traced, as the gate runs it)
        g_said: list[str] = []
        g_out = root / "gen-out.txt"
        first = generate_main(repo.path, "windows", '{"suite": ""}', g_out, g_said.append, graph, manifest)
        made = json.loads(dict(ln.split("=", 1) for ln in g_out.read_text(encoding="utf-8").splitlines())["digests"])
        again = generate_main(repo.path, "windows", json.dumps(made), None, g_said.append, graph, manifest)
        (repo.path / buildgraph.GENERATED_ROOT / "suite" / "suite" / "manifest.json").write_text("{}\n", encoding="utf-8", newline="\n")
        differs = generate_main(repo.path, "windows", json.dumps({"suite": "0" * 64}), None, g_said.append, graph, manifest)
        arm("`generate` makes the plan's generated targets once, traced, and outputs their digests; it reproduces a "
            "digest the plan took; a generation that does not reproduce the digest the plan keyed on is a named "
            "Nondeterministic red",
            first == 0 and again == 0 and set(made) == {"suite"} and len(made["suite"]) == 64
            and differs == 1 and any(s.startswith("::error::GENERATED TARGET RED — Nondeterministic") for s in g_said),
            f"{first} {again} {differs} {made} {g_said}")
        generate_main(repo.path, "windows", '{"suite": ""}', g_out, g_said.append, graph, manifest)
        made = json.loads(dict(ln.split("=", 1) for ln in
                               g_out.read_text(encoding="utf-8").splitlines()[-1:])["digests"])
        gen_root = repo.path / buildgraph.GENERATED_ROOT
        shard_results = root / "shard-results"
        shard_results.mkdir()
        conf_log, unit_log = (buildgraph.observation_log(shard_results, a) for a in ("Conformance", "Unit"))
        own_line = f"RAN  x\n{suite.own_mark} :: /tmp/s :: no WISEOWL_COBOL_GATE_SUITE was handed\n"
        conf_log.write_text("RAN  some test\n", encoding="utf-8", newline="\n")
        h_said: list[str] = []
        clean = handed_main(repo.path, '["Conformance", "Unit"]', gen_root, json.dumps(made), shard_results, h_said.append, graph)
        # The bounce of #1530: the Unit host, which reads the suite's manifest and depends on no generated target, was
        # handed nothing and generated its own; that line in ITS log is no Conformance red.
        unit_log.write_text(own_line, encoding="utf-8", newline="\n")
        unit_own = handed_main(repo.path, '["Conformance", "Unit"]', gen_root, json.dumps(made), shard_results, h_said.append,
                               graph)
        conf_log.write_text(own_line, encoding="utf-8", newline="\n")
        own = handed_main(repo.path, '["Conformance"]', gen_root, json.dumps(made), shard_results, h_said.append, graph)
        conf_log.write_text("", encoding="utf-8", newline="\n")
        (gen_root / "suite" / "suite" / "manifest.json").write_text("{}\n", encoding="utf-8", newline="\n")
        changed = handed_main(repo.path, '["Conformance"]', gen_root, json.dumps(made), shard_results, h_said.append, graph)
        unit_only = handed_main(repo.path, '["Unit"]', gen_root, json.dumps(made), shard_results, h_said.append, graph)
        arm("`handed`: a shard whose hosts ran the handed suite is GREEN, and so is one whose Unit host (no dependent) "
            "logged generating its own in its own log; the Conformance host's own log recording its own generation and "
            "an output changed after the hosts are each a named red; a shard that ran no dependent target checks nothing",
            clean == 0 and unit_own == 0 and own == 1 and changed == 1 and unit_only == 0
            and any("the Conformance host generated its own suite" in s and str(conf_log) in s for s in h_said)
            and any("GENERATED OUTPUT CHANGED" in s for s in h_said), str(h_said))
        listed: list[tuple[str, dict]] = []

        def fake_lister(project, configuration, cwd=None, handed=None):
            listed.append((Path(project).relative_to(repo.path).as_posix(), dict(handed or {})))
            return {"x": 1}, "The following Tests are available:\n    x\n"

        l_dir = root / "listings"
        list_main(repo.path, '["Conformance", "Unit"]', "Debug", gen_root, l_dir, lambda _s: None, graph, fake_lister)
        arm("`list` lists each assembly of the plan's set from its target's project, handing a dependent target's "
            "listing its generated outputs' directories and an independent one nothing",
            listed == [("tests/C", {"WISEOWL_COBOL_GATE_SUITE": str(gen_root.resolve() / "suite")}), ("tests/U", {})]
            and (l_dir / "Conformance.list.txt").is_file() and (l_dir / "Unit.list.txt").is_file(), str(listed))
        # the store ref, read loudly (review R1-2): each way it can hold nothing acceptable is a named warning
        host = ScratchRepo.init(root / "ref-host")
        host.commit("base", {"a.txt": "a\n"})
        remote = host.origin(root / "ref-remote.git")
        url = str(remote.path)

        def read(label: str) -> tuple[buildgraph.Published, list[str]]:
            heard: list[str] = []
            got, _ = read_records(host.path, url, heard.append)
            return got, heard

        def warned(heard: list[str], *words: str) -> bool:
            return any(h.startswith("::warning::build store:") and all(w in h for w in words) for h in heard)

        absent, h_absent = read("absent")
        bad_json = host.git("hash-object", "-w", "--stdin", input="not json")
        good = json.dumps({"target": "unit", "key": "7" * 64, "result": "pass"})
        good_blob = host.git("hash-object", "-w", "--stdin", input=good)
        forged_blob = host.git("hash-object", "-w", "--stdin", input=json.dumps({"target": "unit", "key": "8" * 64,
                                                                                    "result": "pass"}))

        def mktree(lines: list[str]) -> str:
            # bytes, as git reads them: text input would end each line CRLF on Windows, and the CR is part of the name
            return host.run("mktree", input="".join(ln + chr(10) for ln in lines).encode(), binary=True).stdout.decode().strip()

        inner = mktree([f"100644 blob {bad_json}{chr(9)}{'0' * 64}.json", f"100644 blob {good_blob}{chr(9)}{'7' * 64}.json",
                        f"100644 blob {forged_blob}{chr(9)}{'9' * 64}.json", f"100644 blob {good_blob}{chr(9)}notes.txt"])
        tree_id = mktree([f"040000 tree {inner}{chr(9)}unit"])
        commit = host.git("commit-tree", tree_id, "-m", "a store with three bad entries")
        host.git("push", "-q", "-f", url, f"{commit}:{buildgraph.STORE_REF}")
        mixed, h_mixed = read("mixed")
        empty_tree = mktree([])
        empty = host.git("commit-tree", empty_tree, "-m", "an empty store")
        host.git("push", "-q", "-f", url, f"{empty}:{buildgraph.STORE_REF}")
        nothing, h_nothing = read("empty")
        host.git("push", "-q", "-f", url, f"{tree_id}:{buildgraph.STORE_REF}")
        as_tree, h_tree = read("tree")
        host.git("push", "-q", "-f", url, f"{bad_json}:{buildgraph.STORE_REF}")
        as_blob, h_blob = read("blob")
        unreachable, h_unreachable = (lambda h: (read_records(host.path, str(root / "no-such-remote.git"), h.append)[0],
                                                 h))([])
        arm("the store ref is read loudly: an absent ref, a commit with no record, a ref naming a tree or a blob, and an "
            "unreachable remote each print a named ::warning:: and accept nothing; a record that is not JSON, one "
            "filed under a key it does not name and a path that is no record are each named and ignored, while the "
            "good record beside them is accepted",
            absent.records == {} and warned(h_absent, "has no", buildgraph.STORE_REF)
            and nothing.records == {} and warned(h_nothing, "holds no pass")
            and as_tree.records == {} and warned(h_tree, "names a tree", "not a commit")
            and as_blob.records == {} and warned(h_blob, "names a blob", "not a commit")
            and unreachable.records == {} and warned(h_unreachable, "could not be read")
            and set(mixed.records) == {("unit", "7" * 64)}
            and warned(h_mixed, f"unit/{'0' * 64}.json: not JSON")
            and warned(h_mixed, f"unit/{'9' * 64}.json: not a pass")
            and warned(h_mixed, "unit/notes.txt: not <target>"),
            f"{h_absent} {h_nothing} {h_tree} {h_blob} {h_unreachable} {h_mixed} {sorted(mixed.records)}")
        wiring = ci_wiring(REPO)
        arm("the real workflow runs each leg as CI_LEGS states it: its generated targets made once by its generate job "
            "and handed to every shard, its configuration and target set from the plan's outputs, a step per target "
            "assembly gated on that set, the after-host check, and ci-gate's population check over that set",
            wiring == [], "; ".join(wiring))
        wf = (REPO / ".github" / "workflows" / "build-and-test.yml").read_text(encoding="utf-8")

        def planted(name: str, text: str) -> list[str]:
            at = root / name
            (at / ".github" / "workflows").mkdir(parents=True)
            (at / "scripts").mkdir()
            (at / ".github" / "workflows" / "build-and-test.yml").write_text(text, encoding="utf-8", newline="\n")
            (at / "scripts" / "build_targets.json").write_text(
                (REPO / "scripts" / "build_targets.json").read_text(encoding="utf-8"), encoding="utf-8", newline="\n")
            return ci_wiring(at)

        ungated = wf.replace("        if: contains(fromJSON(needs.changes.outputs.windows_targets), 'Unit')\n", "", 1)
        found = planted("ungated", ungated)
        hand = f"          WISEOWL_COBOL_GATE_SUITE: {CI_LEGS['windows'].handed_root}/conformance-suite\n"
        head, sep, tail = wf.rpartition(hand)  # the last one is the Conformance step's: Unit is handed the suite too
        unhanded = head + tail if sep else wf
        found_h = planted("unhanded", unhanded)
        # merge group 43116013863: a PATTERN download of the one artifact extracts it flat into the path, not under
        # its name, so every host found no suite where it was handed one
        linux = CI_LEGS["linux"]
        by_name = f"          name: {linux.artifact}\n          path: {linux.handed_dir}\n"
        by_pattern = wf.replace(by_name, f"          pattern: {linux.artifact}\n          path: {HANDED}\n", 1)
        found_p = planted("by-pattern", by_pattern)
        # merge groups 43130044782 and 43132487649: one harness log for the shard's hosts, so the Unit host's own
        # generation of the suite read as the Conformance host's
        own_log = "TestResults/${{ runner.os }}-shard/Unit.observations.log"
        shared = wf.replace(own_log, "TestResults/${{ runner.os }}-shard/harness-observations.log", 1)
        found_s = planted("shared-log", shared)
        arm("the wiring check names a Windows Unit step that runs whatever the plan says, a Windows Conformance step "
            "not handed the leg's generated suite, a Linux shard that downloads its leg's artifact by a pattern, and a "
            "Linux Unit step writing the shard's one shared harness log",
            ungated != wf and any("windows-shard: `Unit, this shard` runs whatever the plan says" in f for f in found)
            and unhanded != wf and any("windows-shard: `Conformance, this shard` is not handed" in f for f in found_h)
            and by_pattern != wf and any(f"linux-shard: it does not download {linux.artifact} by name" in f
                                         for f in found_p)
            and shared != wf and any("linux-shard: `Unit, this shard` does not write its harness log to "
                                     f"{own_log}" in f for f in found_s),
            f"{found} {found_h} {found_p} {found_s}")
    finally:
        remove_tree(root)
    bad = [n for n, ok, _ in results if not ok]
    print(f"=== ci_plan SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results)} arms"
          + (f"; failed: {'; '.join(bad)}" if bad else "") + ") ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    p = sub.add_parser("plan", help="each target's key per leg, and what each leg runs and generates")
    p.add_argument("--tree", default="HEAD^{tree}")
    src = p.add_mutually_exclusive_group(required=True)
    src.add_argument("--remote", help="read the passes this remote's store ref holds (a merge group)")
    src.add_argument("--no-records", action="store_true", help="read none: every target runs (any other event)")
    p.add_argument("--github-output", type=Path, help="append the legs' outputs here ($GITHUB_OUTPUT)")
    p.add_argument("--summary", type=Path, help="append the plan as Markdown here ($GITHUB_STEP_SUMMARY)")
    g = sub.add_parser("generate", help="make a leg's generated targets once, for every shard of the leg")
    g.add_argument("--leg", required=True, choices=sorted(CI_LEGS))
    g.add_argument("--expect", required=True, help="the plan's <leg>_generate output")
    g.add_argument("--github-output", type=Path, help="append the `digests` output here ($GITHUB_OUTPUT)")
    ls = sub.add_parser("list", help="list the populations of the plan's assemblies, handed their generated outputs")
    ls.add_argument("--targets", required=True, help="the plan's <leg>_targets output")
    ls.add_argument("--configuration", required=True)
    ls.add_argument("--generated", required=True, type=Path, help="the directory holding each generated output")
    ls.add_argument("--out-dir", required=True, type=Path)
    h = sub.add_parser("handed", help="after the hosts: they ran the generated outputs they were handed")
    h.add_argument("--targets", required=True, help="the plan's <leg>_targets output")
    h.add_argument("--generated", required=True, type=Path, help="the directory holding each generated output")
    h.add_argument("--digests", required=True, help="the generate job's `digests` output")
    h.add_argument("--results", required=True, type=Path,
                   help="the shard's results directory, holding each host's <assembly>.observations.log")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.cmd == "plan":
        return plan_main(REPO, args.tree, None if args.no_records else args.remote, args.github_output, args.summary)
    if args.cmd == "generate":
        return generate_main(REPO, args.leg, args.expect, args.github_output)
    if args.cmd == "list":
        return list_main(REPO, args.targets, args.configuration, args.generated, args.out_dir)
    if args.cmd == "handed":
        return handed_main(REPO, args.targets, args.generated, args.digests, args.results)
    ap.error("name a command: plan, generate or handed (or --self-test)")
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
