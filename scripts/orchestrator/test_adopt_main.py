#!/usr/bin/env python3
"""Self-test for adopt_main.py (kb/Work PB2699): the loop adopts a landed main commit by itself, and only a stable one.

AVAILABLE: the checkout fast-forwards to a fetched origin/main that is ahead of it; it does not move when it is ahead,
diverged, off main, or has a tracked modification (the per-machine settings file excepted); an untracked file is no
obstacle. STABLE: only CI's SUCCESSFUL push run at that commit adopts it; a red, pending, unrecorded, unreadable or
garbled run keeps the checkout where it is, and so does a GitHub CLI that is not there. The supervisor's own script is
flagged when the fast-forward changes it (the restart trigger); another file is not. The adopted commit is recorded, and
`require` / the planner's guard refuse a checkout that does not contain it, naming both shas.
Run: python scripts/orchestrator/test_adopt_main.py   (a fabricated repo and its bare origin in a temp directory)."""
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import adopt_main as am  # noqa: E402
from scratch_repo import ScratchRepo, remove_tree, scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)
from standard_streams import ensure_utf8  # noqa: E402

ensure_utf8()

TMP = scratch_dir("adopt-main-test-")
REPO, COORD, ORIGIN, LANDER = TMP / "repo", TMP / "coord", TMP / "origin.git", TMP / "lander"
COORD.mkdir(parents=True)
fails = []


def check(name, got, want):
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def gh_says(runs, rc=0, err=""):
    """A fake GitHub CLI that records what it was asked and answers with `runs` (or a failure)."""
    asked = []

    def gh(repo, args):
        asked.append(args)
        return rc, runs if isinstance(runs, str) else json.dumps(runs), err
    gh.asked = asked
    return gh


GREEN = gh_says([{"status": "completed", "conclusion": "success"}])

scratch = ScratchRepo.init(REPO)
scratch.origin(ORIGIN)
scratch.write({".claude/settings.local.json": "{}\n"})
scratch.git("add", "-f", ".claude/settings.local.json")   # a machine's global ignore may name it; the real one is tracked
scratch.commit("base", {"scripts/orchestrator/orchestrate.ps1": "v1\n", "kb/Work/PB1.md": "a\n"})
scratch.git("push", "-q", "origin", "main")
lander = ScratchRepo.clone(ORIGIN, LANDER)


def land(msg, files):
    lander.commit(msg, files)
    lander.git("push", "-q", "origin", "main")
    return lander.git("rev-parse", "HEAD")


def head():
    return scratch.git("rev-parse", "HEAD")


# 1. nothing to adopt: current, and the commit is recorded as the floor
r = am.adopt(REPO, COORD, GREEN)
check("current when the checkout is origin/main", (r.action, r.after == head()), (am.CURRENT, True))
check("... and recorded", am.adopted_sha(COORD), head())
check("a current checkout asks CI nothing", GREEN.asked, [])

# 2. a landing: adopted once CI's push run at THAT commit succeeded
base = head()
new = land("a landing", {"kb/Work/PB2.md": "b\n"})
r = am.adopt(REPO, COORD, GREEN)
check("a stable landing is adopted (action, before, after)", (r.action, r.before, r.after), (am.ADOPTED, base, new))
check("the checkout moved", head(), new)
check("a landing that leaves the supervisor alone does not restart it", (r.supervisor_changed, r.changed), (False, ()))
check("the adopted commit is recorded", am.adopted_sha(COORD), new)
q = GREEN.asked[0]
check("CI is asked about the push run on main at the new commit",
      (q[:3], new in q, "--branch" in q and "main" in q, "push" in q, am.CI_WORKFLOW in q), (["run", "list", "--workflow"], True, True, True, True))

# 3. unstable: red, pending, unrecorded, unreadable, garbled, no CLI -> the checkout stays, the reason is named
base = head()
new = land("main goes red", {"kb/Work/PB3.md": "c\n"})
for name, gh, want in (
        ("a failed run", gh_says([{"status": "completed", "conclusion": "failure"}]), "did not succeed"),
        ("a run still going", gh_says([{"status": "in_progress", "conclusion": ""}]), "still running"),
        ("no run recorded", gh_says([]), "no push run"),
        ("a gh failure", gh_says("", rc=1, err="HTTP 502"), "HTTP 502"),
        ("an answer that is not the run list", gh_says("not json"), "not the run list"),
        ("an answer that is not a list", gh_says({"runs": []}), "not the run list")):
    r = am.adopt(REPO, COORD, gh)
    check(f"{name} keeps the checkout", (r.action, head()), (am.KEPT, base))
    check(f"{name} says why", want in r.reason, True)
check("the floor stays at the last ADOPTED commit", am.adopted_sha(COORD), base)
rerun = gh_says([{"status": "completed", "conclusion": "failure"}, {"status": "completed", "conclusion": "success"}])
r = am.adopt(REPO, COORD, rerun)
check("a red run followed by a green re-run is stable", (r.action, head()), (am.ADOPTED, new))

# 4. the supervisor's own script changed: flagged, so the supervisor restarts itself
loaded = head()   # the commit the running supervisor loaded its script at
new = land("a process fix", {"scripts/orchestrator/orchestrate.ps1": "v2\n"})
r = am.adopt(REPO, COORD, GREEN, loaded=loaded)
check("a changed orchestrate.ps1 is the restart trigger", (r.action, r.supervisor_changed, r.changed),
      (am.ADOPTED, True, ("scripts/orchestrator/orchestrate.ps1",)))
check("its line says so", "restarts" in r.line, True)
check("a supervisor that loaded the adopted commit has nothing to restart for", am.adopt(REPO, COORD, GREEN, loaded=head()).supervisor_changed, False)

# 4b. the restart does not depend on WHO fast-forwarded the checkout (review R1-1): the attended session runs
# `git merge --ff-only origin/main` by hand when ledger_state.py says a ledger publish is owed, and the next adopt then finds
# the checkout current; the supervisor that loaded the older script must still be told to restart, in every outcome.
PENDING = gh_says([{"status": "in_progress", "conclusion": ""}])
new = land("a second process fix", {"scripts/orchestrator/orchestrate.ps1": "v3\n"})
scratch.git("fetch", "-q", "origin", "main")
scratch.git("merge", "-q", "--ff-only", new)   # by hand, CI not consulted
r = am.adopt(REPO, COORD, PENDING, loaded=loaded)
check("a hand fast-forward is still seen: current, restart flagged", (r.action, r.supervisor_changed, r.changed),
      (am.CURRENT, True, ("scripts/orchestrator/orchestrate.ps1",)))
check("... and nothing asked CI", PENDING.asked, [])
land("an unstable landing", {"kb/Work/PB8.md": "h\n"})
r = am.adopt(REPO, COORD, PENDING, loaded=loaded)
check("a kept outcome flags the restart too", (r.action, r.supervisor_changed), (am.KEPT, True))
check("a caller that holds no loaded commit is never told to restart", am.adopt(REPO, COORD, PENDING).supervisor_changed, False)
check("a loaded commit git cannot read never restarts the supervisor",
      am.adopt(REPO, COORD, PENDING, loaded="0" * 40).supervisor_changed, False)
check("the unchanged-script landing does not flag a supervisor loaded after the fix",
      am.adopt(REPO, COORD, PENDING, loaded=head()).supervisor_changed, False)
am.adopt(REPO, COORD, GREEN)   # adopt the unstable landing once CI is green: the next sections start from origin/main

# 5. a tracked modification blocks (never stashed); the settings file and an untracked file do not
land("another landing", {"kb/Work/PB4.md": "d\n"})
scratch.write({"kb/Work/PB1.md": "edited\n"})
r = am.adopt(REPO, COORD, GREEN)
check("a modified tracked file blocks the adoption", (r.action, "tracked file" in r.reason), (am.KEPT, True))
check("... and is left as it is", (REPO / "kb/Work/PB1.md").read_text(encoding="utf-8"), "edited\n")
scratch.git("checkout", "--", "kb/Work/PB1.md")
scratch.write({".claude/settings.local.json": '{"edited": true}\n', "untracked.txt": "x\n"})
r = am.adopt(REPO, COORD, GREEN)
check("the settings file and an untracked file do not block", r.action, am.ADOPTED)
check("... and the settings edit survives", (REPO / ".claude/settings.local.json").read_text(encoding="utf-8"), '{"edited": true}\n')

# 6. ahead, diverged, off main -> kept
scratch.git("checkout", "--", ".claude/settings.local.json")
scratch.commit("an unpushed local commit", {"local.txt": "l\n"})
land("one more landing", {"kb/Work/PB5.md": "e\n"})
r = am.adopt(REPO, COORD, GREEN)
check("a diverged checkout is kept, said so", (r.action, "diverged" in r.reason), (am.KEPT, True))
scratch.git("reset", "-q", "--hard", "origin/main~1")   # back onto the old main, then ahead of nothing
scratch.git("fetch", "-q", "origin", "main")
scratch.git("checkout", "-q", "-b", "side")
r = am.adopt(REPO, COORD, GREEN)
check("a checkout off main is kept", (r.action, "not main" in r.reason), (am.KEPT, True))
scratch.git("checkout", "-q", "main")
scratch.commit("ahead only", {"ahead.txt": "a\n"}, allow_empty=True)
scratch.git("reset", "-q", "--hard", "origin/main")
scratch.commit("ahead for real", {"ahead2.txt": "a\n"})
r = am.adopt(REPO, COORD, GREEN)
check("a checkout ahead of origin/main is kept", (r.action, "ahead" in r.reason), (am.KEPT, True))
scratch.git("reset", "-q", "--hard", "origin/main")

# 7. dry run decides and moves nothing
base = head()
new = land("dry run landing", {"kb/Work/PB6.md": "f\n"})
r = am.adopt(REPO, COORD, GREEN, dry_run=True)
check("a dry run moves nothing and records nothing", (head(), am.adopted_sha(COORD) != new, "DRY RUN" in r.reason), (base, True, True))

# 8. the planner's guard: the floor is the adopted commit, and a checkout without it is behind
am.record(COORD, new, base)
problem = am.require_current(REPO, COORD)
check("a checkout behind the adopted commit is refused", bool(problem), True)
check("... naming both shas", (base[:9] in problem, new[:9] in problem), (True, True))
check("... and the way out", ("--allow-stale" in problem, "adopt_main.py adopt" in problem), (True, True))
scratch.git("merge", "-q", "--ff-only", new)
check("a checkout at the adopted commit passes", am.require_current(REPO, COORD), "")
check("no adoption record (no loop ever ran) passes", am.require_current(REPO, TMP / "no-coord"), "")
am.record(COORD, "f" * 40, new)
check("an adopted commit the checkout never fetched is behind", bool(am.require_current(REPO, COORD)), True)

# 9. the command line: `require` is an exit code, a missing GitHub CLI keeps the checkout and says so, and the planner refuses
py = [sys.executable, str(HERE / "adopt_main.py")]
r = subprocess.run([*py, "require", "--repo", str(REPO), "--coord", str(COORD)], capture_output=True, text=True, encoding="utf-8")
check("require exits 2 behind the adopted commit", r.returncode, 2)
am.record(COORD, head(), head())
r = subprocess.run([*py, "require", "--repo", str(REPO), "--coord", str(COORD)], capture_output=True, text=True, encoding="utf-8")
check("require exits 0 at it", r.returncode, 0)
base = head()
land("cli landing", {"kb/Work/PB7.md": "g\n"})
r = subprocess.run([*py, "adopt", "--repo", str(REPO), "--coord", str(COORD), "--gh", "no-such-gh-exe-pb2699", "--json"],
                   capture_output=True, text=True, encoding="utf-8")
out = json.loads(r.stdout)
check("without the GitHub CLI nothing is adopted: an unobserved CI is never a pass", (r.returncode, out["action"], head()), (0, am.KEPT, base))
check("... and the line says why", "not on PATH" in out["line"], True)
am.record(COORD, "e" * 40, base)
r = subprocess.run([sys.executable, str(HERE / "plan_wave.py"), "--budget-points", "1", "--dry-run", "--coord", str(COORD)],
                   capture_output=True, text=True, encoding="utf-8")
check("plan_wave.py refuses a checkout behind the adopted commit (exit 2)", (r.returncode, "behind the commit the loop adopted" in r.stdout), (2, True))

remove_tree(TMP)   # the scratch repository, its origin, the lander's clone and the coordination
if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("adopt_main self-test: all checks OK")
