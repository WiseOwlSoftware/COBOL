"""THE PLANNABLE POPULATION (kb/Work PB3728; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.3): the work
notes (Note, note_of, load_notes, the harm weights), the lead notes that exist only on a report's own branch
(branch_lead_notes, kb/Work PB3150, PB3699), the notes an unlanded branch closes (closed_by_branch, add_closed_notes,
kb/Work PB3473), the fix lane's clusters (fix_clusters.py over the notes that wait on nothing: run_fix_clusters,
declare_fix_lane), a campaign's clusters on the sites its notes declare (campaign_clusters, kb/Work PB2120, PB3242) and
the tiers that order them (lift_tiers, kb/Work PB3423), and a cluster's claim (cluster_claim, set_claim).

Its readers: the wave planner (plan_wave.py), the ledger's estimate (scripts/spec/ledger_plan.py: the same
run_fix_clusters call, kb/Work PB2912), the branch survey's train order (prune_worktrees.note_rank: harm_weights) and
the landing lane's conflict brief (land_lane._note_line: note_of). In the import DAG of section 16.10
(scripts/script_layering.py) it imports reports and inflight.
"""
from __future__ import annotations

import dataclasses
import importlib.util
import json
import pathlib
import re
import shutil
import subprocess
import sys
from typing import Any, Callable, Mapping

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "spec"))
import coord  # noqa: E402
import work  # noqa: E402  (cluster_order: the ONE reader of cluster: and blocked_by:)
from work import parse_frontmatter  # noqa: E402  (the ONE frontmatter reader, with its C# parity twin)
from reports import FINISH_STATUSES, Report  # noqa: E402
from inflight import LANDED_CLASSES, Claim, changed_files, collision_set  # noqa: E402

REPO = coord.REPO
FIX_CLUSTERS = REPO / "tools" / "claude-skills" / "skills" / "agent-fleet" / "references" / "fix_clusters.py"

FIX_CLUSTERS = REPO / "tools" / "claude-skills" / "skills" / "agent-fleet" / "references" / "fix_clusters.py"


@dataclasses.dataclass
class Note:
    id: str
    status: str
    area: str
    title: str
    rows: list[str]
    body: str
    harm: int = 0


def harm_weights() -> dict[str, int]:
    """Each harm flag's weight, from `.agent-fleet.json` `harm` (`wrong_answer=8,crashes=4,...`): the one reading, used
    by the planner's ranks and by the branch survey's train order (prune_worktrees.py note_rank, kb/Work PB2981)."""
    fleet = json.loads((REPO / ".agent-fleet.json").read_text(encoding="utf-8"))
    return {k: int(v) for k, v in (p.split("=") for p in fleet["harm"].split(","))}


def note_of(text: str, stem: str, harm_weights: dict[str, int]) -> Note | None:
    """One note from its file's text (the one frontmatter reader), or None when it has no frontmatter."""
    fm = parse_frontmatter(text)
    if not fm:
        return None
    rows = fm.get("inventory_rows") or []
    return Note(str(fm.get("id") or stem), str(fm.get("status", "")), str(fm.get("area", "")), str(fm.get("title", "")),
                list(rows) if isinstance(rows, list) else [], str(fm.get("_body", "")),
                sum(w for k, w in harm_weights.items() if fm.get(k) is True))


def load_notes(work_dir: pathlib.Path, harm_weights: dict[str, int]) -> dict[str, Note]:
    notes = {}
    for p in sorted(work_dir.glob("PB*.md")):
        text = p.read_text(encoding="utf-8", errors="replace")
        if n := note_of(text, p.stem, harm_weights):
            notes[n.id] = n
        elif work.NOTE_FILE.match(p.name):   # a damaged note leaves the plan as silently as it left the register (PB3654)
            raise SystemExit(f"⛔ {p.name}: unreadable as a work note — {work.unreadable_why(text)}: no plan is made on a "
                             f"register missing a note (python scripts/spec/work.py check)")
    return notes


def branch_lead_notes(reports: list[Report], notes: dict[str, Note], harm_weights: dict[str, int],
                      git: Callable[..., Any]) -> dict[str, Note]:
    """The lead notes of FINDINGS and unfinished (SPLIT, NOT STARTED) reports that main does not have, read from each
    report's own branch: an owner task files its note on its own branch (memory: owner-task-branch-needs-a-note), and
    the planner reads main's register, so the review finisher of `license-product-name` (PB3146) was never planned
    (kb/Work PB3150), nor was the finisher of a SPLIT branch that filed its lead the same way (kb/Work PB3699)."""
    out: dict[str, Note] = {}
    for r in reports:
        if (r.lead in notes or r.lead in out or not r.branch
                or not ((r.review and r.review.verdict == "FINDINGS") or r.status in FINISH_STATUSES)):
            continue
        shown = git("show", f"{r.branch}:kb/Work/{r.lead}.md")
        if shown.returncode == 0 and (n := note_of(shown.stdout, r.lead, harm_weights)):
            out[r.lead] = n
    return out


BRANCH_NOTE_FILE = re.compile(r"kb/Work/(PB\d+)\.md")


def closed_by_branch(branch: str, git: Callable[..., Any]) -> list[str]:
    """The notes a branch CLOSES: every `kb/Work/PB*.md` it changed since it left origin/main whose version ON THE BRANCH
    has a terminal status (work.TERMINAL_STATUSES). The register rule is that a fix flips its note's status in the commit
    that lands it, so this is the mechanical record of what the work finished: a group's implementer routinely closes a
    note beyond the ones it was dispatched with (`PB3383 (+ PB925)`), and neither the report's file name nor the dispatch
    ledger names it (kb/Work PB3473, branch review R1-1)."""
    out = []
    for path in sorted(changed_files(git, f"origin/main...{branch}", "--", "kb/Work")):
        if not (m := BRANCH_NOTE_FILE.fullmatch(path)):
            continue
        shown = git("show", f"{branch}:{path}")
        if shown.returncode == 0 and (n := note_of(shown.stdout, m.group(1), {})) and n.status in work.TERMINAL_STATUSES:
            out.append(m.group(1))
    return sorted(out, key=work.id_order)


def add_closed_notes(reports: list[Report], classify: Callable[[str], str], git: Callable[..., Any],
                     is_ancestor: Callable[[str, str], bool]) -> None:
    """Complete the `notes` of every report whose branch is not on main with the notes the branch closes
    (closed_by_branch), so a DONE branch waiting to land holds every note it finished in `awaiting landing`, not only
    its lead and its dispatch group's (answered_notes), and a SPLIT or FINDINGS branch's finisher, which takes that
    branch's work, is planned for them too: left out, each would be planned again as a fresh cluster group that redoes
    the work the finisher cherry-picks (branch review R2-1). With answered_notes, the only readers of "the notes a
    report answers" (kb/Work PB3473).

    A note that a CONTAINED branch closes belongs to that branch, never to the branch that carries it. A same-file
    successor merges its predecessor's head, so the predecessor's terminal flip is in the successor's diff from main
    too. When the note was credited to the successor's report (the newest), that report superseded the predecessor's
    FINDINGS report. The planner then read the note as `awaiting landing` with the successor and never planned the
    predecessor's review finisher, while the survey held the successor for those same findings. Wave 1143 planned
    nothing that way (PB3489 behind fix-pb3504, kb/Work PB3508). The contained branch's own report decides the note,
    as plan()'s carrier rule already says (`branch_carrier`). `is_ancestor` is the local branches' graph's
    (local_branches), never a `merge-base` per pair (kb/Work PB3582)."""
    unlanded = list(dict.fromkeys(r.branch for r in reports if r.branch and classify(r.branch) not in LANDED_CLASSES))
    closed = {b: closed_by_branch(b, git) for b in unlanded}
    for r in reports:
        if r.branch not in closed:
            continue
        carried = {n for c in unlanded if c != r.branch and is_ancestor(c, r.branch) for n in closed[c]}
        r.notes = list(dict.fromkeys([*r.notes, *(n for n in closed[r.branch] if n not in carried)]))


def ready_notes_view(status: str, view: pathlib.Path, work_dir: pathlib.Path | None = None) -> pathlib.Path:
    """The notes of `status` that wait on nothing (work.waiting_on), copied into the empty directory `view` for
    fix_clusters.py to read (kb/Work PB3389). fix_clusters skips a note by a flag of its stored frontmatter
    (`.agent-fleet.json` `skip_flag`), and whether a note is blocked is derived, never stored, so the fix lane's
    population is the register's own answer and the public script reads only what is ready."""
    work_dir = work_dir or work.WORK
    items = work.load(work_dir)
    by_id = {i.get("id"): i for i in items}
    if view.exists():
        shutil.rmtree(view)
    view.mkdir(parents=True)
    for it in items:
        if it.get("status") == status and not work.waiting_on(it, by_id):
            shutil.copyfile(work_dir / it["_file"], view / it["_file"])
    return view


def run_fix_clusters(status: str, out: pathlib.Path, rules: dict[str, Any]) -> dict[str, Any]:
    """The fix lane's groups: fix_clusters.py --json over .agent-fleet.json's population (`kind`, `skip_flag`) of the
    notes that wait on nothing (ready_notes_view), at the planner's group cap, written to `out` and returned whole
    (`clusters`, `unsited`). The ONE call, shared by this planner and the ledger's estimate
    (scripts/spec/ledger_plan.py, kb/Work PB2912), so both size the lane alike."""
    if not FIX_CLUSTERS.exists():
        raise SystemExit(f"⛔ {FIX_CLUSTERS} is missing: run `git submodule update --init tools/claude-skills`")
    view = ready_notes_view(status, out.with_name(f"{out.stem}-notes"))
    subprocess.run([sys.executable, str(FIX_CLUSTERS), "--notes", str(view), "--max", str(rules["wave"]["max_notes_per_group"]),
                    "--open-status", status, "--json", str(out)], cwd=REPO, check=True, capture_output=True)
    return json.loads(out.read_text(encoding="utf-8"))


def declare_fix_lane(clusters: list[dict[str, Any]], declared: Callable[[str], Claim], work_dir: pathlib.Path) -> None:
    """Give each fix-lane cluster its collision set (kb/Work PB3242, PB3670): fix_clusters.py's `files` are the sites
    its scored reading of the notes' prose found (every path and Type.Member a note mentions), which rank and group the
    lane's notes; the partition checks the notes' DECLARED sets plus the cluster's primary file instead
    (collision_set), so a note that discusses a file it does not change never holds a group back."""
    for c in clusters:
        texts = [(work_dir / f"{n['id']}.md") for n in c["notes"]]
        set_claim(c, collision_set(c["file"], [declared(f.read_text(encoding="utf-8", errors="replace"))
                                               for f in texts if f.is_file()]))


def cluster_claim(c: Mapping[str, Any]) -> Claim:
    """A cluster's (or a groups-file entry's) claim: its `files` list and its `members` {file: [names]}."""
    return Claim.of(c.get("files", []), c.get("members"))


def set_claim(c: dict[str, Any], claim: Claim) -> None:
    """Write a claim into a cluster as its `files` list and `members` {file: [names]} (JSON: the campaign's clusters
    file)."""
    c["files"], c["members"] = sorted(claim.files), claim.member_lists()


def load_fix_clusters() -> Any:
    """The public submodule's fix_clusters.py as a module: a campaign reuses its site index and clustering (never a
    fork of them) over a wider tree than .agent-fleet.json gives the fix lane."""
    if not FIX_CLUSTERS.exists():
        raise SystemExit(f"⛔ {FIX_CLUSTERS} is missing: run `git submodule update --init tools/claude-skills`")
    spec = importlib.util.spec_from_file_location("fix_clusters", FIX_CLUSTERS)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def campaign_clusters(view: dict[str, Any], notes: dict[str, Note], texts: dict[str, str], rules: dict[str, Any],
                      fc: Any, repo: pathlib.Path, declared: Callable[[str], set[str]]
                      ) -> tuple[list[dict], list[dict], dict[str, list[str]], dict[str, str]]:
    """A campaign's plannable notes as fix_clusters-shaped clusters: `(open, half, deps, waiting)`.

    `view` is work.cluster_order(): every open note of the cluster in tier, then blocked_by order, each with its
    effective tier. A note is PLANNABLE when it is ready, or when every note it waits on is plannable (then plan() runs
    it after them, `deps`); every other note is
    `waiting`, with the reason. Plannable notes are clustered on their DECLARED sites (kb/Work PB3242: the files of
    their site blocks, work.note_sites, each at fix_clusters' path weight, through fix_clusters' `cluster`), never on
    what their prose mentions (57 of PB1754's 63 ready notes DISCUSS plan_wave.py and landing_check.py, and the prose
    scan planned zero groups), one topological depth at a time so a cluster never mixes a blocker with what it blocks;
    a note declaring no site file is a group of its own, ordered by its blocked_by chain. A cluster's `files`, the set
    the partition checks, is collision_set: its notes' declared sets (`declared(text)`, declared_sets: the sites plus
    the member index's callers of what each note moves or changes; PB2118 Draft 8, J2), which never steer the
    clustering itself, plus its primary file. The clusters holding one dependent's blockers merge when they share a
    depth and fit the cap, so the dependent can follow one successor chain. `texts` is {note: its file text}."""
    cap = rules["wave"]["max_notes_per_group"]
    plannable: dict[str, dict] = {}
    waiting: dict[str, str] = {}
    for r in view["notes"]:  # topological order: every blocker is decided before what it blocks
        if r["id"] not in notes or r["id"] not in texts:
            waiting[r["id"]] = "not a kb/Work/PB*.md note: the wave planner dispatches PB notes only"
        elif all(w in plannable for w in r["waiting_on"]):
            plannable[r["id"]] = r
        else:
            waiting[r["id"]] = "waiting on " + ", ".join(w for w in r["waiting_on"] if w not in plannable)
    by_status: dict[str, dict[int, list[dict]]] = {"open": {}, "half": {}}
    for nid, r in plannable.items():
        entry = {"id": nid, "area": r["area"], "harm": notes[nid].harm, "title": r["title"][:140], "tier": r["tier"],
                 "sites": {p: 3 for p in work.note_sites(texts[nid], repo=repo).paths if (repo / p).is_file()},
                 "fileset": declared(texts[nid])}
        by_status["half" if r["status"] == "half" else "open"].setdefault(r["depth"], []).append(entry)
    out: dict[str, list[dict]] = {"open": [], "half": []}
    for key, by_depth in by_status.items():
        for d in sorted(by_depth):
            ns = by_depth[d]
            cl = fc.cluster([n for n in ns if n["sites"]], cap)
            cl += [{"file": "", "harm": n["harm"], "files": [], "notes": [n]} for n in ns if not n["sites"]]
            for c in cl:
                set_claim(c, collision_set(c["file"], [n["fileset"] for n in c["notes"]]))
                c["depth"] = d
                c["tier"] = min((n["tier"] for n in c["notes"]), key=work.tier_rank)
                c["notes"] = [{k: n[k] for k in ("id", "area", "harm", "title")} for n in c["notes"]]
            out[key] += cl
    deps = {nid: list(r["waiting_on"]) for nid, r in plannable.items() if r["waiting_on"]}
    # A successor follows ONE chain, so a note whose blockers fell into two clusters of one depth would wait a whole
    # wave. Merge those clusters while they fit the cap: wave 1025 was planned so by hand (PB2108 + PB2109 in one
    # group, PB2110 after it).
    for nid in deps:  # topological order
        holders = [c for c in out["open"] if any(n["id"] in deps[nid] for n in c["notes"])]
        if len(holders) < 2 or len({c["depth"] for c in holders}) > 1 or sum(len(c["notes"]) for c in holders) > cap:
            continue
        head = holders[0]
        for c in holders[1:]:
            head["notes"] += c["notes"]
            head["harm"] += c["harm"]
            set_claim(head, cluster_claim(head) | cluster_claim(c))
            head["file"] = head["file"] or c["file"]
            head["tier"] = min(head["tier"], c["tier"], key=work.tier_rank)
            out["open"].remove(c)
    lift_tiers(out["open"], deps)
    return out["open"], out["half"], deps, waiting


def lift_tiers(clusters: list[dict], deps: dict[str, list[str]]) -> None:
    """Lower each cluster's `tier` to the lowest tier of every cluster that waits on it, in place (kb/Work PB3423).

    plan() ranks the tier first and letters a dependent only after its blockers' groups, so a blocker's cluster must
    never rank after its dependent's. A NOTE's tier already satisfies that (work.cluster_order's effective tier), but a
    cluster takes its LOWEST note's, so it can outrank the cluster holding the blocker of one of its OTHER notes (a
    tier-1 note beside an untiered dependent whose blocker sits in an untiered cluster). `deps` is {note: its in-wave
    blockers}; a pass only ever lowers a tier, so the loop ends."""
    home = {n["id"]: c for c in clusters for n in c["notes"]}
    lowered = True
    while lowered:
        lowered = False
        for nid, blockers in deps.items():
            for b in blockers:
                if nid in home and b in home and work.tier_rank(home[nid]["tier"]) < work.tier_rank(home[b]["tier"]):
                    home[b]["tier"], lowered = home[nid]["tier"], True
