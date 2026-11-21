#!/usr/bin/env python3
"""The `editions:` field of the work register (kb/Work PB5995): a non-terminal note must declare it, its value is a list
of the mission's editions, an open note that cites an edition's text must declare it unless a recorded judgment says
why; the seed is the text match united with the citations; the line is written directly after `tags:` (a wrapped list
too) and replaced in place; the backfill writes only notes that lack it and is re-runnable; set-editions writes the
value and an addendum, all or nothing."""
import json
import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

C = selftest_cases.subject(__file__)   # scripts/spec/work_editions.py


def _note(ident, status="open", body="", **fm):
    head = {"id": ident, "kind": "defect", "status": status, "area": "x", **fm, "tags": "[wiseowl-cobol, work]"}
    return "---\n" + "".join(f"{k}: {v}\n" for k, v in head.items()) + f"---\n# {ident}\n\n{body}\n"


def _items(d):
    return C.work.load(d)


def _write(d, ident, text):
    (d / f"{ident}.md").write_text(text, encoding="utf-8", newline="\n")


def case_00_a_non_terminal_note_must_declare(expect):
    d = scratch_dir("work-editions-required-")
    _write(d, "PB1", _note("PB1"))
    _write(d, "PB2", _note("PB2", "blocked"))
    _write(d, "PB3", _note("PB3", "landed"))
    _write(d, "PB4", _note("PB4", editions="[]"))
    bad = C.problems(_items(d))
    expect.equal("the open and the blocked note are refused, the landed one and [] are not",
                 sorted(b.split(":")[0] for b in bad), ["PB1.md", "PB2.md"])
    expect("the refusal says what the field means and how to seed it",
           all(C.MEANING in b and "backfill-editions" in b for b in bad), bad)


def case_01_a_value_outside_the_editions_is_refused(expect):
    for value in ("[1977]", "[1985, 1985]", "1985", "[85]"):
        d = scratch_dir("work-editions-value-")
        _write(d, "PB1", _note("PB1", "landed", editions=value))
        expect.equal(f"{value} refused, on a terminal note too", len(C.problems(_items(d))), 1)


def case_02_the_drift_rule_and_its_recorded_judgment(expect):
    d = scratch_dir("work-editions-drift-")
    _write(d, "PB1", _note("PB1", body="The rule §1985:VI.5.3.4 GR3 is broken.", editions="[2002]"))
    _write(d, "PB2", _note("PB2", body="Amendment 1's §1989:A.2.2 is cited.", editions="[1985]"))
    _write(d, "PB3", _note("PB3", "landed", body="§2014:8.3.1 cited", editions="[]"))
    bad = C.problems(_items(d))
    expect.equal("an open note citing 1985 that declares only 2002 is refused, naming it", len(bad), 1)
    expect("the refusal names the edition", bad and bad[0].startswith("PB1.md: cites the text of [1985]"), bad)
    (d / "PB1").mkdir()
    (d / "PB1" / "2026-11-21-editions.md").write_text(
        "## 2026-11-21 - editions set to [2002]\n\n**editions:** [2002] (was [1985, 2002])\n\nOnly 2002 is wrong.\n",
        encoding="utf-8", newline="\n")
    expect.equal("a recorded judgment of exactly that value excuses it", C.problems(_items(d)), [])
    _write(d, "PB1", _note("PB1", body="The rule §1985:VI.5.3.4 GR3 is broken.", editions="[2002, 2014]"))
    expect.equal("a hand edit past the judgment is refused again", len(C.problems(_items(d))), 1)


def case_03_the_seed_is_the_match_and_the_citations(expect):
    expect.equal("a year, a --std spelling", C.seed({"title": "the 1985 listing", "_body": "at --std 2002"}),
                 frozenset({1985, 2002}))
    expect.equal("2023 by citation or rule id only", C.seed({"title": "ISO/IEC 1989:2023 and PB2014"}), frozenset())
    expect.equal("an amendment's citation counts for 1985", C.seed({"_body": "§1993:p.B-16"}), frozenset({1985}))
    expect.equal("the reader's own keys are not text", C.seed({"_file": "1985.md"}), frozenset())
    expect.equal("a malformed citation cites nothing", C.cited({"_body": "§1985:"}), frozenset())


def case_04_the_line_goes_after_tags(expect):
    text = "---\nid: PB1\nstatus: open\ntags: [a,\n  b]\nfiled: x\n---\nbody\n"
    got = C.with_field(text, {2002, 1985})
    expect.equal("after a wrapped tags list, in the editions' order", got,
                 "---\nid: PB1\nstatus: open\ntags: [a,\n  b]\neditions: [1985, 2002]\nfiled: x\n---\nbody\n")
    expect.equal("replaced in place", C.with_field(got, set()),
                 "---\nid: PB1\nstatus: open\ntags: [a,\n  b]\neditions: []\nfiled: x\n---\nbody\n")
    block = "---\nid: PB1\ntags:\n  - a\n  - b\n---\nbody\n"
    expect.equal("after a block list", C.with_field(block, {2023}),
                 "---\nid: PB1\ntags:\n  - a\n  - b\neditions: [2023]\n---\nbody\n")
    expect.equal("before the closing line when there is no tags", C.with_field("---\nid: PB1\n---\n", {2014}),
                 "---\nid: PB1\neditions: [2014]\n---\n")
    expect.equal("seeded keeps a declared value", C.seeded(got), got)
    expect("seeded writes the seed of a new note", "editions: [1985]" in C.seeded(_note("PB1", body="--std 85")))


def case_05_the_backfill_writes_only_what_lacks_it(expect):
    d = scratch_dir("work-editions-backfill-")
    _write(d, "PB1", _note("PB1", body="the 1985 listing"))
    _write(d, "PB2", _note("PB2", editions="[2002]", body="the 1985 listing"))
    _write(d, "PB3", _note("PB3", "landed", body="the 1985 listing"))
    before = {p.name: p.read_bytes() for p in d.glob("*.md")}
    C.backfill(d, write=False)
    expect.equal("a dry run writes nothing", {p.name: p.read_bytes() for p in d.glob("*.md")}, before)
    C.backfill(d, write=True)
    items = {it["id"]: it for it in _items(d)}
    expect.equal("the open note lacking it gets the seed", C.work.declared_editions(items["PB1"]), frozenset({1985}))
    expect.equal("a declared note is untouched", (d / "PB2.md").read_bytes(), before["PB2.md"])
    expect.equal("a terminal note is untouched", (d / "PB3.md").read_bytes(), before["PB3.md"])
    once = (d / "PB1.md").read_bytes()
    C.backfill(d, write=True)
    expect.equal("re-runnable: a second run changes nothing", (d / "PB1.md").read_bytes(), once)
    expect.equal("and the register then passes the field's check", C.problems(_items(d)), [])


def case_06_set_editions_records_the_judgment(expect):
    d = scratch_dir("work-editions-set-")
    _write(d, "PB1", _note("PB1", editions="[1985, 2002]", body="§1985:VI.5.3.2 fixed beside the 2002 defect"))
    _write(d, "PB2", _note("PB2", editions="[2014]"))
    batch = d.parent / f"{d.name}-batch.json"
    batch.write_text(json.dumps([{"id": "PB1", "editions": [2002], "reason": "Only --std 2002 is wrong."},
                                 {"id": "PB2", "editions": [1977], "reason": "x"}]), encoding="utf-8", newline="\n")
    before = {p.name: p.read_bytes() for p in d.glob("*.md")}
    expect.equal("one bad record refuses the batch", C.set_editions(d, batch, write=True, day="2026-11-21"), 1)
    expect.equal("and nothing is written", ({p.name: p.read_bytes() for p in d.glob("*.md")}, (d / "PB1").exists()),
                 (before, False))
    batch.write_text(json.dumps([{"id": "PB1", "editions": [2002], "reason": "Only --std 2002 is wrong."}]),
                     encoding="utf-8", newline="\n")
    expect.equal("a good batch is written", C.set_editions(d, batch, write=True, day="2026-11-21"), 0)
    items = {it["id"]: it for it in _items(d)}
    expect.equal("the value", C.work.declared_editions(items["PB1"]), frozenset({2002}))
    expect.equal("the addendum is a new file", sorted(p.name for p in (d / "PB1").iterdir()), ["2026-11-21-editions.md"])
    expect("it carries the reason", "Only --std 2002 is wrong." in items["PB1"]["_body"])
    expect.equal("so the narrowed note passes the drift rule", C.problems(list(items.values())), [])
    C.set_editions(d, batch, write=True, day="2026-11-21")
    expect.equal("a second judgment the same day is a second file", sorted(p.name for p in (d / "PB1").iterdir()),
                 ["2026-11-21-editions-2.md", "2026-11-21-editions.md"])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
