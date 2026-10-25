#!/usr/bin/env python3
"""THE ONE ROUTINE THAT BRINGS THE LOOP'S MAIN CHECKOUT UP TO DATE (kb/Work PB2699; DESIGN-orchestrator-loop.md section 4.10).

  adopt_main.py adopt   [--repo R] [--coord C] [--gh EXE] [--json]   fetch origin/main; fast-forward the checkout to it
  adopt_main.py require [--repo R] [--coord C]                       exit 2 when the checkout is behind the adopted commit

Everything the loop plans from (the register `kb/Work`, DEVLOG, `src/`, the planners and the unit prompts) is read from
ONE checkout, and a landing is pushed from a worktree, so that checkout falls one landing further behind origin/main
with every merge for as long as the loop runs: wave 1042 planned against a register seven commits old, wave 1079 ran a
planner that predated the planner on main. The owner (2026-10-24 ~10:58 PDT, kb/Work PB2699): "fix the loop so it picks
up landed changes itself. We should adopt efficiency improvements as soon as they are available and stable".

`adopt` is called by the supervisor (orchestrate.ps1) BETWEEN units, when no unit runs, and by autostart.ps1 before the
supervisor starts. It is NOT the only thing that moves the checkout: the attended session fast-forwards it by hand when
ledger_state.py says a ledger publish is owed (`git merge --ff-only origin/main`), and `adopt` then finds nothing left to
do. It adopts origin/main only when
  AVAILABLE  the checkout is on main, fetched origin/main is ahead of it (a fast-forward, never a merge or a reset) and
             nothing TRACKED but `.claude/settings.local.json` is modified (never a stash), and
  STABLE     CI's push run on main (`build-and-test.yml`) at that commit has SUCCEEDED. A red, pending or unrecorded run,
             or a CI state that could not be read, keeps the last adopted commit and says why: an unobserved state is
             never a pass.
and it records the adopted commit in `<coord>/adopted-main.json`, the floor `require` (the planner's guard) reads: a
checkout that does not contain the adopted commit is behind it, and a planner run from it names both shas and refuses
(plan_wave.py --allow-stale overrides). `supervisor_changed` is true when the fast-forward changed a file the running
supervisor holds in memory (SUPERVISOR_FILES): the supervisor then restarts itself in place before its next unit.
That flag does NOT depend on who moved the checkout. The supervisor passes `--loaded <sha>`, the checkout's HEAD when it
loaded its script, and `supervisor_changed` is whether SUPERVISOR_FILES differ between that commit and the checkout NOW,
in every outcome (adopted, current or kept): a fast-forward done by hand between two iterations is seen exactly as one
done by `adopt`. Every other file the loop uses (the Python tools, units/*.md, handoff.schema.json, steer.ps1) is re-read at each use, so a
fast-forward is all their adoption needs.
"""
from __future__ import annotations

import argparse
import dataclasses
import datetime as dt
import json
import pathlib
import shutil
import subprocess
import sys
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: What the running supervisor process holds in memory: PowerShell reads its script once. A change to one of these
#: needs a restart; every other loop file is read afresh at each use.
SUPERVISOR_FILES = ("scripts/orchestrator/orchestrate.ps1",)
CI_WORKFLOW = "build-and-test.yml"
BRANCH = "main"
REMOTE_REF = f"origin/{BRANCH}"
MARKER = "adopted-main.json"
SETTINGS = "settings.local.json"   # the always-dirty per-machine file (.claude/settings.local.json)
TIMEOUT_S = 120

ADOPTED, CURRENT, KEPT = "adopted", "current", "kept"

#: A gh runner: (repo, argv after `gh`) -> (exit code, stdout, stderr). The self-test passes a fake.
Gh = Callable[[pathlib.Path, list[str]], tuple[int, str, str]]


@dataclasses.dataclass(frozen=True)
class Adoption:
    action: str                      # ADOPTED | CURRENT | KEPT
    before: str                      # the checkout's HEAD before
    after: str                       # ... and after
    target: str                      # fetched origin/main ("" when it could not be read)
    reason: str                      # why KEPT, or what ADOPTED/CURRENT means
    supervisor_changed: bool = False
    changed: tuple[str, ...] = ()    # the SUPERVISOR_FILES the fast-forward changed

    @property
    def line(self) -> str:
        def short(sha: str) -> str:
            return sha[:9] or "?"
        moved = f"{short(self.before)}..{short(self.after)}" if self.action == ADOPTED else short(self.after)
        return (f"adopt main: {self.action} {moved} (origin/main {short(self.target)}): {self.reason}"
                f"{'; the supervisor itself changed since it loaded, so it restarts before the next unit' if self.supervisor_changed else ''}")


def _git(repo: pathlib.Path, *args: str) -> tuple[int, str]:
    try:
        p = subprocess.run(["git", *args], cwd=repo, capture_output=True, text=True, encoding="utf-8", errors="replace",
                           timeout=TIMEOUT_S)
    except (OSError, subprocess.TimeoutExpired) as e:
        return 128, f"git {args[0]}: {e}"
    return p.returncode, (p.stdout if p.returncode == 0 else p.stdout + p.stderr).strip()


def real_gh(repo: pathlib.Path, args: list[str], exe: str = "gh") -> tuple[int, str, str]:
    path = shutil.which(exe)
    if not path:
        return 127, "", f"the GitHub CLI ({exe}) is not on PATH"
    try:
        p = subprocess.run([path, *args], cwd=repo, capture_output=True, text=True, encoding="utf-8", errors="replace",
                           timeout=TIMEOUT_S)
    except (OSError, subprocess.TimeoutExpired) as e:
        return 124, "", f"`gh {' '.join(args[:2])}` gave no answer: {e}"
    return p.returncode, p.stdout, p.stderr


def ci_state(repo: pathlib.Path, sha: str, gh: Gh) -> tuple[str, str]:
    """CI's push run on main at `sha`: ("success" | "failure" | "pending" | "none" | "unobserved", detail). Any
    SUCCESSFUL run wins (a re-run of a red one); else any run still going is pending; else it is a failure."""
    rc, out, err = gh(repo, ["run", "list", "--workflow", CI_WORKFLOW, "--branch", BRANCH, "--event", "push",
                             "--commit", sha, "--limit", "20", "--json", "status,conclusion"])
    if rc != 0:
        return "unobserved", (err.strip() or f"gh exit {rc}").splitlines()[0]
    try:
        runs = json.loads(out)
        if not isinstance(runs, list):
            raise ValueError("not a list")
    except ValueError as e:
        return "unobserved", f"gh answered something that is not the run list ({e})"
    if not runs:
        return "none", f"CI has recorded no push run on main at {sha[:9]}"
    if any(r.get("status") == "completed" and r.get("conclusion") == "success" for r in runs):
        return "success", f"CI's push run on main at {sha[:9]} succeeded"
    if any(r.get("status") != "completed" for r in runs):
        return "pending", f"CI's push run on main at {sha[:9]} is still running"
    return "failure", f"CI's push run on main at {sha[:9]} did not succeed ({runs[0].get('conclusion')})"


def tracked_changes(repo: pathlib.Path) -> list[str]:
    """The tracked files modified in the checkout, but the per-machine settings file."""
    rc, out = _git(repo, "status", "--porcelain", "--untracked-files=no")
    if rc != 0:
        return [out]
    return [l for l in out.splitlines() if l.strip() and SETTINGS not in l]


def adopt(repo: pathlib.Path, cdir: pathlib.Path, gh: Gh | None = None, *, dry_run: bool = False, loaded: str = "") -> Adoption:
    """Fast-forward the checkout when it is available and stable, and say whether the supervisor must restart.
    `loaded` is the checkout's HEAD when the calling supervisor loaded its script ("" for a caller that holds none)."""
    r = _decide(repo, cdir, gh or real_gh, dry_run)
    changed = supervisor_files_changed(repo, loaded, r.after) if loaded else ()
    return dataclasses.replace(r, supervisor_changed=True, changed=changed) if changed else r


def supervisor_files_changed(repo: pathlib.Path, loaded: str, now: str) -> tuple[str, ...]:
    """The SUPERVISOR_FILES that differ between the commit the running supervisor loaded and the checkout's HEAD now,
    however the checkout got there. An unreadable commit (a pruned sha) answers nothing changed, never a restart loop."""
    if not now or loaded == now:
        return ()
    rc, out = _git(repo, "diff", "--name-only", loaded, now, "--", *SUPERVISOR_FILES)
    return tuple(f for f in out.splitlines() if f) if rc == 0 else ()


def _decide(repo: pathlib.Path, cdir: pathlib.Path, gh: Gh, dry_run: bool) -> Adoption:
    rc, head = _git(repo, "rev-parse", "HEAD")
    if rc != 0:
        return Adoption(KEPT, "", "", "", f"{repo} is not a git checkout ({head.splitlines()[0] if head else 'no answer'})")

    def kept(reason: str, target: str = "") -> Adoption:
        return Adoption(KEPT, head, head, target, reason)

    rc, out = _git(repo, "fetch", "--quiet", "origin", BRANCH)
    if rc != 0:
        return kept(f"origin/{BRANCH} could not be fetched ({out.splitlines()[0] if out else 'no answer'}); the last adopted commit stays")
    rc, target = _git(repo, "rev-parse", REMOTE_REF)
    if rc != 0:
        return kept(f"{REMOTE_REF} names no commit ({target.splitlines()[0] if target else 'no answer'})")
    if target == head:
        record(cdir, head, head)
        return Adoption(CURRENT, head, head, target, "the checkout is origin/main")
    if _git(repo, "merge-base", "--is-ancestor", target, head)[0] == 0:
        return kept("the checkout is ahead of origin/main (unpushed commits); nothing to adopt", target)
    if _git(repo, "merge-base", "--is-ancestor", head, target)[0] != 0:
        return kept("the checkout has diverged from origin/main (not a fast-forward); it needs an operator", target)
    rc, branch = _git(repo, "symbolic-ref", "--short", "-q", "HEAD")
    if rc != 0 or branch != BRANCH:
        return kept(f"the checkout is on {branch or 'a detached HEAD'}, not {BRANCH}", target)
    if dirty := tracked_changes(repo):
        return kept(f"{len(dirty)} tracked file(s) are modified in the checkout ({dirty[0].strip()}); never stashed or reset", target)
    state, why = ci_state(repo, target, gh)
    if state != "success":
        return kept(f"origin/main is not stable yet: {why}", target)
    if dry_run:
        return Adoption(KEPT, head, head, target, f"DRY RUN: would fast-forward to {target[:9]} ({why})")
    rc, out = _git(repo, "merge", "--ff-only", "--quiet", target)
    if rc != 0:
        return kept(f"git merge --ff-only {target[:9]} refused ({out.splitlines()[0] if out else 'no answer'})", target)
    record(cdir, target, head)
    return Adoption(ADOPTED, head, target, target, why)


def record(cdir: pathlib.Path, sha: str, previous: str) -> None:
    """The adopted commit: the floor the planner's guard (`require`) reads."""
    coord.write_json(cdir / MARKER, {"sha": sha, "previous": previous, "recorded_at": dt.datetime.now(dt.timezone.utc).isoformat()})


def adopted_sha(cdir: pathlib.Path) -> str:
    rec = coord.read_json(cdir / MARKER, {})
    return str(rec.get("sha", "")) if isinstance(rec, dict) else ""


def require_current(repo: pathlib.Path, cdir: pathlib.Path) -> str:
    """"" when the checkout contains the adopted commit (or none was ever adopted: no loop has run), else the refusal,
    naming both shas. The adopted commit is the floor, not origin/main: a red main is deliberately not adopted."""
    want = adopted_sha(cdir)
    if not want:
        return ""
    rc, head = _git(repo, "rev-parse", "HEAD")
    if rc == 0 and _git(repo, "merge-base", "--is-ancestor", want, head)[0] == 0:
        return ""
    return (f"this checkout ({head[:9] if rc == 0 else 'no HEAD'}) is behind the commit the loop adopted ({want[:9]}, "
            f"{cdir / MARKER}): it would plan from a register and run planner code older than the loop's. Bring it up to "
            f"date with `python scripts/orchestrator/adopt_main.py adopt` (or `git merge --ff-only {want[:9]}`), or "
            f"pass --allow-stale to plan from it knowingly.")


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("cmd", choices=("adopt", "require"))
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--gh", default="gh", help="the GitHub CLI executable (a test seam: the self-tests name a fake)")
    ap.add_argument("--dry-run", action="store_true", help="adopt: decide, but move nothing and record nothing")
    ap.add_argument("--loaded", default="", help="adopt: the checkout's HEAD when the calling supervisor loaded its script")
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args(argv)
    repo, cdir = pathlib.Path(a.repo), coord.coord_dir(a.coord)
    if a.cmd == "require":
        problem = require_current(repo, cdir)
        print(problem or f"checkout is at or past the adopted commit ({adopted_sha(cdir)[:9] or 'none adopted'})")
        return 2 if problem else 0
    r = adopt(repo, cdir, lambda rp, args: real_gh(rp, args, a.gh), dry_run=a.dry_run, loaded=a.loaded)
    print(json.dumps({**dataclasses.asdict(r), "line": r.line}) if a.json else r.line)
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
