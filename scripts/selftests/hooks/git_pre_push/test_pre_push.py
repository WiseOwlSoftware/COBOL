#!/usr/bin/env python3
"""The push-time licence-stamp scan (kb/Work PB6136), on planted pushes over scratch repositories: a stamp on an added
line, and in a commit message, refused, naming the file, line and commit and never the matched text; the clean
neighbour, a deletion and a stamp already on the remote passed; a ref selector scanned; a source git would read as an
option refused; and the tracked `.githooks/pre-push` wrapper stopping a real `git push` while letting a clean one
through. The planted stamp is built at run time, so this file holds no stamp."""
import sys

import selftest_cases
from scratch_repo import ScratchRepo, scratch_dir
from standard_streams import ensure_utf8

P = selftest_cases.subject(__file__)   # scripts/hooks/git_pre_push.py

STAMP = "Licensed to " + "Jane" + " Doe"
ZERO = "0" * 40
ROOT = scratch_dir("pre-push-")
REPO = ScratchRepo.init(ROOT / "repo")
BARE = REPO.origin(ROOT / "origin.git")
REPO.commit("base", {"a.txt": "one\n"})
REPO.git("push", "-q", "origin", "main")
BASE = REPO.git("rev-parse", "HEAD")
REPO.commit("clean", {"b.txt": "two\nthree\n"})
CLEAN = REPO.git("rev-parse", "HEAD")
REPO.commit("stamped", {"c.txt": "x\n" + STAMP + " page 3\n"})
STAMPED = REPO.git("rev-parse", "HEAD")
REPO.git("branch", "stamped", STAMPED)   # a branch `git push --all` publishes
REPO.git("reset", "-q", "--hard", CLEAN)
REPO.commit("A message that quotes it: " + STAMP, {"d.txt": "four\n"})
MESSAGE = REPO.git("rev-parse", "HEAD")
REPO.git("reset", "-q", "--hard", CLEAN)


def _push(sha: str, remote: str = "origin") -> tuple[int, str]:
    return P.pre_push(REPO.path, [remote, str(BARE.path)], f"refs/heads/main {sha} refs/heads/main {BASE}\n")


def case_a_clean_commit_passes(expect):
    expect.equal("a clean commit passes", _push(CLEAN), (0, ""))


def case_b_added_stamp_refused(expect):
    code, said = _push(STAMPED)
    expect("an added stamp line is refused, naming the file, line and commit",
           code == 1 and "c.txt:2" in said and STAMPED[:12] in said and "naming a purchaser" in said, said)
    expect("the refusal never echoes the matched text", "Jane" not in said, said)
    expect("a remote named by URL is scanned against every remote's tracking refs", _push(STAMPED, str(BARE.path))[0] == 1)
    expect("a ref selector (`git push --all`) is scanned", bool(P.scan(REPO.path, ["--branches"])))


def case_c_message_stamp_refused(expect):
    code, said = _push(MESSAGE)
    expect("a stamp in a commit message is refused", code == 1 and "the commit message" in said, said)


def case_d_nothing_published(expect):
    expect.equal("a deletion publishes nothing",
                 P.pre_push(REPO.path, ["origin", "u"], f"(delete) {ZERO} refs/heads/x {BASE}\n"), (0, ""))
    expect.equal("a source that names no commit publishes nothing", P.scan(REPO.path, ["no-such-branch"]), [])
    expect("a source git would read as an option is refused unscanned", P.scan(REPO.path, ["--exec=x"]) is None)
    expect("a directory git cannot read is refused unscanned", P.scan(ROOT / "nowhere", ["HEAD"]) is None)


def case_e_already_on_remote(expect):
    REPO.git("update-ref", "refs/remotes/origin/main", STAMPED)
    try:
        REPO.git("checkout", "-q", "-b", "after", STAMPED)
        REPO.commit("after", {"e.txt": "five\n"})
        expect.equal("a stamp in a commit already on the remote is not refused again",
                     _push(REPO.git("rev-parse", "HEAD")), (0, ""))
    finally:
        REPO.git("update-ref", "refs/remotes/origin/main", BASE)


def case_f_wrapper_end_to_end(expect):
    REPO.git("config", "core.hooksPath", (P.REPO / ".githooks").as_posix())
    r = REPO.run("push", "origin", f"{STAMPED}:refs/heads/topic")
    expect("the .githooks/pre-push wrapper stops a real push",
           r.returncode != 0 and "per-copy licence stamp" in r.stderr and "Jane" not in r.stderr, r.stderr)
    expect("and the remote did not receive it", not BARE.run("rev-parse", "--verify", "-q", "refs/heads/topic").stdout.strip())
    r = REPO.run("push", "origin", f"{CLEAN}:refs/heads/clean")
    expect("the wrapper lets a clean push through", r.returncode == 0, r.stderr)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
