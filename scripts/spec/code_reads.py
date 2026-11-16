#!/usr/bin/env python3
"""THE CODE READS of an older edition's rules: the tracked record of a rule adjudicated on the CODE at its own edition, by
a reader and a refuter (kb/Work PB5759, PB5683, PB5692; DESIGN-spec-conformance-review.md section 10.6.1).

    python scripts/spec/code_reads.py import --edition YEAR --reads DIR --pin COMMIT [--record DIR] [--replace]
    python scripts/spec/code_reads.py work --edition YEAR RULE-ID PBnnnn          # the mechanism note of a record
    python scripts/spec/code_reads.py adjudicate --edition YEAR RULE-ID --verdict V --reason TEXT --ref PATH [--work PBnnnn]
    python scripts/spec/code_reads.py withdraw --edition YEAR RULE-ID --reason TEXT  # remove a read defective as a read
    python scripts/spec/code_reads.py plan --edition YEAR [--witnessed] [--json]  # the KEPT rows owed a read, and the re-reads
    python scripts/spec/code_reads.py verify-pins --edition YEAR                  # every stored read resolved at its own pin
    python scripts/spec/code_reads.py --check [YEAR]                              # the gate's CODE READS audit

WHY (kb/Work PB5692's ruling, 2026-11-15). A verdict shared from the 2023 adjudication does not close a 1985 row: every
KEPT row needs a verdict adjudicated on the code at its own `--std`. The reads arrive as the pipeline's files (the briefs
`BRIEF-coderead.md` and `BRIEF-coderead-refuter.md`): `<slice>.json` = `{edition, records[]}` from the reader and
`<slice>-refute.json` = `{cases[]}` from the refuter, who attacks every CONFORMS. A read compares a rule with the compiler,
not two texts, so it has no place in a lineage read record (`lineage_reads.py`, SAME / DIFFERENT / DOUBTFUL), and a KEPT row
holds no verdict of its own (`lineage.next_errors`). It is kept HERE, beside the row, until the switch (kb/Work PB5682)
folds it in: `tests/version-matrix/code-reads-<year>/<rule-id>.json`, one file per rule, written only by this module
(`inventory_store.write_store`, the one writer of a one-file-per-rule store). No row's state changes here.

THE RECORD holds the read as the two agents gave it and nothing invented: `reader` (the reader's record, verbatim) and
`refuter` (the refuter's case, verbatim; absent for a non-conforming read, which the refuter does not attack); `rule-digest`
(the catalog digest of the rule's text when the read was imported, `inventory_view.rule_digest`); `read` (the `pin`, the
commit the code was read at, which the import is told; the `slice`; `sources`, the SHA-256 of each source file's content;
`imported`, the SHA-256 of each part the record holds, so a hand edit of a held part is red, the lineage_reads lesson of
PB5597 review R1-6); `adjudication` (`verdict`, `reason`, `ref`, a tracked file; written by `adjudicate` only); `work` (the
kb/Work note of the mechanism, required of a contradicted record; written by `work` or `adjudicate`). What the reader's
record must carry is `READER_REQUIRED` and `reader_problems`, decided field by field from the reader brief; a missing or
malformed required field refuses the whole import, naming the slice, the rule and the field.

THREE ACCEPTANCES beyond the inventory's shape (kb/Work PB5759, the Mythos lane's decision of 2026-11-15: nothing is read twice
and no fact is dropped; PB5842 the third). (3) A CITATION OF ANOTHER HELD EDITION is accepted and checked against its own
edition's text (`citation_problems`), beside at least one citation of the rule's own edition, of the edition its catalog
citation names, or of `§1989:`; a read that cites only another edition is refused, naming the rule. (1) A REMARK AFTER A LOCATION: a `code-location` entry may be `path#Member (a remark)`, one
parenthesised remark after white space (`take_remarks`); the record holds the location list in the inventory's shape and the
remarks in the reader's `location-remarks` map, location to remark, and the remark decides nothing. (2) A READER WHO DID NOT
DECIDE: the verdict `UNDECIDED` is accepted as "no verdict" (`code_read_store.READER_UNDECIDED`): the record is `undecided`.
Anything else outside the schema's verdicts, or in a `code-location` entry, is refused.

THE OUTCOME OF A RECORD is ONE function, `code_read_store.outcome` (confirmed, contradicted, undecided, stale), which
the audit, the view and the planner all ask; the store's reader and writer live beside it, in the half the inventory view
imports (`code_read_store.py`, whose docstring says why the code reads are two files).

A READ IS EVIDENCE AT ITS PIN (kb/Work PB5863). The import resolves every `code-location` entry in the tree of the commit
the code was read at (`code_location.PinnedTree`, by the rule the gate holds the inventory's rows to: `code_location` is its
Python twin, kept one rule by a parity fixture and a whole-tree comparison in the C# gate) and refuses a slice with an
entry that does not resolve there, naming the rule, the entry and the pin. That is the only time a read's locations are
judged: code moves, and a read made at an older commit is not wrong because it did, it is OLD. `moved_since` (an entry no
longer resolves in the working tree) puts such a row in `plan`'s RE-READ group beside a contradicted read whose fix has
landed (`fixed_since`), each with its reason; nothing is red for it. `verify-pins` is the hand verb that resolves every
stored record at its own pin (the records imported before the rule); it is not part of `--check`, because a pin's commit
may be absent from a shallow checkout.

THE AUDIT (`--check`, the gate's CODE READS) reads every store of an older held edition and is RED on: a record that does
not parse to the shape (`record_problems`, the import's own checks); a rule the edition does not have; a citation that
does not parse (`citation.parse`), is of an edition the repository does not hold, does not check (`cite.resolve`), or a
read none of whose citations is of the rule's own edition, its catalog citation's or `§1989:`; a code-location of no inventory shape
(`inventory_schema.witness_shape_error`); a held part edited since its import; a digest of the wrong shape; an
adjudication whose `ref` is no file of the tree; a contradicted record whose `work` is no note of kb/Work, or is a note
already `landed` at the record's own pin (`landed_at_pin_problem`). Stale, undecided and owed are counted, never red, and so
are the reads owed a RE-READ (`reread`: fixed since, kb/Work PB5854, or moved since, kb/Work PB5863).
"""
from __future__ import annotations

import argparse
import contextlib
import dataclasses
import json
import pathlib
import re
import subprocess
import sys
from collections import Counter
from typing import Any, Iterable, Iterator

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import cite  # noqa: E402  (THE checker of a citation)
import citation  # noqa: E402  (THE citation parser)
import code_location  # noqa: E402  (THE resolver of a code-location, the gate's rule: at a pin and in the working tree)
import code_read_store as store  # noqa: E402  (the store, the outcome, the counts: the view's half)
from code_read_store import CONFIRMED, CONFORMS, CONTRADICTED, OUTCOMES, READER_UNDECIDED, UPHELD  # noqa: E402
import inventory_schema  # noqa: E402  (the editions, the verdict vocabulary, the code-location shape)
import inventory_view  # noqa: E402  (each edition's rows and their digests)
import lineage  # noqa: E402  (which rows are KEPT, and a row's own witnesses)
import lineage_reads  # noqa: E402  (the content digest every read record carries, and the refuter's vocabulary)
import work as register  # noqa: E402  (THE reader of a kb/Work note's frontmatter: `read_note`)
from inventory_schema import Edition, Schema  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "code_reads"
REPO = inventory_schema.REPO
WORK_DIR = "kb/Work"

#: The refuter's verdicts: the one vocabulary every refuter brief of the repository uses (`lineage_reads`).
REFUTATIONS = lineage_reads.REFUTE_VERDICTS
#: The causes a non-conforming reader names (the reader brief's step 5): (a) another arm at this edition, (b) the same
#: failure at every edition, (c) the rule covers more or less in this edition.
CAUSES = ("a", "b", "c")
#: A slice's name: any label a file can carry that pairs `<name>.json` with `<name>-refute.json`.
SLICE = re.compile(r"\A[A-Za-z0-9][A-Za-z0-9_.-]*\Z")
REFUTE_SUFFIX = "-refute"
SHA256 = re.compile(r"\A[0-9a-f]{64}\Z")
PIN = re.compile(r"\A[0-9a-f]{40}\Z")
NOTE = re.compile(r"\APB[0-9]+\Z")
#: The status of a note whose fix has landed (the register's own word).
LANDED = "landed"

#: The keys every record has (`code_read_store.RECORD_KEYS` are all it may have).
RECORD_KEYS = store.RECORD_KEYS
REQUIRED_KEYS = ("rule-id", "rule-digest", "read", "reader")
#: The reader's text fields every record carries non-empty (the reader brief's record): its step 1's sentence, how the
#: rule's behaviour differs by edition, what its witnesses cover and its notes.
READER_TEXT = ("requirement", "behaviour-differs-by-edition", "witnesses-cover", "notes")
#: ... and the ones a NON-CONFORMING read carries non-empty (step 5: the minimal program, the expected result quoted from
#: the text, the compiler's output, the harm, the cause, and the note searched for or "none").
NONCONFORMING_TEXT = ("harm", "repro", "expected", "observed", "known_note")
#: The reader's fields that are text when present, possibly empty (a CONFORMS read leaves its defect fields empty).
READER_OPTIONAL_TEXT = ("edition-search", "cause", "at-2023") + NONCONFORMING_TEXT
#: The key the IMPORT adds to a reader's record when a `code-location` entry carried a remark; a reader's file never has it.
REMARKS_KEY = "location-remarks"
PROBE_TEXT = ("class", "file", "printed", "expected")
REFUTER_TEXT = ("classes", "evidence")


class ReadError(ValueError):
    """A read no record can hold: its slice, its rule and what is wrong."""


@dataclasses.dataclass(frozen=True)
class Context:
    """What a record is judged against: its edition, the schema, the edition's rows by id (each a catalog rule with its
    `rule-digest` and its `citation`, `inventory_view.inventory`) and the tree whose kb/Work and files a record names."""
    ed: Edition
    schema: Schema
    rows: dict[str, dict[str, Any]]
    root: pathlib.Path = REPO

    @classmethod
    def of(cls, year: int, root: pathlib.Path = REPO, schema: Schema | None = None,
           rows: Iterable[dict[str, Any]] | None = None) -> "Context":
        """The context of the older held edition `year`; the newest edition is refused: its rows carry their own verdicts."""
        schema = schema or inventory_schema.load_schema()
        ed = schema.edition(year)
        if ed.newest:
            raise SystemExit(f"{TOOL}: {year} is the newest held edition: its rows carry their own verdicts, and it has no "
                             "store of code reads")
        if CONFORMS not in schema.verdicts:
            raise SystemExit(f"{TOOL}: the schema's verdicts {sorted(schema.verdicts)} lack {CONFORMS}, the verdict a code "
                             "read confirms")
        rows = inventory_view.inventory(year, schema) if rows is None else rows
        return cls(ed, schema, {r["rule-id"]: r for r in rows}, root)


# -- the shape of a read: the ONE place the reader's and the refuter's records are checked ------------------------------

def _text(case: dict[str, Any], key: str, required: bool) -> str | None:
    """Why `case[key]` is not text (non-empty when `required`), or None."""
    v = case.get(key)
    if v is None and not required:
        return None
    if not isinstance(v, str) or (required and not v.strip()):
        return f"`{key}` must be {'a non-empty string' if required else 'a string'}, not {v!r}"
    return None


def citation_problems(cites: Any, rule: dict[str, Any], year: int, held: Iterable[int] = ()) -> list[str]:
    """Why a reader's `citations` do not stand: each a `{cite, check}` whose `cite` parses (`citation.parse`), is of a text
    the repository holds and checks (`cite.resolve`, the structure: clause, rule list, ordinal, sub-item). THREE EDITIONS
    ANCHOR a read: the rule's own (`year`), the one the catalog's own `citation` of the rule names, and the 1989 amendment
    (`§1989:`); a read must carry at least one citation of them, because a read that cites only another edition judged
    nothing by its own text. A citation of ANOTHER held edition (`held`, the schema's years) is accepted beside them and is
    checked against its own edition's text exactly as the others are (kb/Work PB5842: a defect wrong at 2023 too is cited
    at 2023)."""
    if not isinstance(cites, list) or not cites:
        return [f"`citations` must be a non-empty list of {{cite, check}}, not {cites!r}"]
    anchors = {year, 1989}
    try:
        anchors.add(citation.parse(rule["citation"]).edition)
    except (KeyError, citation.CitationError):
        pass   # a catalog row with no citation of its own: only the edition's
    allowed = anchors | set(held)
    anchored = False
    out = []
    for c in cites:
        if not isinstance(c, dict) or (bad := _text(c, "cite", True) or _text(c, "check", True)):
            out.append(f"a citation is a {{cite, check}} of two non-empty strings, not {c!r}")
            continue
        try:
            parsed = citation.parse(c["cite"])
        except citation.CitationError as e:
            out.append(f"citation {c['cite']!r} does not parse: {e}")
            continue
        if parsed.edition not in allowed:
            out.append(f"citation {c['cite']!r} is of {parsed.edition}, an edition the repository does not hold "
                       f"({sorted(allowed)})")
            continue
        if (v := cite.resolve(parsed)).kind != "ok":
            out.append(f"citation {c['cite']!r} does not check: {v.kind}: {v.message}")
            continue
        anchored = anchored or parsed.edition in anchors
    if not anchored and not out:
        out.append(f"no citation is of the rule's own text ({sorted(anchors)}): a read that cites only another edition "
                   f"judged nothing by {rule.get('rule-id', 'its rule')}'s own text")
    return out


def _entries(location: str, sep: str) -> tuple[list[str], str | None]:
    """The entries of a `code-location` (split at `sep` outside parentheses, each trimmed, the empty ones dropped), or why
    its parentheses do not balance."""
    out, depth, start = [], 0, 0
    for i, ch in enumerate(location):
        if ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
            if depth < 0:
                return [], f"`code-location` has a ')' with no '(' ({location!r})"
        elif ch == sep and depth == 0:
            out.append(location[start:i])
            start = i + 1
    if depth:
        return [], f"`code-location` has a '(' that is never closed ({location!r})"
    out.append(location[start:])
    return [e for e in (e.strip() for e in out) if e], None


def split_remark(entry: str) -> tuple[str, str | None, str | None]:
    """(location, remark, problem) of one `code-location` entry: `path#Member`, or `path#Member (remark)`: white space and
    ONE parenthesised remark, balanced inside, ending the entry. The location is not judged here (its shape is the
    schema's); a remark with no location, an empty one, a second one, text after the closing parenthesis and a '(' not
    preceded by white space are the problems."""
    if "(" not in entry:
        return entry, None, None
    i = entry.index("(")
    location = entry[:i].rstrip()
    if not location:
        return "", None, f"a remark with no location: {entry!r}"
    if not entry[i - 1].isspace():
        return location, None, f"the remark must follow its location after white space: {entry!r}"
    depth = 0
    for j in range(i, len(entry)):
        depth += (entry[j] == "(") - (entry[j] == ")")
        if depth == 0:
            if entry[j + 1:].strip():
                return location, None, f"text after the closing parenthesis of the remark (one remark only): {entry!r}"
            remark = entry[i + 1:j].strip()
            return (location, remark, None) if remark else (location, None, f"an empty remark: {entry!r}")
    return location, None, f"unbalanced parenthesis: {entry!r}"


def take_remarks(case: dict[str, Any], ctx: Context) -> tuple[dict[str, Any], list[str]]:
    """The reader's record `case` as a record holds it: its `code-location` the locations alone (the inventory's shape),
    the remarks in `location-remarks` (location to remark), and the problems of the entries (each naming the text). A
    `code-location` that is no string is left for `reader_problems`; a file that already carries `location-remarks` is
    refused (the import derives it)."""
    location = case.get("code-location")
    if REMARKS_KEY in case:
        return case, [f"`{REMARKS_KEY}` is derived by the import from the remarks of `code-location`; a reader's file has none"]
    if not isinstance(location, str):
        return case, []
    sep = ctx.schema.code_location_sep.strip()
    entries, problem = _entries(location, sep)
    if problem:
        return case, [problem]
    locations: list[str] = []
    remarks: dict[str, str] = {}
    problems: list[str] = []
    for entry in entries:
        loc, remark, bad = split_remark(entry)
        if bad:
            problems.append(f"`code-location`: {bad}")
            continue
        locations.append(loc)
        if remark is not None:
            if loc in remarks:
                problems.append(f"`code-location`: {loc!r} carries two remarks (second: {entry!r})")
            remarks[loc] = remark
    if problems or not remarks:
        return case, problems
    return {**case, "code-location": f"{sep} ".join(locations), REMARKS_KEY: remarks}, []


def reader_problems(case: Any, ctx: Context) -> list[str]:
    """Why the reader's record `case` is no read a record can hold (every problem, each naming its field). The rule must be
    a rule of the edition; the verdict one of the inventory schema's; every site of the code-location of the inventory's
    shape (the schema's `requires` of the verdict says whether it may be empty)."""
    if not isinstance(case, dict):
        return [f"a reader's record is a map, not {type(case).__name__}"]
    out = [e for k in READER_TEXT if (e := _text(case, k, True))]
    out += [e for k in READER_OPTIONAL_TEXT if (e := _text(case, k, False))]
    rid = case.get("rule-id")
    rule = ctx.rows.get(rid) if isinstance(rid, str) else None
    if rule is None:
        out.append(f"`rule-id` {rid!r} is no rule of the {ctx.ed.year} catalog")
    verdict = case.get("verdict")
    if verdict not in ctx.schema.verdicts and verdict != READER_UNDECIDED:
        out.append(f"`verdict` {verdict!r} is not one of the inventory's verdicts {sorted(ctx.schema.verdicts)} "
                   f"or {READER_UNDECIDED} (the reader did not decide)")
    location = case.get("code-location")
    sites: list[str] = []
    if not isinstance(location, str):
        out.append(f"`code-location` must be a string, not {location!r}")
    else:
        sites = ctx.schema.split(location, ctx.schema.code_location_sep)
        if not sites and verdict in ctx.schema.verdicts and "code-location" in ctx.schema.requires(verdict):
            out.append(f"`code-location` is empty, and a {verdict} read names the code")
        out += [f"`code-location`: {e}" for s in sites
                if (e := inventory_schema.witness_shape_error("code-location", s, ctx.schema))]
    if (remarks := case.get(REMARKS_KEY)) is not None:
        if not isinstance(remarks, dict) or not all(
                k in sites and isinstance(v, str) and v.strip() for k, v in remarks.items()):
            out.append(f"`{REMARKS_KEY}` maps a location of `code-location` to its non-empty remark, not {remarks!r}")
    consulted = case.get("edition-consulted")
    if not isinstance(consulted, list):
        out.append(f"`edition-consulted` must be a list of {{site, switches, same-arm-as-latest}}, not {consulted!r}")
    else:
        for e in consulted:
            if not (isinstance(e, dict) and not _text(e, "site", True) and not _text(e, "switches", True)
                    and isinstance(e.get("same-arm-as-latest"), bool)):
                out.append(f"an `edition-consulted` entry is {{site, switches, same-arm-as-latest (a boolean)}}, not {e!r}")
        if not consulted and not (case.get("edition-search") or "").strip():
            out.append("`edition-consulted` is empty and `edition-search` does not say how the reader looked")
    probes = case.get("probes")
    if not isinstance(probes, list) or not probes:
        out.append(f"`probes` must be a non-empty list (a rule with no probe is not decided), not {probes!r}")
    else:
        out += [f"a probe is {{{', '.join(PROBE_TEXT)}}} of strings, its class and file non-empty, not {p!r}"
                for p in probes if not isinstance(p, dict) or any(_text(p, k, k in ("class", "file")) for k in PROBE_TEXT)]
    if not isinstance(case.get("seeded"), bool):
        out.append(f"`seeded` must be a boolean (the case's own value), not {case.get('seeded')!r}")
    if rule is not None:
        out += citation_problems(case.get("citations"), rule, ctx.ed.year,
                                 [e.year for e in ctx.schema.all_editions if e.held])
    if verdict in ctx.schema.verdicts and verdict != CONFORMS:   # an UNDECIDED reader owes no defect
        out += [e for k in NONCONFORMING_TEXT if (e := _text(case, k, True))]
        if (case.get("cause") or "").strip() not in CAUSES:
            out.append(f"a {verdict} read names its `cause`, one of {CAUSES}, not {case.get('cause')!r}")
        elif case["cause"].strip() == "b" and (e := _text(case, "at-2023", True)):
            out.append(f"a cause (b) read says what its repro does at the newest edition: {e}")
    return out


def refuter_problems(case: Any) -> list[str]:
    """Why the refuter's case `case` is no refutation a record can hold (every problem, each naming its field)."""
    if not isinstance(case, dict):
        return [f"a refuter's case is a map, not {type(case).__name__}"]
    out = [e for k in REFUTER_TEXT if (e := _text(case, k, True))]
    out += [e for e in (_text(case, "rule-id", True), _text(case, "corrected", False)) if e]
    if case.get("refutation") not in REFUTATIONS:
        out.append(f"`refutation` {case.get('refutation')!r} is not one of {REFUTATIONS}")
    elif case["refutation"] != UPHELD and (e := _text(case, "corrected", True)):
        out.append(f"a {case['refutation']} says the right verdict: {e}")
    missed = case.get("edition-sites-missed")
    if not isinstance(missed, list) or not all(isinstance(s, str) and s.strip() for s in missed):
        out.append(f"`edition-sites-missed` must be a list of non-empty strings, not {missed!r}")
    return out


# -- the record -----------------------------------------------------------------------------------------------------------

def record_of(case: dict[str, Any], refute: dict[str, Any] | None, rule_digest: str, pin: str, slice_name: str,
              sources: dict[str, str]) -> dict[str, Any]:
    """The record of one read: the reader's and the refuter's parts verbatim, the digest of each, and where they came from."""
    parts = {"reader": case, **({"refuter": refute} if refute is not None else {})}
    return {"rule-id": case["rule-id"], "rule-digest": rule_digest,
            "read": {"pin": pin, "slice": slice_name, "sources": sources,
                     "imported": {k: lineage_reads.digest(v) for k, v in parts.items()}},
            **parts}


def work_problem(work: Any, root: pathlib.Path) -> str | None:
    """Why `work` names no note of the work register in the tree at `root`, or None."""
    if not isinstance(work, str) or not NOTE.match(work):
        return f"`work` must name a kb/Work note (PBnnnn), not {work!r}"
    if not (root / WORK_DIR / f"{work}.md").is_file():
        return f"`work` {work} is no note of {WORK_DIR}"
    return None


def note_status(work: str, text: str | None) -> str | None:
    """The `status` of the kb/Work note `work` whose file holds `text` (None: no such file), read by THE REGISTER'S reader
    of a note (`work.read_note`), so a note at a pin goes through the one parser a note in the tree does. A file that is
    no readable note refuses loudly: it is never read as a note that is not landed (kb/Work PB3654)."""
    if text is None:
        return None
    note, why = register.read_note(f"{work}.md", text)
    if note is None:
        raise SystemExit(f"{TOOL}: {work}: {why or 'not a work note'}")
    return note.get("status")


def status_in_tree(work: str, root: pathlib.Path) -> str | None:
    """`work`'s status in the tree at `root` (None when the note is no file there)."""
    path = root / WORK_DIR / f"{work}.md"
    return note_status(work, path.read_text(encoding="utf-8") if path.is_file() else None)


_OPEN_PINS: dict[pathlib.Path, code_location.PinnedTrees] = {}


@contextlib.contextmanager
def pins_open(root: pathlib.Path) -> Iterator[code_location.PinnedTrees]:
    """The trees of the commits of the repository at `root`: inside the outermost `with`, every read at a pin goes through
    ONE `git cat-file` process per pin (`code_location.PinnedTrees`), closed when it ends. The audit reads a note at the pin
    of every contradicted record and the import every location at its pin: a process per read cost the audit 33 s of its
    47 on a busy host."""
    key = root.resolve()
    if key in _OPEN_PINS:
        yield _OPEN_PINS[key]
        return
    with code_location.PinnedTrees(root) as pins:
        _OPEN_PINS[key] = pins
        try:
            yield pins
        finally:
            del _OPEN_PINS[key]


def status_at(work: str, pin: str, root: pathlib.Path) -> str | None:
    """`work`'s status in the commit `pin` of the repository at `root` (None when the note was no file there). A pin that is
    no commit of the repository refuses: a status that cannot be read is no answer of "absent"."""
    with pins_open(root) as pins:
        if not pins.is_commit(pin):
            raise SystemExit(f"{TOOL}: {work}: the pin {pin} is no commit of the repository at {root}, so the note's status "
                             "at the read cannot be known")
        return note_status(work, pins.at(pin).text(f"{WORK_DIR}/{work}.md"))


def landed_at_pin(work: str, pin: str, root: pathlib.Path) -> bool:
    """Was the note `work` already `landed` in the commit `pin` the code was read at?"""
    return status_at(work, pin, root) == LANDED


def fixed_since(record: dict[str, Any], ctx: Context) -> bool:
    """THE PREDICATE of kb/Work PB5854: is `record` a contradicted read whose mechanism's note has LANDED since the read? Its
    outcome is `contradicted`, it names a `work` note whose status in the tree is `landed`, and at the record's own pin the
    note was absent or not `landed`. Such a row is owed a RE-READ on the fixed code (`plan`): the contradiction was true
    when it was made, and the count of contradicted rows can reach zero only by reading the row again."""
    work = record.get("work")
    if outcome(record, ctx) != CONTRADICTED or not isinstance(work, str) or not NOTE.match(work):
        return False
    if status_in_tree(work, ctx.root) != LANDED:
        return False
    return not landed_at_pin(work, record["read"]["pin"], ctx.root)


def landed_at_pin_problem(work: str, pin: str, root: pathlib.Path) -> str | None:
    """Why `work` cannot be the mechanism of a contradiction read at `pin`, or None. A note already `landed` at the read
    describes a defect that was fixed when the code was read: the contradiction is no defect of that note, and `fixed_since`
    could never see it fixed 'since' (kb/Work PB5854)."""
    if landed_at_pin(work, pin, root):
        return (f"`work` {work} was already `{LANDED}` at the read's pin {pin[:10]}: a contradiction read after the fix is "
                "no defect of that note; file a NEW kb/Work note for it")
    return None


def ref_problem(ref: Any, root: pathlib.Path) -> str | None:
    """Why `ref` names no file of the tree at `root` (repository-relative, `/`-separated), or None."""
    if not isinstance(ref, str) or not ref.strip() or ref.startswith("/") or "\\" in ref or ".." in ref.split("/"):
        return f"`ref` must be a repository-relative path, not {ref!r}"
    if not (root / ref).is_file():
        return f"`ref` {ref} is no file of the tree"
    return None


def record_problems(record: Any, ctx: Context) -> list[str]:
    """Why a STORED record does not stand (the audit): its shape, its rule, its parts (the import's own checks), its
    digests, its adjudication and its work."""
    if not isinstance(record, dict):
        return ["a record is a map"]
    out = [f"lacks `{k}`" for k in REQUIRED_KEYS if k not in record]
    out += [f"holds `{k}`, which no record holds" for k in record if k not in RECORD_KEYS]
    if out:
        return out
    rid = record["rule-id"]
    if rid not in ctx.rows:
        return [f"{rid} is no rule of the {ctx.ed.year} catalog"]
    reader, refute = record["reader"], record.get("refuter")
    out += [f"reader: {e}" for e in reader_problems(reader, ctx)]
    if isinstance(reader, dict) and reader.get("rule-id") != rid:
        out.append(f"its reader reads {reader.get('rule-id')!r}")
    if refute is not None:
        out += [f"refuter: {e}" for e in refuter_problems(refute)]
        if isinstance(refute, dict) and refute.get("rule-id") != rid:
            out.append(f"its refuter attacks {refute.get('rule-id')!r}")
    if out:
        return out
    if reader["verdict"] == CONFORMS and refute is None:
        out.append("a CONFORMS read with no refuter's case")
    if reader["verdict"] not in (CONFORMS, READER_UNDECIDED) and refute is not None:
        out.append(f"a refuter's case on a {reader['verdict']} read: the refuter attacks only CONFORMS")
    if not isinstance(record["rule-digest"], str) or not re.fullmatch(r"[0-9a-f]{12}", record["rule-digest"]):
        out.append(f"`rule-digest` {record['rule-digest']!r} is not a rule digest (inventory_view.rule_digest)")
    read = record["read"]
    parts = {"reader": reader, **({"refuter": refute} if refute is not None else {})}
    if not isinstance(read, dict) or set(read) != {"pin", "slice", "sources", "imported"}:
        out.append(f"`read` is {{pin, slice, sources, imported}}, not {read!r}")
    else:
        if not isinstance(read["pin"], str) or not PIN.match(read["pin"]):
            out.append(f"`read.pin` {read['pin']!r} is not a commit (40 hexadecimal digits)")
        if not isinstance(read["slice"], str) or not SLICE.match(read["slice"]):
            out.append(f"`read.slice` {read['slice']!r} is no slice name")
        # `sources` digests the slice's FILES: a non-conforming read's slice may have a refuter's file without a case of
        # the rule. `imported` digests the PARTS the record holds, exactly.
        for k, legal in (("sources", lambda keys: set(parts) <= keys <= {"reader", "refuter"}),
                         ("imported", lambda keys: keys == set(parts))):
            held = read[k]
            if not isinstance(held, dict) or not legal(set(held)) or not all(
                    isinstance(v, str) and SHA256.match(v) for v in held.values()):
                out.append(f"`read.{k}` must hold a SHA-256 of each of {sorted(parts)}, not {held!r}")
        if isinstance(read["imported"], dict):
            out += [f"its {k} was edited since its import (its digest is not the one it was imported with)"
                    for k, v in parts.items() if read["imported"].get(k) not in (None, lineage_reads.digest(v))]
    if (adj := record.get("adjudication")) is not None:
        if not isinstance(adj, dict) or set(adj) != {"verdict", "reason", "ref"}:
            out.append(f"`adjudication` is {{verdict, reason, ref}}, not {adj!r}")
        else:
            if adj["verdict"] not in ctx.schema.verdicts:
                out.append(f"`adjudication.verdict` {adj['verdict']!r} is not one of the inventory's verdicts")
            if e := _text(adj, "reason", True):
                out.append(f"adjudication: {e}")
            if e := ref_problem(adj["ref"], ctx.root):
                out.append(f"adjudication: {e}")
    if "work" in record and (e := work_problem(record["work"], ctx.root)):
        out.append(e)
    elif "work" in record and outcome(record, ctx) == CONTRADICTED and (
            e := landed_at_pin_problem(record["work"], record["read"]["pin"], ctx.root)):
        out.append(e)
    if not out and outcome(record, ctx) == CONTRADICTED and "work" not in record:
        out.append("a contradicted record names its mechanism's kb/Work note (`work`): "
                   f"python scripts/spec/code_reads.py work --edition {ctx.ed.year} {rid} PBnnnn")
    return out


def outcome(record: dict[str, Any], ctx: Context) -> str:
    """`record`'s outcome in `ctx` (`code_read_store.outcome`, the one function)."""
    return store.outcome(record, ctx.rows)


def load(ctx: Context, directory: pathlib.Path | None = None) -> dict[str, dict[str, Any]]:
    """Every record of the store of `ctx`'s edition (`directory`, the tracked store by default) (`code_read_store.load`)."""
    return store.load(ctx.ed.year, ctx.root, directory)


# -- the import -----------------------------------------------------------------------------------------------------------

def _source(path: pathlib.Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def import_reads(ctx: Context, reads: pathlib.Path, out: pathlib.Path, pin: str, replace: bool = False
                 ) -> tuple[dict[str, dict[str, Any]], list[str], list[str]]:
    """The records of every slice of `ctx`'s edition in `reads` (each `<slice>.json` whose own `edition` is the year, with
    its `<slice>-refute.json`), merged into the store at `out`. Returns (the store's records after the import, the rules
    imported, the rules whose earlier record they replace). Nothing is written by this function (`write` does), and every
    problem of every slice is raised together: a rule of no catalog row, a row the read does not apply to (not KEPT), a
    read of a rule read twice (in two slices, or already recorded and not `replace`), a malformed reader's record or
    refuter's case, a `code-location` entry that does not resolve in the tree AT THE PIN (`location_problems`, kb/Work
    PB5863), a refuter's case of no CONFORMS read, a CONFORMS read with no refuter's case, an orphan refute file."""
    if not reads.is_dir():
        raise SystemExit(f"{TOOL}: {reads} is not a directory of read files")
    if not PIN.match(pin):
        raise SystemExit(f"{TOOL}: --pin {pin!r} is not a full commit id (40 hexadecimal digits)")
    if subprocess.run(["git", "-C", str(ctx.root), "cat-file", "-e", f"{pin}^{{commit}}"], capture_output=True).returncode:
        raise SystemExit(f"{TOOL}: --pin {pin} is no commit of the repository at {ctx.root}")
    year = ctx.ed.year
    existing = load(ctx, out)
    with pins_open(ctx.root) as pins:
        return _import_reads(ctx, reads, existing, pins.at(pin), pin, replace)


def location_problems(case: dict[str, Any], tree: code_location.Tree, ctx: Context) -> list[str]:
    """Why the entries of the reader's `code-location` (the locations alone, `take_remarks`) do not resolve in `tree`, by
    the gate's rule (`code_location`): one problem per entry that does not."""
    return code_location.unresolved(ctx.schema.split(case.get("code-location") or "", ctx.schema.code_location_sep),
                                    tree, ctx.schema.code_location_re)


def _import_reads(ctx: Context, reads: pathlib.Path, existing: dict[str, dict[str, Any]], at_pin: code_location.Tree,
                  pin: str, replace: bool) -> tuple[dict[str, dict[str, Any]], list[str], list[str]]:
    year = ctx.ed.year
    errs: list[str] = []
    orphans = sorted(p.name for p in reads.glob(f"*{REFUTE_SUFFIX}.json")
                     if not (reads / p.name.replace(f"{REFUTE_SUFFIX}.json", ".json")).exists())
    errs += [f"{name}: a refuter's file with no reader's file" for name in orphans]
    new: dict[str, dict[str, Any]] = {}
    where: dict[str, str] = {}
    for path in sorted(p for p in reads.glob("*.json") if not p.stem.endswith(REFUTE_SUFFIX)):
        name = path.stem
        try:
            doc = _source(path)
        except (UnicodeDecodeError, json.JSONDecodeError) as e:
            errs.append(f"{name}: not JSON ({e})")
            continue
        if not isinstance(doc, dict) or doc.get("edition") != year:
            continue   # another edition's slice (its own `edition` says so): not this import's
        if not SLICE.match(name):
            errs.append(f"{name}: a slice's name pairs <name>.json with <name>{REFUTE_SUFFIX}.json; this one is no label")
            continue
        if not isinstance(doc.get("records"), list):
            errs.append(f"{name}: the reader's file is {{edition, records[]}}; it holds no `records` list")
            continue
        refute_path = reads / f"{name}{REFUTE_SUFFIX}.json"
        refute_doc: Any = None
        if refute_path.exists():
            try:
                refute_doc = _source(refute_path)
            except (UnicodeDecodeError, json.JSONDecodeError) as e:
                errs.append(f"{name}{REFUTE_SUFFIX}: not JSON ({e})")
                continue
            if not isinstance(refute_doc, dict) or not isinstance(refute_doc.get("cases"), list):
                errs.append(f"{name}{REFUTE_SUFFIX}: the refuter's file is {{cases[]}}; it holds no `cases` list")
                continue
        sources = {"reader": lineage_reads.digest(doc),
                   **({"refuter": lineage_reads.digest(refute_doc)} if refute_doc is not None else {})}
        readers: dict[str, dict[str, Any]] = {}
        for case in doc["records"]:
            rid = case.get("rule-id") if isinstance(case, dict) else None
            label = f"{name}: {rid if isinstance(rid, str) else '(no rule-id)'}"
            remark_problems: list[str] = []
            if isinstance(case, dict):
                case, remark_problems = take_remarks(case, ctx)
            if bad := remark_problems + reader_problems(case, ctx):
                errs += [f"{label}: {e}" for e in bad]
                continue
            if bad := location_problems(case, at_pin, ctx):   # evidence at its pin (kb/Work PB5863)
                errs += [f"{label}: {e}" for e in bad]
                continue
            if rid in readers or rid in where:
                errs.append(f"{label}: read again (first read in {where.get(rid, name)})")
                continue
            if not lineage.chained(ctx.rows[rid], ctx.schema):
                errs.append(f"{label}: its row is not {ctx.schema.next.chained.name}: a code read adjudicates a row "
                            "whose verdict is shared (kb/Work PB5692)")
                continue
            if rid in existing and not replace:
                errs.append(f"{label}: already recorded (slice {existing[rid]['read']['slice']}); --replace to replace it")
                continue
            readers[rid] = case
            where[rid] = name
        refutes: dict[str, dict[str, Any]] = {}
        for case in (refute_doc or {}).get("cases", []):
            rid = case.get("rule-id") if isinstance(case, dict) else None
            label = f"{name}{REFUTE_SUFFIX}: {rid if isinstance(rid, str) else '(no rule-id)'}"
            if bad := refuter_problems(case):
                errs += [f"{label}: {e}" for e in bad]
            elif rid in refutes:
                errs.append(f"{label}: the refuter attacks the rule twice")
            elif rid not in readers and not any(isinstance(c, dict) and c.get("rule-id") == rid for c in doc["records"]):
                errs.append(f"{label}: the refuter attacks a rule the reader did not read")
            else:
                refutes[rid] = case
        for rid, case in readers.items():
            refute = refutes.get(rid)
            if case["verdict"] == CONFORMS and refute is None:
                errs.append(f"{name}: {rid}: a CONFORMS read with no refuter's case")
            elif case["verdict"] not in (CONFORMS, READER_UNDECIDED) and refute is not None:
                errs.append(f"{name}: {rid}: a refuter's case on a {case['verdict']} read: the refuter attacks only CONFORMS")
            else:
                new[rid] = record_of(case, refute, ctx.rows[rid]["rule-digest"], pin, name, sources)
    if errs:
        raise SystemExit(f"{TOOL}: {len(errs)} problem(s) in the reads of {year} at {reads}; nothing is written:\n  "
                         + "\n  ".join(errs))
    replaced = sorted(rid for rid in new if rid in existing)
    return {**existing, **new}, sorted(new), replaced


# -- adjudication and the mechanism note ------------------------------------------------------------------------------------

def _one(ctx: Context, directory: pathlib.Path, rid: str) -> tuple[dict[str, dict[str, Any]], dict[str, Any]]:
    records = load(ctx, directory)
    if rid not in records:
        raise SystemExit(f"{TOOL}: {rid} has no code read in {directory}")
    return records, records[rid]


def attach_work(ctx: Context, directory: pathlib.Path, rid: str, work: str) -> dict[str, dict[str, Any]]:
    """The store with `rid`'s record naming the kb/Work note `work` of its mechanism."""
    records, record = _one(ctx, directory, rid)
    if e := work_problem(work, ctx.root) or landed_at_pin_problem(work, record["read"]["pin"], ctx.root):
        raise SystemExit(f"{TOOL}: {rid}: {e}")
    records[rid] = {**record, "work": work}
    return records


def adjudicate(ctx: Context, directory: pathlib.Path, rid: str, verdict: str, reason: str, ref: str,
               work: str | None, replace: bool = False) -> dict[str, dict[str, Any]]:
    """The store with `rid`'s record carrying an adjudication: the verdict a tracked file `ref` decides, and why. A verdict
    that is not CONFORMS makes the record contradicted, which names its `work`."""
    records, record = _one(ctx, directory, rid)
    problems = []
    if verdict not in ctx.schema.verdicts:
        problems.append(f"--verdict {verdict!r} is not one of the inventory's verdicts {sorted(ctx.schema.verdicts)}")
    if not reason.strip():
        problems.append("--reason is empty")
    if e := ref_problem(ref, ctx.root):
        problems.append(e)
    if work is not None and (e := work_problem(work, ctx.root)
                             or landed_at_pin_problem(work, record["read"]["pin"], ctx.root)):
        problems.append(e)
    if verdict != CONFORMS and work is None and "work" not in record:
        problems.append(f"a {verdict} adjudication names its mechanism's note: --work PBnnnn")
    if "adjudication" in record and not replace:
        problems.append(f"already adjudicated {record['adjudication']['verdict']} ({record['adjudication']['ref']}); "
                        "--replace to replace it")
    if problems:
        raise SystemExit(f"{TOOL}: adjudicate {rid}:\n  " + "\n  ".join(problems))
    records[rid] = {**record, "adjudication": {"verdict": verdict, "reason": reason.strip(), "ref": ref},
                    **({"work": work} if work is not None else {})}
    return records


def withdraw(ctx: Context, directory: pathlib.Path, rid: str, reason: str) -> tuple[dict[str, dict[str, Any]], dict[str, Any]]:
    """The store without `rid`'s record, and the record removed. For a read that is defective AS A READ (a location that
    never resolved at its own pin, `verify-pins`), never for one someone disagrees with (`adjudicate`) and never for one whose
    code has moved since (`moved_since`: owed a re-read, kb/Work PB5863); the row is owed a read again (kb/Work PB5842)."""
    if not reason.strip():
        raise SystemExit(f"{TOOL}: withdraw {rid}: --reason is empty")
    records, record = _one(ctx, directory, rid)
    del records[rid]
    return records, record


# -- the plan, the counts, the audit ----------------------------------------------------------------------------------------

def owed(ctx: Context, records: dict[str, dict[str, Any]], witnessed: bool = False) -> list[str]:
    """The KEPT rows of `ctx`'s edition still owed a read (`code_read_store.owed`)."""
    return store.owed(ctx.rows, ctx.schema, records, witnessed)


def moved_since(record: dict[str, Any], ctx: Context, tree: code_location.Tree | None = None) -> list[str]:
    """THE PREDICATE of kb/Work PB5863: the entries of `record`'s `code-location` that no longer resolve in the working tree
    (`tree`, by default the tree at `ctx.root`), each with why; empty when none has moved. The read resolved at its pin (the
    import's rule), so an entry that does not resolve now names code that moved since the read: the row is owed a re-read,
    and nothing is wrong with the record."""
    return location_problems(record["reader"], tree or code_location.working_tree(ctx.root), ctx)


@dataclasses.dataclass(frozen=True)
class ReRead:
    """A row owed a RE-READ and why: `fixed`, the note of its contradiction that has landed since the read
    (`fixed_since`), and `moved`, the entries of its location that no longer resolve (`moved_since`). One or both."""
    rule_id: str
    fixed: str | None
    moved: list[str]


def reread(ctx: Context, records: dict[str, dict[str, Any]], witnessed: bool = False) -> list[ReRead]:
    """The rows owed a RE-READ, in the catalog's order: each a DECIDED record (confirmed or contradicted: an undecided or
    stale one is in `owed` already) that is `fixed_since` its read (kb/Work PB5854) or `moved_since` it (kb/Work PB5863).
    `witnessed` keeps those that carry a test-ref of their own, as `owed` does. The record stays in the store, counted by
    its outcome, until the re-read replaces it (`import --replace`)."""
    tree = code_location.working_tree(ctx.root)
    out = []
    with pins_open(ctx.root):
        for rid in ctx.rows:
            record = records.get(rid)
            if record is None or outcome(record, ctx) not in (CONFIRMED, CONTRADICTED):
                continue
            if witnessed and not lineage.own_witness_refs(ctx.rows[rid], ctx.schema):
                continue
            fixed = record["work"] if fixed_since(record, ctx) else None
            moved = moved_since(record, ctx, tree)
            if fixed or moved:
                out.append(ReRead(rid, fixed, moved))
    return out


def plan_doc(ctx: Context, records: dict[str, dict[str, Any]], witnessed: bool = False) -> dict[str, Any]:
    """What `plan --json` prints: the rows owed a read, and the rows owed a re-read, each with why it is there."""
    return {"owed": owed(ctx, records, witnessed),
            "re-read": [{"rule-id": r.rule_id, "fixed": r.fixed, "moved": r.moved} for r in reread(ctx, records, witnessed)]}


def reread_text(r: ReRead) -> str:
    why = ([f"fixed: {r.fixed} has landed since the read"] if r.fixed else []) + [f"moved: {m}" for m in r.moved]
    return f"{r.rule_id} ({'; '.join(why)})"


def verify_pins(ctx: Context, records: dict[str, dict[str, Any]]) -> dict[str, list[str]]:
    """Every record whose `code-location` does not resolve in the tree of its OWN pin, with why (kb/Work PB5863): the import
    refuses such a read now; this lists the ones imported before it did, for the dispatching session to judge. A pin that
    is no commit of the repository is named as such (a shallow checkout lacks old commits: this is a hand verb, never part
    of `--check`)."""
    out: dict[str, list[str]] = {}
    with pins_open(ctx.root) as pins:
        for rid, record in sorted(records.items()):
            pin = record["read"]["pin"]
            if not pins.is_commit(pin):
                out[rid] = [f"the pin {pin} is no commit of the repository at {ctx.root}"]
            elif bad := location_problems(record["reader"], pins.at(pin), ctx):
                out[rid] = bad
    return out


def older_years(root: pathlib.Path = REPO) -> list[int]:
    """Every older held edition that has a store of code reads."""
    return [e.year for e in inventory_schema.load_schema().all_editions
            if e.held and not e.newest and store.load(e.year, root)]


def check_edition(ctx: Context, records: dict[str, dict[str, Any]]) -> bool:
    """The audit of one edition's store (`check`): prints its counts, the reads owed a re-read (information) and every
    problem; True when it is RED."""
    with pins_open(ctx.root):
        problems = {rid: record_problems(r, ctx) for rid, r in sorted(records.items())}
        errs = [f"{rid}: {e}" for rid, es in problems.items() for e in es]
        # `reread` asks the tree and the pin: a record that does not stand is no question for it
        again = reread(ctx, records) if not errs else []
    c = store.counts(ctx.rows, ctx.schema, {rid: r for rid, r in records.items() if not problems[rid]})
    print(f"{ctx.ed.year}: {len(records)} code read(s); {store.count_text(c)}")
    if not errs:
        if again:
            print(f"  {len(again)} read(s) owed a re-read (information, never red): "
                  f"{sum(1 for r in again if r.fixed)} whose fix has landed since its read, "
                  f"{sum(1 for r in again if r.moved)} whose code has moved since its read: `plan` lists them")
        else:
            print("  no read is owed a re-read: no fix has landed and no code has moved since a read")
    for e in errs:
        print(f"  ⛔ {e}")
    return bool(errs)


def check(years: list[int]) -> int:
    """THE GATE'S CODE READS AUDIT over `years` (module docstring)."""
    red: list[int] = []
    schema = inventory_schema.load_schema()
    for y in years:
        ctx = Context.of(y, schema=schema)
        try:
            records = load(ctx)
        except SystemExit as e:
            print(f"  ⛔ {e.code}")
            red.append(y)
            continue
        if check_edition(ctx, records):
            red.append(y)
    print(f"=== CODE READS: {'GREEN' if not red else f'RED ({red})'} ===")
    return 1 if red else 0


# -- the command line -------------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The code reads of an older edition's rules (kb/Work PB5759).")
    ap.add_argument("--check", type=int, nargs="?", const=0, metavar="YEAR",
                    help="every stored code read stands; with no YEAR every older held edition with a store: the gate's "
                         "CODE READS audit")
    sub = ap.add_subparsers(dest="cmd")
    im = sub.add_parser("import", help="the reader's and the refuter's files of an edition's slices become records")
    im.add_argument("--edition", type=int, required=True)
    im.add_argument("--reads", required=True, help="a directory of <slice>.json and <slice>-refute.json")
    im.add_argument("--pin", required=True, help="the commit the code was read at (40 hexadecimal digits)")
    im.add_argument("--record", help="the store to write (default the tracked one; another directory is a dry run)")
    im.add_argument("--replace", action="store_true", help="a read of a rule already recorded replaces its record")
    wk = sub.add_parser("work", help="name the kb/Work note of a record's mechanism")
    wk.add_argument("--edition", type=int, required=True)
    wk.add_argument("rule")
    wk.add_argument("note")
    ad = sub.add_parser("adjudicate", help="record the verdict a tracked file decides for a disagreement")
    ad.add_argument("--edition", type=int, required=True)
    ad.add_argument("rule")
    ad.add_argument("--verdict", required=True)
    ad.add_argument("--reason", required=True)
    ad.add_argument("--ref", required=True, help="the tracked file that decides it (repository-relative)")
    ad.add_argument("--work", help="the kb/Work note of the mechanism (required of a verdict that is not CONFORMS)")
    ad.add_argument("--replace", action="store_true")
    wd = sub.add_parser("withdraw", help="remove the record of a read that is defective as a read (its row is owed again)")
    wd.add_argument("--edition", type=int, required=True)
    wd.add_argument("rule")
    wd.add_argument("--reason", required=True)
    pl = sub.add_parser("plan", help="the KEPT rows still owed a read: what the campaign's slices are built from")
    pl.add_argument("--edition", type=int, required=True)
    pl.add_argument("--witnessed", action="store_true", help="only the rows that carry a test-ref of their own")
    pl.add_argument("--json", action="store_true")
    vp = sub.add_parser("verify-pins", help="resolve every stored read's code-location at its own pin (a hand verb; "
                                            "not part of --check)")
    vp.add_argument("--edition", type=int, required=True)
    a = ap.parse_args(argv)
    if a.cmd is None:
        if a.check is None:
            ap.print_help()
            return 2
        return check([a.check] if a.check else older_years())
    ctx = Context.of(a.edition)
    tracked = store.store_dir(a.edition)
    if a.cmd == "plan":
        records = load(ctx)
        if a.json:
            print(json.dumps(plan_doc(ctx, records, a.witnessed), indent=1))
        else:
            rows, again = owed(ctx, records, a.witnessed), reread(ctx, records, a.witnessed)
            print(f"plan {a.edition}: {len(rows)} KEPT row(s){' with a witness of their own' if a.witnessed else ''} "
                  "owed a code read")
            print("\n".join(rows))
            print(f"plan {a.edition}: {len(again)} row(s) owed a RE-READ (import --replace): "
                  f"{sum(1 for r in again if r.fixed)} whose fix has landed since the read, "
                  f"{sum(1 for r in again if r.moved)} whose code has moved since the read")
            print("\n".join(reread_text(r) for r in again))
        return 0
    if a.cmd == "verify-pins":
        records = load(ctx)
        bad = verify_pins(ctx, records)
        for rid, problems in bad.items():
            print(f"{rid} (pin {records[rid]['read']['pin'][:10]}, slice {records[rid]['read']['slice']}):")
            print("\n".join(f"  {p}" for p in problems))
        pins = len({r["read"]["pin"] for r in records.values()})
        print(f"verify-pins {a.edition}: {len(records)} record(s) at {pins} pin(s); {len(bad)} whose code-location does not "
              "resolve at its own pin")
        return 1 if bad else 0
    if a.cmd == "import":
        directory = pathlib.Path(a.record) if a.record else tracked
        records, imported, replaced = import_reads(ctx, pathlib.Path(a.reads), directory, a.pin, a.replace)
        store.write(records, directory)
        by = Counter(outcome(records[rid], ctx) for rid in imported)
        print(f"import {a.edition}: {len(imported)} read(s) -> {directory}: "
              + " · ".join(f"{by.get(o, 0)} {o}" for o in OUTCOMES))
        for rid in replaced:
            print(f"  replaced the earlier record of {rid}")
        return 0
    if a.cmd == "withdraw":
        records, removed = withdraw(ctx, tracked, a.rule, a.reason)
        store.write(records, tracked)
        print(f"withdraw {a.rule}: removed the record of slice {removed['read']['slice']} at pin {removed['read']['pin']} "
              f"({outcome(removed, ctx)}); reason: {a.reason.strip()}; the row is owed a read again")
        return 0
    if a.cmd == "work":
        store.write(attach_work(ctx, tracked, a.rule, a.note), tracked)
    else:
        store.write(adjudicate(ctx, tracked, a.rule, a.verdict, a.reason, a.ref, a.work, a.replace), tracked)
    record = load(ctx)[a.rule]
    print(f"{a.cmd} {a.rule}: {outcome(record, ctx)}" + (f", work {record['work']}" if "work" in record else ""))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
