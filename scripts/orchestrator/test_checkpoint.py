#!/usr/bin/env python3
"""Self-test for checkpoint.py: the checkpoint names every worktree's state, and a synthesized handoff is schema-valid.
Run: python scripts/orchestrator/test_checkpoint.py   (a fabricated repo with real worktrees in a temp directory)."""
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)
from standard_streams import ensure_utf8  # noqa: E402

ensure_utf8()

CP = HERE / "checkpoint.py"
SCHEMA = json.loads((HERE / "handoff.schema.json").read_text(encoding="utf-8"))
TMP = scratch_dir("checkpoint-test-")
REPO, COORD = TMP / "repo", TMP / "coord"
fails = []


def check(name, got, want):
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def run(*a):
    r = subprocess.run([sys.executable, str(CP), *a, "--coord", str(COORD), "--repo", str(REPO)], capture_output=True, text=True)
    check(f"exit of {a[0]}", r.returncode, 0)
    return r


scratch = ScratchRepo.init(REPO)
scratch.commit("base", {"a.txt": "a"})
scratch.git("update-ref", "refs/remotes/origin/main", "HEAD")           # the base the worktrees are measured against
committed = scratch.worktree(TMP / "wt-committed", branch="committed")
scratch.worktree(TMP / "wt-dirty", branch="dirty")
scratch.worktree(TMP / "wt-clean", branch="clean")
committed.commit("wip", {"b.txt": "b"})
(TMP / "wt-committed" / "STATUS.md").write_text("STATUS-AT: abc\nDONE: x\n", encoding="utf-8", newline="\n")
(TMP / "wt-dirty" / "c.txt").write_text("c", encoding="utf-8", newline="\n")                      # uncommitted
(TMP / "wt-dirty" / "STATUS.md").write_text("ignored in the dirty count", encoding="utf-8", newline="\n")
COORD.mkdir()
(COORD / "milestones.jsonl").write_text('{"at":"t1","what":"plan written"}\nnot json\n{"at":"t2","what":"train 1 landed"}\n',
                                        encoding="utf-8", newline="\n")

run("write", "--unit", "wave", "--session", "s1", "--started", "2026-10-04T18:00:00-07:00", "--calls", "40",
    "--context", "90000", "--cost", "3.5", "--bg-tasks", "1")
cp = json.loads((COORD / "checkpoint.json").read_text(encoding="utf-8"))
trees = {t["branch"]: t for t in cp["worktrees"]}
check("main checkout excluded", sorted(trees), ["clean", "committed", "dirty"])
check("committed: ahead 1, clean", (trees["committed"]["ahead_of_main"], trees["committed"]["uncommitted"]), (1, 0))
check("dirty: ahead 0, one uncommitted file (STATUS.md ignored)", (trees["dirty"]["ahead_of_main"], trees["dirty"]["uncommitted"]), (0, 1))
check("STATUS.md headline captured", trees["committed"]["status_md"], "STATUS-AT: abc")
check("counters recorded", (cp["calls"], cp["context_tokens"], cp["background_tasks"], cp["unit"]), (40, 90000, 1, "wave"))
check("milestones kept, a malformed line is not dropped", [m.get("what") for m in cp["milestones"]], ["plan written", "not json", "train 1 landed"])

out = TMP / "handoff.json"
run("synthesize", "--unit", "wave", "--out", str(out))
h = json.loads(out.read_text(encoding="utf-8"))
check("schema-required keys present", all(k in h for k in SCHEMA["required"]), True)
check("no key the schema forbids", sorted(set(h) - set(SCHEMA["properties"])), [])
check("outcome split, next unit resume", (h["outcome"], h["next_unit"], h["synthesized"]), ("split", "resume", True))
check("summary within the cap", len(h["summary"]) <= 900, True)
check("only worktrees holding work are pending", sorted(b["branch"] for b in h["branches_pending"]), ["committed", "dirty"])
check("summary quotes the last milestone", "train 1 landed" in h["summary"], True)
check("branch statuses are schema values", {b["status"] for b in h["branches_pending"]} <= {"DONE", "SPLIT", "DISCHARGED", "BLOCKED"}, True)

(COORD / "checkpoint.json").unlink()
(COORD / "milestones.jsonl").unlink()
run("synthesize", "--unit", "meter", "--out", str(out))
h = json.loads(out.read_text(encoding="utf-8"))
check("with no checkpoint and no milestones it still synthesizes", ("no checkpoint" in h["summary"], h["next_unit"]), (True, "resume"))

# kb/Work PB3250: a worktree removed between the listing and its measurement is left out; the checkpoint is still written.
# The subject runs git itself, in this process, so the caller's repository-selection variables are dropped first.
isolate()
sys.path.insert(0, str(HERE))
import checkpoint  # noqa: E402

vanishing = TMP / "wt-vanishing"
scratch.worktree(vanishing, branch="vanishing")
real_git = checkpoint.git


def git_then_vanish(cwd, *args):
    """The real git; right after the worktree listing the worktree it names is deleted (prune_worktrees.py --apply, an
    agent's cleanup), the race the note measured: the next call's directory is gone."""
    out = real_git(cwd, *args)
    if args[:2] == ("worktree", "list"):
        remove_tree(vanishing)
    return out


checkpoint.git = git_then_vanish
try:
    survivors = checkpoint.worktrees(REPO)
except OSError as e:     # NotADirectoryError (WinError 267) on Windows, FileNotFoundError elsewhere
    fails.append(f"a worktree removed after the listing aborted the survey with {type(e).__name__}: {e}")
    survivors = []
finally:
    checkpoint.git = real_git
check("a worktree gone after the listing is left out, the others measured",
      sorted(t["branch"] for t in survivors), ["clean", "committed", "dirty"])
check("the survivors are still measured", {t["branch"]: t["ahead_of_main"] for t in survivors}.get("committed"), 1)

# a worktree whose directory is there but whose git cannot read it stays, its uncommitted count unknown (never a clean 0)
broken = TMP / "wt-broken"
scratch.worktree(broken, branch="broken")
(broken / ".git").unlink()                                       # a hidden file on Windows: replaced, not rewritten in place
(broken / ".git").write_text("not a gitdir pointer\n", encoding="utf-8", newline="\n")
unreadable = {t["branch"]: t for t in checkpoint.worktrees(REPO)}
check("an unreadable worktree stays, uncommitted unknown", (sorted(unreadable), unreadable.get("broken", {}).get("uncommitted", "absent")),
      (["broken", "clean", "committed", "dirty"], None))

# a listing that fails (the directory is no repository) records no worktrees and says so on stderr, and does not raise: a
# dead unit's handoff is still synthesized (the supervisor's own self-tests run these commands against a plain directory)
(TMP / "no-such-repo").mkdir()
r = subprocess.run([sys.executable, str(CP), "synthesize", "--unit", "wave", "--out", str(TMP / "plain.json"),
                    "--coord", str(COORD), "--repo", str(TMP / "no-such-repo")], capture_output=True, text=True)
check("a failed listing still synthesizes", (r.returncode, (TMP / "plain.json").is_file()), (0, True))
check("a failed listing is said on stderr", "worktree list" in r.stderr, True)

remove_tree(TMP)   # the scratch repository, its worktrees and the coordination directory
if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("checkpoint self-test: all checks OK")
