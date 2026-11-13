#!/usr/bin/env python3
"""The public-API baselines' rules on planted baselines (DESIGN-orchestrator-loop.md section 16.9, kb/Work PB5396): a symbol
listed twice (RS0025, kb/Work PB3694), an Unshipped baseline out of order, the fixer that puts it in order without losing or
adding a line, and the audit over a planted repository, RED on each of the two breaches and GREEN on the ordered tree."""
import sys

import selftest_cases
from scratch_repo import ScratchRepo, scratch_dir
from standard_streams import ensure_utf8

pab = selftest_cases.subject(__file__)   # scripts/public_api_baselines.py

SHIPPED, UNSHIPPED = "src/Lib/PublicAPI.Shipped.txt", "src/Lib/PublicAPI.Unshipped.txt"


def _api(*symbols):
    """A baseline's text: the header, then `symbols`, one per line."""
    return "".join(line + "\n" for line in ("#nullable enable", *symbols))


def _repo(**files):
    """A planted repository whose tracked files are `files` ({path: text}), as a path."""
    repo = ScratchRepo.init(scratch_dir("public-api-baselines-"))
    repo.commit("base", files)
    return repo.path


def case_00_a_symbol_listed_twice_or_in_both_baselines_is_a_finding(expect):
    expect.equal("a symbol twice in one file names the file and the symbol",
                 pab.baseline_duplicates({UNSHIPPED: _api("C.C", "E.E", "E.E")}), [f"{UNSHIPPED}: E.E is listed twice"])
    expect.equal("a plain symbol in Shipped and Unshipped of one directory is listed in both",
                 pab.baseline_duplicates({SHIPPED: _api("A.A"), UNSHIPPED: _api("A.A", "C.C")}),
                 [f"{SHIPPED} and {UNSHIPPED}: A.A is listed in both"])
    expect.equal("`*REMOVED*B.B` beside the Shipped `B.B` it removes is no duplicate",
                 pab.baseline_duplicates({SHIPPED: _api("B.B"), UNSHIPPED: _api("*REMOVED*B.B", "Z.Z")}), [])
    expect.equal("a header, a blank line and a CR are no symbol",
                 pab.baseline_duplicates({UNSHIPPED: "#nullable enable\r\n\n\nA.A\r\nA.B\n"}), [])
    expect.equal("the same symbol in two directories is two APIs",
                 pab.baseline_duplicates({"src/A/PublicAPI.Unshipped.txt": _api("X.X"), "src/B/PublicAPI.Unshipped.txt": _api("X.X")}), [])


def case_01_an_unshipped_baseline_out_of_order_is_a_finding_and_shipped_is_exempt(expect):
    expect.equal("an ordered Unshipped baseline is clean",
                 pab.order_problems({UNSHIPPED: _api("*REMOVED*B.B", "A.A", "C.C")}), [])
    found = pab.order_problems({UNSHIPPED: _api("C.C", "A.A", "B.B")})
    expect("a line that sorts before the one above it is named, with the fixer",
           len(found) == 1 and "line 3 ('A.A') sorts before the line above it" in found[0] and "--write" in found[0], str(found))
    expect.equal("Shipped keeps its order (the ABI pin reads it line by line)", pab.order_problems({SHIPPED: _api("C.C", "A.A")}), [])
    expect.equal("a file under no src/ directory is no baseline", pab.order_problems({"docs/PublicAPI.Unshipped.txt": "b\na\n"}), [])
    blank = pab.order_problems({UNSHIPPED: "#nullable enable\n\nA.A\n"})
    expect("a blank line is the file differing from its ordered form, named as such", len(blank) == 1 and "blank line" in blank[0], str(blank))
    expect("a missing final LF is a finding", len(pab.order_problems({UNSHIPPED: "#nullable enable\nA.A"})) == 1)


def case_02_ordered_is_the_ordinal_form_and_loses_nothing(expect):
    text = _api("b.B", "B.B", "*REMOVED*Z.Z", "A.A", "A.A")
    out = pab.ordered(text)
    expect.equal("the header first, `*REMOVED*` before the symbols, ordinal order (uppercase before lowercase), duplicates kept",
                 out.split("\n"), ["#nullable enable", "*REMOVED*Z.Z", "A.A", "A.A", "B.B", "b.B", ""])
    expect.equal("the line multiset is unchanged", sorted(out.split("\n")), sorted(text.split("\n")))
    expect.equal("ordering twice is ordering once", pab.ordered(out), out)
    expect.equal("a CRLF file comes out LF", pab.ordered("#nullable enable\r\nA.A\r\n"), "#nullable enable\nA.A\n")


def case_03_the_audit_reads_the_tracked_baselines_of_a_repository(expect):
    clean = _repo(**{SHIPPED: _api("A.A"), UNSHIPPED: _api("B.B", "C.C")})
    rc, report = pab.check(clean)
    expect("an ordered tree with no duplicate is GREEN", rc == 0 and "GREEN — 2 baseline(s)" in report, report)
    rc, report = pab.check(_repo(**{SHIPPED: _api("A.A"), UNSHIPPED: _api("C.C", "B.B")}))
    expect("an out-of-order Unshipped baseline is RED, naming the file", rc == 1 and f"{UNSHIPPED}: not in ordinal order" in report
           and "RED — 1 finding(s)" in report, report)
    rc, report = pab.check(_repo(**{SHIPPED: _api("A.A"), UNSHIPPED: _api("A.A", "B.B")}))
    expect("a symbol in both baselines is RED", rc == 1 and "A.A is listed in both" in report, report)
    rc, report = pab.check(_repo(**{"src/readme.txt": "x\n"}))
    expect("a tree with no Unshipped baseline is RED, never a pass on nothing", rc == 1 and "no tracked Unshipped baseline" in report, report)


def case_04_the_fixer_orders_the_unshipped_baseline_and_only_it(expect):
    root = _repo(**{SHIPPED: _api("Z.Z", "A.A"), UNSHIPPED: _api("C.C", "B.B", "*REMOVED*Q.Q")})
    changed = pab.write(root)
    expect.equal("only the Unshipped baseline is rewritten", changed, [UNSHIPPED])
    expect.equal("it is now the ordered text", (root / UNSHIPPED).read_bytes().decode(), "#nullable enable\n*REMOVED*Q.Q\nB.B\nC.C\n")
    expect.equal("Shipped is untouched", (root / SHIPPED).read_bytes().decode(), _api("Z.Z", "A.A"))
    expect.equal("a second write changes nothing", pab.write(root), [])
    rc, report = pab.check(root)
    expect("and the audit is GREEN", rc == 0, report)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
