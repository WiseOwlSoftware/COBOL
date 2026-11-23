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


def case_ea_shape_across_lines(expect):
    # kb/Work PB6136 review R1-2: the reviewer's probe, an order number broken from its `order #` across a line break,
    # in one commit and completed by a later one against the unchanged first line; both found as the gate finds them
    order, number = "order " + "#\n", "1234" + "56\n"
    REPO.git("checkout", "-q", "-b", "split", CLEAN)
    try:
        REPO.commit("split", {"g.txt": "x\n" + order + number})
        code, said = _push(REPO.git("rev-parse", "HEAD"))
        expect("a shape that crosses a line break is refused, on the line it starts",
               code == 1 and "reseller order number: g.txt:2" in said, said)
        REPO.git("reset", "-q", "--hard", CLEAN)
        REPO.commit("first half", {"h.txt": order})
        REPO.git("update-ref", "refs/remotes/origin/main", "HEAD")
        REPO.commit("second half", {"h.txt": order + number})
        code, said = _push(REPO.git("rev-parse", "HEAD"))
        expect("an added line that completes a shape against an unchanged line is refused",
               code == 1 and "h.txt:1" in said, said)
    finally:
        REPO.git("update-ref", "refs/remotes/origin/main", BASE)
        REPO.git("checkout", "-q", "main")
        REPO.git("reset", "-q", "--hard", CLEAN)


def case_eb_annotated_tag_message(expect):
    # kb/Work PB6136 review R1-3: an annotated tag's message is published by a tag push, `--tags` and `--follow-tags`
    REPO.git("tag", "-a", "v-stamped", "-m", "release " + STAMP, CLEAN)
    try:
        tag = REPO.git("rev-parse", "v-stamped")
        code, said = P.pre_push(REPO.path, ["origin", str(BARE.path)], f"refs/tags/v-stamped {tag} refs/tags/v-stamped {ZERO}\n")
        expect("a tag push whose annotated message carries the stamp is refused, never echoing it",
               code == 1 and "the annotated tag message" in said and "Jane" not in said, said)
        expect("`git push origin v-stamped` (the tag named as a source) is scanned", bool(P.scan(REPO.path, ["v-stamped"])))
        expect("`git push --tags` is scanned", bool(P.scan(REPO.path, ["--tags"])))
        expect("`git push --follow-tags` of the commit it points at is scanned",
               bool(P.scan(REPO.path, [CLEAN, "--follow-tags"])))
        expect.equal("a push without its tags is not refused for it", P.scan(REPO.path, [CLEAN]), [])
    finally:
        REPO.git("tag", "-d", "v-stamped")


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
