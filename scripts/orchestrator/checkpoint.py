#!/usr/bin/env python3
"""Frequent handoffs: the supervisor's periodic checkpoint of a running unit, and the handoff it synthesizes when the unit
ends without writing one (docs/rearchitecture/DESIGN-orchestrator-loop.md section 5.1).

  checkpoint.py write --unit wave --session <id> --started <iso> --calls 40 --context 90000 --cost 3.1 --bg-tasks 1
      writes <coord>/checkpoint.json: the facts a successor needs if this unit dies this minute. Deterministic, no model:
      the unit's stream counters, every agent worktree's head, commits ahead of origin/main and uncommitted-file count,
      its STATUS.md headline, and the unit's own milestone lines (<coord>/milestones.jsonl).
  checkpoint.py synthesize --unit wave --out <handoff.json>
      the unit ended with no valid handoff (a crash, a kill, a terminated background task): write a schema-valid handoff
      from the last checkpoint and the CURRENT worktrees, outcome `split`, `next_unit: resume`, flagged `synthesized`.

A unit's model still writes the real handoff at the end; this exists so a death at minute 100 costs minutes, not the
unit (wave 1017 died with no handoff and its successor rebuilt everything from stderr and git).
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402

CHECKPOINT = "checkpoint.json"
MILESTONES = "milestones.jsonl"
SUMMARY_CAP = 900     # handoff.schema.json: summary is at most 900 characters


def git(cwd: pathlib.Path, *args: str) -> str | None:
    """git's stdout, or None when git failed. The directory goes through `git -C`, never `subprocess.run(cwd=)`: a worktree
    removed after it was listed (prune_worktrees.py --apply, a finished agent's cleanup) makes a subprocess `cwd` raise
    NotADirectoryError (WinError 267) or FileNotFoundError and abort the whole checkpoint write (kb/Work PB3250), where
    `git -C` on it merely fails (exit 128), which the caller reads as "gone"."""
    r = subprocess.run(["git", "-C", str(cwd), *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return r.stdout if r.returncode == 0 else None


def listed_worktrees(repo: pathlib.Path) -> list[dict]:
    """`git worktree list --porcelain`, one dict per worktree: its `worktree`, `HEAD` and `branch` lines keyed by name."""
    listing = git(repo, "worktree", "list", "--porcelain")
    if listing is None:
        # Not fatal: a unit that died still needs its handoff synthesized, and a checkpoint without the worktree view beats
        # none (the supervisor's self-tests run these commands against a directory that is no repository). Said, not hidden.
        print(f"checkpoint.py: git worktree list --porcelain failed in {repo}; no worktrees recorded", file=sys.stderr)
        return []
    out, cur = [], {}
    for line in listing.splitlines() + [""]:
        if not line.strip():
            if cur:
                out.append(cur)
            cur = {}
            continue
        key, _, val = line.partition(" ")
        cur[key] = val
    return out


def worktrees(repo: pathlib.Path) -> list[dict]:
    """Every linked worktree except the main checkout: path, branch, head, commits ahead of origin/main, dirty files.
    A worktree that vanishes between the listing and its measurement (git cannot read its status and its directory is
    gone) is no worktree any more and is left out; the rest of the checkpoint is still written (kb/Work PB3250). One
    whose directory exists but whose status git cannot read stays, its `uncommitted` None: unknown, never a clean 0."""
    main = str(repo.resolve()).replace("\\", "/").lower()
    result = []
    for w in listed_worktrees(repo):
        path = pathlib.Path(w.get("worktree", ""))
        if str(path.resolve()).replace("\\", "/").lower() == main or "branch" not in w:
            continue
        status_out = git(path, "status", "--porcelain")
        if status_out is None and not path.is_dir():
            continue
        ahead = (git(path, "rev-list", "--count", "origin/main..HEAD") or "").strip()
        headline = ""
        try:
            first = (path / "STATUS.md").read_text(encoding="utf-8", errors="replace").splitlines()[0:1]
            headline = first[0][:120] if first else ""
        except OSError:
            pass                                   # no STATUS.md (yet), or the worktree went away mid-read: no headline
        result.append({"worktree": str(path).replace("\\", "/"), "branch": w["branch"].removeprefix("refs/heads/"),
                       "head": w.get("HEAD", "")[:9], "ahead_of_main": int(ahead) if ahead.isdigit() else None,
                       "uncommitted": None if status_out is None else len(coord.porcelain_work(status_out.splitlines())),
                       "status_md": headline})
    return result


def milestones(cdir: pathlib.Path, last: int = 10) -> list[dict]:
    p = cdir / MILESTONES
    if not p.exists():
        return []
    rows = []
    for line in p.read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            rows.append(json.loads(line))
        except ValueError:
            rows.append({"what": line[:200]})     # a malformed line is still evidence; never dropped
    return rows[-last:]


def build(a: argparse.Namespace, cdir: pathlib.Path, repo: pathlib.Path) -> dict:
    return {
        "schema_version": 1,
        "written_at": dt.datetime.now(dt.timezone.utc).astimezone().isoformat(),
        "unit": a.unit, "session_id": a.session, "started_at": a.started,
        "calls": a.calls, "context_tokens": a.context, "cost_usd": a.cost,
        "background_tasks": a.bg_tasks, "last_event_at": a.last_event,
        "worktrees": worktrees(repo), "milestones": milestones(cdir),
    }


def synthesize(cdir: pathlib.Path, repo: pathlib.Path, unit: str) -> dict:
    cp = coord.read_json(cdir / CHECKPOINT, {})
    trees = worktrees(repo)
    pending = [{"branch": t["branch"], "worktree": t["worktree"], "status": "SPLIT",
                **({"report": t["worktree"] + "/STATUS.md"} if t["status_md"] else {})}
               for t in trees if t["ahead_of_main"] or t["uncommitted"]]
    ms = milestones(cdir, 5)
    said = "; ".join(str(m.get("what", m))[:90] for m in ms) or "no milestone lines"
    seen = f"last checkpoint {cp.get('written_at', 'none')} ({cp.get('calls', '?')} calls)" if cp else "no checkpoint"
    summary = (f"The {unit} unit ended with no valid handoff (crash, kill or terminated background task); synthesized by "
               f"the supervisor. {seen}. {len(pending)} worktree(s) hold work (see checkpoint.json). Last milestones: {said}")
    return {"schema_version": 1, "unit": unit, "outcome": "split", "summary": summary[:SUMMARY_CAP],
            "next_unit": "resume", "next_unit_reason": "the unit ended without a handoff; rebuild from the checkpoint",
            "branches_pending": pending, "workflow": {"state": "stopped", "scratch": str(cdir / "scratch").replace("\\", "/")},
            "synthesized": True, "checkpoint": str(cdir / CHECKPOINT).replace("\\", "/")}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("write", "synthesize"):
        s = sub.add_parser(name)
        s.add_argument("--unit", required=True)
        s.add_argument("--coord")
        s.add_argument("--repo", default=str(coord.REPO))
    w = sub.choices["write"]
    w.add_argument("--session", default="")
    w.add_argument("--started", default="")
    w.add_argument("--last-event", default="")
    w.add_argument("--calls", type=int, default=0)
    w.add_argument("--context", type=int, default=0)
    w.add_argument("--cost", type=float, default=0.0)
    w.add_argument("--bg-tasks", type=int, default=0)
    sub.choices["synthesize"].add_argument("--out", required=True)
    a = ap.parse_args(argv)
    cdir, repo = coord.coord_dir(a.coord), pathlib.Path(a.repo)
    if a.cmd == "write":
        coord.write_json(cdir / CHECKPOINT, build(a, cdir, repo))
    else:
        coord.write_json(pathlib.Path(a.out), synthesize(cdir, repo, a.unit))
    return 0


if __name__ == "__main__":
    sys.exit(main())
