"""A GROUP OF A WAVE (kb/Work PB3728; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.3): its shape (Group:
its kind, notes, primary file and claim, letter, successor link, tier), the model and the cost it is planned at
(choose_model, group_cost, landing_cost; model_rules.json), the instruction a finisher or a continuation is given
(finisher_pred, continuation_pred, branch_resume, replay_clause), the standing requirements of the campaigns its notes
belong to (group_requirements, kb/Work PB3359), and its rendering as a groups.json entry and a spec body (as_group_json,
body_of, sites_text, unpartitioned, title_of), the format make_dispatch_specs.py reads.

Its readers: the wave planner (plan_wave.py), check_practices.py (group_requirements, campaign_requirements) and the
ledger's estimate (scripts/spec/ledger_plan.py: the cost rule). In the import DAG of section 16.10
(scripts/script_layering.py) it imports reports, inflight and clusters.
"""
from __future__ import annotations

import dataclasses
import pathlib
import re
import sys
from typing import TYPE_CHECKING, Any, Mapping, Sequence

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
from reports import Report  # noqa: E402
from inflight import Claim  # noqa: E402
from clusters import Note, cluster_claim  # noqa: E402
if TYPE_CHECKING:
    from prune_worktrees import Replay  # noqa: E402  (imported lazily at run time: it imports the planner back)

def continuation_pred(reps: list[Report], shared: dict[str, list[str]]) -> str:
    """The pred of a group planned as the CONTINUATION of finished branches whose files it meets (kb/Work PB2807):
    merge each first (finisher_pred's merge rule: never a rebase, never a cherry-pick), so the two never meet again as
    a conflict at the merge queue, and orient from each report. `shared` is {branch: the files the group shares}."""
    return ("CONTINUATION (the file-set partition, kb/Work PB2807): " + "; ".join(
        f"{r.slug} ({r.lead}) finished on branch {r.branch} (worktree {r.worktree or 'removed'}, head {r.head or '?'}), "
        f"which changes {', '.join(shared.get(r.branch or '', [])[:3])}" for r in reps)
        + ". Before touching code, merge each of those branches into yours with `python scripts/merge_drivers.py merge <branch> --keep` (never a rebase, never a cherry-pick, never a bare `git merge`: the merge drivers of kb/Work PB3610 keep both sides of a keyed catalogue), "
          "read each report's \"For the next implementer\" section, and build on its work; a branch that has not "
          "landed by your landing lands with yours (MANDATORY-PRACTICES O2: a merged branch with no clean stamp at "
          "its tip lands through the branch that carries it; one reviewed CLEAN lands on its own first).")


@dataclasses.dataclass
class Group:
    kind: str                      # finisher | cluster
    notes: list[str]
    file: str                      # primary file ('' when unknown)
    files: list[str]
    pred: str = ""
    letter: str = ""
    after: str = ""
    model: str = ""
    why_model: str = ""
    cost: float = 0.0
    blocked_by: list[str] = dataclasses.field(default_factory=list)   # campaign: open blockers planned before it
    continues: list[str] = dataclasses.field(default_factory=list)    # the branches its pred merges or cherry-picks
    members: dict[str, list[str]] = dataclasses.field(default_factory=dict)   # {file: the members it changes there}
    tier: int | None = None        # campaign: the lowest tier of the clusters it holds (the placement order, PB3638)
    link: int = 0                  # its place in its successor chain: 0 heads it (wave.links_per_chain, PB3638)

    @property
    def claim(self) -> Claim:
        """The group's declared set (Claim): `files`, each claimed whole unless `members` names its members."""
        return Claim.of(self.files, self.members)

    def take(self, claim: Claim) -> None:
        """Unite `claim` into the group's own (a file stays whole when either side claims it whole)."""
        merged = self.claim | claim
        self.files, self.members = sorted(merged.files), merged.member_lists()


def group_of_cluster(kind: str, ids: list[str], c: Mapping[str, Any], **kw: Any) -> Group:
    """A group on cluster `c`'s primary file and its claim (cluster_claim), at the cluster's tier."""
    claim = cluster_claim(c)
    return Group(kind, ids, c["file"], sorted(claim.files), members=claim.member_lists(), tier=c.get("tier"), **kw)


def letters():
    n = 0
    while True:
        n += 1
        s, k = "", n
        while k:
            k, r = divmod(k - 1, 26)
            s = chr(65 + r) + s
        yield s


def attempts(nid: str, reports: list[Report]) -> int:
    return sum(1 for r in reports if nid in r.notes)


def choose_model(g: Group, notes: dict[str, Note], reports: list[Report], rules: dict[str, Any]) -> tuple[str, str]:
    rt = rules["routing"]
    ow = rt["opus_when"]
    for f in [g.file, *g.files]:
        for pat in ow["file_patterns"]:
            if f and pat in f:
                return "opus", f"design-heavy file {f} ({pat})"
    for nid in g.notes:
        n = notes[nid]
        for pat in ow["area_patterns"]:
            if re.search(pat, n.area, re.I):
                return "opus", f"{nid} area {n.area}"
        for pat in ow["body_patterns"]:
            if re.search(pat, n.body):
                return "opus", f"{nid}: an open design question ({pat})"
        if attempts(nid, reports) >= ow["failed_attempts_at_least"]:
            return "opus", f"{nid} already attempted {attempts(nid, reports)} times without landing"
    model = rt["default"]
    if model not in rt["allowed"]:
        raise SystemExit(f"⛔ model_rules.json routing.default {model!r} is not in routing.allowed")
    return model, "default"


def group_cost(model: str, n_notes: int, rules: dict[str, Any]) -> float:
    c, cal = rules["cost"], rules["calibration"]
    return (c["per_group_tokens"][model] + c["per_note_tokens"][model] * n_notes) / cal["tokens_per_point"][model]


def landing_cost(rules: dict[str, Any]) -> float:
    """The points one group's landing costs: the enqueue unit's model turns for its branch (kb/Work PB3023)."""
    c = rules["cost"]
    return c["landing_tokens"] / rules["calibration"]["tokens_per_point"][c["landing_model"]]


def replay_clause(replay: "Replay | None") -> str:
    """What a finisher is told when OTHER unlanded branches replay some of the branch's commits (kb/Work PB3170): take
    only the commits no outranking branch holds, because the branches that hold the rest are planned from and land them,
    and a second copy would land twice. Empty when nothing is replayed."""
    if not replay or not replay.by:
        return ""
    held = ", ".join(f"{b} ({n})" for b, n in replay.by)
    return (f" {len(replay.fresh)} of its commits are in no other branch ({', '.join(c[:9] for c in replay.fresh)}): "
            f"cherry-pick ONLY those; the others are replayed on {held} (patch-equivalent commits), which carry them to "
            f"main, and picking them again would land them twice.")


def branch_resume(branch: str, nid_list: list[str], replay: "Replay | None" = None) -> str:
    return (f"Branch {branch} carries unlanded work for {', '.join(nid_list)} with no report. Run "
            f"`python tools/claude-skills/skills/agent-fleet/references/status_delta.py <its worktree>` if the "
            f"worktree still exists, read its STATUS.md, cherry-pick its commits onto current main (never merge the "
            f"branch), resolve conflicts against the spec, re-gate, then finish the notes at their roots."
            + replay_clause(replay))


def finisher_pred(rep: Report | None, branch: str | None, cls: str, nid_list: list[str],
                  carried_by: str | None = None, replay: "Replay | None" = None) -> str:
    if rep is None:
        return "FINISH-FIRST: " + branch_resume(branch or "?", nid_list, replay)
    if rep.finished and rep.review and rep.review.verdict == "FINDINGS":
        return (f"FINISH-FIRST: the branch review of {rep.slug} ({rep.lead}) found {rep.review.findings} finding(s) at "
                f"head {rep.review.sha}: the records of kind `finding` in {rep.review.file} "
                f"(`python scripts/orchestrator/review_findings.py show {rep.review.file}`). Its branch {rep.branch} "
                f"(worktree {rep.worktree or 'removed'}) is {cls}: `python scripts/merge_drivers.py merge {rep.branch} --keep` into your branch"
                + (f" (it is also contained in {carried_by}, whose report does not answer these findings and which may "
                   f"land its commits first: merge {rep.branch} only, never {carried_by}; kb/Work PB3150)"
                   if carried_by else "")
                + f", read {rep.path} and its \"For the next implementer\" section, then answer EVERY finding by its "
                f"id: fix it at its root (a regression test or golden with it) or answer `no_change_needed` with the reason, one line "
                f"each in your report's `## Review findings` section; re-gate; your branch is reviewed again when it "
                f"returns DONE (kb/Work PB3027).")
    where = (f"Its branch {rep.branch} (worktree {rep.worktree or 'removed'}, head {rep.head or '?'}) is {cls}: "
             + (f"it is contained in {carried_by}, which carries its work to main: `python scripts/merge_drivers.py merge {carried_by} --keep` into your "
                f"branch (never cherry-pick {rep.branch}: its commits would land twice) and re-gate before new work."
                if carried_by else
                "cherry-pick its unlanded commits onto current main (never merge the branch) and re-gate before new work."
                + replay_clause(replay)
                if cls in ("UNLANDED", "CHECK") else
                "its landed part is already on main, so orient from the report only."))
    return (f"FINISH-FIRST: predecessor {rep.slug} ({rep.lead}) returned {rep.status or 'an unfinished report'} for "
            f"{', '.join(nid_list)}. Read {rep.path} first, its NEXT and its \"For the next implementer\" section, and "
            f"run `python tools/claude-skills/skills/agent-fleet/references/status_delta.py <its worktree>` when the "
            f"worktree still exists. {where}")


def title_of(g: Group) -> str:
    stem = pathlib.Path(g.file).name.split(".")[0] if g.file else ""
    words = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", stem).upper() if stem else "NOTES WITHOUT A COMMON FILE"
    return ("FINISH: " if g.kind == "finisher" else "") + words


#: The heading of the section of a CAMPAIGN LEAD note that holds the requirements every group of the campaign must meet
#: (kb/Work PB3359): the owner's standing instructions for the campaign, one bullet each. A prose section, not a
#: frontmatter list: a requirement is a sentence with commas, which the frontmatter list grammar (work._split_list,
#: with its C# parity twin) splits on.
REQUIREMENTS_HEADING = "## Standing requirements"


def campaign_requirements(note_text: str) -> list[str]:
    """The requirements a campaign lead note's REQUIREMENTS_HEADING section holds: its `- ` bullets, a bullet wrapped
    over several lines read as one sentence with its whitespace collapsed, in the order written. [] when the note has
    no such section. The section ends at the next heading."""
    out: list[str] = []
    inside = False
    for line in note_text.replace("\r\n", "\n").split("\n"):
        if line.startswith("#"):
            inside = line.strip() == REQUIREMENTS_HEADING
            continue
        if not inside:
            continue
        if line.startswith("- "):
            out.append(line[2:].strip())
        elif line.strip() and out and line.startswith(" "):
            out[-1] += " " + line.strip()
    return [" ".join(r.split()) for r in out]


def requirements_text(campaign: str, requirements: Sequence[str]) -> str:
    """One campaign's requirements as a group's spec body states them. The text of each requirement is carried
    verbatim, which is what check_practices.py looks for in every rendered spec."""
    return "".join(f" ⛔ OWNER REQUIREMENT (campaign {campaign}, standing; the branch reviewer raises a finding when "
                   f"your branch lacks it): {r}" for r in requirements)


def group_requirements(groups: Sequence[Group], cluster_of: Mapping[str, Any], notes_dir: pathlib.Path,
                       always: Sequence[str] = ()) -> list[dict[str, list[str]]]:
    """The standing requirements each group must meet, aligned with `groups`: `{campaign lead: requirements}`, {} for a
    group no campaign binds (kb/Work PB3359, review R1-1). Keyed to MEMBERSHIP, never to the planning mode: a group is
    bound by every campaign its notes' `cluster:` lists name (`cluster_of`: note id -> that list), so a fix-lane wave
    that plans a campaign member, or the finisher of a campaign branch, carries the requirement exactly as a
    `--cluster` wave does. `always` names campaigns that bind every group (the one a `--cluster` wave runs). A lead
    whose note has no REQUIREMENTS_HEADING section binds nothing."""
    read: dict[str, list[str]] = {}

    def of(lead: str) -> list[str]:
        if lead not in read:
            f = notes_dir / f"{lead}.md"
            read[lead] = campaign_requirements(f.read_text(encoding="utf-8")) if f.is_file() else []
        return read[lead]

    out = []
    for g in groups:
        leads = list(always)
        for nid in g.notes:
            cl = cluster_of.get(nid)
            leads += [c for c in cl if c not in leads] if isinstance(cl, list) else []
        out.append({lead: reqs for lead in dict.fromkeys(leads) if (reqs := of(lead))})
    return out


def unpartitioned(g: Group) -> str:
    """The plan table's and the spec's naming of a group whose set is EMPTY (kb/Work PB3263, PB3670): no silence, and
    no hold either way."""
    return ("⚠ UNPARTITIONED (kb/Work PB3263, PB3670): no note of this group declares a file the partition covers (a "
            "site block's path under a campaign root) and no primary file was located, so it holds nothing and nothing "
            "holds it: it runs beside every other group, and from its first edit its worktree's actual diff is in "
            "flight like any other.")


def sites_text(g: Group, limit: int = 8) -> str:
    """The group's sites as its spec names them: each file, with the members its notes claim there in parentheses (keep
    your edits inside them: another group may hold the file's other members, kb/Work PB3670)."""
    return ", ".join(f + (f" ({', '.join(g.members[f][:8])})" if f in g.members else "")
                     for f in g.files[:limit]) if g.files else "see the notes"


def body_of(g: Group, notes: dict[str, Note], requirements: Mapping[str, Sequence[str]] = {}) -> str:
    parts = []
    for nid in g.notes:
        n = notes[nid]
        title = re.sub(rf"^{nid}\s*[—-]\s*", "", n.title)[:220]
        parts.append(f"{nid} ({len(n.rows)} inventory rows claimed): {title}")
    sites = sites_text(g)
    tail = ("Re-probe each note on your build, fix each at its root, sweep the sibling arms, and write closes_rows for "
            "every row the landing really closes.")
    if g.model == "opus" and g.why_model != "default":
        tail += f" Routed to Opus: {g.why_model}; settle any open design question against its design doc first."
    if not g.files:
        tail += (f" {unpartitioned(g)} In your first checkpoint, give each note a `**Sites:**` paragraph naming the "
                 "files it changes and the members it changes in them (`path` (`Member`, ...)), so later plans "
                 "partition on them.")
    # A declared set is a PREDICTION (kb/Work PB3640: PB3628's block named land_lane.py and its change also rewrote
    # prune_worktrees.lifecycle, beside PB3275): the note's block must end the change naming what the change changed.
    tail += (" SITES FOLLOW THE DIFF (kb/Work PB3640): when your diff changes a code file, or a member in one, that "
             "no note's `**Sites:**` paragraph names, add it to the paragraph of the note it serves in the same "
             "change, so a successor, a finisher and the next plan partition on what you really changed.")
    if g.blocked_by:
        tail += (f" ⛔ DEPENDENCY SUCCESSOR (blocked_by, kb/Work PB2120): these notes are blocked by "
                 f"{', '.join(g.blocked_by)}, which group {g.after}'s chain does in this wave. Build on that work, never "
                 f"around it: merge the predecessor branch the wave names before touching code, and if it did not "
                 f"finish its blocker, stop and return BLOCKED naming it.")
    return ("NOTES: " + " · ".join(parts) + f" CODE SITES the notes name: {sites}. {tail}"
            + "".join(requirements_text(lead, reqs) for lead, reqs in requirements.items()))


def as_group_json(g: Group, wave: str, notes: dict[str, Note], codes: str,
                  requirements: Mapping[str, Sequence[str]] = {}) -> dict[str, Any]:
    lead = max(g.notes, key=lambda i: (len(notes[i].rows), -g.notes.index(i)))
    out = {"letter": g.letter, "model": g.model, "slug": f"w{wave}{g.letter.lower()}", "group": title_of(g),
           "notes": ", ".join(g.notes), "lead": lead, "codes": codes,
           "root": (g.file + (" (also " + ", ".join(f for f in g.files if f != g.file)[:300] + ")"
                              if len(g.files) > 1 else "")) if g.file else "the files the notes name",
           "files": ", ".join(f"`kb/Work/{i}.md`" for i in g.notes), "body": body_of(g, notes, requirements),
           # the standing requirements of the campaigns this group's notes belong to: {campaign lead: [requirement]}
           "requirements": {k: list(v) for k, v in requirements.items()},
           # the group's file set as a LIST, which make_dispatch_specs.py records in the dispatch ledger (kb/Work PB3186),
           # and the members it claims in them, {file: [names]} (a file absent is claimed whole; kb/Work PB3670)
           "file_set": sorted(g.files), "member_set": g.claim.member_lists()}
    if g.pred:
        out["pred"] = g.pred
    if g.after:
        out["after"] = g.after
    return out
