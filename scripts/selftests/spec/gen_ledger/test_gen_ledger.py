#!/usr/bin/env python3
"""The completion ledger, per edition (kb/Work PB5994): every edition's verdict on the page IS edition_status's for the
same parts, at the top and in its own section, in the owner's order; the page reads COMPLETE for an edition only when all
four of its parts are measured and zero; the work in flight is attributed to an edition by its notes and its name, and a
landed branch is not in flight; an input the render could not read says UNMEASURED; and the landings' notes are counted
filed and closed per edition."""
import itertools
import re
import sys

import fixtures as F
import selftest_cases
from standard_streams import ensure_utf8

C = selftest_cases.subject(__file__)   # scripts/spec/gen_ledger.py

def case_00_the_owners_order(expect):
    expect.equal("1985, 2002, 2023, 2014", C.ORDER, (1985, 2002, 2023, 2014))
    expect.equal("every edition edition_status measures", sorted(C.ORDER), sorted(C.edition_status.EDITIONS))
    page = C.render(F.ledger({}))
    expect.equal("the top table in that order", [int(y) for y in re.findall(r'href="#e(\d+)"', page)], list(C.ORDER))


def case_01_each_verdict_is_edition_status_s(expect):
    counts = {1985: (0, 0, 5, 840), 2002: (1173, 704, 3, None), 2023: (4, 0, 2, None), 2014: (None, None, 1, None)}
    led = F.ledger(counts)
    page = C.render(led)
    want = [C.edition_status.verdict(y, C.edition_status.status(y, F.sources(*counts[y])))[1] for y in C.ORDER]
    expect.equal("the top section prints edition_status's lines, in order", F.verdicts(page)[:4], want)
    expect.equal("each edition's section prints its own again", F.verdicts(page)[4:], want)
    expect.equal("none is COMPLETE", [ed.complete for ed in led.editions], [False] * 4)


def case_02_complete_only_when_every_part_is_measured_and_zero(expect):
    for counts in itertools.product((0, 3, None), repeat=4):
        page = C.render(F.ledger({1985: counts, **{y: (1, 0, 0, 0) for y in C.ORDER[1:]}}))   # only 1985 may be complete
        lines = F.verdicts(page)
        complete = all(c == 0 for c in counts)
        expect.equal(f"{counts}: the line", lines[0] == "=== EDITION 1985: COMPLETE ===", complete)
        expect.equal(f"{counts}: the pill", '<span class="pill good">COMPLETE</span>' in page, complete)
        expect.equal(f"{counts}: its section agrees", lines[4], lines[0])
    expect("an unmeasured part is named on the page", "not measured" in C.render(F.ledger({1985: (0, 0, 0, None)})))


def case_03_the_work_in_flight_by_edition(expect):
    editions_of = {"PB10": frozenset({1985}), "PB20": frozenset({2002, 2023})}
    rows = [F.row("group-1985-fixes", "IN FLIGHT"), F.row("worktree-wf_x-1", "WAITING TO LAND", lead="PB20"),
            F.row("pb10-thing", "BOUNCED"), F.row("tidy-tools", "DECISION"), F.row("done-1985", "LANDED")]
    flight = C.in_flight(rows, editions_of)
    expect.equal("a landed branch is not in flight", [b.name for b in flight],
                 ["group-1985-fixes", "worktree-wf_x-1", "pb10-thing", "tidy-tools"])
    by = {b.name: b for b in flight}
    expect.equal("the name names an edition", by["group-1985-fixes"].editions, frozenset({1985}))
    expect.equal("the report's lead note", (by["worktree-wf_x-1"].notes, by["worktree-wf_x-1"].editions),
                 (("PB20",), frozenset({2002, 2023})))
    expect.equal("a note id in the name", (by["pb10-thing"].notes, by["pb10-thing"].editions), (("PB10",), frozenset({1985})))
    page = C.render(F.ledger({}, flight=flight))
    sec = {y: page[page.index(f'id="e{y}"'):] for y in C.ORDER}
    expect("1985's section lists its branches", "group-1985-fixes" in sec[1985].split('id="e2002"')[0]
           and "pb10-thing" in sec[1985].split('id="e2002"')[0])
    expect("2002's lists the lead's", "worktree-wf_x-1" in sec[2002].split('id="e2023"')[0])
    tail = page[page.index("Work in flight on no edition"):]
    expect("a branch of no edition has its own list", "tidy-tools" in tail and "group-1985-fixes" not in tail)


def case_04_an_unread_input_is_unmeasured(expect):
    page = C.render(F.ledger({}, flight=C.Unmeasured("planted: survey failed")))
    expect("the survey's reason", "planted: survey failed" in page)
    expect("the quota's reason", "planted: no quota" in page)
    expect("the log's reason", "planted: no log" in page)
    expect.equal("each said UNMEASURED: the survey per edition and for no edition, the quota, the log",
                 page.count("UNMEASURED"), 4 + 1 + 1 + 1)


def case_05_the_landings_filed_and_closed(expect):
    got = C.landings(F.LOG, {"PB1": frozenset({1985}), "PB2": frozenset({1985, 2002}), "PB3": frozenset({2023})})
    expect.equal("a new note is filed", got["filed"], {1985: 1, 2002: 0, 2023: 0, 2014: 0})
    expect.equal("a status set to landed closes, an addendum never", got["closed"], {1985: 1, 2002: 1, 2023: 0, 2014: 0})
    page = C.render(F.ledger({}, landings={**got, "total": 2, "days": 7}))
    expect("the landings are counted", "<strong>2</strong>" in page)


def case_06_the_stamp_reads_every_part_s_inputs(expect):
    transcription = C.spec_pdf.markdown(1985, C.REPO).relative_to(C.REPO).as_posix()
    for path in ("kb/Work", transcription, "tests/version-matrix/inventory-1985",
                 "tests/version-matrix/code-reads-1985", "docs/rearchitecture/spec-rule-catalog-1985.json"):
        expect(f"{path} stamps the page", path in C.STAMP_PATHS)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
