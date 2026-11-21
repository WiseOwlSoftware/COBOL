#!/usr/bin/env python3
"""THE READ RECORDS of an older edition's rules: the durable record of each READ decision of a rule against its
counterpart in the next edition, and what may become a kept change record (kb/Work PB5597; DESIGN-spec-conformance-
review.md section 10.6).

    python scripts/selftests/spec/lineage_reads/test_reads.py         # its cases (the case layout, kb/Work PB5428)
    python scripts/spec/audit_batch.py import-reads --edition 2002 --reads DIR [--asked DIR] --state NEW
                                                    # the IMPORT: DIR's <slice>-read.json and <slice>-refute.json become
                                                    # the tracked read records and an audit run's state (audit_batch.py)
    python scripts/spec/lineage_reads.py --check [YEAR]                 # the gate's LINEAGE READS audit
    python scripts/spec/lineage_reads.py widen --edition YEAR [--state DIR] --out PLAN [--apply --compiler EXE --scratch DIR]
                                                    # widen the witnesses of the KEPT rows with none of their own
    python scripts/spec/lineage_reads.py witness-batch --edition YEAR --out BATCH  # the stored KEPT rows' own witnesses

THE OWNER'S RULE (2026-11-14): "2002 spec covers 2002 rules. It should not matter to the 2002 rules whether or not 2023
removes/adds/changes a clause." An older rule is judged on its own edition's text at its own `--std`; the next edition's
rule is only a POINTER to the code and to candidate tests. So a row closes only on evidence produced at its own edition.

A READ is a reader's verdict on one rule (SAME, DIFFERENT, DOUBTFUL: what the rule governs in its own text and the
evidence at its edition) and a refuter's attack on every SAME (UPHELD, OVERTURNED, DOUBTFUL). The reads arrive as JSON
files a fleet writes in scratch, one pair per slice; `audit_batch.py import-reads` copies each into the repository,
verbatim, as `tests/version-matrix/lineage-reads-<year>/<slice>.json` (`record_path`), with the slice's own questions (the
flags the reader was asked) and the digest of every file it came from, so a change record names the record that justifies
it (`next.source` is `lineage-reads-<year>/<slice>.json`) and the record names its reading. `load` NORMALIZES every
record in ONE place (`normal_read`, `normal_refute`): the shapes vary between slices (a citations list may be a string, a
flags-answered list may be a map) and a record it cannot interpret fails loudly by slice and rule, never a default.

WHAT MAY BECOME A KEPT RECORD (`decide`, which the writer `audit_batch.change_batch` and this module's `--check` share):
  1. the reader's verdict is SAME and the refuter's is UPHELD; a SAME with no refuter record is refused (`NO_REFUTER`);
  2. every flag the edition's lineage screen raises on the pair is answered by trigger, for the flag's detail as the
     screen now gives it (`next.answers`: lineage_screen.answer_problems). The read's `flags-answered` is the only source:
     an answer counts when it names the trigger and the detail it answered is the screen's (the detail its own key names,
     or the one the slice asked), and a flag with no such answer refuses the row (`FLAG_UNANSWERED`), to be read again,
     never answered by a script;
  3. the evidence is at the row's OWN edition: every next-edition rule the record carries into has a conformance-suite
     case among its witnesses that runs at the edition (the generator places it there: its `accept` or its flag's `at`
     names the edition, and every rule it cites has the edition in its band), so the gate's run of that edition's suite
     passes it (`Witnesses.search`, `NO_OWN_WITNESS`), and the cases found are written as the row's OWN `test-ref`
     (kb/Work PB5675: one rule at every older edition, no edition key). `widen` widens the witnesses that can run there.
     At 1985 the pairing (word-identical text) is empty, so no new 1985 record is KEPT: 1985 is the earliest edition,
     and its rows are recorded with their own verdicts, each with a test-ref that runs at 85.
WHAT THE OTHER VERDICTS BECOME (`decide`'s work items, never a kept record): a DIFFERENT, or a SAME the refuter overturned,
is `needs-own-witness` when its reader or refuter showed the compiler correct at the edition (the probe is the candidate
witness), `defect` when it showed it wrong (the records worker matches it to a kb/Work note), `undecided` when nobody
showed the compiler (or the read is DOUBTFUL), and `uninterpretable` when the judgment of the compiler is a sentence no
reading turns into one of those. The list is generated into the audit run's state (`reads-decisions.json`), never tracked:
it is a view of the read records, not a register (CLAUDE.md rule 8). `record_verdicts.py --edition YEAR` records each
once a witness or a note exists.
AN OWN VERDICT FROM A POINTER: a SAME its refuter UPHELD, of a rule no candidate pair pairs (its `to` a nearest-rule
pointer), is the work item `own-verdict-from-pointer`. `audit_batch.py own-verdict-batch` writes its record, the
pointers' one verdict with their code and, as its OWN `test-ref`, the cases the candidate search finds (`own_verdict`,
`Witnesses.search`: the KEPT record's search), once `own_verdict_problems` holds nothing against it: every pointer
witnessed at the edition by that search, one resolving verdict, a declined facility's licence, a citation of the
edition, and a register entry of its own where the rule's kind owes one.

THE AUDIT (`--check`, the gate's LINEAGE READS): every read record loads and normalizes; every stored change record whose
source names a read record is that record's decision (the read SAME, the refuter UPHELD, the same `to`); and, at every
older held edition, every KEPT row's own witness (its `test-ref`) is a suite case that runs at the edition, of the kind
its rule needs (`Witnesses.own_problems`, which reads no target); and every row's own suite witnesses are written in a
reference format the edition defines (`format_problem`, kb/Work PB5754: before 2002 a free-form case witnesses nothing).
Every stored OWN verdict of a rule whose tracked read decides `own-verdict-from-pointer` is held to the same test of its
own `test-ref`, whatever its notes say, and one whose notes name its read (`OWN_SOURCE`) to that read's standing as well
(`own_stored_errors`). A later change that narrows a witness's band, moves
it, or rewrites a read, turns it red, naming the row and the case; a KEPT row with no witness of its own is a GAP. (A flag the screen raises AFTER a record is stored is the screen's change, listed as owed by
lineage_screen.py --check, never red: kb/Work PB5597, branch review R2-1 of PB5589.)
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import functools
import hashlib
import json
import pathlib
import re
import subprocess
import sys
from typing import Any, Iterable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import inventory_schema  # noqa: E402  (the editions, the repository root, the test-ref forms)
import inventory_view  # noqa: E402  (each edition's rows: the next edition's witnesses, the stored records)
import lineage  # noqa: E402  (the candidate pairing's file, the form of a suite witness)
import lineage_screen  # noqa: E402  (the flags a decision answers)
import rule_moves  # noqa: E402  (the tracked moves a read record follows: kb/Work PB6020)
from inventory_schema import Edition, edition  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "lineage_reads"
REPO = inventory_schema.REPO
SUITE = REPO / "conformance-suite"

READ_VERDICTS = ("SAME", "DIFFERENT", "DOUBTFUL")
REFUTE_VERDICTS = ("UPHELD", "OVERTURNED", "DOUBTFUL")
#: The judgments of the compiler at the edition a reader or refuter states (`compiler-at-<year>`, the read's OWN edition:
#: `compiler-at-2002` in a 2002 read, `compiler-at-1985` in a 1985 one; kb/Work PB5453).
CORRECT, WRONG, NOT_PROBED = "correct", "wrong", "not probed"
COMPILER = re.compile(r"\A\s*(correct|wrong|not probed)\b", re.I)
#: THE EDITION-KEYED FIELDS of a read or refute case (kb/Work PB5453): each names the edition it speaks of, and a case
#: speaks only of its record's own edition. `compiler-at` and `licence` are read (`_judgment`, `normal_read`); `scope`
#: and `evidence` are the brief's own-edition fields, kept verbatim. A key of these families that names another edition,
#: or no edition at all, is REFUSED by slice and rule, never dropped: until 2026-11-21 `_judgment` read only
#: `compiler-at-2002`, so a 1985 read's `compiler-at-1985` was ignored and 13 rows the pilot proved correct imported
#: `undecided`. A suffix after the year (`compiler-at-2002-probe`, `answer-evidence-2002`) is the same edition's.
EDITION_KEY = re.compile(r"(?:\A|-)(compiler-at|licence|scope|evidence)(?:-(?P<year>[^-]*))?(?=-|\Z)")
#: Why a SAME read upheld by its refuter is still refused a kept record.
NO_REFUTER, FLAG_UNANSWERED, NO_OWN_WITNESS = "no refuter record", "flag unanswered", "no own-edition witness"
#: ... and why a read's targets cannot be a KEPT record's (kb/Work PB5597, branch review R1-1, R1-2).
NO_LICENCE, NOT_THE_PAIRING = "no own-edition licence stated", "targets are not the pairing's"
#: The work items of the reads that are not kept. A SAME for a rule with no candidate pair (its `to` a nearest-rule
#: POINTER, or empty) is never a KEPT record: the row is recorded with its own verdict, citing the pointer's witness
#: once that runs at the edition (`OWN_VERDICT_FROM_POINTER`).
NEEDS_OWN_WITNESS, DEFECT, UNDECIDED, UNINTERPRETABLE = "needs-own-witness", "defect", "undecided", "uninterpretable"
OWN_VERDICT_FROM_POINTER = "own-verdict-from-pointer"
KEPT = "KEPT"
#: A slice's name is an opaque label (`p-01`, `q-44`, `t-annexA`, `u-07`): any name a file can carry and that pairs a
#: `<name>-read.json` with its `<name>-refute.json`.
SLICE = re.compile(r"\A[A-Za-z0-9][A-Za-z0-9_.-]*\Z")
#: THE EVIDENCE TABLE (data, kb/Work PB5597 branch review R1-1): which catalog `kind`s a DIAGNOSIS can witness, and the
#: verdict of a next-edition rule whose facility the compiler declines.
EVIDENCE_TABLE = pathlib.Path(__file__).resolve().parent / "data" / "lineage-evidence-kinds.json"
#: The head of a flags-answered entry: the trigger's name first.
FLAG_HEAD = re.compile(r"\A\s*([A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)(?![A-Za-z0-9-])")


class RecordError(ValueError):
    """A read record no reading can interpret: its slice, its rule and what is wrong."""


def reads_dir(year: int, repo: pathlib.Path = REPO) -> pathlib.Path:
    """The tracked read records of the older edition `year` (one file per slice)."""
    return repo / "tests" / "version-matrix" / f"lineage-reads-{year}"


def source_of(year: int, slice_name: str) -> str:
    """The `next.source` of a change record a read decided: the read record's path under tests/version-matrix/."""
    return f"lineage-reads-{year}/{slice_name}.json"


# -- normalization: the ONE place a read's shape is read --------------------------------------------------------------

@dataclasses.dataclass(frozen=True)
class Judgment:
    """A reader's or refuter's judgment of the compiler at the edition: one of CORRECT, WRONG, NOT_PROBED, or None when the
    sentence names none of them (`UNINTERPRETABLE` when it is needed), with the sentence as written."""
    kind: str | None
    text: str


@dataclasses.dataclass(frozen=True)
class Read:
    rule_id: str
    slice: str
    to: tuple[str, ...]
    verdict: str
    reason: str
    probe: str | None
    compiler: Judgment | None
    #: trigger -> each (detail the answer names, or None when it names none; the answer): a trigger whose flag named
    #: several terms may be answered once per term
    answers: dict[str, list[tuple[str | None, str]]]
    #: The read's `licence-<year>` as written (None when absent or empty): `licence_problem` decides whether it is one.
    licence: str | None = None
    #: Each citation the read names, as written (`cite`): an own verdict's notes carry those of the row's own edition.
    citations: tuple[str, ...] = ()
    #: The derivation the reader proposes for a rule no program can witness (DESIGN-spec-conformance-review.md section
    #: 1.1; `derivation_problems`, `derivation_record`): None when it proposes none.
    derivation: "Derivation | None" = None
    #: The slice whose read of the same rule this one REPLACES (a second read, `supersedes`; `supersede`): None for a
    #: first read.
    supersedes: str | None = None


@dataclasses.dataclass(frozen=True)
class Derivation:
    """A reader's proposed derivation (section 1.1): one of the schema's arms, its `Names` cell, and the code location
    the row's verdict needs when the store holds no record of the rule (None when the read gives none)."""
    arm: str
    names: str
    code_location: str | None


@dataclasses.dataclass(frozen=True)
class Refute:
    rule_id: str
    slice: str
    verdict: str
    reason: str
    probe: str | None
    compiler: Judgment | None
    #: Each citation the refuter names, as written (`cite`): a derivation's refuter record carries them.
    citations: tuple[str, ...] = ()
    #: A derivation refuter's `attempt`, the witness it tried to write (section 1.1): None for a SAME's refuter.
    attempt: str | None = None


def _fail(slice_name: str, rule: Any, message: str) -> RecordError:
    return RecordError(f"{slice_name}: {rule if isinstance(rule, str) else '(no rule-id)'}: {message}")


def _text(case: dict[str, Any], key: str, slice_name: str, rid: Any, required: bool = True) -> str | None:
    v = case.get(key)
    if v is None and not required:
        return None
    if not isinstance(v, str) or (required and not v.strip()):
        raise _fail(slice_name, rid, f"`{key}` must be a non-empty string, not {v!r}")
    return v


def _citations(case: dict[str, Any], slice_name: str, rid: str) -> list[dict[str, Any]]:
    """A citations field as a list of {cite, quote, check}: a list of such maps, a list of strings, or one string, each
    string a citation alone. Anything else is uninterpretable."""
    raw = case.get("citations")
    items = [raw] if isinstance(raw, str) else raw
    if not isinstance(items, list):
        raise _fail(slice_name, rid, f"`citations` must be a list or a string, not {type(raw).__name__}")
    out = []
    for c in items:
        if isinstance(c, str) and c.strip():
            out.append({"cite": c.strip()})
        elif isinstance(c, dict) and isinstance(c.get("cite"), str) and c["cite"].strip():
            out.append(c)
        else:
            raise _fail(slice_name, rid, f"a citation must be a string or a map with `cite`, not {c!r}")
    return out


def _edition_keys(case: dict[str, Any], slice_name: str, rid: Any, year: int) -> None:
    """Refuse a case key of the edition-keyed families (`EDITION_KEY`) that names another edition than the record's own
    `year`, or none: the field would otherwise be silently unread (kb/Work PB5453)."""
    for key in case:
        if isinstance(key, str) and (m := EDITION_KEY.search(key)) and m.group("year") != str(year):
            raise _fail(slice_name, rid, f"`{key}` speaks of {'no edition' if m.group('year') is None else 'edition ' + repr(m.group('year'))}, "
                                         f"and a {year} record's case speaks only of {year}: write `{m.group(1)}-{year}`")


def _judgment(case: dict[str, Any], slice_name: str, rid: str, year: int) -> Judgment | None:
    text = _text(case, f"compiler-at-{year}", slice_name, rid, required=False)
    if text is None:
        return None
    m = COMPILER.match(text)
    return Judgment(m.group(1).lower() if m else None, text)


def _flag_answers(case: dict[str, Any], slice_name: str, rid: str) -> dict[str, list[tuple[str | None, str]]]:
    """`flags-answered`: a list of `"<trigger>: <answer>"` lines, or a map of `"<trigger>[: <detail>]"` to the answer."""
    raw = case.get("flags-answered")
    if raw is None:
        return {}
    heads: list[tuple[str, str]] = []
    if isinstance(raw, list):
        for line in raw:
            if not isinstance(line, str) or ":" not in line:
                raise _fail(slice_name, rid, f"a flags-answered line is `<trigger>[ <detail>]: <answer>`, not {line!r}")
            head, _, answer = line.partition(":")
            heads.append((head, answer))
    elif isinstance(raw, dict):
        for key, answer in raw.items():
            if not isinstance(answer, str):
                raise _fail(slice_name, rid, f"flags-answered[{key!r}] must be a sentence, not {answer!r}")
            heads.append((key, answer))
    else:
        raise _fail(slice_name, rid, f"`flags-answered` must be a list or a map, not {type(raw).__name__}")
    out: dict[str, list[tuple[str | None, str]]] = {}
    for head, answer in heads:
        # The head is the trigger's name, then the detail it answers, if it names one: `cross-reference 8.8.4`,
        # `changed-definition-term: external data`, `changed-definition-term 'procedure'`, `target-not-CONFORMS-OK (...)`.
        m = FLAG_HEAD.match(head)
        answer = answer.strip()
        if m is None or not answer:
            raise _fail(slice_name, rid, f"a flags-answered entry names a trigger and answers it, not {head!r}: {answer!r}")
        name = m.group(1)
        rest = head[m.end():].strip().lstrip(":").strip()
        detail = rest[1:-1].strip() if len(rest) > 1 and rest[0] + rest[-1] in ("''", '""', "()") else rest
        if any(d == (detail or None) for d, _a in out.get(name, [])):
            raise _fail(slice_name, rid, f"flags-answered answers {name!r} for {detail or 'no detail'!r} twice")
        out.setdefault(name, []).append((detail or None, answer))
    return out


#: The read class whose rule no program can show (BRIEF-reader step 4): its read proposes a `derivation`.
DEFINITIONAL_CLASS = "definitional"


def _derivation(case: dict[str, Any], slice_name: str, rid: str, verdict: str) -> Derivation | None:
    """A read's proposed derivation: `{"arm", "names"[, "code-location"]}` on a DIFFERENT read, its arm one of the
    schema's (section 1.1). A `definitional` class with none, a derivation on a SAME or DOUBTFUL read, an unknown arm or
    an empty `names` is refused loudly: the read would otherwise land `undecided` with its proposal unread."""
    raw = case.get("derivation")
    if raw is None:
        # A DOUBTFUL definitional read is a reader who found no arm that fits (a row for a ruling): it proposes nothing.
        if case.get("class") == DEFINITIONAL_CLASS and verdict == "DIFFERENT":
            raise _fail(slice_name, rid, f"a DIFFERENT read of class `{DEFINITIONAL_CLASS}` proposes a derivation: the "
                                         "read carries no `derivation` {arm, names}")
        return None
    if verdict != "DIFFERENT":
        raise _fail(slice_name, rid, f"a derivation is proposed by a DIFFERENT read (no pointer states the rule), not a "
                                     f"{verdict}")
    if not isinstance(raw, dict):
        raise _fail(slice_name, rid, f"`derivation` is a map {{arm, names[, code-location]}}, not {raw!r}")
    arms = sorted(_schema().derivation.arms)
    arm = raw.get("arm")
    if arm not in arms:
        raise _fail(slice_name, rid, f"the derivation's arm {arm!r} is not one of the schema's {arms}")
    names = _text(raw, "names", slice_name, rid)
    loc = _text(raw, "code-location", slice_name, rid, required=False)
    return Derivation(arm, names.strip(), loc.strip() if loc and loc.strip() else None)


def normal_read(slice_name: str, case: Any, year: int) -> Read:
    if not isinstance(case, dict):
        raise _fail(slice_name, None, f"a read case is a map, not {type(case).__name__}")
    rid = _text(case, "rule-id", slice_name, case.get("rule-id"))
    _edition_keys(case, slice_name, rid, year)
    to = case.get("to")
    if not isinstance(to, list) or not all(isinstance(t, str) and t for t in to):
        raise _fail(slice_name, rid, f"`to` must be a list of rule ids (empty for a rule with no pointer), not {to!r}")
    verdict = case.get("verdict")
    if verdict not in READ_VERDICTS:
        raise _fail(slice_name, rid, f"the reader's verdict {verdict!r} is not one of {READ_VERDICTS}")
    cites = tuple(c["cite"].strip() for c in _citations(case, slice_name, rid))
    licence = _text(case, f"licence-{year}", slice_name, rid, required=False)
    supersedes = case.get("supersedes")
    if supersedes is not None and (not isinstance(supersedes, str) or not SLICE.match(supersedes)
                                   or supersedes == slice_name):
        raise _fail(slice_name, rid, f"`supersedes` names the slice whose read this one replaces, not {supersedes!r}")
    return Read(rid, slice_name, tuple(to), verdict, _text(case, "reason", slice_name, rid),
                _text(case, "probe", slice_name, rid, required=False), _judgment(case, slice_name, rid, year),
                _flag_answers(case, slice_name, rid),
                licence.strip() if licence and licence.strip() else None, cites,
                _derivation(case, slice_name, rid, verdict), supersedes)


def normal_refute(slice_name: str, case: Any, year: int, derivation: bool = False) -> Refute:
    """A refuter's case. A DERIVATION refuter's (`derivation`, the refuter brief's shape) states the witness it tried to
    write (`attempt`, required) and may cite nothing: its argument is the attempt and its reason."""
    if not isinstance(case, dict):
        raise _fail(slice_name, None, f"a refute case is a map, not {type(case).__name__}")
    rid = _text(case, "rule-id", slice_name, case.get("rule-id"))
    _edition_keys(case, slice_name, rid, year)
    verdict = case.get("verdict")
    if verdict not in REFUTE_VERDICTS:
        raise _fail(slice_name, rid, f"the refuter's verdict {verdict!r} is not one of {REFUTE_VERDICTS}")
    cites = () if derivation and case.get("citations") is None else \
        tuple(c["cite"].strip() for c in _citations(case, slice_name, rid))
    attempt = _text(case, "attempt", slice_name, rid) if derivation else None
    return Refute(rid, slice_name, verdict, _text(case, "reason", slice_name, rid),
                  _text(case, "probe", slice_name, rid, required=False), _judgment(case, slice_name, rid, year), cites,
                  attempt)


@dataclasses.dataclass
class Slice:
    """One read record: its reads and refutes by rule, the flags the slice asked, and the reads its refuter disputes."""
    name: str
    reads: dict[str, Read]
    refutes: dict[str, Refute]
    asked: dict[str, dict[str, str]]
    disputed: set[str]
    #: the derivation refuter's case of each read that proposes a derivation (`<slice>-derivation-refute.json`): it tried
    #: to write the witness the derivation says cannot exist (section 1.1)
    derivation_refutes: dict[str, Refute] = dataclasses.field(default_factory=dict)


#: The parts of a read record, each imported verbatim from its own file with its digest (`import_records`): the reader's
#: `<slice>-read.json`, the refuter's `<slice>-refute.json`, the derivation refuter's `<slice>-derivation-refute.json` and
#: the slice's input `<slice>.json` (its asked flags).
PARTS = {"read": "-read.json", "refute": "-refute.json", "derivation-refute": "-derivation-refute.json"}


def normal_slice(name: str, doc: dict[str, Any]) -> Slice:
    """A read record (the tracked file's content) as a `Slice`; a RecordError names the slice and rule of the first case
    it cannot interpret. A rule read twice, a refute of a rule the slice did not read, a refute of a read that is not
    SAME, or a derivation refute of a read that proposes no derivation is uninterpretable too. Every case is read at the
    record's own `edition` (`_edition_keys`)."""
    year = doc.get("edition")
    if not isinstance(year, int) or isinstance(year, bool):
        raise _fail(name, None, f"the record names no edition year (`edition`: {year!r}): its cases' fields are keyed by it")
    read = doc.get("read")
    if not isinstance(read, dict) or not isinstance(read.get("cases"), list):
        raise _fail(name, None, "the record holds no reader's `cases`")
    reads: dict[str, Read] = {}
    for c in read["cases"]:
        r = normal_read(name, c, year)
        if r.rule_id in reads:
            raise _fail(name, r.rule_id, "the reader decides the rule twice")
        reads[r.rule_id] = r
    derivation_refutes: dict[str, Refute] = {}
    if (dref := doc.get("derivation-refute")) is not None:
        if not isinstance(dref, dict) or not isinstance(dref.get("cases"), list):
            raise _fail(name, None, "the derivation refuter's record holds no `cases`")
        for c in dref["cases"]:
            f = normal_refute(name, c, year, derivation=True)
            if f.rule_id in derivation_refutes:
                raise _fail(name, f.rule_id, "the derivation refuter decides the rule twice")
            if f.rule_id not in reads or reads[f.rule_id].derivation is None:
                raise _fail(name, f.rule_id, "the derivation refuter attacks a rule whose read proposes no derivation")
            if (arm := c.get("arm")) is not None and arm != reads[f.rule_id].derivation.arm:
                raise _fail(name, f.rule_id, f"the derivation refuter attacks the arm {arm!r}; the read proposes "
                                             f"{reads[f.rule_id].derivation.arm!r}")
            derivation_refutes[f.rule_id] = f
    refutes: dict[str, Refute] = {}
    disputed: set[str] = set()
    if (refute := doc.get("refute")) is not None:
        if not isinstance(refute, dict) or not isinstance(refute.get("cases"), list):
            raise _fail(name, None, "the refuter's record holds no `cases`")
        for c in refute["cases"]:
            f = normal_refute(name, c, year)
            if f.rule_id in refutes:
                raise _fail(name, f.rule_id, "the refuter decides the rule twice")
            if f.rule_id not in reads or reads[f.rule_id].verdict != "SAME":
                raise _fail(name, f.rule_id, "the refuter attacks a rule the reader did not decide SAME")
            refutes[f.rule_id] = f
        dd = refute.get("disputed-different", [])
        if not isinstance(dd, list):
            raise _fail(name, None, f"`disputed-different` must be a list, not {type(dd).__name__}")
        for x in dd:
            rid = x if isinstance(x, str) else x.get("rule-id") if isinstance(x, dict) else None
            if not isinstance(rid, str) or rid not in reads:
                raise _fail(name, rid, f"a disputed-different entry names a rule the reader decided, not {x!r}")
            disputed.add(rid)
    asked_doc = doc.get("asked") or {}
    flags = asked_doc.get("flags", {}) if isinstance(asked_doc, dict) else None
    if not isinstance(flags, dict) or not all(isinstance(v, dict) for v in flags.values()):
        raise _fail(name, None, "the slice's asked flags must be a map of rule to {trigger: detail}")
    return Slice(name, reads, refutes, {k: dict(v) for k, v in flags.items()}, disputed, derivation_refutes)


def supersede(slices: Iterable[Slice]) -> list[str]:
    """Resolve the SECOND READS among `slices`, in place, and return why a rule is read in more than one slice and no
    single line of second reads explains it. A rule may be read in several slices only as a line: one first read (no
    `supersedes`) and each later read naming the slice of the read it replaces, every slice of the line reading it. The
    newest read of the line (the one no read supersedes) decides the rule; every older one is taken out of its slice's
    reads, with its refuter's and derivation refuter's cases, so the decisions, the writers and the audit see one read
    of each rule. The older records stay in the tree verbatim (their digests are of their files): a second read replaces
    a decision, never the record of the first."""
    slices = list(slices)
    readers: dict[str, list[Slice]] = {}
    for s in slices:
        for rid in s.reads:
            readers.setdefault(rid, []).append(s)
    errs = []
    for rid, ss in sorted(readers.items()):
        line = {s.name: s.reads[rid].supersedes for s in ss}
        if len(ss) == 1:
            if (old := line[ss[0].name]) is not None:
                errs.append(f"{ss[0].name}: {rid}: supersedes the read of slice {old}, which does not read the rule")
            continue
        replaced = [v for v in line.values() if v is not None]
        newest = [n for n in line if n not in replaced]
        walk, at = [], newest[0] if len(newest) == 1 else None
        while at is not None and at in line and at not in walk:
            walk.append(at)
            at = line[at]
        if len(newest) != 1 or at is not None or sorted(walk) != sorted(line):
            errs.append(f"{ss[0].name}: {rid}: read again in {sorted(line)}, and no line of second reads explains it "
                        f"(each read's `supersedes`: {dict(sorted(line.items()))})")
            continue
        for s in ss:
            if s.name != walk[0]:
                s.reads.pop(rid)
                s.refutes.pop(rid, None)
                s.derivation_refutes.pop(rid, None)
                s.disputed.discard(rid)
    return errs


def record_path(year: int, slice_name: str, directory: pathlib.Path | None = None) -> pathlib.Path:
    return (directory or reads_dir(year)) / f"{slice_name}.json"


def follow_moves(slices: Iterable[Slice], moves: list[list[tuple[str, str, frozenset[str]]]]) -> None:
    """Each slice a recorded MOVE names (`rule_moves.recorded_moves`: old id, new id, the slices that read the old id
    when it was applied) reads the moved rule under its NEW id, in place (kb/Work PB6020). The record itself is sealed by
    its digests and stays as imported; a batch's moves are one simultaneous renaming (p4 to p3 and p5 to p4 at once),
    and the batches apply in order. A slice the move does not name keeps its ids: it was read after the move."""
    slices = list(slices)
    for batch in moves:
        for s in slices:
            ren = {old: new for old, new, named in batch if s.name in named}
            if not ren:
                continue
            def key(rid: str) -> str:
                return ren.get(rid, rid)
            s.reads = {key(k): dataclasses.replace(v, rule_id=key(k)) for k, v in s.reads.items()}
            s.refutes = {key(k): dataclasses.replace(v, rule_id=key(k)) for k, v in s.refutes.items()}
            s.derivation_refutes = {key(k): dataclasses.replace(v, rule_id=key(k))
                                    for k, v in s.derivation_refutes.items()}
            s.asked = {key(k): v for k, v in s.asked.items()}
            s.disputed = {key(k) for k in s.disputed}


def load(year: int, directory: pathlib.Path | None = None,
         moves: list[list[tuple[str, str, frozenset[str]]]] | None = None, superseded: bool = False) -> list[Slice]:
    """Every read record of `year`, normalized, each read of a moved rule under its new id (`follow_moves`, from the
    edition's tracked moves unless `moves` is given) and each second read replacing the read it supersedes
    (`supersede`; with `superseded`, every read stays in its slice: the record's readers, which a move names); a
    SystemExit lists every record that cannot be interpreted, and a rule two slices read with no line of second reads."""
    d = directory or reads_dir(year)
    out, errs = [], []
    for path in sorted(d.glob("*.json")) if d.is_dir() else []:
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
            if doc.get("edition") != year or doc.get("slice") != path.stem:
                raise _fail(path.stem, None, f"the record names edition {doc.get('edition')!r} and slice "
                                             f"{doc.get('slice')!r}; its file is {year}'s {path.stem}")
            s = normal_slice(path.stem, doc)
        except (RecordError, json.JSONDecodeError) as e:
            errs.append(str(e))
            continue
        if bad := digest_errors(path.stem, doc):
            errs += bad
            continue
        out.append(s)
    follow_moves(out, rule_moves.recorded_moves(year, REPO) if moves is None else moves)
    if not superseded:
        errs += supersede(out)
    if errs:
        raise SystemExit(f"{TOOL}: {len(errs)} read record(s) cannot be interpreted:\n  " + "\n  ".join(errs))
    return out


def digest(data: Any) -> str:
    """The SHA-256 of a source's CONTENT, as the record holds it (canonical JSON): what `load` recomputes from the record
    itself, so an edit of a record's reads after the import is refused (kb/Work PB5597, branch review R1-6)."""
    return hashlib.sha256(json.dumps(data, sort_keys=True, ensure_ascii=False).encode("utf-8")).hexdigest()


def digest_errors(name: str, doc: dict[str, Any]) -> list[str]:
    """Why the record `doc` does not hold what it was imported from: a part whose digest no longer matches."""
    held = doc.get("imported")
    if not isinstance(held, dict) or "read" not in held:
        return [f"{name}: the record carries no digest of its sources (`imported`)"]
    return [f"{name}: its {k} no longer matches the digest it was imported with" for k in (*PARTS, "asked")
            if (k in held) != (doc.get(k) is not None) or (k in held and held[k] != digest(doc[k]))]


def import_records(year: int, reads: pathlib.Path, asked: pathlib.Path | None, out: pathlib.Path,
                   rules: set[str] | None = None) -> list[str]:
    """Copy every slice of `reads` (`<slice>-read.json`, with its `<slice>-refute.json` when it exists, and the slice's
    `asked`/<slice>.json when `asked` is given) into the read record `out`/<slice>.json, verbatim, with the digest of each
    source's content. Whichever slices exist are imported; a refute with no read is refused. Nothing is written unless
    every slice normalizes (`normal_slice`), reads only rules of the edition's catalog `rules` (when given), and reads no
    rule another slice (imported now or already recorded) reads: the errors are listed by slice and rule. Returns the
    slices written or rewritten."""
    if not reads.is_dir():
        raise SystemExit(f"{TOOL}: {reads} is not a directory of read files")
    names = sorted(p.name[:-len("-read.json")] for p in reads.glob("*-read.json"))

    def slice_of(path: pathlib.Path) -> str:   # the longest part suffix wins: `-derivation-refute.json` over `-refute.json`
        suffix = max((s for s in PARTS.values() if path.name.endswith(s)), key=len)
        return path.name[:-len(suffix)]
    if orphans := sorted(p.name for p in reads.glob("*-refute.json") if slice_of(p) not in names):
        raise SystemExit(f"{TOOL}: refuter records with no reader's record: {orphans}")
    docs, errs = {}, []
    held: list[Slice] = []   # the records already there and not re-imported now: their rules are read too
    if out.is_dir():
        for path in sorted(out.glob("*.json")):
            if path.stem not in names:
                try:
                    held.append(normal_slice(path.stem, json.loads(path.read_text(encoding="utf-8"))))
                except (RecordError, json.JSONDecodeError) as e:
                    errs.append(str(e))
    taken: list[Slice] = []
    for name in names:
        if not SLICE.match(name):
            errs.append(f"{name}: a slice's name pairs <name>-read.json with <name>-refute.json; this one is no file label")
            continue
        sources: dict[str, pathlib.Path] = {part: f for part, suffix in PARTS.items()
                                            if (f := reads / f"{name}{suffix}").exists()}
        if asked is not None and (f := asked / f"{name}.json").exists():
            sources["asked"] = f
        raw = {k: p.read_bytes() for k, p in sources.items()}
        try:
            loaded = {k: json.loads(b.decode("utf-8")) for k, b in raw.items()}
        except (UnicodeDecodeError, json.JSONDecodeError) as e:
            errs.append(f"{name}: a source is not JSON ({e})")
            continue
        doc = {"$comment": [f"THE READ RECORD of slice {name} of the {year} rules (kb/Work PB5597; DESIGN-spec-conformance-"
                            "review.md section 10.6): the reader's and the refuter's files and the flags the slice asked, "
                            "verbatim, with the digest of each source. Written by audit_batch.py import-reads; read only "
                            "through lineage_reads.load, the one normalization."],
               "slice": name, "edition": year, "asked": loaded.get("asked"), "read": loaded["read"],
               "refute": loaded.get("refute"),
               # only when the slice has one, so a record of a slice with none stays byte for byte as it was imported
               **({"derivation-refute": loaded["derivation-refute"]} if "derivation-refute" in loaded else {}),
               "imported": {k: digest(v) for k, v in sorted(loaded.items())}}
        try:
            s = normal_slice(name, doc)
        except RecordError as e:
            errs.append(str(e))
            continue
        if rules is not None and (unknown := sorted(rid for rid in s.reads if rid not in rules)):
            errs += [f"{name}: {rid}: no rule of the {year} catalog" for rid in unknown]
            continue
        taken.append(s)
        docs[name] = doc
    # A rule read in two slices, imported now or already recorded, with no line of second reads: refused BEFORE any
    # write (review R1-5).
    errs += supersede([*held, *taken])
    if errs:
        raise SystemExit(f"{TOOL}: {len(errs)} read file(s) cannot be interpreted; nothing is written:\n  " + "\n  ".join(errs))
    out.mkdir(parents=True, exist_ok=True)
    written = []
    for name, doc in docs.items():
        text = json.dumps(doc, indent=1, ensure_ascii=False) + "\n"
        path = record_path(year, name, out)
        if not path.exists() or path.read_text(encoding="utf-8") != text:
            path.write_text(text, encoding="utf-8", newline="\n")
            written.append(name)
    return written


# -- the evidence at the row's own edition ------------------------------------------------------------------------------

def _generator() -> Any:
    if str(SUITE / "harness") not in sys.path:
        sys.path.insert(0, str(SUITE / "harness"))
    import generate  # noqa: PLC0415  (THE one reader of the suite's headers and its band check)
    return generate


@functools.cache
def _order() -> tuple[str, ...]:
    return tuple(inventory_schema.load_schema().editions)


@functools.cache
def format_problem(source_format: str, spelling: str) -> str | None:
    """THE ONE PREDICATE OF A WITNESS'S TEXT (kb/Work PB5754; DESIGN-spec-conformance-review.md section 10.6): a
    conformance-suite case witnesses a row at an edition only when its text is written in a reference format that
    edition defines (`generate.format_defined_at`: fixed form at every edition, free form from 2002), "each compiler
    accepts source programs written in reference format" (§1985:IV.7.1). Accepted or refused, `cases/` or `wiseowl/`, a
    KEPT row's own witness or an own verdict's: before 2002 a free-form case witnesses nothing, though it may still run
    there as a regression test of WiseOwl's `--source-format free` extension. Why the case is no witness at the edition
    spelled `spelling`, or None when its format is that edition's. Every asker calls this: the audit (`own_problems`,
    `stored_errors`), the writer (`ref_format_problem`, record_verdicts.validate) and the planner (`Witnesses.at`,
    `accepted_at`, `case_status`)."""
    if _generator().format_defined_at(source_format, spelling, list(_order())):
        return None
    return (f"its text is in {source_format} form, a reference format {spelling} does not define: no witness at "
            f"{spelling} (kb/Work PB5754)")


def ref_format_problem(ref: str, ed: Edition, suite: pathlib.Path | None = None) -> str | None:
    """THE WRITER'S QUESTION (record_verdicts.validate, before a record of an older edition is written): why the
    test-ref `ref` is no witness at `ed` for its text's reference format (`format_problem`), or None. Only a
    conformance-suite case has a reference format; its header is read as the generator reads it (`generate.read_header`,
    `generate.source_format_of`). A case whose header does not read fails the record: its format cannot be told. A
    reference that names no case on disk is not judged here: that it names a real test is the battery gate's check
    (`SpecTraceabilityInventoryDriftTests`, the division of labour record_verdicts.py's docstring states), which fails
    it loudly at every build."""
    form, _, key = ref.partition(":")
    g = _generator()
    if form.strip() != lineage.SUITE_FORM or all(format_problem(f, ed.spelling) is None for f in g.SOURCE_FORMATS):
        return None
    path = (suite or SUITE) / f"{key.strip()}.cob"
    if not path.is_file():
        return None
    header, _ = g.read_header(path.read_bytes().decode("utf-8"))
    if header is None:
        return f"{ref}: its metadata block does not read, so the reference format of its text cannot be told"
    why = format_problem(g.source_format_of(header), ed.spelling)
    return f"{ref}: {why}" if why else None


@dataclasses.dataclass(frozen=True)
class CaseInfo:
    """One conformance-suite case as the generator reads it: its test-ref key (`cases/<folder>/<id>` or
    `wiseowl/cases/<folder>/<id>`), the editions it runs at (`accept`, its flag's `at` and, for a REFUSAL test, its
    `wiseowl/diagnostics/` entry's `refuse-at`: the gate compiles it there and requires the refusal), each of the three,
    the rules it cites, its control and its refusal entry's file (None for a test with none)."""
    key: str
    path: pathlib.Path
    runs: tuple[str, ...]
    accept: tuple[str, ...]
    flag_at: tuple[str, ...]
    rules: tuple[str, ...]
    control: str | None
    refuse_at: tuple[str, ...] = ()
    refusal: pathlib.Path | None = None
    expect: str = ""
    source_format: str = "fixed"
    #: Is the case's band the COMMITTED tree's (review R2-1)? A case file or refusal entry changed and not committed is a
    #: band no gate has run, so it is no witness: on a committed tree every band ran green in the gate before it landed.
    committed: bool = True
    #: The generator's own `Case`, for `Witnesses.entries` (None for a planted one).
    case: Any = None


@dataclasses.dataclass(frozen=True)
class EvidenceTable:
    """What a witness of each kind shows (`EVIDENCE_TABLE`, data): a DIAGNOSIS (a refusal test's `refuse-at`, a flagged
    case's `at`) witnesses a rule only when what the rule asks of a compiler IS a diagnosis (its catalog `kind` is one
    of `diagnosed`: a syntax rule, which a program can violate; never a general format, which GRANTS forms that only an
    accepted case shows compile, kb/Work PB5740) or the next-edition rule's facility
    is DECLINED (its verdict is `declined`, and then the read must state the older text's licence to decline it), or the
    rule of any kind WITHHOLDS something (`withholds`: "does not imply", whose one observable consequence is the refusal
    of a program that relies on what is withheld, kb/Work PB5783). Every other rule needs a case that RUNS and passes at
    the edition."""
    diagnosed: frozenset[str]
    declined: str
    #: `needs-accepting-case`: the patterns that mark a rule text as GRANTING a form ("may be specified", "is optional",
    #: "in any order"), which no refusal of a forbidden form can witness (review R2-3). A text heuristic that fails
    #: closed: a match sends the rule to an accepted case, whatever its kind and whatever else it says.
    grants: tuple[re.Pattern[str], ...] = ()
    #: `withholding-rules`: the patterns that mark a rule text, of any kind, as WITHHOLDING a property from a clause or
    #: phrase ("does not imply"): no accepted program can tell it was withheld (a local declaration wins either way, for
    #: EXTERNAL and GLOBAL), so a refusal of a program that relies on it is its witness (kb/Work PB5783, option 1).
    withholds: tuple[re.Pattern[str], ...] = ()

    def diagnosable(self, kind: str, text: str) -> bool:
        """Can a DIAGNOSIS witness a rule of catalog `kind` whose text is `text`? Only one that grants nothing the
        `needs-accepting-case` patterns see (they fail closed and win) and that is of a diagnosed kind or withholds
        something a `withholding-rules` pattern sees."""
        if any(p.search(text) for p in self.grants):
            return False
        return kind in self.diagnosed or any(p.search(text) for p in self.withholds)

    @classmethod
    def load(cls, path: pathlib.Path = EVIDENCE_TABLE, kinds: Iterable[str] | None = None) -> "EvidenceTable":
        doc = json.loads(path.read_text(encoding="utf-8"))
        diagnosed = doc.get("diagnosed-kinds")
        if not isinstance(diagnosed, dict) or not all(isinstance(v, str) and v.strip() for v in diagnosed.values()):
            raise SystemExit(f"{TOOL}: {path.name}: diagnosed-kinds maps each kind to why a diagnosis witnesses it")
        if kinds is not None and (stray := sorted(set(diagnosed) - set(kinds))):
            raise SystemExit(f"{TOOL}: {path.name}: {stray} are no rule kind of the catalog ({sorted(set(kinds))})")
        if not isinstance(doc.get("declined-verdict"), str) or not doc["declined-verdict"]:
            raise SystemExit(f"{TOOL}: {path.name}: declined-verdict names the verdict of a declined facility")
        grants = doc.get("needs-accepting-case")
        if not isinstance(grants, list) or not all(isinstance(g, dict) and g.get("pattern") and (g.get("why") or "").strip()
                                                     for g in grants):
            raise SystemExit(f"{TOOL}: {path.name}: needs-accepting-case lists each pattern with its why")
        withholds = doc.get("withholding-rules")
        if not isinstance(withholds, list) or not withholds or not all(
                isinstance(g, dict) and g.get("pattern") and (g.get("why") or "").strip() for g in withholds):
            raise SystemExit(f"{TOOL}: {path.name}: withholding-rules lists each pattern with its why")
        return cls(frozenset(diagnosed), doc["declined-verdict"], tuple(re.compile(g["pattern"], re.I) for g in grants),
                   tuple(re.compile(g["pattern"], re.I) for g in withholds))


@dataclasses.dataclass(frozen=True)
class Lineage:
    """What a read's rule is in its own edition and its pairing: the catalog's `kind` of each rule, the candidate
    pairing's targets of each paired rule (docs/rearchitecture/spec-lineage-candidates-<year>.json), and the evidence
    table. A KEPT record carries exactly the pairing's targets (review R1-2)."""
    kinds: dict[str, str]
    pairs: dict[str, tuple[str, ...]]
    table: EvidenceTable
    texts: dict[str, str] = dataclasses.field(default_factory=dict)

    def diagnosable(self, rid: str) -> bool:
        return self.table.diagnosable(self.kinds.get(rid, ""), self.texts.get(rid, ""))

    @classmethod
    def of(cls, ed: Edition) -> "Lineage":
        rules = ed.rules()
        kinds = {r["id"]: r["kind"] for r in rules}
        pairs = {p["rule-id"]: tuple(p["to"]) for p in
                 json.loads(lineage.candidates_path(ed.year).read_text(encoding="utf-8"))["pairs"]} if ed.next_held else {}
        return cls(kinds, pairs, EvidenceTable.load(kinds=kinds.values()), {r["id"]: r.get("text", "") for r in rules})


def licence_problem(ed: Edition, licence: str | None) -> str | None:
    """Why the read's `licence-<year>` is no licence (None when it is one), review R2-2: it must OPEN with a citation of
    the row's own edition, and every citation in it must parse (citation.py, the one parser), be of that edition and name
    a unit of its text (cite.resolve, the one checker, on the structure: a licence quotes nothing it must hold). The
    forms the reads write (`none`, `not needed: ...`, a sentence) are no licence, never guessed into one."""
    import cite  # noqa: PLC0415  (THE checker of a citation)
    import citation  # noqa: PLC0415  (THE citation format)
    if licence is None:
        return "the read states none"
    found = list(citation.scan(licence))
    if not found or found[0].start != 0:
        return f"{licence[:120]!r} does not open with a citation of {ed.year}"
    for c in found:
        if c.error:
            return f"{licence[c.start:c.end]!r}: {c.error}"
        if c.edition != ed.year:
            return f"{licence[c.start:c.end]!r} cites {c.edition}, not the row's own edition {ed.year}"
        if not (v := cite.resolve(c)).ok:
            return f"{licence[c.start:c.end]!r}: {v.message}"
    return None


def uncommitted(repo: pathlib.Path = REPO) -> set[str]:
    """The suite's files the working tree changes or adds against HEAD (repository-relative POSIX paths): bands no gate
    has proven (review R2-1). git's own status, read once."""
    r = subprocess.run(["git", "-C", str(repo), "status", "--porcelain=v1", "-z", "--untracked-files=all", "--",
                        "conformance-suite"], capture_output=True, text=True, encoding="utf-8")
    if r.returncode != 0:
        raise SystemExit(f"{TOOL}: git status failed, so no band can be told committed: {r.stderr.strip()}")
    out = set()
    for item in r.stdout.split("\0"):
        if len(item) > 3:
            out.add(item[3:])
    return out


@dataclasses.dataclass(frozen=True)
class Suite:
    """The conformance suite as the generator reads it: every case by its test-ref key, and the generator with its inputs.
    Read once (`read`: about 25 s, the headers of every case) and shared by every edition's `Witnesses`."""
    cases: dict[str, CaseInfo]
    generator: tuple[Any, Any] | None

    @classmethod
    def read(cls) -> "Suite":
        g = _generator()
        inputs, _problems = g.repository_inputs()
        reader = g.Reader(g.SUITE, inputs)
        reader.read_tree()
        dirty = uncommitted()
        cases = {}
        for c in reader.cases:
            key = pathlib.PurePath(c.path).with_suffix("").as_posix()
            at = tuple(c.flag.at) if c.flag is not None else ()
            refusal = inputs.diagnostics.get(c.test_id)
            refuse_at = tuple(str(e) for e in refusal.get("refuse-at", ())) if refusal else ()   # a warn-at entry has none (PB5461)
            sidecar = g.SUITE / g.sidecar_path(g.DIAGNOSTICS_DIR, c.test_id) if refusal else None
            files = [g.SUITE / c.path] + ([sidecar] if sidecar else [])
            cases[key] = CaseInfo(key, g.SUITE / c.path, tuple(dict.fromkeys((*c.accept, *at, *refuse_at))),
                                  tuple(c.accept), at, tuple(c.rules), c.control, refuse_at, sidecar,
                                  refusal["expect"] if refusal else "", c.source_format,
                                  not any(f.relative_to(REPO).as_posix() in dirty for f in files), c)
        return cls(cases, (g, inputs))


class Witnesses:
    """The next edition's rules' conformance-suite witnesses and where each runs. A case runs at an edition when its header
    names it (the generator places it in that edition's manifest only then, and refuses it there when a rule it cites does
    not exist at the edition: `generate.check_cases`); the gate runs every case of every edition's manifest, so a case
    that runs at the edition on a green tree passes there."""

    def __init__(self, cases: dict[str, CaseInfo], refs: dict[str, list[str]], bands: dict[str, list[str]],
                 verdicts: dict[str, str] | None = None, locations: dict[str, list[str]] | None = None):
        self.cases, self.refs, self.bands, self.verdicts = cases, refs, bands, verdicts or {}
        #: Each next-edition rule's `code-location` entries: what an own verdict from a pointer names (`own_verdict`).
        self.locations = locations or {}
        #: (the generator module, its inputs): what `entries` builds manifest entries with; None for planted witnesses.
        self.generator: tuple[Any, Any] | None = None

    @classmethod
    def of(cls, newer: int, suite: "Suite | None" = None) -> "Witnesses":
        """The repository's: the generator's reading of every case (both trees; `suite` when it was read already), and
        the test-refs and bands of the rows of the edition `newer` (the next held edition the records carry into)."""
        suite = suite or Suite.read()
        schema = inventory_schema.load_schema()
        rows = inventory_view.inventory(newer, schema)
        refs = {r["rule-id"]: schema.split(r.get("test-ref", "") or "", schema.test_ref_sep) for r in rows}
        bands = {r["rule-id"]: schema.split(r.get("editions", "") or "", schema.editions_sep) for r in rows}
        out = cls(suite.cases, refs, bands, {r["rule-id"]: r.get("verdict") or "" for r in rows},
                  {r["rule-id"]: schema.split(r.get("code-location", "") or "", schema.code_location_sep) for r in rows})
        out.generator = suite.generator
        return out

    def entries(self, keys: Iterable[str], sp: str) -> tuple[dict[str, list[dict[str, Any]]], dict[str, str]]:
        """Each case of `keys` with the edition `sp` added to its band IN MEMORY (its `accept`, its flag's `at`, a flagged
        case's control with it), checked by the generator's own `check_cases` and given its manifest entries by the
        generator's own `entry_for`: the cases the generator accepts there with their entries (the case's, then its
        control's), and why each other is refused. The tracked suite is not touched (review R2-1)."""
        if self.generator is None:
            raise SystemExit(f"{TOOL}: these witnesses were not read from the suite: no manifest entry can be made")
        g, inputs = self.generator
        order = inputs.editions
        widened, controls = {}, {}
        for k in keys:
            c = self.cases[k].case
            flag = dataclasses.replace(c.flag, at=tuple(add_edition(c.flag.at, sp, order))) if c.flag else None
            widened[k] = dataclasses.replace(c, accept=tuple(add_edition(c.accept, sp, order)) if c.accept else c.accept,
                                             flag=flag)
            if flag and c.control and (ctl := self.cases.get(f"{k.rsplit('/', 1)[0]}/{c.control}")):
                controls[k] = ctl.case if sp in ctl.accept else dataclasses.replace(
                    ctl.case, accept=tuple(add_edition(ctl.case.accept, sp, order)))
        reader = g.Reader(g.SUITE, inputs)
        reader.cases = [*widened.values(), *{id(c): c for c in controls.values()}.values()]
        g.check_cases(reader)
        why: dict[str, list[str]] = {}
        for x in reader.refusals:
            why.setdefault(x.path, []).append(x.message)
        entries, refused = {}, {}
        for k, c in widened.items():
            parts = [c] + ([controls[k]] if k in controls else [])
            if msgs := [m for p in parts for m in why.get(pathlib.PurePath(p.path).as_posix(), [])]:
                refused[k] = msgs[0]
            else:
                entries[k] = [g.entry_for(p, sp, inputs) for p in parts]
        return entries, refused

    def suite_cases(self, rule: str) -> list[str]:
        """The keys of the conformance-suite cases the rule's test-ref names (`conformance:<key>`), in its order."""
        return [ref.partition(":")[2].strip() for ref in self.refs.get(rule, []) if ref.partition(":")[0] == lineage.SUITE_FORM]

    def at(self, rule: str, spelling: str) -> list[str]:
        """The rule's suite witnesses that run at the edition spelled `spelling`, of any kind, in the COMMITTED tree, in
        a reference format the edition defines (`format_problem`)."""
        return [k for k in self.suite_cases(rule) if k in self.cases and self.cases[k].committed
                and spelling in self.cases[k].runs and format_problem(self.cases[k].source_format, spelling) is None]

    def accepted_at(self, rule: str, spelling: str) -> list[str]:
        """The rule's suite witnesses ACCEPTED at the edition, in the COMMITTED tree, in a reference format the edition
        defines: a program that runs there and whose output the gate compares."""
        return [k for k in self.suite_cases(rule) if k in self.cases and self.cases[k].committed
                and spelling in self.cases[k].accept and format_problem(self.cases[k].source_format, spelling) is None]

    def search(self, ed: Edition, diagnosable: bool, to: Iterable[str], licence: str | None,
               table: EvidenceTable) -> tuple[list[str], list[tuple[str, str]]]:
        """THE CANDIDATE SEARCH of the writer (kb/Work PB5675): through the cases of each rule of `to`, the committed
        cases that witness the older rule at `ed`, by `witnesses_at` (review R1-1: an accepted case always; a diagnosis
        only for a `diagnosable` rule or a declined facility with a licence), and why each rule of `to` that has none
        lacks one. A target is a POINTER to candidate cases here and nowhere else: the cases found become the row's own
        `test-ref`, and the audit checks them with no target (`own_problems`)."""
        sp, found, out = ed.spelling, [], []
        for t in to:
            if acc := self.accepted_at(t, sp):
                found += acc
                continue
            diagnoses = self.at(t, sp)
            if diagnoses and diagnosable:
                found += diagnoses
                continue
            if diagnoses and self.verdicts.get(t) == table.declined:
                if why := licence_problem(ed, licence):
                    out.append((NO_LICENCE, f"{t} is {table.declined} and only a diagnosis at {sp} shows the decline; "
                                            f"licence-{ed.year}: {why}"))
                else:
                    found += diagnoses
                continue
            out.append((NO_OWN_WITNESS, f"{t}: no conformance-suite case runs and passes at {sp}" + (
                " (only a diagnosis, which witnesses no rule that grants a form or states a behaviour)" if diagnoses else "")))
        return list(dict.fromkeys(found)), out

    def own_problems(self, ed: Edition, refs: Iterable[str], diagnosable: bool, licence: str | None) -> list[str]:
        """THE AUDIT'S EVIDENCE TEST of a stored chained row (kb/Work PB5675): why each test-ref the row carries of its
        own is no witness at `ed` of the kind its rule needs. It reads the row's own references, the suite and the
        row's rule, never a target: each names a conformance-suite case (`lineage.SUITE_FORM`) written in a reference
        format the edition defines (`format_problem`) that runs at the edition, ACCEPTED there, or only diagnosed there
        for a `diagnosable` rule or a read whose `licence` `licence_problem` accepts (the declined facility the writer
        found it through). A diagnosis beside a case ACCEPTED at the edition is the rule's restriction witnessed beside
        its grant, so it is reported only when no reference of the row is accepted there (a stored own verdict cites
        both: kb/Work PB5597 review R1-4). Empty when every one witnesses."""
        sp, out = ed.spelling, []
        refs = list(refs)
        keys = [key.strip() for form, _, key in (r.partition(":") for r in refs) if form == lineage.SUITE_FORM]
        granted = any(k in self.cases and sp in self.cases[k].accept
                      and format_problem(self.cases[k].source_format, sp) is None for k in keys)
        for ref in refs:
            form, _, key = ref.partition(":")
            c = self.cases.get(key.strip()) if form == lineage.SUITE_FORM else None
            if form != lineage.SUITE_FORM:
                out.append(f"{ref}: a {form} test runs at no older edition")
            elif c is None:
                out.append(f"{ref}: names no conformance-suite case")
            elif why := format_problem(c.source_format, sp):
                out.append(f"{ref}: {why}")
            elif sp in c.accept:
                continue
            elif sp not in c.runs:
                out.append(f"{ref}: does not run at {sp} (it runs at {list(c.runs)})")
            elif not diagnosable and not granted and (why := licence_problem(ed, licence)):
                out.append(f"{ref}: only a diagnosis at {sp}, which witnesses no rule that grants a form or states a "
                           f"behaviour, and no licence to decline it ({why})")
        return out

    def format_problems(self, ed: Edition, refs: Iterable[str]) -> list[str]:
        """THE AUDIT'S FORMAT TEST of a row with a verdict of its own (kb/Work PB5754): why each conformance-suite case
        among the row's own references is no witness at `ed` for its text's reference format (`format_problem`). The
        rest of an own verdict's evidence is its own fields' (`Schema.state_for`); a KEPT row's is `own_problems`."""
        out = []
        for ref in refs:
            form, _, key = ref.partition(":")
            c = self.cases.get(key.strip()) if form == lineage.SUITE_FORM else None
            if c is not None and (why := format_problem(c.source_format, ed.spelling)):
                out.append(f"{ref}: {why}")
        return out


def pairing_problems(rid: str, to: Iterable[str], lin: Lineage) -> list[tuple[str, str]]:
    """Why a NEW KEPT record's targets are not the candidate pairing's for the rule (review R1-2); empty when they are. The
    writer asks it of a new record, never the audit of a stored one (kb/Work PB5675): the pairing is word-identical
    text, so an edition whose wording differs (1985 pairs 0 of 2,274 rules) records its rows with their own verdicts."""
    want = lin.pairs.get(rid)
    if want is None:
        return [(NOT_THE_PAIRING, f"{rid} has no candidate pair: a KEPT record carries only a pairing's targets")]
    if sorted(want) != sorted(to):
        return [(NOT_THE_PAIRING, f"the read carries into {sorted(to)}, the pairing into {sorted(want)}")]
    return []


def kept_witnesses(ed: Edition, rid: str, to: Iterable[str], licence: str | None, lin: Lineage,
                   w: Witnesses | None) -> tuple[list[str], list[tuple[str, str]]]:
    """THE WHOLE TEST OF A NEW KEPT RECORD, at every older edition (kb/Work PB5675; the writer and the import's decisions
    share it): its targets are the pairing's (`pairing_problems`), and, with `w`, the candidate search finds a witness at
    the edition of the kind the rule needs for each (`Witnesses.search`). The cases found (as `conformance:` test-refs)
    are what the writer records as the row's own witness; the problems refuse the record."""
    to = list(to)
    out = pairing_problems(rid, to, lin)
    if w is None:
        return [], out
    found, missing = w.search(ed, lin.diagnosable(rid), to, licence, lin.table)
    return [f"{lineage.SUITE_FORM}:{k}" for k in found], out + missing


# -- an own verdict from a pointer --------------------------------------------------------------------------------------

#: Why an unpaired rule's upheld SAME is not yet its own verdict (`own_verdict_problems`), beside the evidence's own
#: NO_OWN_WITNESS and NO_LICENCE.
NO_POINTER, POINTER_OPEN, POINTERS_DISAGREE, NO_OWN_CITATION, NO_REGISTER_ENTRY, NO_CODE_LOCATION = (
    "no pointer", "pointer verdict does not resolve", "pointers disagree", "no own-edition citation",
    "no register entry of its own", "no code location")
#: How an own verdict's `notes` open (`own_verdict`), naming its read record: its provenance, which the audit holds to
#: that read's standing as `next.source` does a KEPT record's (`own_stored_errors`). The audit does not FIND own verdicts
#: by it: every own verdict of a rule whose tracked read decides `own-verdict-from-pointer` is audited (`own_reads`).
OWN_LEAD = "Own verdict from the read"
OWN_SOURCE = re.compile(rf"\A{OWN_LEAD} (lineage-reads-(\d+)/([A-Za-z0-9][A-Za-z0-9_.-]*)\.json)")


def own_citations(ed: Edition, read: Read) -> list[str]:
    """The read's citations of the row's own edition, as written (citation.py, the one parser): each a citation of
    `ed` that parses."""
    import citation  # noqa: PLC0415  (THE citation format)
    out = []
    for c in read.citations:
        found = list(citation.scan(c))
        if found and found[0].start == 0 and not found[0].error and found[0].edition == ed.year:
            out.append(c)
    return out


def own_verdict_problems(ed: Edition, read: Read, lin: Lineage, w: Witnesses) -> list[tuple[str, str]]:
    """THE ONE CHECK of an own verdict from a pointer (kb/Work PB5597; DESIGN-spec-conformance-review.md section 10.6),
    shared by `decide`, the writer (`audit_batch.py own-verdict-batch`), `widen` and the audit: the read names a pointer;
    every pointer's verdict resolves and all agree (the record states ONE verdict); a declined facility
    (DOCUMENTED-NON-SUPPORT) is declined at the edition only with the read's licence (`licence_problem`), whatever its
    witness; the read cites the row's own edition; and EVERY pointer has a witness at the edition of the kind the rule
    needs (`Witnesses.search`, the candidate search a new KEPT record passes). Empty when the record may be written."""
    rid = read.rule_id
    if not read.to:
        return [(NO_POINTER, f"{rid}: the read names no next-edition rule, so no verdict and no witness to carry")]
    out = w.search(ed, lin.diagnosable(rid), read.to, read.licence, lin.table)[1]
    verdicts = {t: w.verdicts.get(t, "") for t in read.to}
    if open_ := [t for t, v in verdicts.items() if v not in _resolving()]:
        out.append((POINTER_OPEN, f"{open_}: a pointer's verdict {[verdicts[t] for t in open_]} does not resolve"))
    elif len(set(verdicts.values())) > 1:
        out.append((POINTERS_DISAGREE, f"the pointers' verdicts differ ({verdicts}): an own verdict states one"))
    elif lin.table.declined in verdicts.values() and not any(r == NO_LICENCE for r, _ in out) \
            and (why := licence_problem(ed, read.licence)):
        out.append((NO_LICENCE, f"the pointers are {lin.table.declined}: declining the facility at {ed.spelling} needs "
                                f"the older text's licence; licence-{ed.year}: {why}"))
    else:
        probe = _anchor_probe(read, lin, verdicts[read.to[0]])
        if "code-location" in _schema().requires(probe["verdict"]) and not _locations(read, w):
            out.append((NO_CODE_LOCATION, f"the pointers {list(read.to)} name no code location, which the verdict "
                                          f"{probe['verdict']} requires"))
        if _schema().anchor_obliged(probe) and rid not in _schema().register_items():
            out.append((NO_REGISTER_ENTRY, f"kind {probe['kind']} with verdict {probe['verdict']} owes its own "
                                           f"determination {_schema().anchor_for(probe)} (docs/conformance/), which "
                                           "the register does not hold: the pointer's is another edition's"))
    if not own_citations(ed, read):
        out.append((NO_OWN_CITATION, f"{rid}: the read cites nothing of {ed.year} that parses: {list(read.citations)}"))
    return out


_SCHEMA: list[Any] = []


def _schema() -> Any:
    if not _SCHEMA:
        _SCHEMA.append(inventory_schema.load_schema())
    return _SCHEMA[0]


def _resolving() -> set[str]:
    return _schema().resolving


def _locations(read: Read, w: Witnesses) -> list[str]:
    """The pointer rows' code locations an own verdict carries: each but one in an anchored file (the register), which
    is the pointer's own determination, never this row's."""
    return list(dict.fromkeys(loc for t in read.to for loc in w.locations.get(t, [])
                              if loc.partition("#")[0] not in _schema().anchored_files))


def _anchor_probe(read: Read, lin: Lineage, verdict: str) -> dict[str, str]:
    """The row an own verdict would write, as the schema's register-anchor rule reads it (`anchor_for`,
    `anchor_obliged`): its rule, its catalog kind and the verdict."""
    return {"rule-id": read.rule_id, "kind": lin.kinds.get(read.rule_id, ""), "verdict": verdict}


def own_verdict(ed: Edition, read: Read, lin: Lineage, w: Witnesses) -> dict[str, Any]:
    """The record_verdicts record of an own verdict from a pointer whose `own_verdict_problems` are empty, in the
    edition store's own-verdict shape: the pointers' one verdict; `code-location` the pointer rows'; `test-ref` ONLY the
    pointers' cases the candidate search finds at the edition (`Witnesses.search`: an accepted case, a diagnosis only
    where the search allows one), the row's own witness as a new KEPT record's is; `notes` opening `OWN_LEAD` and the read record (what the audit recognises), then the read's
    reason and its citations of the edition."""
    schema = _schema()
    keys = w.search(ed, lin.diagnosable(read.rule_id), read.to, read.licence, lin.table)[0]
    verdict = w.verdicts[read.to[0]]
    probe = _anchor_probe(read, lin, verdict)
    own_anchor = [schema.anchor_for(probe)] if schema.anchor_obliged(probe) else []   # its own, never the pointer's
    locations = dict.fromkeys([*own_anchor, *_locations(read, w)])
    notes = (f"{OWN_LEAD} {source_of(ed.year, read.slice)}: a SAME its refuter UPHELD, for a rule with no candidate "
             f"pair; the verdict of its pointer {', '.join(read.to)}, witnessed at {ed.spelling} by the cases "
             f"test-ref names. The read: {read.reason.strip()} Cited: {'; '.join(dict.fromkeys(own_citations(ed, read)))}.")
    return {"rule-id": read.rule_id, "verdict": verdict,
            **({"code-location": schema.code_location_sep.join(locations)} if locations else {}),
            "test-ref": schema.test_ref_sep.join(f"{lineage.SUITE_FORM}:{k}" for k in keys), "notes": notes}


# -- a derivation from a read (kb/Work PB5453; DESIGN-spec-conformance-review.md section 10.6, "A derivation from a read") --

#: The work item of a read that proposes a derivation its refuter UPHELD (or of a mechanical arm, which needs none):
#: `audit_batch.py derivation-batch` writes its section 8 register row, its refuter record and its store record.
DERIVATION = "derivation"
#: Why a proposed derivation is not yet written (beside NO_REFUTER, NO_OWN_CITATION and NO_CODE_LOCATION).
NAMES_SHAPE, HAS_TEST, VERDICT_OPEN = "names out of the arm's shape", "has a spec-derived test", "verdict does not resolve"
#: The verdict a derivation's row takes when the store holds no record of the rule: section 1.1's derivation stands in
#: for the covering test and for nothing else, so (a) is the read's located `code-location` and (b) the one resolving
#: verdict that states the implementation meets a rule that obliges it to nothing observable.
DERIVED_VERDICT = "CONFORMS"
#: How a derivation's store `notes` open, naming its read record (its provenance).
DERIVATION_LEAD = "Derivation from the read"
#: Where a slice's derivation refuter records are kept, in the refuter-record form the section 8 `Refuted` cell names
#: (`Schema.refuter_outcome` reads JSON lines): generated from the tracked read record, one file per slice.
REFUTATIONS = "tests/version-matrix/derivation-refutations/lineage-reads-{year}-{slice}.jsonl"


def argued(arm: str) -> bool:
    """Does the schema's `arm` need a reader beyond the checker (an UPHELD refuter record for a refuter-bound signer)?"""
    return _schema().derivation.arms[arm]["argued"] is not False


def derivation_problems(ed: Edition, read: Read) -> list[tuple[str, str]]:
    """Why a read's proposed derivation cannot be written from the read alone: its `Names` cell out of its arm's shape
    (the schema's `names-pattern`, what `Derivation.refusals` reads), or no citation of the row's own edition. The rest
    of section 1.1's refusals read the store and the register (`derivation_record`, `record_verdicts.validate`)."""
    d = read.derivation
    out = []
    pattern = _schema().derivation.arms[d.arm]["names"]
    if not pattern.match(d.names):
        out.append((NAMES_SHAPE, f"the {d.arm} arm's Names cell matches {pattern.pattern!r}, not {d.names!r}"))
    if not own_citations(ed, read):
        out.append((NO_OWN_CITATION, f"{read.rule_id}: the read cites nothing of {ed.year} that parses: "
                                     f"{list(read.citations)}"))
    return out


#: Why a located code location is not taken: the read names another one.
LOCATION_DISAGREES = "located code location disagrees with the read's"


@dataclasses.dataclass(frozen=True)
class Located:
    """A rule's code location found AFTER its read, by a locator who searched the tree for it (a read that judged the
    compiler correct need not name the member that does it): the location, the locator's one-line evidence, and the
    records file it comes from, which the store record's notes name."""
    code_location: str
    evidence: str
    source: str


def load_located(path: pathlib.Path, repo: pathlib.Path = REPO) -> dict[str, Located]:
    """A locator's records file (`{"records": [{"rule-id", "code-location", "evidence"}]}`) by rule; a record with no
    location (a locator's `none-why`) is left out. Every location must resolve in `repo` by the gate's own rule
    (`code_location`) and carry its evidence, or the whole file is refused, naming each record."""
    import code_location  # noqa: PLC0415  (the Python twin of the gate's resolver)
    schema, tree = _schema(), code_location.working_tree(repo)
    out, errs = {}, []
    for r in json.loads(path.read_text(encoding="utf-8"))["records"]:
        rid, loc, why = r.get("rule-id"), (r.get("code-location") or "").strip(), (r.get("evidence") or "").strip()
        if not loc:
            continue
        if rid in out:
            errs.append(f"{rid}: located twice")
        if not why:
            errs.append(f"{rid}: a located code location carries its evidence")
        errs += [f"{rid}: {p}" for p in code_location.unresolved(schema.split(loc, schema.code_location_sep), tree,
                                                                 schema.code_location_re)]
        out[rid] = Located(loc, why, path.name)
    if errs:
        raise SystemExit(f"{TOOL}: {path}: {len(errs)} located code location(s) cannot be taken:\n  " + "\n  ".join(errs))
    return out


def refutations_file(year: int, slice_name: str) -> str:
    return REFUTATIONS.format(year=year, slice=slice_name)


def refutation_lines(year: int, s: Slice) -> str:
    """The derivation refuter records of slice `s`, one JSON line each in the refuter-record form (`rule-id`, `outcome`,
    `citation`, `reason`, and `source`, the read record): a function of the tracked read record alone, so the file is
    rewritten identically and `--check` holds it to the record."""
    return "".join(json.dumps({"rule-id": f.rule_id, "outcome": f.verdict, "citation": "; ".join(f.citations),
                               **({"attempt": f.attempt} if f.attempt else {}),
                               "reason": f.reason, "source": source_of(year, s.name)}, ensure_ascii=False) + "\n"
                   for _rid, f in sorted(s.derivation_refutes.items()))


def derivation_record(ed: Edition, read: Read, refute: Refute | None, row: dict[str, Any], signature: str,
                      schema: inventory_schema.Schema, located: Located | None = None
                      ) -> tuple[dict[str, Any], list[str], list[tuple[str, str]]]:
    """A proposed derivation's store record, its section 8 register cells (Rule, Arm, Names, The derivation, Signed,
    Refuted) and what the store holds against it. The record is the anchor COMPUTED from the rule id; a row the store
    already adjudicates (a verdict that resolves, or a chained KEPT record) gains the derivation alone, an unrecorded
    row takes `DERIVED_VERDICT` with the read's `code-location`, or, when the read names none, the `located` one (a
    locator's, its evidence and its file named in the notes). Refused: a row with a spec-derived test (refusal 1), a
    verdict that does not resolve (refusal 2), an unrecorded row with no code location, a located location that is not
    the read's own."""
    rid, d = read.rule_id, read.derivation
    if located is not None and d.code_location and located.code_location != d.code_location:
        return {}, [], [(LOCATION_DISAGREES, f"{rid}: the read names {d.code_location!r}, {located.source} "
                                             f"{located.code_location!r}")]
    if located is not None and not d.code_location:
        d = dataclasses.replace(d, code_location=located.code_location)
        found = f" The code location was located after the read ({located.source}): {located.evidence}"
    else:
        found = ""
    anchor, key = schema.derivation.anchor_for({"rule-id": rid}), schema.derivation.key_for({"rule-id": rid})
    problems: list[tuple[str, str]] = []
    if schema.has_spec_derived_test(row):
        problems.append((HAS_TEST, f"{rid} carries a spec-derived test-ref: it has an observable obligation"))
    kind = schema.next.kinds.get((row.get("next") or {}).get("kind", ""))
    if row.get("verdict") or (kind is not None and kind.chained):
        if row.get("verdict") and row["verdict"] not in schema.resolving:
            problems.append((VERDICT_OPEN, f"{rid}'s verdict {row['verdict']} does not resolve"))
        record: dict[str, Any] = {"rule-id": rid, "derivation": anchor}
    else:
        if not d.code_location:
            problems.append((NO_CODE_LOCATION, f"{rid} has no record, and its read's derivation names no "
                                               f"`code-location`, which {DERIVED_VERDICT} requires"))
        record = {"rule-id": rid, "verdict": DERIVED_VERDICT, "code-location": d.code_location or "",
                  "derivation": anchor,
                  "notes": f"{DERIVATION_LEAD} {source_of(ed.year, read.slice)}: {DERIVED_VERDICT} on a {d.arm} "
                           f"derivation of its own (docs/CONFORMANCE.md section 8, {key}); "
                           f"the code location is where the compiler meets the rule's subject. The read: {read.reason.strip()}"
                           f"{found}"}
    refuted = refutations_file(ed.year, read.slice) if refute is not None else "—"
    cited = "; ".join(dict.fromkeys(own_citations(ed, read)))
    argument = (f"{read.reason.strip()} Cited: {cited}. Proposed by the read {source_of(ed.year, read.slice)}; "
                + (f"its refuter, trying to write the witness the derivation says cannot exist"
                   f"{f' ({refute.attempt.strip()})' if refute.attempt else ''}, UPHELD it: "
                   f"{refute.reason.strip()}" if refute is not None else "a mechanical arm, whose checker is its reader."))
    cells = [key, d.arm, d.names, register_prose(argument), signature, refuted]
    return record, cells, problems


#: An Annex A.1 determination's register key (`DOC-A.1-<n>`): in the register's prose it is cited `A.1 item <n>`, since
#: the register anchors an inventory row on the key by a word search over the whole file (the ANNEX A.1 REGISTER audit).
A1_KEY = re.compile(r"\bDOC-A\.1-(\d+)\b")


def register_prose(text: str) -> str:
    """A reader's or refuter's text as register prose: each A.1 row key it names written `A.1 item <n>`."""
    return A1_KEY.sub(r"A.1 item \1", text)


def own_reads(ed: Edition, slices: Iterable[Slice], lin: Lineage) -> dict[str, Read]:
    """Each rule whose tracked read decides `own-verdict-from-pointer` (a SAME its refuter UPHELD, of a rule with no
    candidate pair: `decide`), with that read: the rules whose stored own verdict the audit holds (review R1-4: found
    from the read records, never from a record's prose)."""
    slices = list(slices)
    by = {(s.name, rid): s.reads[rid] for s in slices for rid in s.reads}
    return {d.rule_id: by[(d.slice, d.rule_id)] for d in decide(ed, slices, None, None, lin)
            if d.work == OWN_VERDICT_FROM_POINTER}


def own_stored_errors(ed: Edition, row: dict[str, Any], read: Read | None, by_slice: dict[str, "Slice"],
                      w: Witnesses | None, lin: Lineage, schema: inventory_schema.Schema) -> list[str]:
    """Why a STORED own verdict (a verdict, no `next` kind) no longer stands as one from a read. `read` is the rule's
    tracked read when it decides `own-verdict-from-pointer` (`own_reads`). A record whose notes name its read
    (`OWN_SOURCE`, which `own_verdict` writes) claims that read: it must hold a read of the rule, a SAME its refuter
    UPHELD, of a rule with no candidate pair. Every such record, whatever its notes, is held (with `w`) to the KEPT row's
    test of its OWN witnesses (`Witnesses.own_problems`: each test-ref a conformance-suite case that runs at the edition,
    of the kind the rule needs, the read's licence for a diagnosis), and must carry one: no pointer is read here, as
    none is for a KEPT row (kb/Work PB5675)."""
    rid = row["rule-id"]
    if src := OWN_SOURCE.match(row.get("notes") or ""):
        if int(src.group(2)) != ed.year:
            return [f"{rid}: its notes name the read {src.group(1)}, of another edition"]
        s = by_slice.get(src.group(3))
        named = s.reads.get(rid) if s else None
        f = s.refutes.get(rid) if s else None
        if named is None:
            return [f"{rid}: its own verdict names the read {src.group(1)}, which holds no read of the rule"]
        if named.verdict != "SAME" or f is None or f.verdict != "UPHELD":
            return [f"{rid}: its read is {named.verdict} and its refuter {f.verdict if f else 'absent'}: an own verdict "
                    "from a read needs a SAME the refuter UPHELD"]
        if rid in lin.pairs:
            return [f"{rid}: the rule has a candidate pair: its read writes a KEPT record, not an own verdict"]
        read = named
    if read is None or w is None:
        return []
    refs = lineage.own_witness_refs(row, schema)
    if not refs:
        return [f"{rid}: {NO_OWN_WITNESS}: an own verdict from a read carries no test-ref of its own"]
    return [f"{rid}: {NO_OWN_WITNESS}: {why}"
            for why in w.own_problems(ed, refs, lin.diagnosable(rid), read.licence)]


# -- what a read becomes ------------------------------------------------------------------------------------------------

@dataclasses.dataclass(frozen=True)
class Decision:
    """A read rule's outcome: a KEPT change record (`answers` keyed by the flags it answers, `source` its read record), or
    refused (`refused`: why, each with its detail), or a work item (`work`: its kind, with `probe` and `why`)."""
    rule_id: str
    slice: str
    to: tuple[str, ...]
    source: str
    answers: dict[str, dict[str, str]] = dataclasses.field(default_factory=dict)
    refused: tuple[tuple[str, str], ...] = ()
    work: str | None = None
    probe: str | None = None
    why: str = ""

    @property
    def kept(self) -> bool:
        return self.work is None and not self.refused

    def row(self) -> dict[str, Any]:
        out: dict[str, Any] = {"rule-id": self.rule_id, "slice": self.slice, "to": list(self.to), "source": self.source}
        if self.kept:
            out["outcome"] = KEPT
            if self.answers:
                out["answers"] = self.answers
        elif self.work:
            out.update({"outcome": "work", "kind": self.work, "probe": self.probe, "why": self.why})
            if self.refused:   # an own verdict's blockers (`own_verdict_problems`)
                out["blocked"] = [{"reason": r, "detail": d} for r, d in self.refused]
        else:
            out.update({"outcome": "refused", "refused": [{"reason": r, "detail": d} for r, d in self.refused]})
        return out


def answers_for(read: Read, asked: dict[str, str], flags: dict[str, str]) -> tuple[dict[str, dict[str, str]], list[str]]:
    """The read's answers to the screen's `flags` on its pair ({trigger: detail}), keyed by the flag's content, and the
    flags it leaves unanswered. An answer counts for a flag when it names the trigger and the detail it answered (its
    own, or the one the slice `asked`) is the screen's detail: a read asked another question answered that one."""
    answers, missing = {}, []
    for name, detail in flags.items():
        got = [a for d, a in read.answers.get(name, []) if (d if d is not None else asked.get(name)) == detail]
        if got:
            answers[name] = {"flag": detail, "answer": got[0]}
        else:
            missing.append(f"{name}: {detail}")
    return answers, missing


def work_of(judgment: Judgment | None, probe: str | None, who: str) -> tuple[str, str]:
    """A not-kept read's work kind from the judgment of the compiler at the edition `who` gave, and why."""
    if judgment is None:
        return UNDECIDED, f"the {who} does not say how the compiler behaves at the edition"
    if judgment.kind is None:
        return UNINTERPRETABLE, f"the {who}'s judgment of the compiler names none of correct, wrong, not probed: " \
                                f"{judgment.text!r}"
    if judgment.kind == CORRECT:
        return NEEDS_OWN_WITNESS, f"the {who} showed the compiler correct at the edition" + (
            "" if probe else " and names no probe: its witness is still to write")
    if judgment.kind == WRONG:
        return DEFECT, f"the {who} showed the compiler wrong at the edition: {judgment.text}"
    return UNDECIDED, f"the {who} did not probe the compiler at the edition"


def derivation_decision(ed: Edition, s: Slice, r: Read, base: dict[str, Any]) -> Decision:
    """A DIFFERENT read that proposes a derivation (kb/Work PB5453): its derivation refuter is read first, as a SAME's
    refuter is. DOUBTFUL leaves it `undecided`; OVERTURNED (a program can show the rule) is the refuter's work item; an
    UPHELD record, or none for a mechanical arm, makes it the `DERIVATION` work item, blocked by `derivation_problems`
    and, for an argued arm with no refuter record, by NO_REFUTER."""
    g, arm = s.derivation_refutes.get(r.rule_id), r.derivation.arm
    if g is not None and g.verdict == "DOUBTFUL":
        return Decision(**base, work=UNDECIDED, probe=g.probe, why="the derivation's refuter is DOUBTFUL: " + g.reason)
    if g is not None and g.verdict == "OVERTURNED":
        kind, why = work_of(g.compiler, g.probe, "derivation refuter")
        return Decision(**base, work=kind, probe=g.probe, why="the refuter OVERTURNED the derivation; " + why)
    refused = [] if g is not None or not argued(arm) else [
        (NO_REFUTER, f"no refuter has attacked the {arm} derivation: an argued arm stands on an UPHELD refutation")]
    return Decision(**base, work=DERIVATION, probe=None, refused=tuple(refused + derivation_problems(ed, r)),
                    why=f"a {arm} derivation" + (", its refuter UPHELD" if g is not None else
                                                 (" awaiting its refuter" if argued(arm) else
                                                  ", a mechanical arm whose checker is its reader")))


def decide(ed: Edition, slices: Iterable[Slice], screen: dict[str, Any] | None,
           witnesses: Witnesses | None, lin: Lineage) -> list[Decision]:
    """Each read rule's `Decision` (module docstring). The own-edition evidence (`kept_witnesses`, at every edition) is
    checked when `witnesses` is given; None leaves it to the writer (the audit run's import). `screen` is the edition's committed lineage screen; `lin` the catalog's kinds and the pairing. A SAME
    of a rule with no candidate pair is the `OWN_VERDICT_FROM_POINTER` work item, never a KEPT record."""
    flags = (screen or {}).get("marks", {})
    out = []
    for s in slices:
        for rid, r in sorted(s.reads.items()):
            base = dict(rule_id=rid, slice=s.name, to=r.to, source=source_of(ed.year, s.name))
            f = s.refutes.get(rid)
            if r.verdict == "DOUBTFUL":
                out.append(Decision(**base, work=UNDECIDED, probe=r.probe, why="the reader is DOUBTFUL: " + r.reason))
            elif r.verdict == "DIFFERENT" and rid in s.disputed:
                out.append(Decision(**base, work=UNDECIDED, probe=r.probe or (r.compiler.text if r.compiler else None),
                                    why="the refuter disputes the reader's DIFFERENT"))
            elif r.verdict == "DIFFERENT" and r.derivation is not None:
                out.append(derivation_decision(ed, s, r, base))
            elif r.verdict == "DIFFERENT":
                kind, why = work_of(r.compiler, r.probe, "reader")
                out.append(Decision(**base, work=kind, probe=r.probe or (r.compiler.text if r.compiler else None), why=why))
            elif f is None:
                out.append(Decision(**base, refused=((NO_REFUTER, "no refuter has attacked the SAME"),)))
            elif f.verdict == "DOUBTFUL":
                out.append(Decision(**base, work=UNDECIDED, probe=f.probe, why="the refuter is DOUBTFUL: " + f.reason))
            elif f.verdict == "OVERTURNED":
                kind, why = work_of(f.compiler or r.compiler, f.probe or r.probe, "refuter" if f.compiler else "reader")
                out.append(Decision(**base, work=kind, probe=f.probe or r.probe, why="the refuter OVERTURNED the SAME; " + why))
            elif rid not in lin.pairs:
                # A SAME its refuter UPHELD, for a rule no candidate pair pairs (its `to` a nearest-rule pointer): never
                # KEPT; its own verdict follows from the pointer once each target is witnessed at the edition
                # (`own_verdict_problems`, checked when `witnesses` is given; `own_verdict` writes the record). The
                # refuter is read first (kb/Work PB5699).
                out.append(Decision(**base, work=OWN_VERDICT_FROM_POINTER, probe=r.probe,
                                    refused=tuple(own_verdict_problems(ed, r, lin, witnesses)) if witnesses else (),
                                    why=f"a SAME its refuter upheld, for a rule with no candidate pair (its `to` "
                                        f"{list(r.to)} is a pointer): its own verdict, citing the pointer's witness at "
                                        "the edition"))
            else:
                answers, unanswered = answers_for(r, s.asked.get(rid, {}), flags.get(rid, {}))
                refused = [(FLAG_UNANSWERED, u) for u in unanswered]
                refused += kept_witnesses(ed, rid, r.to, r.licence, lin, witnesses)[1]
                out.append(Decision(**base, answers=answers, refused=tuple(refused)))
    return out


def summary(decisions: list[Decision]) -> dict[str, Any]:
    """The counts the import and the dry runs report: kept, refused by each reason (a row counted under every reason it
    carries, and under its first), and the work items by kind."""
    refused = [d for d in decisions if d.refused and not d.work]
    return {"rules": len(decisions), "kept": sum(1 for d in decisions if d.kept),
            "refused": len(refused),
            "refused-by-reason": dict(sorted(collections.Counter(r for d in refused for r in {x for x, _ in d.refused}).items())),
            "refused-by-first-reason": dict(sorted(collections.Counter(d.refused[0][0] for d in refused).items())),
            "work": dict(sorted(collections.Counter(d.work for d in decisions if d.work).items()))}


# -- widening the witnesses ---------------------------------------------------------------------------------------------

#: What a suite case of a kept-but-unwitnessed row can do at the edition (`widen_plan`).
RUNS, WIDEN, WIDEN_REFUSAL, BAND, NO_EDITION, MISSING = "runs", "widen", "widen-refusal", "band", "no-edition", "missing"
#: A case written in a reference format the edition does not define (`format_problem`, kb/Work PB5754): no witness there,
#: and no widening makes it one; converting its text (conformance-suite/harness/reformat.py) does.
FORMAT = "format"
#: A case whose file the working tree changes: neither a witness nor a candidate until it is committed and gated.
UNCOMMITTED = "uncommitted"
#: What a widening that does not hold leaves: the row needs a witness of its own at the edition.
NEEDS_OWN = "needs an own witness"
#: A planned row's state: a widening gives every lacking target a witness (`WIDENABLE`); none does for some target
#: (`NEEDS_OWN_WITNESS`, the work kind of the same name); the search already finds a witness for every target, so
#: `witness-batch` writes it and nothing need widen (`WITNESSED`: a stored row only, kb/Work PB5730).
WIDENABLE, WITNESSED = "widenable", "witness-batch"
#: WHY a lacking target of a row has no widening that helps it (kb/Work PB5730), one reason per case by its
#: `case_status` (`REASON_OF`), and `NO_CASE` for a target whose test-ref names no suite case: the input of the witness
#: lane, which writes the row's own witness.
NO_CASE, BAND_HELD, NOT_EVIDENCE, DIAGNOSIS_ONLY, PLACED_NOWHERE, NOT_COMMITTED, NOT_ITS_FORMAT = (
    "no-case", "band", "not-evidence", "diagnosis-only", "no-edition", "uncommitted", "reference-format")
REASON_OF = {
    FORMAT: NOT_ITS_FORMAT,      # its text is in a reference format the edition does not define (kb/Work PB5754)
    MISSING: NO_CASE,            # the test-ref names no case: the target has no suite case at all
    BAND: BAND_HELD,             # its cases cite a rule the edition does not have: the generator refuses it there
    WIDEN: NOT_EVIDENCE,         # a flag (or a refusal, below) is no evidence for the rule's kind (`EvidenceTable`)
    WIDEN_REFUSAL: NOT_EVIDENCE,
    RUNS: DIAGNOSIS_ONLY,        # it runs at the edition already, but as a diagnosis, for a rule whose kind takes none
    NO_EDITION: PLACED_NOWHERE,  # the case is placed at no edition
    UNCOMMITTED: NOT_COMMITTED,  # its band is the working tree's, which no gate ran
}


@dataclasses.dataclass(frozen=True)
class Unwitnessed:
    """A KEPT row of an edition with no witness of its own: its rule, the targets it carries into (where the candidate
    cases are looked for) and where it comes from (`source`: a stored record's `next.source`, or the read record of a
    read decision)."""
    rule_id: str
    to: tuple[str, ...]
    source: str
    #: What the row is for: a KEPT record, or an own verdict from a pointer (`OWN_VERDICT_FROM_POINTER`, kb/Work PB5597)
    #: whose read decided own-verdict work and whose only blocker is its evidence (`own_verdict_problems`).
    purpose: str = KEPT


def bare_kept(rows: Iterable[dict[str, Any]], schema: inventory_schema.Schema) -> tuple[int, list[dict[str, Any]]]:
    """Of an edition's inventory `rows`, the number of STORED KEPT records and those that carry no witness of their own
    (`lineage.own_witness_refs`): the walk `witness-batch` and `widen` share. A row closed on a derivation of its own
    (kb/Work PB5822) is left out: its view state, which `lineage.state_of` computed through `lineage.own_evidence`, is
    OK with no witness of its own, and no program can witness its rule."""
    kept = [r for r in rows if (r.get("next") or {}).get("kind") == KEPT]
    return len(kept), [r for r in kept if not lineage.own_witness_refs(r, schema) and r.get("state") != "OK"]


def unwitnessed(rows: Iterable[dict[str, Any]], decisions: Iterable[Decision],
                schema: inventory_schema.Schema) -> list[Unwitnessed]:
    """THE POPULATION `widen` plans (kb/Work PB5730): the KEPT rows of the edition that lack a witness of their own, from
    BOTH sources through this one function: the STORED KEPT records with no own `test-ref` (`bare_kept`, 1985's 1,373
    records of its comparison audit among them) and the READ DECISIONS refused only for their evidence
    (`NO_OWN_WITNESS`: a SAME, upheld, every flag answered, the pairing's targets; or an own verdict from a pointer,
    `own_verdict_problems`, blocked only for its evidence: its `purpose`). A rule both sources name is planned
    once, as its stored record; the two must then carry into the same targets, or the tool stops naming the rule."""
    out: dict[str, Unwitnessed] = {}
    for r in bare_kept(rows, schema)[1]:
        nxt = r["next"]
        out[r["rule-id"]] = Unwitnessed(r["rule-id"], tuple(nxt.get("to") or ()), nxt.get("source") or "")
    for d in decisions:
        if d.work not in (None, OWN_VERDICT_FROM_POINTER) or not d.refused \
                or any(r != NO_OWN_WITNESS for r, _ in d.refused):
            continue
        if (prior := out.get(d.rule_id)) is None:
            out[d.rule_id] = Unwitnessed(d.rule_id, tuple(d.to), d.source, OWN_VERDICT_FROM_POINTER if d.work else KEPT)
        elif sorted(prior.to) != sorted(d.to):
            raise SystemExit(f"{TOOL}: {d.rule_id}: its stored KEPT record ({prior.source}) carries into "
                             f"{sorted(prior.to)}, its read ({d.source}) into {sorted(d.to)}: one row, two populations")
    return list(out.values())


def lacking_reasons(ks: list[str], statuses: dict[str, dict[str, Any]]) -> list[str]:
    """Why a lacking target whose suite cases `ks` give it no helping widening still needs a witness of its own: the
    closed set `REASON_OF`, one per distinct cause among its cases, or `NO_CASE` when it has none."""
    return sorted({REASON_OF[statuses[k]["status"]] for k in ks}) if ks else [NO_CASE]


def case_status(c: CaseInfo | None, sp: str, bands: dict[str, list[str]]) -> str:
    """`RUNS` at the edition spelled `sp`; `WIDEN` (a case accepted or flagged at other editions) or `WIDEN_REFUSAL` (a
    refusal test refused at other editions) when every rule it cites exists at `sp` (the generator's own band check,
    `generate.check_cases`), else `BAND`; `NO_EDITION` for a case placed at no edition; `MISSING` for a test-ref naming
    no case; `FORMAT` for a case whose text is in a reference format `sp` does not define, whatever it runs at
    (`format_problem`, kb/Work PB5754)."""
    if c is None:
        return MISSING
    if not c.committed:
        return UNCOMMITTED
    if format_problem(c.source_format, sp):
        return FORMAT
    if sp in c.runs:
        return RUNS
    if not c.runs:
        return NO_EDITION
    if any(sp not in bands.get(r, [sp]) for r in c.rules):
        return BAND
    return WIDEN if (c.accept or c.flag_at) else WIDEN_REFUSAL


def widen_plan(ed: Edition, population: Iterable[Unwitnessed], w: Witnesses, lin: Lineage) -> dict[str, Any]:
    """For each row of `population` (`unwitnessed`: the KEPT rows of `ed` with no witness of their own, stored or read,
    and the own verdicts from a pointer blocked only by their evidence; the row's `for` says which), each target that
    lacks a witness of the kind the rule needs: its suite cases and what each can do at `ed`
    (`case_status`). A widening helps a target when it would give it such a witness: an ACCEPTED case always; a
    diagnosis (a flag or a refusal) only for a kind the evidence table names, or a declined facility (review R1-1). A row
    is `WIDENABLE` (every lacking target has a case that helps), `NEEDS_OWN_WITNESS` (with `reasons`: for each target no
    widening helps, why, `lacking_reasons`), or `WITNESSED` (no target lacks one: `witness-batch` writes it). The counts
    give rows by state and by reason (a row under each reason it carries) and cases by status."""
    sp = ed.spelling
    cases: dict[str, dict[str, Any]] = {}
    rows = []
    for u in population:
        kind = lin.kinds.get(u.rule_id, "")
        lacking = [t for t in u.to if w.search(ed, lin.diagnosable(u.rule_id), [t], None, lin.table)[1]]
        per_rule, reasons = {}, {}
        for t in lacking:
            ks = w.suite_cases(t)
            for k in ks:
                c = w.cases.get(k)
                cases.setdefault(k, {"status": case_status(c, sp, w.bands), "rules": list(c.rules) if c else []})
            diagnosis_counts = lin.diagnosable(u.rule_id) or w.verdicts.get(t) == lin.table.declined
            if not any(cases[k]["status"] == WIDEN and (w.cases[k].accept or diagnosis_counts)
                       or cases[k]["status"] == WIDEN_REFUSAL and diagnosis_counts for k in ks):
                reasons[t] = lacking_reasons(ks, cases)
            per_rule[t] = ks
        row = {"rule-id": u.rule_id, "to": list(u.to), "source": u.source, "for": u.purpose, "kind": kind,
               "cases": per_rule, "state": WITNESSED if not lacking else NEEDS_OWN_WITNESS if reasons else WIDENABLE}
        if reasons:
            row["reasons"] = reasons
        rows.append(row)
    return {"edition": ed.year, "rows": rows, "cases": cases,
            "counts": {"rows": dict(sorted(collections.Counter(r["state"] for r in rows).items())),
                       "reasons": dict(sorted(collections.Counter(
                           x for r in rows for x in {x for v in r.get("reasons", {}).values() for x in v}).items())),
                       "cases": dict(sorted(collections.Counter(c["status"] for c in cases.values()).items()))}}


def add_edition(values: Iterable[str], spelling: str, order: list[str]) -> list[str]:
    """An edition list with `spelling` added, in the editions' order."""
    return sorted(set(values) | {spelling}, key=order.index)


HEADER_FIELD = re.compile(r"^(?P<lead>\s*\*>\s*)(?P<key>accept|flag):(?P<value>.*)$")


def _listed(text: str) -> list[str]:
    return [e.strip().strip('"') for e in text.strip().strip("[]").split(",") if e.strip()]


def widen_case(path: pathlib.Path, spelling: str, order: list[str]) -> list[tuple[str, str]]:
    """Add `spelling` to the case header's `accept` list and to its flag's `at` list (the S1 measurement's method, kb/Work
    PB5574): the lines changed, as (old, new); the file's bytes are otherwise kept."""
    lines = path.read_text(encoding="utf-8").split("\n")
    edits = []
    for i, line in enumerate(lines):
        m = HEADER_FIELD.match(line)
        if m is None:
            if line.strip() == "*>---" or line.rstrip().endswith("*>---"):
                break
            continue
        if m["key"] == "accept":
            new = line.replace(m["value"].strip(), "[" + ", ".join(add_edition(_listed(m["value"]), spelling, order)) + "]", 1)
        else:
            new = re.sub(r"at:\s*(\[[^\]]*\])", lambda a: "at: [" + ", ".join(
                add_edition(_listed(a.group(1)), spelling, order)) + "]", line, count=1)
        if new != line:
            edits.append((line, new))
            lines[i] = new
    if edits:
        path.write_text("\n".join(lines), encoding="utf-8", newline="")
    return edits


def widen_refusal(sidecar: pathlib.Path, spelling: str, order: list[str]) -> list[tuple[str, str]]:
    """Add `spelling` to a refusal entry's `refuse-at` (wiseowl/diagnostics/<id>.json), the file's form otherwise kept."""
    raw = sidecar.read_text(encoding="utf-8")
    m = re.search(r'"refuse-at"\s*:\s*(\[[^\]]*\])', raw)
    if m is None:
        raise SystemExit(f"{TOOL}: {sidecar} holds no refuse-at list")
    new = '"refuse-at": [' + ", ".join(f'"{e}"' for e in add_edition(json.loads(m.group(1)), spelling, order)) + "]"
    if new == m.group(0):
        return []
    sidecar.write_text(raw.replace(m.group(0), new, 1), encoding="utf-8", newline="")
    return [(m.group(0), new)]


def revert(path: pathlib.Path, edits: list[tuple[str, str]]) -> None:
    raw = path.read_text(encoding="utf-8")
    for old, new in reversed(edits):
        if new not in raw:
            raise SystemExit(f"{TOOL}: {path}: the widened text {new!r} is no longer there to revert")
        raw = raw.replace(new, old, 1)
    path.write_text(raw, encoding="utf-8", newline="")


#: The exit statuses of conformance-suite/harness/run.py that mean it ran every test and wrote its results: 0 (no
#: verdict worse than PASS) and 1 (a verdict worse: each test's is in the results). Any other (2, a usage error; a
#: crash) means the results are not this run's, and no case it was to judge is proven (review R1-3).
RUN_RAN = (0, 1)
#: The run's two records in its scratch (review R2-1): what it PROVED (written once every proof is in) and what it has
#: WRITTEN to the tracked tree since (each case added after its files are written).
PROOF, APPLIED = "proof.json", "applied.json"


def prove(plan: dict[str, Any], w: Witnesses, ed: Edition, compiler: pathlib.Path, scratch: pathlib.Path,
          run: Any = subprocess.run) -> dict[str, str]:
    """PROVE FIRST, WITHOUT TOUCHING THE TRACKED SUITE (review R2-1): each `WIDEN` case of `plan` (a flagged one with its
    control), its band widened IN MEMORY, through the generator's own check and manifest entry (`Witnesses.entries`),
    run at the edition by the suite's runner (run.py, on a manifest written into `scratch`); each `WIDEN_REFUSAL` case
    compiled at the edition. Returns why each case is NOT proven, by key ("" for one that is): a case the generator
    refuses there, one whose verdict in this run's results is not PASS, every one when run.py exits outside `RUN_RAN`
    or writes no results, a refusal test not refused with its expected diagnostic or refused there also for one it does
    not expect (`refused_at`, against its newest refusing edition)."""
    sp = ed.spelling
    accepted = [k for k, c in sorted(plan["cases"].items()) if c["status"] == WIDEN]
    out: dict[str, str] = {}
    entries, refused = w.entries(accepted, sp) if accepted else ({}, {})
    out.update({k: f"the generator refuses it at {sp}: {why}" for k, why in refused.items()})
    if entries:
        suites = scratch / "run-suites" / sp
        suites.mkdir(parents=True)
        (suites / "manifest.json").write_text(json.dumps({"edition": sp, "generator": "lineage_reads.prove",
                                                          "tests": [e for es in entries.values() for e in es]}, indent=1),
                                              encoding="utf-8", newline="\n")
        results = scratch / "results"          # made by this run only: the scratch was empty
        r = run([sys.executable, str(SUITE / "harness" / "run.py"), "--compiler", "wiseowl", "--set", f"cobol={compiler}",
                 "--suites", str(scratch / "run-suites"), "--editions", sp, "--out", str(results)], cwd=REPO, check=False)
        file = results / "results.json"
        verdicts: dict[str, str] = {}
        if r.returncode in RUN_RAN and file.is_file():
            verdicts = {t["id"]: t["verdict"] for t in json.loads(file.read_text(encoding="utf-8"))["tests"]
                        if t.get("edition") == sp}
        for k, es in entries.items():
            bad = [f"{e['id']}: {verdicts.get(e['id'])}" for e in es if verdicts.get(e["id"]) != "PASS"]
            out[k] = (f"the harness's verdict at {sp} is not PASS ({'; '.join(bad)})" if verdicts else
                      f"the harness exited {r.returncode} and wrote no results of this run") if bad else ""
    order = inventory_schema.load_schema().editions
    for k in [k for k, c in sorted(plan["cases"].items()) if c["status"] == WIDEN_REFUSAL]:
        c = w.cases[k]
        out[k] = refused_at(c, sp, compiler, scratch, run, max(c.refuse_at, key=order.index)) or ""
    return out


def _write(w: Witnesses, k: str, sp: str, order: list[str]) -> None:
    """Write one PROVEN widening into the tracked tree: idempotent (a band that names the edition is left alone), so a
    run killed half way is finished by running it again."""
    info = w.cases[k]
    if info.accept or info.flag_at:
        widen_case(info.path, sp, order)
        if info.flag_at and info.control and (ctl := w.cases.get(f"{k.rsplit('/', 1)[0]}/{info.control}")) \
                and sp not in ctl.accept:
            widen_case(ctl.path, sp, order)
    else:
        widen_refusal(info.refusal, sp, order)


def widen_apply(plan: dict[str, Any], w: Witnesses, ed: Edition, compiler: pathlib.Path, scratch: pathlib.Path,
                run: Any = subprocess.run, on_write: Any = None) -> dict[str, Any]:
    """Widen to `ed` only what is PROVEN at it, and write it LAST (review R2-1). In a fresh `scratch` it proves every
    candidate without touching the tracked suite (`prove`), writes the proof (`PROOF`) and then writes each proven
    widening into the tree, recording each in `APPLIED` once its files are written. A kill before the writes leaves the
    tree untouched; a kill during them leaves only PROVEN widenings, and running `--apply` again with the same scratch
    reads the proof and finishes the writes, saying so (each write is idempotent). Everything not proven is listed as
    needing an own witness and never written. `run` is subprocess.run and `on_write` a hook after each case's write,
    parameters so the self-test plants the harness and a kill."""
    order = inventory_schema.load_schema().editions
    sp = ed.spelling
    proof_file, applied_file = scratch / PROOF, scratch / APPLIED
    resumed = proof_file.is_file()
    if resumed:
        proof = json.loads(proof_file.read_text(encoding="utf-8"))
        if proof.get("edition") != ed.year:
            raise SystemExit(f"{TOOL}: {proof_file} proves widenings to {proof.get('edition')}, not {ed.year}")
    else:
        if scratch.exists() and any(scratch.iterdir()):
            raise SystemExit(f"{TOOL}: widen --apply needs an empty --scratch, or one holding an unfinished run's "
                             f"{PROOF}, so no result it reads is an earlier run's")
        scratch.mkdir(parents=True, exist_ok=True)
        why = prove(plan, w, ed, compiler, scratch, run)
        proof = {"edition": ed.year, "proven": sorted(k for k, v in why.items() if not v),
                 "not-proven": {k: v for k, v in sorted(why.items()) if v}}
        proof_file.write_text(json.dumps(proof, indent=1) + "\n", encoding="utf-8", newline="\n")
    applied = set(json.loads(applied_file.read_text(encoding="utf-8"))) if applied_file.is_file() else set()
    finished = [k for k in proof["proven"] if k in applied]
    for k in proof["proven"]:
        if k in applied:
            continue
        _write(w, k, sp, order)
        applied.add(k)
        applied_file.write_text(json.dumps(sorted(applied)) + "\n", encoding="utf-8", newline="\n")
        if on_write is not None:
            on_write(k)
    return {"widened": proof["proven"], "reverted": {k: f"{NEEDS_OWN}: {v}" for k, v in proof["not-proven"].items()},
            "resumed": resumed, "already-written": finished}


#: An error diagnosis in the compiler's output, by its code. Only an error refuses a program; a warning diagnoses nothing.
ERROR_CODE = re.compile(r"\berror (COBOL\d{4})\b")
#: A diagnostic code named in a refusal entry's `expect` text.
CODE = re.compile(r"\bCOBOL\d{4}\b")


def _compile(c: CaseInfo, spelling: str, compiler: pathlib.Path, scratch: pathlib.Path, run: Any) -> str | None:
    """The compiler's output for the case at `--std spelling`, None when it compiles. It runs in its own scratch
    directory, never inside the git tree (as the suite's runner compiles in its work directory)."""
    out = scratch / "refusals" / c.key.replace("/", "_") / spelling
    out.mkdir(parents=True, exist_ok=True)
    r = run([str(compiler), str(c.path), "--std", spelling, "--source-format", c.source_format, "-o", str(out / "p.dll")],
            capture_output=True, text=True, encoding="utf-8", errors="replace", cwd=out, timeout=600)
    return None if r.returncode == 0 else (r.stdout or "") + (r.stderr or "")


def refused_at(c: CaseInfo, spelling: str, compiler: pathlib.Path, scratch: pathlib.Path, run: Any = subprocess.run,
               reference: str | None = None) -> str | None:
    """Why the refusal test `c` does not WITNESS its rule by a refusal at the edition (None when it does). Compiled at
    `--std` with `compiler`, the compilation must fail and report the entry's code or text, as the gate's refusal row
    requires, AND every error it draws there must be one the case expects (review R1-1 of records-2002-reads-2): a code
    its entry's `expect` names, or one it draws too at `reference`, an edition it is already refused at and witnesses its
    rule (a cascade the gate's row passes there). Any other error, an introduction gate (COBOL0900: the construct does
    not exist at the edition) or the edition's own limit (COBOL1567: a word longer than it allows), makes the program
    illegal there before the rule applies: its refusal passes whatever the rule does, so it witnesses no rule there."""
    text = _compile(c, spelling, compiler, scratch, run)
    if text is None:
        return f"accepted at {spelling}"
    if not (c.expect and c.expect in text):
        return f"refused at {spelling} without {c.expect!r}"
    unexpected = set(ERROR_CODE.findall(text)) - set(CODE.findall(c.expect))
    if unexpected and reference is not None and reference != spelling:
        unexpected -= set(ERROR_CODE.findall(_compile(c, reference, compiler, scratch, run) or ""))
    if not unexpected:
        return None
    return (f"refused at {spelling} also for {', '.join(sorted(unexpected))}, which its entry does not expect "
            f"({c.expect!r}) and it does not draw at {reference or 'an edition it witnesses'}: the program is illegal at "
            f"{spelling} before its rule applies, so it witnesses no rule there")


# -- the audit ----------------------------------------------------------------------------------------------------------

def licences(slices: Iterable[Slice]) -> dict[str, str | None]:
    """Each read rule's stated `licence-<year>` (None when its read states none), by rule id."""
    return {rid: r.licence for s in slices for rid, r in s.reads.items()}


def stored_errors(ed: Edition, rows: Iterable[dict[str, Any]], slices: list[Slice], w: Witnesses | None,
                  lin: Lineage, schema: inventory_schema.Schema | None = None) -> list[str]:
    """Why a STORED change record of `ed` does not stand. One whose source names a read record is that record's decision:
    the record (whose digests `load` verified against its content) holds a read of the rule, the read is a SAME its
    refuter UPHELD, the record is KEPT and carries into the read's rules. And at EVERY older edition (kb/Work PB5675: one
    rule, no edition key), every stored KEPT row's OWN witnesses (its `test-ref`, with `w`) are conformance-suite cases
    that run at the edition, of the kind its rule needs (`Witnesses.own_problems`; the licence is the read's when the
    record came from one). A KEPT row with no witness of its own is no error here: its state is GAP (`lineage.state_of`).
    A witness whose band is later narrowed past the edition, or that moves, turns the audit red, naming row and case. And
    EVERY row's own conformance-suite witnesses, a verdict's of its own among them, are written in a reference format
    the edition defines (`format_problem`, kb/Work PB5754). A stored OWN verdict of a rule whose tracked read decides
    `own-verdict-from-pointer` (`own_reads`), or whose notes name its read, is held to its read and to the same test of
    its own witnesses (`own_stored_errors`)."""
    schema = schema or inventory_schema.load_schema()
    by_slice = {s.name: s for s in slices}
    owned = own_reads(ed, slices, lin)
    prefix = f"lineage-reads-{ed.year}/"
    errs = []
    for r in rows:
        nxt = r.get("next") or {}
        src = nxt.get("source") or ""
        if nxt.get("kind") != KEPT and w is not None:
            # a row with a verdict of its own (or none yet): its own witnesses are held to the edition's reference
            # format, as a KEPT row's are by `own_problems` (kb/Work PB5754)
            errs += [f"{r['rule-id']}: {NO_OWN_WITNESS}: {why}"
                     for why in w.format_problems(ed, lineage.own_witness_refs(r, schema))]
        if not nxt.get("kind"):
            if r.get("verdict"):
                errs += own_stored_errors(ed, r, owned.get(r["rule-id"]), by_slice, w, lin, schema)
            continue
        rid, licence = r["rule-id"], None
        if src.startswith(prefix):
            s = by_slice.get(src[len(prefix):].removesuffix(".json"))
            read = s.reads.get(rid) if s else None
            f = s.refutes.get(rid) if s else None
            if read is None:
                errs.append(f"{rid}: its source {src} holds no read of the rule")
                continue
            if nxt["kind"] != KEPT:
                errs.append(f"{rid}: a {nxt['kind']} record from a read: a read writes KEPT records only")
                continue
            if read.verdict != "SAME" or f is None or f.verdict != "UPHELD":
                errs.append(f"{rid}: its read is {read.verdict} and its refuter {f.verdict if f else 'absent'}: a KEPT "
                            "record needs a SAME the refuter UPHELD")
                continue
            if sorted(read.to) != sorted(nxt.get("to") or []):
                errs.append(f"{rid}: it carries into {nxt.get('to')}, its read into {list(read.to)}")
                continue
            licence = read.licence
        if nxt["kind"] == KEPT and w is not None:
            errs += [f"{rid}: {NO_OWN_WITNESS}: {why}" for why in w.own_problems(
                ed, lineage.own_witness_refs(r, schema), lin.diagnosable(rid), licence)]
    # an own verdict from a read meets the format test twice (`format_problems`, then `own_stored_errors`' own_problems)
    return list(dict.fromkeys(errs))


def refutation_errors(year: int, slices: Iterable[Slice], repo: pathlib.Path = REPO) -> list[str]:
    """Why a derivation refuter record file generated from the read records (`REFUTATIONS`) no longer is what its slice's
    tracked record gives (`refutation_lines`): edited by hand, or left behind by a slice that no longer has one. A file
    `derivation-batch` has not written yet is no error: the register names the files its rows stand on."""
    by = {s.name: s for s in slices}
    pattern = pathlib.Path(REFUTATIONS.format(year=year, slice="*"))
    out = []
    for path in sorted((repo / pattern.parent).glob(pattern.name)):
        name = path.name[len(f"lineage-reads-{year}-"):-len(".jsonl")]
        s = by.get(name)
        if s is None or not s.derivation_refutes:
            out.append(f"{path.relative_to(repo).as_posix()}: no read record of {year} holds a derivation refute of slice {name}")
        elif path.read_text(encoding="utf-8") != refutation_lines(year, s):
            out.append(f"{path.relative_to(repo).as_posix()}: not what the read record {source_of(year, name)} gives "
                       "(rewrite it with audit_batch.py derivation-batch)")
    return out


def older_years() -> list[int]:
    """Every held edition older than the newest: each one's chained rows close by the one own-edition rule."""
    return [e.year for e in inventory_schema.load_schema().all_editions if e.held and not e.newest]


def check(years: list[int]) -> int:
    """THE GATE'S LINEAGE READS AUDIT over `years`: one reading of the suite shared by every edition."""
    red = []
    schema = inventory_schema.load_schema()
    suite = Suite.read()
    for y in years:
        ed = edition(y)
        slices = load(y)
        rows = inventory_view.inventory(y, schema)
        from_reads = [r for r in rows if ((r.get("next") or {}).get("source") or "").startswith(f"lineage-reads-{y}/")]
        kept = [r for r in rows if (r.get("next") or {}).get("kind") == KEPT]
        witnessed = [r for r in kept if lineage.own_witness_refs(r, schema)]
        lin = Lineage.of(ed)
        owned = own_reads(ed, slices, lin)
        own = [r for r in rows if not (r.get("next") or {}).get("kind") and r.get("verdict")
               and (r["rule-id"] in owned or OWN_SOURCE.match(r.get("notes") or ""))]
        errs = stored_errors(ed, rows, slices, Witnesses.of(ed.next_held, suite), lin, schema) \
            + refutation_errors(y, slices)
        print(f"{y}: {len(slices)} read record(s), {sum(len(s.reads) for s in slices)} rule(s) read, each record's "
              f"digests verified; {len(from_reads)} stored record(s) from reads; {len(kept)} KEPT record(s), "
              f"{len(witnessed)} with a witness of their own, each held to a case that runs at {ed.spelling} of the kind "
              f"its rule needs; {len(kept) - len(witnessed)} without one (GAP); {len(own)} own verdict(s) of rules "
              f"whose read decides {OWN_VERDICT_FROM_POINTER}, each held to the same test")
        for e in errs:
            print(f"  ⛔ {e}")
        if errs:
            red.append(y)
    print(f"=== LINEAGE READS: {'GREEN' if not red else f'RED ({red})'} ===")
    return 1 if red else 0


# -- the command line -----------------------------------------------------------------------------------------------------

def witness_batch(year: int, out: pathlib.Path) -> int:
    """`witness-batch` (kb/Work PB5675): a record_verdicts batch giving each stored KEPT row of `year` that carries no
    witness of its own the cases the candidate search finds at the edition (`Witnesses.search`, the read's licence where
    the record came from one), when it finds one of the kind the rule needs for EVERY rule the record carries into. Each
    record is witness-only (`{"rule-id", "test-ref"}`): it adds that field and restates nothing, so the batch is
    deterministic and a second run, once applied, writes no record. It prints how many rows have such a witness for
    every rule, for some and for none: the rows it leaves stay GAP until a witness of their own exists."""
    ed = edition(year)
    if ed.newest:
        raise SystemExit(f"{TOOL}: witness-batch --edition {year}: the newest edition has no chained rows")
    schema = inventory_schema.load_schema()
    w, lin = Witnesses.of(ed.next_held), Lineage.of(ed)
    lic = licences(load(year))
    prefix = f"lineage-reads-{year}/"
    kept, bare = bare_kept(inventory_view.inventory(year, schema), schema)
    records, some, none = [], 0, 0
    for r in bare:
        to = r["next"].get("to") or []
        src = r["next"].get("source") or ""
        found, missing = w.search(ed, lin.diagnosable(r["rule-id"]), to,
                                  lic.get(r["rule-id"]) if src.startswith(prefix) else None, lin.table)
        if not missing and found:
            records.append({"rule-id": r["rule-id"],
                            "test-ref": schema.test_ref_sep.join(f"{lineage.SUITE_FORM}:{k}" for k in found)})
        elif len(missing) < len(to):
            some += 1
        else:
            none += 1
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps({"batch": f"own-edition-witnesses-{year}", "records": records}, indent=1,
                              ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    print(f"witness-batch {year}: {kept} stored KEPT row(s), {kept - len(bare)} with a witness of their own; "
          f"of the {len(bare)} without one, a witness at {ed.spelling} of the kind the rule needs for every rule it "
          f"carries into: {len(records)} (written -> {out}); for some: {some}; for none: {none}\n"
          f"  apply: python scripts/spec/record_verdicts.py --edition {year} {out}")
    return 0


def widen_reads(year: int, state: pathlib.Path | None, tracked: pathlib.Path | None = None) -> list[Slice]:
    """The read records whose decisions `widen` plans beside the stored rows (kb/Work PB5730): the audit run's (`state`,
    the run import-reads wrote), or none when the edition has no read records (`tracked`, its tracked records'
    directory). An edition WITH read records and no `state` stops the tool, saying so: its decisions would be silently
    missing from the plan."""
    if state is not None:
        run = json.loads((state / "reads.json").read_text(encoding="utf-8"))
        return load(year, pathlib.Path(run["record"]))
    d = tracked or reads_dir(year)
    if held := load(year, d):
        raise SystemExit(f"{TOOL}: widen --edition {year}: {year} has {len(held)} read record(s) ({d}), whose decisions "
                         "are planned from the audit run import-reads wrote: give it as --state RUN")
    return []


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The read records of an older edition's rules (kb/Work PB5597).")
    ap.add_argument("--check", type=int, nargs="?", const=0, metavar="YEAR",
                    help="every read record interprets, every stored record from a read stands and every KEPT row's own "
                         "witness runs at its edition; with no YEAR every older held edition: the gate's LINEAGE READS audit")
    sub = ap.add_subparsers(dest="cmd")
    wd = sub.add_parser("widen", help="plan (and with --apply, make) the band widening of the kept rows' witnesses")
    wd.add_argument("--edition", type=int, required=True)
    wd.add_argument("--state", help="the audit run import-reads wrote (its read records); required when the edition has "
                                    "read records, needless when it has none (its stored KEPT rows are planned either way)")
    wd.add_argument("--out", required=True, help="the plan (JSON) to write")
    wd.add_argument("--apply", action="store_true", help="edit the suite's case headers, keeping what passes at the edition")
    wd.add_argument("--compiler", help="with --apply: the cobol executable the harness runs")
    wd.add_argument("--scratch", help="with --apply: an EMPTY scratch directory for the generated suites and the run")
    wb = sub.add_parser("witness-batch", help="the record_verdicts batch of the stored KEPT rows' own-edition witnesses")
    wb.add_argument("--edition", type=int, required=True)
    wb.add_argument("--out", required=True)
    a = ap.parse_args(argv)
    if a.cmd == "witness-batch":
        return witness_batch(a.edition, pathlib.Path(a.out))
    if a.cmd == "widen":
        ed = edition(a.edition)
        if ed.newest:
            raise SystemExit(f"{TOOL}: widen --edition {a.edition}: the newest edition has no chained rows")
        slices = widen_reads(a.edition, pathlib.Path(a.state) if a.state else None)
        w, lin = Witnesses.of(ed.next_held), Lineage.of(ed)
        decisions = decide(ed, slices, lineage_screen.committed(a.edition), w, lin) if slices else []
        schema = inventory_schema.load_schema()
        plan = widen_plan(ed, unwitnessed(inventory_view.inventory(a.edition, schema), decisions, schema), w, lin)
        if a.apply:
            if not a.compiler or not a.scratch:
                raise SystemExit(f"{TOOL}: widen --apply needs --compiler and --scratch")
            plan["applied"] = widen_apply(plan, w, ed, pathlib.Path(a.compiler), pathlib.Path(a.scratch))
        pathlib.Path(a.out).write_text(json.dumps(plan, indent=1) + "\n", encoding="utf-8", newline="\n")
        print(f"widen {a.edition}{'' if a.apply else ' (dry run: nothing edited)'}: rows {plan['counts']['rows']}; rows "
              f"needing an own witness, by reason {plan['counts']['reasons']}; cases {plan['counts']['cases']} -> {a.out}")
        if a.apply:
            got = plan["applied"]
            print(f"  {'resumed an unfinished run: ' if got['resumed'] else ''}proven and written {len(got['widened'])} "
                  f"({len(got['already-written'])} written before), not proven {len(got['reverted'])} (never written)")
        return 0
    if a.check is None:
        ap.print_help()
        return 2
    return check([a.check] if a.check else older_years())


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
