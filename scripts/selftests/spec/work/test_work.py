#!/usr/bin/env python3
"""Cases of scripts/spec/work.py, the work register's one reader and its `check` (kb/Work PB3202): the frontmatter reader
against `tests/version-matrix/work-frontmatter-cases.json`, a fixture directory read as a register, the `upstream` kind,
addenda, the views, `process_only`, the site rules, the harm vocabulary, the frontier and running-limit fields, the
topology and tiers, the R3 program's rules and the architecture edges file's removers.

Every case reads a register and a tree it builds itself (a scratch register directory, a scratch git repository through
scratch_repo), never the live kb/Work or the live tree: a rule held only against live state passes vacuously, or turns
red on a commit that changed no rule, when that state moves (kb/Work PB3202, which moved these cases here from the wave
planner's test file and converted work.py's in-file `--self-test`). The one exception is the harm vocabulary case,
whose subject IS the tree: the files that restate the vocabulary must agree with HARM_FLAGS.
Run: python scripts/self_tests.py --only scripts/selftests/spec/work/test_work.py"""
import contextlib
import functools
import io
import json
import sys

import selftest_cases
from scratch_repo import ScratchRepo, scratch_dir
from standard_streams import ensure_utf8

W = selftest_cases.subject(__file__)   # scripts/spec/work.py

NL = chr(10)
FIXTURE_REL = "tests/version-matrix/work-frontmatter-cases.json"
#: The repository files that restate the harm vocabulary (a flag list in a brief, an enum in a workflow template) and
#: are the places a new harm class is forgotten: any file under these folders that names one of HARM_FLAGS must name
#: them all.
HARM_RESTATEMENT_ROOTS = (".claude/skills/workstream/templates",)


# ── the frontmatter reader against its fixture ──────────────────────────────────────────────────────────────────────

def _fixture() -> dict:
    return json.loads((W.REPO / FIXTURE_REL).read_text(encoding="utf-8"))


def _reading_view(file: str, text: str) -> dict:
    """What a reader of the register SEES in the file `file` holding `text`: the normalized answer it must give.

    Absent and empty are deliberately the same answer for a list (`[]`) and for a scalar (`""`): the register's checks
    ask what a note SAYS, and a key that is missing says exactly as much as a key that is empty. What is NOT
    normalized away is `errors`: a malformed value is a third outcome, and collapsing it into "empty" is the
    fail-open that `kb/Work/PB875` is about.

    `terminal` is what the DONE vocabulary (TERMINAL_STATUSES) makes of the status: the register's checks read DONE
    through it, so it is compared like any field (kb/Work PB3196). `refusal` is what read_note says of a file that is
    not a note: empty for an index or a README (skipped), the reason for a file NAMED like a note whose frontmatter
    does not parse (kb/Work PB3654)."""
    d, refusal = W.read_note(file, text)
    if d is None:
        return {"note": False, "id": "", "kind": "", "status": "", "terminal": False, "inventory_rows": [],
                "closes_rows": [], "closes_rows_reason": "", "errors": [], "refusal": refusal}
    scalar = lambda k: (v if isinstance(v := d.get(k, ""), str) else str(v))       # noqa: E731
    lst = lambda k: (v if isinstance(v := d.get(k, []), list) else [])             # noqa: E731
    return {"note": True, "id": scalar("id"), "kind": scalar("kind"), "status": scalar("status"),
            "terminal": scalar("status") in W.TERMINAL_STATUSES,
            "inventory_rows": lst("inventory_rows"), "closes_rows": lst("closes_rows"),
            "closes_rows_reason": scalar("closes_rows_reason"), "errors": sorted(d.get("_errors", [])),
            "refusal": ""}


def _answers(fixture: dict) -> list[dict]:
    return [{"name": c["name"], **_reading_view(c["file"], c["text"])} for c in fixture["cases"]]


def _wrapped_list(text: str) -> bool:
    """True when some line opens a `[` that does not close on the same line: the PB875 shape."""
    return any("]" not in line[line.index("["):] for line in text.splitlines() if "[" in line)


def _case_findings(fixture: dict, answers: list[dict]) -> list[str]:
    """Where the reader disagrees with the fixture's recorded expectation, field by field."""
    bad = []
    for case, got in zip(fixture["cases"], answers, strict=True):
        for k, want in case["expect"].items():
            # A fixture naming a field the view does not have is a FINDING, not a traceback.
            if k not in got:
                bad.append(f'case "{case["name"]}": the fixture expects a field {k!r} that the reader does not report')
            elif got[k] != want:
                bad.append(f'case "{case["name"]}": {k} is {got[k]!r}, fixture says {want!r}')
    return bad


def _scalar_findings(fixture: dict) -> list[str]:
    """The fixture's `scalars` against `_scalar` (kb/Work PB3812): the value and the error code of each, and a case for
    every escape the reader defines (a character added to `_YAML_ESCAPES` stops this until a case states it)."""
    cases = fixture.get("scalars", [])
    bad = []
    for c in cases:
        got = W._scalar(c["value"])
        if got != (c["expect"], c["error"]):
            bad.append(f'scalar case "{c["name"]}": {c["value"]!r} reads as {got!r}, fixture says '
                       f'{(c["expect"], c["error"])!r}')
    backslash = chr(92)
    stated = {c["value"][c["value"].index(backslash) + 1] for c in cases
              if not c["error"] and c["value"][:1] == '"' and backslash in c["value"]}
    bad += [f"the fixture carries no scalar case for the YAML escape backslash-{e!r}, which _scalar decodes"
            for e in (*W._YAML_ESCAPES, "x", "u", "U") if e not in stated]
    bad += [f"the fixture's scalars carry no case reporting {code}"
            for code in ("unknown-escape", "unterminated-quote") if not any(c["error"] == code for c in cases)]
    return bad


def case_00_the_reader_agrees_with_its_fixture(expect):
    # ⛔ THE FIXTURE ASSERTS ITS OWN POPULATION. A run that measured no wrapped list, no malformed value and no non-note
    # file would be green about nothing at all: precisely the state `work.py check` was in while `kb/Work/PB390` claimed
    # four rows the gate could not see.
    fixture = _fixture()
    answers = _answers(fixture)
    expect.equal(f"parse_frontmatter answers every case of {FIXTURE_REL} as the fixture says", _case_findings(fixture, answers), [])
    covered = {
        "a list wrapped across lines": any(_wrapped_list(c["text"]) for c in fixture["cases"]),
        "a malformed value reported as an error": any(a["errors"] for a in answers),
        "a file that is not a note": any(not a["note"] for a in answers),
        "CRLF line endings": any("\r\n" in c["text"] for c in fixture["cases"]),
        "a file named like a note whose frontmatter does not parse": any(a["refusal"] for a in answers),
    }
    expect.equal("the fixture carries a case for each shape that once broke a reader", [w for w, ok in covered.items() if not ok], [])
    # EVERY STATUS OF THE VOCABULARY, so the `terminal` answer is measured on each one (kb/Work PB3196): a status added
    # to STATUSES or TERMINAL_STATUSES stops this until a case states what it is.
    expect.equal("the fixture carries a note of every status (the `terminal` answer is measured on each)",
                 sorted((W.STATUSES | set(W.TERMINAL_STATUSES)) - {a["status"] for a in answers if a["note"]}), [])
    # EVERY FIELD, on every case: a field the fixture leaves unstated is a field the reader may change unseen.
    expect.equal("every case states every field the view reports",
                 [(c["name"], k) for c, a in zip(fixture["cases"], answers) for k in a if k != "name" and k not in c["expect"]], [])
    expect("enough cases to be measuring the reader (8 or more)", len(fixture["cases"]) >= 8, f"{len(fixture['cases'])} case(s)")


def case_01_the_fixture_comparisons_can_fail(expect):
    # FALSIFICATION: the comparison must be able to fail, or "no disagreement" is a statement about nothing.
    corrupted = _fixture()
    corrupted["cases"][0]["expect"]["id"] = corrupted["cases"][0]["expect"].get("id", "") + "-X"
    expect("a fixture whose expected id is WRONG is a finding", bool(_case_findings(corrupted, _answers(corrupted))))
    corrupted["scalars"][0]["expect"] += "-X"
    expect("a fixture whose expected scalar is WRONG is a finding", bool(_scalar_findings(corrupted)))


def case_02_the_scalar_reader_agrees_with_its_fixture(expect):
    expect.equal("_scalar reads each fixture scalar, every escape it decodes has a case, and both error codes do",
                 _scalar_findings(_fixture()), [])


def case_03_a_fixture_directory_read_as_a_register(expect):
    # The fixture's files written into a directory and read as a register (kb/Work PB3654): `register_problems` and `load`
    # refuse each file named like a note that does not parse, `check --work` counts the DIRECTORY's notes (it once checked
    # `kb/Work` whatever `--work` said, so the seam could not witness a refusal), and an index is skipped.
    fixture = _fixture()
    everything = scratch_dir("work-register-cases-")
    refused = {}
    for c in fixture["cases"]:
        (everything / c["file"]).write_bytes(c["text"].encode("utf-8"))
        if c["expect"]["refusal"]:
            refused[c["file"]] = c["expect"]["refusal"]
    _, problems = W.register_problems(everything)
    expect.equal("register_problems over the fixture directory reports every refusal of the fixture",
                 [why for why in refused.values() if why not in problems], [])
    expect.equal("... and no unreadable note the fixture does not refuse",
                 [p for p in problems if "unreadable as a work note" in p and p not in refused.values()], [])
    try:
        W.load(everything)
        stopped = None
    except SystemExit as stop:
        stopped = str(stop)
    expect("load() stops on a directory holding an unreadable note, naming each", stopped is not None
           and all(name in stopped for name in refused), f"stopped with {stopped!r}")
    good = scratch_dir("work-register-good-")
    (good / "PB1.md").write_bytes(NL.join(["---", "id: PB1", "kind: defect", "status: landed", "area: x", "closes_rows: []",
                                           'closes_rows_reason: "none"', "---", "body", ""]).encode("utf-8"))
    (good / "README.md").write_bytes(b"not a note")
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        code = W.check(good)
    expect.equal("check --work over one good note and an index passes and counts the directory's one note",
                 (code, "1 work items" in out.getvalue()), (0, True))
    for name in refused:
        (good / name).write_bytes(next(c["text"] for c in fixture["cases"] if c["file"] == name).encode("utf-8"))
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        code = W.check(good)
    expect("check --work over a directory holding unreadable notes fails, naming each",
           code != 0 and all(name in out.getvalue() for name in refused), f"{code}: {out.getvalue().strip()!r}")


# ── the `upstream` kind ─────────────────────────────────────────────────────────────────────────────────────────────

def _upstream_note(**changes: str | None) -> bytes:
    """One well-formed `upstream` note; `changes` replaces a frontmatter line by key (a `None` drops it) or a body
    section's text by `section_<index into UPSTREAM_SECTIONS>` (a `None` drops the whole section)."""
    fm = {"id": "PB9100", "kind": "upstream", "status": "open", "area": "tooling/upstream", "wrong_answer": "true",
          "process_only": "true", "spec_refs": '["14.9.25"]', "upstream_project": "GnuCOBOL", "upstream_version": "3.2.0",
          "upstream_class": "wrong-output", "upstream_status": "unreported", "upstream_edition": "2014",
          "upstream_ref": "", "cluster": "[]", "blocked_by": "[]", "inventory_rows": "[]", "editions": "[]"}
    fm = {k: v for k, v in {**fm, **{k: v for k, v in changes.items() if not k.startswith("section_")}}.items()
          if v is not None}
    lines = ["---", *(f"{k}: {v}" for k, v in fm.items()), "---", "", "# PB9100 — a record", ""]
    fence = "`" * 3
    for i, name in enumerate(W.UPSTREAM_SECTIONS):
        default = {0: f"{fence}{NL}MOVE A TO B.{NL}{fence}", 7: f"**Title:** a title{NL}{NL}stated"}.get(i, "stated")
        text = changes.get(f"section_{i}", default)
        if text is not None:
            lines += [f"## {name}", "", text, ""]
    return NL.join(lines).encode("utf-8")


def case_04_an_upstream_record_is_held_to_its_bar(expect):
    # The `upstream` kind held to its own rules (kb/Work PB3838): a well-formed record passes `register_problems`, and each
    # way of failing the bar is reported by name.
    fence = "`" * 3
    long_block = fence + NL + NL.join("X" for _ in range(W.UPSTREAM_MAX_FENCE_LINES + 1)) + NL + fence
    cases: list[tuple[str, dict, str]] = [
        ("a well-formed record", {}, ""),
        ("process_only false", {"process_only": "false"}, "process_only: true"),
        ("a cluster", {"cluster": '["PB3838"]'}, "names `cluster`"),
        ("a claimed inventory row", {"inventory_rows": '["AR-1.1-1"]'}, "names `inventory_rows`"),
        ("a frontier model", {"frontier_model": "mythos"}, "names `frontier_model`"),
        ("no version", {"upstream_version": None}, "no `upstream_version`"),
        ("an unknown class", {"upstream_class": "bug"}, "upstream_class 'bug'"),
        ("an unknown status", {"upstream_status": "filed"}, "upstream_status 'filed'"),
        ("a 2023-only behaviour filed as a bug", {"upstream_edition": "2023"}, "feature request"),
        ("a 2023-only behaviour filed as a feature", {"upstream_edition": "2023", "upstream_class": "feature-missing"}, ""),
        ("no citation", {"spec_refs": "[]"}, "has `spec_refs`"),
        ("reported with no ticket", {"upstream_status": "reported"}, "no `upstream_ref`"),
        ("reported with a ticket", {"upstream_status": "reported", "upstream_ref": "https://example.test/t/1"}, ""),
        ("unreported with a ticket", {"upstream_ref": "https://example.test/t/1"}, "is `unreported` but carries"),
        ("declined upstream but open here", {"upstream_status": "declined", "upstream_ref": "t/2"}, "terminal exactly when"),
        ("declined upstream and retired here", {"upstream_status": "declined", "upstream_ref": "t/2", "status": "retired"}, ""),
        ("withdrawn but open here", {"upstream_status": "withdrawn"}, "terminal exactly when"),
        ("withdrawn and retired here", {"upstream_status": "withdrawn", "status": "retired"}, ""),
        ("withdrawn with a ticket", {"upstream_status": "withdrawn", "upstream_ref": "t/3", "status": "retired"},
         "is `withdrawn` but carries"),
        ("no observed result", {"section_2": None}, "no `## Observed result` section"),
        ("an empty fix-site section", {"section_5": ""}, "empty `## Fix site` section"),
        ("a pasted testsuite-sized program", {"section_0": long_block}, "fenced block of"),
        ("a filer's instruction ahead of the draft's title",
         {"section_7": f"**Corrected before filing:** delete X.{NL}{NL}**Title:** t{NL}{NL}body"},
         "does not open with its one-line `**Title:**"),
        ("a draft title wrapped onto a second line", {"section_7": f"**Title:** a long{NL}title{NL}{NL}b"},
         "does not open with its one-line `**Title:**"),
        ("a draft with no title", {"section_7": "body only"}, "does not open with its one-line `**Title:**"),
        ("a correction stated beside the text it contradicts",
         {"section_2": "Corrected: X. Where it differs, this paragraph governs."}, "apply the correction in place"),
        # ⛔ THE FENCE SYNTAX THE GUARD MUST SEE (kb/Work PB3938): every way Markdown fences a block, so a program can
        # hide in none of them, and the `## ` lines inside one are never read as the record's sections.
        ("a short repro in a tilde fence", {"section_0": f"~~~{NL}MOVE A TO B.{NL}~~~"}, ""),
        ("a testsuite-sized program in a tilde fence", {"section_0": long_block.replace("`", "~")}, "fenced block of"),
        ("an unclosed fence", {"section_7": f"**Title:** t{NL}{NL}{fence}{NL}MOVE A TO B."}, "has an unclosed fence"),
        ("a testsuite-sized program in an unclosed fence",
         {"section_7": f"**Title:** t{NL}{NL}{long_block.removesuffix(fence)}"}, "fenced block of"),
        ("a tilde block holding a section heading",
         {"section_0": f"~~~{NL}## Observed result{NL}X{NL}~~~", "section_2": None}, "no `## Observed result` section"),
        ("a long fence not closed by a shorter run", {"section_0": f"`{long_block}`".replace(f"{NL}X{NL}", f"{NL}{fence}{NL}", 1)},
         "fenced block of"),
        ("a tilde fence not closed by backticks",
         {"section_0": f"~~~{NL}{fence}{NL}{NL.join('X' for _ in range(W.UPSTREAM_MAX_FENCE_LINES))}{NL}~~~"},
         "fenced block of"),
        ("a backtick run whose info string holds a backtick is no fence", {"section_1": f"{fence}a`b{fence}"}, ""),
    ]
    root = scratch_dir("work-upstream-cases-")
    for n, (name, changes, want) in enumerate(cases):
        d = root / str(n)
        d.mkdir()
        (d / "PB9100.md").write_bytes(_upstream_note(**changes))
        _, problems = W.register_problems(d)
        if want:
            expect(f"upstream, {name}: a problem names {want!r}", any(want in p for p in problems), str(problems))
        else:
            expect.equal(f"upstream, {name}: no problem", problems, [])


def case_05_an_upstream_record_is_outside_our_work(expect):
    # the record is outside `actionable` and every cluster, an upstream_* field on another kind is refused, and the views
    # list it under `upstream` and under no view of our work, whether or not it sets a harm flag (kb/Work PB3939, PB3799)
    d = scratch_dir("work-upstream-views-")
    (d / "PB9100.md").write_bytes(_upstream_note())
    items = W.load(d)
    expect.equal("an upstream record that sets a harm flag is not actionable (it describes another project's user)",
                 W.actionable(items), [])
    expect.equal("an upstream record is a member of no cluster", W.cluster_order(items, "PB3838")["named"], 0)
    (d / "PB9101.md").write_bytes(_upstream_note(id="PB9101", kind="defect"))
    _, problems = W.register_problems(d)
    expect("an upstream_* field on a defect note is refused",
           any("upstream_project on a note of kind 'defect'" in p for p in problems), str(problems))
    (d / "PB9101.md").unlink()
    (d / "PB9102.md").write_bytes(_upstream_note(id="PB9102", wrong_answer=None))
    items = W.load(d)
    expect.equal("two upstream records, one harmed and one not, are listed by `upstream` and by no view of our work",
                 {v: [i["id"] for i in W.view_notes(items, v)] for v in ("upstream", "unharmed", "blocked", "landed")},
                 {"upstream": ["PB9100", "PB9102"], "unharmed": [], "blocked": [], "landed": []})


# ── addenda ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def case_06_a_note_reads_as_its_file_and_its_addenda(expect):
    # kb/Work PB5110: a note reads as its file followed by its addenda in name order; its frontmatter stays its file's; a
    # fold (`fold_text`, what compact_notes.py writes) reads back as the same note; and a misnamed addendum, an addendum
    # with frontmatter and a directory with no note are each refused, never skipped.
    head = NL.join(["---", "id: PB9001", "kind: decision", "status: open", "area: tooling", "---", "# PB9001", "", "body", ""])
    root = scratch_dir("work-addenda-")
    (root / "PB9001.md").write_text(head, encoding="utf-8", newline=NL)
    (root / "PB9002.md").write_text(head.replace("PB9001", "PB9002"), encoding="utf-8", newline=NL)
    (root / "PB9001").mkdir()
    (root / "PB9001" / "2026-11-11-second.md").write_text("**Second.** two" + NL, encoding="utf-8", newline=NL)
    (root / "PB9001" / "2026-11-10-first.md").write_text("**First.** one" + NL, encoding="utf-8", newline=NL)
    items, refused = W.read_notes(root)
    by_id = {it["id"]: it for it in items}
    body = by_id["PB9001"]["_body"]
    expect("addenda are read after the note, in name order",
           not refused and body.index("body") < body.index("**First.**") < body.index("**Second.**"), f"{refused} {body!r}")
    expect("a note's frontmatter, and an addendum-free note's body, are unchanged by reading addenda",
           by_id["PB9001"].get("status") == "open"
           and by_id["PB9002"]["_body"] == by_id["PB9002"]["_body"].rstrip(NL) + NL)
    text9001 = W.note_text("PB9001", root) or ""
    expect("note_text (the body reader of the planner, the landing check and the member index) is the register's note",
           text9001.startswith("---") and text9001.index("body") < text9001.index("**First.**") < text9001.index("**Second.**")
           and W.note_text("PB9002", root) == head.replace("PB9001", "PB9002") and W.note_text("PB9009", root) is None)
    folded = W.fold_text((root / "PB9001.md").read_text(encoding="utf-8"), [t for _, t in W.read_addenda(root, "PB9001.md")[0]])
    (root / "PB9001.md").write_text(folded, encoding="utf-8", newline=NL)
    for q in (root / "PB9001").iterdir():
        q.unlink()
    (root / "PB9001").rmdir()
    again, refused2 = W.read_notes(root)
    expect("folding the addenda into the note leaves what the reader returns for the note unchanged",
           not refused2 and {it["id"]: it["_body"] for it in again}["PB9001"] == body and not any("_addenda" in it for it in again))
    (root / "PB9002").mkdir()
    for name, text, want in (("notes.md", "x", "not an addendum"), ("2026-11-10-fm.md", "---" + NL + "a: b" + NL + "---", "no frontmatter")):
        (root / "PB9002" / name).write_text(text, encoding="utf-8", newline=NL)
        expect(f"the addendum {name!r} is refused ({want})", any(want in r for r in W.read_notes(root)[1]))
        (root / "PB9002" / name).unlink()
    (root / "PB9002").rmdir()
    (root / "PB9003").mkdir()
    (root / "PB9003" / "2026-11-10-x.md").write_text("x", encoding="utf-8", newline=NL)
    expect("a directory of addenda with no note beside it is refused",
           any("PB9003/" in r and "does not exist" in r for r in W.read_notes(root)[1]))


# ── the views, `next` and process_only ───────────────────────────────────────────────────────────────────────────────

def case_07_each_view_lists_the_notes_of_its_shape(expect):
    # The register's views (VIEWS, `next`) over a scratch register that holds one note of each shape (kb/Work PB3799): which
    # notes each lists is stated here, so a change to a predicate, to HARM_FLAGS or to TERMINAL_STATUSES that moves a note
    # in or out of a view fails by name. `next` and `unharmed` are measured as complements over every harm flag, so a harm
    # class added to HARM_LABELS is covered without an edit here.
    def note(nid: str, **fm: str) -> bytes:
        base = {"id": nid, "kind": "defect", "status": "open", "severity": "MINOR", "area": "x", "process_only": "false",
                "title": f'"{nid} — a note"'}
        return NL.join(["---", *(f"{k}: {v}" for k, v in {**base, **fm}.items()), "---", "", f"# {nid}", ""]).encode("utf-8")

    notes = {
        "PB9001": {"wrong_answer": "true", "severity": "MAJOR"},
        "PB9002": {"process_only": "true"},
        "PB9003": {"crashes": "true", "blocked_by": "[PB9002]"},
        "PB9004": {"status": "owner", "rejects_legal_source": "true"},
        "PB9005": {"status": "landed", "closes_rows": "[]", "closes_rows_reason": '"none"'},
        "PB9006": {"status": "retired", "wrong_answer": "true"},
        "PB9007": {"kind": "analysis", "process_only": "true"},
        "PB9008": {"kind": "analysis", "status": "landed", "process_only": "true"},
        "PB9009": {"kind": "upstream", "process_only": "true", "upstream_status": "unreported"},
        "PB9010": {"blocked_by": "[PB9005]", "under_rejects": "true"},
        "PB9011": {"silent": "true"},
        # a note held `blocked` passes its harm to its ready prerequisite; one held `owner` does not (PB4769)
        "PB9012": {"status": "blocked", "wrong_answer": "true", "blocked_by": "[PB9013]"},
        "PB9013": {"process_only": "true"},
        "PB9014": {"status": "owner", "wrong_answer": "true", "blocked_by": "[PB9015]"},
        "PB9015": {"process_only": "true"},
        # an owner-held link in a chain ends the walk: PB9017 is below undecided work (review R1-3)
        "PB9016": {"wrong_answer": "true", "blocked_by": "[PB9018]"},
        "PB9018": {"status": "owner", "process_only": "true", "blocked_by": "[PB9017]"},
        "PB9017": {"process_only": "true"},
    }
    # one open note per harm flag the register ranks, each of which `next` must list and `unharmed` must not
    notes.update({f"PB92{n:02d}": {flag: "true"} for n, flag in enumerate(W.HARM_FLAGS)})
    harmed = [f"PB92{n:02d}" for n in range(len(W.HARM_FLAGS))]
    want = {
        # PB9002 is process_only with no harm of its own, but PB9003 (a crash) waits on it: it owes that harm (PB4769)
        "next": sorted(["PB9001", "PB9002", "PB9010", "PB9013", *harmed]),
        "blocked": ["PB9003", "PB9004", "PB9012", "PB9014", "PB9016", "PB9018"],
        "unharmed": ["PB9007", "PB9011", "PB9015", "PB9017", "PB9018"],
        "analyses": ["PB9007", "PB9008"],
        "landed": ["PB9005", "PB9006"],
        "upstream": ["PB9009"],
    }
    d = scratch_dir("work-views-")
    for nid, fm in notes.items():
        (d / f"{nid}.md").write_bytes(note(nid, **fm))
    items = W.load(d)
    expect.equal("every view the register has is stated here", sorted(W.VIEWS), sorted(v for v in want if v != "next"))
    expect.equal("each view lists the notes of its shape",
                 {"next": sorted(i["id"] for i in W.actionable(items)), **{v: sorted(i["id"] for i in W.view_notes(items, v)) for v in W.VIEWS}},
                 want)
    expect.equal("HARM_LABELS labels every flag HARM_FLAGS names",
                 [f for f in W.HARM_FLAGS if f not in W.HARM_LABELS or not W.harm_label({f: True})], [])


def case_08_process_only_on_compiler_sites_and_the_order_of_next(expect):
    # `process_only` on a note whose sites are all compiler code (kb/Work PB3836)
    src = "**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs` (`DataBinder.Bind`)"
    mixed = src + ", `tests/WiseOwl.Cobol.Tests.Unit/XTests.cs` (`XTests.Y`)"

    def note(body: str, **fm) -> dict:
        return {"_file": "PB9.md", "id": "PB9", "kind": "defect", "status": "open", "process_only": True, "_body": body, **fm}

    for name, it, want in (
            ("process_only on a src-only note without a reason", note(src), True),
            ("process_only_reason states why", note(src, process_only_reason="a drift test guards the site"), False),
            ("a blank reason is none", note(src, process_only_reason="  "), True),
            ("a restructuring wave kind states it", note(src + NL + "**Wave kind:** unify."), False),
            ("the fix-lane wave kind does not", note(src + NL + f"**Wave kind:** {W.FIX_LANE_WAVE}."), True),
            ("a note that also sites a test", note(mixed), False),
            ("a note with no site block", note("no sites"), False),
            ("a landed note", note(src, status="landed"), False),
            ("an upstream record", note(src, kind="upstream"), False),
            ("a harm flag instead of process_only", note(src, process_only=False, misleads_diagnosis=True), False)):
        got = W.process_only_problems(it)
        expect.equal(f"process_only_problems, {name}: {'a problem' if want else 'no problem'}", bool(got), want)
        if got:
            expect(f"... and it names the rule (PB3836) and `process_only_reason:`",
                   "PB3836" in got[0] and "process_only_reason:" in got[0], got[0])
    # next ranks within a severity by what the harm does, a misleading message last, then by id
    expect.equal("next: severity, then the harm's rank, then the id",
                 [i["id"] for i in W.actionable([
                     {"id": "PB1", "status": "open", "severity": "MINOR", "misleads_diagnosis": True},
                     {"id": "PB2", "status": "open", "severity": "MINOR", "under_rejects": True},
                     {"id": "PB3", "status": "open", "severity": "MINOR", "wrong_answer": True},
                     {"id": "PB4", "status": "open", "severity": "MAJOR", "misleads_diagnosis": True}])],
                 ["PB4", "PB3", "PB2", "PB1"])


def case_09_an_open_note_of_a_checked_kind_needs_a_harm_flag(expect):
    # Every kind in HARM_CHECKED_KINDS is refused open with no harm flag and no `process_only` (kb/Work PB4949), a harm flag
    # or `process_only: true` answers the refusal, and a decision is not held to it.
    def note(pb: str, kind: str, extra: str = "") -> bytes:
        return (f"---{NL}id: {pb}{NL}kind: {kind}{NL}status: open{NL}area: x{NL}closes_rows: []{NL}"
                f'closes_rows_reason: "none"{NL}{extra}---{NL}body{NL}').encode("utf-8")

    cases = [("PB9201", "defect", "", True), ("PB9202", "analysis", "", True),
             ("PB9203", "analysis", f"wrong_answer: true{NL}", False),
             ("PB9204", "analysis", f"process_only: true{NL}", False), ("PB9205", "decision", "", False)]
    d = scratch_dir("work-unflagged-")
    for pb, kind, extra, _ in cases:
        (d / f"{pb}.md").write_bytes(note(pb, kind, extra))
    _, problems = W.register_problems(d)
    expect.equal("an open defect or analysis with no flag is refused as unflagged; a flag, process_only or a decision is not",
                 sorted(pb for pb, *_ in cases if any(f"{pb}.md" in p and "with no harm flag set" in p for p in problems)),
                 ["PB9201", "PB9202"])


# ── the harm vocabulary, the frontier model and the running limit ────────────────────────────────────────────────────

def _harm_vocabulary_findings(repo) -> list[str]:
    """The harm vocabulary written outside work.py agrees with HARM_LABELS (kb/Work PB3836): `.agent-fleet.json` weighs
    exactly HARM_FLAGS, in rank order, each class strictly below the one before it (the planner and the branch survey rank
    on the weights, clusters.harm_weights); and no template that restates the vocabulary leaves a class out, so a harm
    class added to HARM_LABELS stops this until each restatement names it."""
    bad = []
    fleet = json.loads((repo / ".agent-fleet.json").read_text(encoding="utf-8"))
    weights = {k: int(v) for k, v in (p.split("=") for p in fleet["harm"].split(","))}
    if list(weights) != list(W.HARM_FLAGS):
        bad.append(f".agent-fleet.json `harm` weighs {list(weights)}, not HARM_FLAGS {list(W.HARM_FLAGS)} in rank order")
    elif any(a <= b for a, b in zip(weights.values(), list(weights.values())[1:])):
        bad.append(f".agent-fleet.json `harm` weights {list(weights.values())} do not fall strictly with the rank of "
                   f"{list(W.HARM_FLAGS)}: a class must weigh less than the one ranked before it")
    ranked = {flag.replace("_", "-"): flag for flag in W.HARM_FLAGS}      # `silent` qualifies a harm and may be left out
    for root in HARM_RESTATEMENT_ROOTS:
        for path in sorted((repo / root).rglob("*")):
            if not path.is_file() or path.suffix not in (".md", ".js", ".json", ".txt"):
                continue
            text = path.read_text(encoding="utf-8", errors="replace")
            named = [word for word, flag in ranked.items() if word in text or flag in text]
            if 2 <= len(named) < len(ranked):   # two classes written out is a restatement of the list
                bad.append(f"{path.relative_to(repo).as_posix()} restates the harm vocabulary but leaves out "
                           f"{[word for word in ranked if word not in named]}")
    return bad


def case_10_the_tree_s_harm_vocabulary_agrees_with_harm_flags(expect):
    # the one case whose subject is the tree itself: the files that restate the vocabulary
    expect.equal("the tree's .agent-fleet.json weights and every template restating the harm vocabulary agree with HARM_FLAGS",
                 _harm_vocabulary_findings(W.REPO), [])


def case_11_the_harm_vocabulary_check_can_fail(expect):
    # FALSIFICATION: a repository whose weights are flat and whose brief names two classes of the vocabulary is refused on
    # both counts, and one that is consistent is not.
    root = scratch_dir("work-harm-")
    (root / HARM_RESTATEMENT_ROOTS[0]).mkdir(parents=True)
    (root / HARM_RESTATEMENT_ROOTS[0] / "brief.md").write_text("wrong_answer / crashes", encoding="utf-8", newline=NL)
    weights = lambda ws: json.dumps({"harm": ",".join(f"{f}={w}" for f, w in zip(W.HARM_FLAGS, ws))})   # noqa: E731
    (root / ".agent-fleet.json").write_text(weights([1] * len(W.HARM_FLAGS)), encoding="utf-8", newline=NL)
    found = _harm_vocabulary_findings(root)
    expect("flat weights and a brief that leaves classes out are each refused",
           len(found) == 2 and "do not fall strictly" in found[0] and "brief.md restates" in found[1], str(found))
    (root / ".agent-fleet.json").write_text(weights(range(len(W.HARM_FLAGS), 0, -1)), encoding="utf-8", newline=NL)
    (root / HARM_RESTATEMENT_ROOTS[0] / "brief.md").write_text(" ".join(W.HARM_FLAGS), encoding="utf-8", newline=NL)
    expect.equal("a consistent repository is not refused", _harm_vocabulary_findings(root), [])


def case_12_frontier_model_names_a_frontier_family_or_is_absent(expect):
    # kb/Work PB3941: every family is accepted, and a value that is none (the alias `opus`, a dated id, a LIST such as
    # `[mythos]`, which must be refused by name rather than raise a TypeError that stops `work.py check`) is refused, so a
    # typo can never leave a frontier note unheld.
    expect("a frontier family is configured", bool(W.frontier_models()))
    expect.equal("every frontier family is accepted",
                 [m for m in sorted(W.frontier_models()) if W.frontier_model_shape({"_file": "PB1.md", "frontier_model": m})], [])
    expect.equal("a value that is no frontier family is refused",
                 [m for m in ("opus", "claude-mythos-5-1", ["mythos"], 5)
                  if not W.frontier_model_shape({"_file": "PB1.md", "frontier_model": m})], [])
    expect.equal("a note with no field is not refused", W.frontier_model_shape({"_file": "PB1.md"}), [])


def case_13_running_limit_min_belongs_to_a_lead_note(expect):
    # kb/Work PB4944: a positive whole number on a cluster's lead note, refused anywhere else, and a campaign's limit is the
    # largest its notes' lead notes declare.
    field = W.RUNNING_LIMIT_FIELD
    lead = {"_file": "PB10.md", "id": "PB10", "cluster": ["PB10"], field: "150"}
    member = {"_file": "PB11.md", "id": "PB11", "cluster": ["PB10"]}
    expect.equal("a lead note's limit is accepted", W.running_limit_problems([lead, member]), [])
    expect.equal("a limit on a note that leads no cluster is refused",
                 len(W.running_limit_problems([{"_file": "PB12.md", "id": "PB12", "cluster": [], field: "150"}])), 1)
    expect.equal("a limit that is no positive whole number is refused",
                 [v for v in ("0", "-5", "ninety", "1.5", "") if len(W.running_limit_problems([{**lead, field: v}, member])) != 1], [])
    expect.equal("campaign_running_limit reads the lead note's limit and invents none",
                 (W.campaign_running_limit([lead, member], ["PB11"]), W.campaign_running_limit([lead, member], ["PB99"]),
                  W.campaign_running_limit([{**lead, field: "x"}, member], ["PB11"])), (150, None, None))
    other = {"_file": "PB9.md", "id": "PB9", "cluster": ["PB10", "PB9"]}
    expect.equal("note_campaigns names each campaign of the notes once, in id order",
                 W.note_campaigns([lead, member, other], ["PB11", "PB9", "PB99"]), ["PB9", "PB10"])


# ── the topology: blocked_by, clusters and tiers ─────────────────────────────────────────────────────────────────────

def _topo() -> list[dict]:
    """A register of every topology shape: a landed blocker, an unknown one, two cycles (one in a cluster), a stored
    `blocked:` flag, a scalar cluster, holds by status."""
    return [{"_file": "T1.md", "id": "T1", "status": "open", "blocked_by": ["T9"], "cluster": []},
            {"_file": "T9.md", "id": "T9", "status": "landed", "blocked_by": [], "cluster": []},
            {"_file": "T2.md", "id": "T2", "status": "open", "blocked_by": ["nobody"], "cluster": []},
            {"_file": "T3.md", "id": "T3", "status": "open", "blocked_by": ["T4"], "cluster": ["T3"]},
            {"_file": "T4.md", "id": "T4", "status": "open", "blocked_by": ["T3"], "cluster": "T3"},
            {"_file": "T5.md", "id": "T5", "status": "open", "blocked_by": ["T3"], "cluster": []},
            {"_file": "T6.md", "id": "T6", "status": "open", "blocked_by": ["T7"], "cluster": ["C"]},
            {"_file": "T7.md", "id": "T7", "status": "open", "blocked_by": ["T6"], "cluster": ["C"]},
            {"_file": "T8.md", "id": "T8", "status": "open", "blocked": False, "blocked_by": [], "cluster": []},
            {"_file": "T10.md", "id": "T10", "status": "owner", "blocked_by": [], "cluster": []},
            {"_file": "T11.md", "id": "T11", "status": "blocked", "blocked_by": ["T9"], "cluster": []},
            {"_file": "T12.md", "id": "T12", "status": "landed", "blocked_by": ["T3"], "cluster": []}]


def case_14_whether_a_note_waits_is_derived_from_its_blockers(expect):
    # kb/Work PB3389: work.waiting_on is the one reading, so a blocker that lands changes the dependent's answer with no edit
    # to the dependent
    topo = _topo()
    by_id = {i["id"]: i for i in topo}
    expect.equal("a blocker that landed leaves its dependent unblocked, with no edit to the dependent",
                 W.waiting_on(by_id["T1"], by_id), [])
    expect.equal("an open blocker holds its dependent, and so does a blocker that is no note",
                 [W.waiting_on(by_id[n], by_id) for n in ("T5", "T2", "T3")], [["T3"], ["nobody"], ["T4"]])
    expect.equal("a hold that is no note is a status: owner or blocked, whatever the blockers",
                 [W.waiting_on(by_id[n], by_id) for n in ("T10", "T11", "T8")], [["status: owner"], ["status: blocked"], []])
    expect.equal("the planner's cluster view waits on what waiting_on says",
                 [(r["id"], r["waiting_on"]) for r in W.cluster_order(topo + [dict(by_id["T5"], id="T13", cluster=["Q"]),
                                                                            dict(by_id["T10"], id="T14", cluster=["Q"])], "Q")["notes"]],
                 [("T13", ["T3"]), ("T14", ["status: owner"])])
    expect.equal("a cycle never reads as ready", [(r["id"], r["depth"], r["ready"]) for r in W.cluster_order(topo, "C")["notes"]],
                 [("T6", None, False), ("T7", None, False)])
    expect.equal("a scalar cluster is no member (no substring match)", [r["id"] for r in W.cluster_order(topo, "T3")["notes"]], ["T3"])


def case_15_topology_problems_names_each_shape_once(expect):
    # `check` refuses a stored `blocked:` flag, which could only go stale (kb/Work PB3389), an unknown blocker, a cycle among
    # open notes and a cluster that is no list; and nothing else of this register
    rules = (("a stored flag", "`blocked:` is stored"), ("an unknown blocker", "blocked_by names 'nobody'"),
             ("a cycle", "cycle among open notes: T3 -> T4 -> T3"), ("a cycle", "cycle among open notes: T6 -> T7 -> T6"),
             ("a scalar cluster", "cluster must be a list"))
    probs = W.topology_problems(_topo())
    expect.equal("exactly five problems: a stored flag (T8), an unknown blocker (T2), two cycles, a scalar cluster (T4)",
                 sorted((s.split(":", 1)[0] if s[:1] == "T" else "", next((r for r, words in rules if words in s), "UNRECOGNIZED: " + s))
                        for s in probs),
                 sorted([("T8.md", "a stored flag"), ("T2.md", "an unknown blocker"), ("T4.md", "a scalar cluster"),
                         *[(f, "a cycle") for f in (_cycle_file(probs, "T3 -> T4"), _cycle_file(probs, "T6 -> T7"))]]))


def _cycle_file(probs: list[str], ring: str) -> str:
    """The file prefix `topology_problems` gives the cycle through `ring` (the cycle's message names its notes)."""
    return next((s.split(":", 1)[0] if s[:1] == "T" else "" for s in probs if ring in s), "NO CYCLE " + ring)


def _tiers() -> list[dict]:
    """A note per tier shape (kb/Work PB3423): well-formed, two campaigns, zero, not a number, a scalar, a campaign named
    twice, a campaign the note is not in, an entry with no tier."""
    return [{"_file": f"U{k}.md", "id": f"U{k}", "status": "open", "blocked_by": [], "cluster": cl, "tier": t}
            for k, (t, cl) in enumerate([(["C:1"], ["C"]), (["C:12", "D:2"], ["C", "D"]), (["C:0"], ["C"]),
                                          (["C:x"], ["C"]), ("1", ["C"]), (["C:1", "C:2"], ["C"]), (["D:2"], ["C"]),
                                          (["C"], ["C"])])]


def case_16_a_tier_is_a_list_of_campaign_entries(expect):
    # a tier is a list of CAMPAIGN:N entries naming the note's own campaigns once; anything else is reported, and only the
    # well-formed entries are read
    tiers = _tiers()
    expect.equal("a malformed entry, a campaign named twice, or a campaign the note is not in, is a problem",
                 sorted(s.split(":")[0] for s in W.topology_problems(tiers)), ["U2.md", "U3.md", "U4.md", "U5.md", "U6.md", "U7.md"])
    expect.equal("tier_of reads the well-formed entry for that campaign only",
                 [W.tier_of(i, "C") for i in tiers] + [W.tier_of(tiers[1], "D"), W.tier_of(tiers[6], "D")],
                 [1, 12, None, None, None, 1, None, None, 2, 2])


# ── the site rules ──────────────────────────────────────────────────────────────────────────────────────────────────

@functools.cache
def _tree() -> ScratchRepo:
    """A scratch repository holding the files the site and R3 cases name: a Python file and a PowerShell file with
    members, a module-level script, two docs, a sweep's targets, a code file and its grammar, and (untracked) a file a
    glob would match and a local build output, which the committed-tree rules must not see."""
    repo = ScratchRepo.init(scratch_dir("work-tree-") / "repo")
    repo.commit("tree", {
        "scripts/spec/work.py": NL.join(["def load():", "    pass", "", "", "def sites_problems():", "    pass", "", "",
                                         "class NoteSites:", "    def fix_lane(self):", "        pass", ""]),
        "scripts/spec/work_inventory.py": "def live_problems():" + NL + "    pass" + NL,
        "scripts/hooks/dispatch_guard.py": "import sys" + NL + "print(sys.argv)" + NL,
        "scripts/orchestrator/orchestrate.ps1": "function Take-Lock {" + NL + "    param($Name)" + NL + "}" + NL,
        ".claude/skills/workstream/check_practices.py": "def check():" + NL + "    pass" + NL,
        "docs/DOC_INDEX.md": "# Index" + NL, "docs/DESIGN.md": "# Design" + NL + NL + "### 0.5 Deep-dives" + NL, "docs/A.md": "# A" + NL,
        "src/P/Binding/Driver.cs": "class Driver { }" + NL,
        "src/P/Grammar/Parser.g4": "grammar Parser;" + NL,
    })
    repo.write({"scripts/spec/work_untracked.py": "x = 1" + NL, "src/P/obj/project.assets.json": "{}" + NL,
                "src/P/Generated/Parser.cs": "class Parser { }" + NL})
    return repo


def case_17_a_campaign_note_from_the_cutover_declares_its_sites(expect):
    # an open note filed into a cluster from work.SITES_REQUIRED_FROM on declares a site block (kb/Work PB3263): a list of
    # paths, or `**Sites:** none (why)`; an older note, a decision, a terminal note and a note in no cluster are exempt
    new = W.SITES_REQUIRED_FROM
    sited = [{"_file": f"PB{n}.md", "id": f"PB{n}", "kind": k, "status": s, "cluster": cl, "_body": b}
             for n, k, s, cl, b in [
                 (new, "defect", "open", ["C"], "Prose names `src/A.cs` only."),
                 (new + 1, "defect", "open", ["C"], "**Sites:**\n- `src/A.cs`\n"),
                 (new + 2, "analysis", "half", ["C"], "**Sites:** none (it changes no code file)\n"),
                 (new + 3, "decision", "open", ["C"], "An owner question."),
                 (new + 4, "defect", "landed", ["C"], "Landed, no sites."),
                 (new + 5, "defect", "open", [], "In no cluster."),
                 (new - 1, "defect", "open", ["C"], "Filed before the rule."),
                 (new + 6, "analysis", "owner", ["C"], "Held by the owner, no sites.")]]
    got = W.sites_problems(sited, _tree().path)
    expect.equal("an open note in a cluster from the cutover on with no site block is refused, prose paths and all; a block "
                 "naming none is a declaration", [s.split(":")[0] for s in got], [f"PB{new}.md", f"PB{new + 6}.md"])
    expect("... and the refusal names the rule and the planner's UNPARTITIONED plan",
           all(t in got[0] for t in ("**Sites:**", "UNPARTITIONED", "PB3263")), got[0])
    expect.equal("note_sites says whether a block is present, whatever it names",
                 [W.note_sites(i["_body"]).declared for i in sited[:3]], [False, True, True])


def case_18_a_glob_is_read_as_a_glob(expect):
    # A SITE BLOCK THAT NAMES SOMETHING MUST DECLARE IT (kb/Work PB3721): a glob is a sweep over the tracked files it matches,
    # claimed whole
    sweep = "**Sites:** every `scripts/spec/work*.py` that reads the register, and `scripts/spec/work.py` (`load`)."
    expect.equal("without a tree, a glob is read as a glob and claims no file",
                 (W.note_sites(sweep).globs, W.note_sites(sweep).paths, W.note_sites(sweep).members),
                 (("scripts/spec/work*.py",), ("scripts/spec/work.py",), {"scripts/spec/work.py": ("load",)}))
    tree = _tree().path
    swept = W.note_sites(sweep, repo=tree)
    expect.equal("on a tree, the glob claims every TRACKED file it matches (an untracked one is no match), and a sweep "
                 "claims each file whole (it outranks a member)",
                 (swept.paths, swept.members), (("scripts/spec/work.py", "scripts/spec/work_inventory.py"), {}))
    expect.equal("a glob matching nothing tracked expands to nothing; placeholders and `...` are still no site",
                 (W.expand_globs(("scripts/nothing-here/**/*.zz",), tree),
                  W.note_sites("**Sites:** `src/<P>/X.cs`, `src/.../Y.cs`, `src/{A,B}/Z.cs`").globs), ((), ()))
    expect.equal("`none (why)` is a declaration, in the label form and the heading form",
                 [W.note_sites(t).no_code for t in ("**Sites:** none (it changes no code file)", "- **Code site.** None - a finding",
                                                     "## Code site\n\nNone, that is the finding.\n", "**Sites:** `src/A.cs`")],
                 [True, True, True, False])


def case_19_a_held_note_must_claim_what_its_block_names(expect):
    new = W.SITES_REQUIRED_FROM
    guard = [{"_file": f"PB{n}.md", "id": f"PB{n}", "kind": "defect", "status": "open", "cluster": ["C"], "_body": b}
             for n, b in [(new, "**Sites:** `kb/Work/PB1.md` and `conformance-suite/cases/x.cob`."),
                          (new + 1, "**Sites:** none (a golden only)"),
                          (new + 2, "**Sites:** every `scripts/spec/work*.py` that reads the register"),
                          (new + 3, "**Sites:** `scripts/spec/work.py` (`sites_problems`) and `scripts/nothing-here/**/*.zz`"),
                          (new + 4, "**Sites:** `scripts/nothing-here/**/*.zz`"),
                          (new + 5, "**Sites:** `docs/A.md`"),
                          (new + 6, "**Sites:** `.claude/skills/workstream/check_practices.py` (`check`)")]]
    got = W.sites_problems(guard, _tree().path)
    expect.equal("a held note whose block names files it cannot claim is refused, a `none (why)`, a live glob, a path under a "
                 "root (the .claude process code included) are not, and a dead glob is named whether or not a path stands "
                 "beside it", [s.split(":")[0] for s in got], [f"PB{new}.md", f"PB{new + 3}.md", f"PB{new + 4}.md"])
    expect.equal("... and each refusal says what the planner does with such a note and how to declare it",
                 (all(t in got[0] for t in ("UNPARTITIONED", "PB3721", "none (why)")), "matches no tracked file" in got[1],
                  "matches no tracked file" in got[2]), (True, True, True))


def case_20_a_governed_note_names_the_members_of_each_code_file(expect):
    # R3's member rule as `check` enforces it (kb/Work PB3797): a governed note names each code file it changes WITH its
    # members or with `(whole: why)`, and a member the file does not declare is refused. The files are the scratch tree's,
    # read by the real unit readers (source_units for .py and .ps1).
    tree, here = _tree().path, "scripts/spec/work.py"

    def problems(sites: str) -> list[str]:
        return W.sites_problems([{"_file": "PB9999.md", "id": "PB9999", "kind": "defect", "status": "open", "cluster": ["PB1"],
                                  "_body": "**Sites:** " + sites}], tree)

    for name, sites, want in (
            ("a member the file declares", f"`{here}` (`sites_problems`)", ""),
            ("two members, the second qualified", f"`{here}` (`sites_problems`, `NoteSites.fix_lane`)", ""),
            ("a member after a :line suffix", f"`{here}:5` (`sites_problems`)", ""),
            ("the path#Member form", f"`{here}#sites_problems`", ""),
            ("a module-level unit", "`scripts/hooks/dispatch_guard.py` (`<module>`)", ""),
            ("a PowerShell function with a hyphen", "`scripts/orchestrator/orchestrate.ps1` (`Take-Lock`)", ""),
            ("the whole file with a reason", f"`{here}` (whole: the register is restructured)", ""),
            ("the whole file with an empty reason", f"`{here}` (whole: )", "without the members"),
            ("the whole file named bare", f"`{here}`", "without the members"),
            ("a prose parenthesis is no member", f"`{here}` (the register check)", "without the members"),
            ("a :line alone is no member", f"`{here}:5`", "without the members"),
            ("a member the file does not declare", f"`{here}` (`no_such_member_anywhere`)", "declares no such member"),
            ("a file the change creates needs neither", "`scripts/spec/a_file_this_change_creates.py`", ""),
            ("a Markdown file is named by its section number", "`docs/DOC_INDEX.md`, `docs/DESIGN.md` (§0.5)", ""),
            ("a glob sweep is whole by its pattern", "`scripts/spec/work*.py`", "")):
        got = problems(sites)
        if want:
            expect(f"site rule, {name}: one problem, naming the rule ({want!r})", len(got) == 1 and want in got[0], str(got))
        else:
            expect.equal(f"site rule, {name}: no problem", got, [])


def case_21_an_open_note_of_a_campaign_needs_its_block_whatever_its_id(expect):
    # kb/Work PB3640: the campaign is the cluster a `tier:` entry names; wave 20261030 planned PB3628 and PB3275 side by side
    # because PB3275, a PB1957 note filed before the cutover, had no block. The register holds no list of running campaigns.
    old = W.SITES_REQUIRED_FROM - 100
    camp = [{"_file": f"PB{n}.md", "id": f"PB{n}", "kind": k, "status": s, "cluster": cl, "tier": t, "_body": b}
            for n, k, s, cl, t, b in [
                (old, "defect", "open", ["K"], ["K:2"], "Tiered, no block."),
                (old + 1, "defect", "open", ["K"], [], "A member the owner left untiered, no block."),
                (old + 2, "defect", "open", ["K", "Z"], [], "**Sites:** `src/A.cs`\n"),
                (old + 3, "defect", "open", ["Z"], [], "In a cluster no note tiers, filed before the cutover."),
                (old + 4, "decision", "open", ["K"], ["K:1"], "An owner question."),
                (old + 5, "defect", "landed", ["K"], ["K:3"], "Landed, no block."),
                (old + 6, "defect", "open", ["K"], ["K:3"], "**Sites:** none (it changes no code file)\n")]]
    got = W.sites_problems(camp, _tree().path)
    expect.equal("an open note of a cluster some note tiers needs a block whatever its id; a decision, a terminal note, a "
                 "declaration and a cluster no note tiers do not", [s.split(":")[0] for s in got], [f"PB{old}.md", f"PB{old + 1}.md"])
    expect("... and the refusal names the campaign", all("campaign cluster(s) K" in s and "PB3640" in s for s in got), str(got))
    expect.equal("campaign_leads: the clusters the register's tier entries name, malformed entries left out",
                 W.campaign_leads(camp + _tiers()), {"K", "C", "D"})


# ── the R3 program's rules ──────────────────────────────────────────────────────────────────────────────────────────

def _r3(pid: str, *sites: str, blocked_by: tuple[str, ...] = (W.R3_DRIFT_TEST,), wave: str = "extract",
        status: str = "open", cluster: tuple[str, ...] = (W.R3_PROGRAM,)) -> dict:
    """One R3 program note naming `sites` in its site block."""
    body = (NL + f"**Wave kind:** {wave}. **Model:** Opus." + NL + NL + "**Sites:**" + NL
            + "".join(f"- `{s}`" + NL for s in sites))
    return {"_file": pid + ".md", "id": pid, "title": pid + " — planted", "status": status, "blocked_by": list(blocked_by),
            "cluster": list(cluster), "_body": body}


#: The one rule each R3 problem states, by the words only that rule's message carries.
_R3_RULES = (("missing", "which does not exist"), ("build output", "build output"),
             ("unknown member", "the member index"), ("waits for no drift test", f"does not wait for {W.R3_DRIFT_TEST}"))


def _r3_verdicts(problems: list[str]) -> list[tuple[str, str]]:
    """Each problem as (its note's file, the one rule it states); a message no rule's words match is its own verdict, so
    a new or reworded message fails the comparison by name instead of passing a prefix test."""
    return sorted((p.split(":", 1)[0], next((rule for rule, words in _R3_RULES if words in p), "UNRECOGNIZED: " + p))
                  for p in problems)


def case_22_the_r3_program_s_rules_each_refuse_their_own_shape(expect):
    # The register's own checks of the R3 program (cluster PB2119), on a scratch tree and a planted register, never the live
    # ones (kb/Work PB3202): a live plant on a file a rename then moves was refused for the wrong reason, and accepted by a
    # test that took any message starting with the plant's id.
    tree = _tree().path
    gen, g4 = "src/P/Generated/Parser.cs", "src/P/Grammar/Parser.g4"
    items = [
        _r3(W.R3_DRIFT_TEST, "src/P/Binding/Driver.cs", blocked_by=()),        # the drift test itself waits for nothing
        _r3("PB9991", "src/P/Binding/Driver.cs"),                                # well-formed: a tracked file, waits for it
        _r3("PB9992", "src/P/Renamed/Driver.cs"),                                # the file moved since the note was filed
        _r3("PB9993", "src/P/Binding/Driver.cs", blocked_by=()),                 # a measured wave that does not wait
        _r3("PB9994", "src/P/Binding/Driver.cs", blocked_by=(), wave="delete"),  # an unmeasured wave need not
        _r3("PB9995", gen, blocked_by=("PB9991",)),                             # a build output with no grammar (it
                                                                                 # reaches the drift test through PB9991)
        _r3("PB9996", gen, g4),                                                  # ... and with its grammar
        _r3("PB9997", "src/P/Binding/Driver.cs"),                                # moves a member the index does not know
        _r3("PB9998", "src/P/Gone.cs", status="landed"),                         # a landed note is never checked
        _r3("PB9999", "src/P/Gone.cs", cluster=("OTHER",)),                      # nor a note outside the program
    ]
    items[1]["_body"] += "- `src/P/New.cs` (new)" + NL                           # a file the wave creates is not missing
    got = W.r3_program_problems(items, {"PB9997": ["Driver.Gone"]}, repo=tree)
    expect.equal("each R3 rule refuses exactly its own shape, and a well-formed note, an unmeasured wave, the drift test "
                 "itself, a landed note and a note outside the program are refused for nothing",
                 _r3_verdicts(got), [("PB9992.md", "missing"), ("PB9993.md", "waits for no drift test"),
                                     ("PB9995.md", "build output"), ("PB9997.md", "unknown member")])
    expect("the missing-path refusal names the path and how a created file is marked",
           any("`src/P/Renamed/Driver.cs`" in p and "(new)" in p for p in got), str(got))
    expect("a generated file is never reported missing, though the committed tree does not have it",
           "src/P/Generated/Parser.cs" not in " ".join(p for p in got if "does not exist" in p))
    expect.equal("an untracked path is missing even when a local file has that name (the committed tree decides)",
                 W.named_missing_paths("`src/P/obj/project.assets.json` and `src/P/Binding/Driver.cs`", repo=tree),
                 ["src/P/obj/project.assets.json"])
    expect.equal("the verdict reader names a message no rule states (it cannot pass a reworded refusal)",
                 _r3_verdicts(["PB1.md: something new"]), [("PB1.md", "UNRECOGNIZED: PB1.md: something new")])


# ── the architecture edges file's removers ──────────────────────────────────────────────────────────────────────────

def _edges_note(pid: str, status: str, removes: str) -> dict:
    return {"_file": pid + ".md", "id": pid, "status": status, "_body": NL + "**Removes (planted):** " + removes + NL}


_EDGES = {"tolerated": [{"from": "P.Low", "to": "P.Mid", "removedBy": ["PB1"], "plan": "PB1",
                         "pairs": [{"from": "P.Low.A", "to": "P.Mid.B", "removedBy": ["PB1"]}]},
                        {"from": "P.Mid", "to": "P.High", "removedBy": ["PB2"], "plan": "PB2, then PB3", "pairs": []}]}


def _edges_notes() -> list[dict]:
    return [_edges_note("PB1", "open", "`P.Low → P.Mid` 1"), _edges_note("PB2", "open", "`P.Mid → P.High` 1"),
            _edges_note("PB3", "open", "nothing")]


def case_23_the_edges_file_s_removers_are_open_notes_naming_their_rows(expect):
    # kb/Work PB2417 review (R1-1, R1-2): the register's side of the architecture edges file's removers, on a planted file and
    # notes. The live file against the live register is `work.py check`'s, run by CI on every change to either.
    notes = _edges_notes()
    expect.equal("open notes whose Removes lines name their rows pass", W.edges_remover_problems(notes, _EDGES), [])
    expect.equal("a remover the register lacks (a pair's), and a plan's missing note",
                 [m.split(": ", 1)[1] for m in W.edges_remover_problems(notes[1:], _EDGES)],
                 ["tolerated row P.Low -> P.Mid names PB1 as a remover, which kb/Work does not hold",
                  "tolerated row P.Low -> P.Mid: its plan names PB1, which kb/Work does not hold"])
    expect("a landed remover removes nothing",
           any("PB1 is landed" in m for m in W.edges_remover_problems([_edges_note("PB1", "landed", "`P.Low → P.Mid` 1")] + notes[1:], _EDGES)))
    expect("a census-only row's own remover is checked like a pair's (its Removes line names another row)",
           any("PB2's **Removes** line does not name the row (`P.Mid → P.High`)" in m
               for m in W.edges_remover_problems([notes[0], _edges_note("PB2", "open", "`P.Low → P.Mid` 1"), notes[2]], _EDGES)))
    expect("a row whose name is a prefix of another's is not named by the longer one",
           any("PB1's **Removes** line does not name the row" in m
               for m in W.edges_remover_problems([_edges_note("PB1", "open", "`P.Low → P.Mid.Sub` 1")] + notes[1:], _EDGES)))


def case_24_a_landed_remover_leaves_by_the_census_or_by_removedby(expect):
    # PB2417 review R2-1, the PB4319 shape: a census-only row whose every remover landed can leave the file only through a
    # census recorded in the landing, so the refusal names that path and never "delete it from removedBy", which would leave
    # a row no note removes; a landed remover beside an open one, or a pair's, is deleted from removedBy.
    notes = _edges_notes()
    landed_pb2 = [notes[0], _edges_note("PB2", "landed", "`P.Mid → P.High` 1"), notes[2]]
    census_only = [m for m in W.edges_remover_problems(landed_pb2, _EDGES) if "PB2 is landed" in m]
    expect("a census-only row's landed last remover is refused, and the refusal names the census landing",
           len(census_only) == 1 and W.EDGES_CENSUS_LANDING in census_only[0] and "delete it from" not in census_only[0],
           str(census_only))
    two = {"tolerated": [dict(_EDGES["tolerated"][1], removedBy=["PB2", "PB4"], plan="PB2")]}
    expect("a census-only row's landed remover beside an open one is deleted from removedBy",
           any("PB2 is landed" in m and "delete it from" in m
               for m in W.edges_remover_problems(landed_pb2 + [_edges_note("PB4", "open", "`P.Mid → P.High` 1")], two)))
    expect("a pair's landed remover is deleted from removedBy (the ratchet takes a vanished pair)",
           any("PB1 is landed" in m and "delete it from" in m and W.EDGES_RATCHET in m
               for m in W.edges_remover_problems([_edges_note("PB1", "landed", "`P.Low → P.Mid` 1")] + notes[1:], _EDGES)))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
