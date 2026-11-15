#!/usr/bin/env python3
"""THE READ RECORDS of an older edition's rules: the durable record of each READ decision of a rule against its
counterpart in the next edition, and what may become a kept change record (kb/Work PB5597; DESIGN-spec-conformance-
review.md section 10.6).

    python scripts/selftests/spec/lineage_reads/test_reads.py         # its cases (the case layout, kb/Work PB5428)
    python scripts/spec/audit_batch.py import-reads --edition 2002 --reads DIR [--asked DIR] --state NEW
                                                    # the IMPORT: DIR's <slice>-read.json and <slice>-refute.json become
                                                    # the tracked read records and an audit run's state (audit_batch.py)
    python scripts/spec/lineage_reads.py --check [YEAR]                 # the gate's LINEAGE READS audit
    python scripts/spec/lineage_reads.py widen --edition YEAR --state DIR --out PLAN [--apply --compiler EXE]
    python scripts/spec/lineage_reads.py own-edition --edition YEAR --out FILE   # the stored KEPT records' witnesses

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
     passes it (`Witnesses`, `NO_OWN_WITNESS`). `widen` widens the witnesses that can run there.
WHAT THE OTHER VERDICTS BECOME (`decide`'s work items, never a kept record): a DIFFERENT, or a SAME the refuter overturned,
is `needs-own-witness` when its reader or refuter showed the compiler correct at the edition (the probe is the candidate
witness), `defect` when it showed it wrong (the records worker matches it to a kb/Work note), `undecided` when nobody
showed the compiler (or the read is DOUBTFUL), and `uninterpretable` when the judgment of the compiler is a sentence no
reading turns into one of those. The list is generated into the audit run's state (`reads-decisions.json`), never tracked:
it is a view of the read records, not a register (CLAUDE.md rule 8). `record_verdicts.py --edition YEAR` records each
once a witness or a note exists.

THE AUDIT (`--check`, the gate's LINEAGE READS): every read record loads and normalizes; every stored change record whose
source names a read record is that record's decision (the read SAME, the refuter UPHELD, the same `to`), and every rule it
carries into still has a witness at the edition. A later change that narrows a witness's band, or rewrites a read,
turns it red. (A flag the screen raises AFTER a record is stored is the screen's change, listed as owed by
lineage_screen.py --check, never red: kb/Work PB5597, branch review R2-1 of PB5589.)
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
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
import lineage_screen  # noqa: E402  (the flags a decision answers)
from inventory_schema import Edition, edition  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "lineage_reads"
REPO = inventory_schema.REPO
SUITE = REPO / "conformance-suite"

READ_VERDICTS = ("SAME", "DIFFERENT", "DOUBTFUL")
REFUTE_VERDICTS = ("UPHELD", "OVERTURNED", "DOUBTFUL")
#: The judgments of the compiler at the edition a reader or refuter states (`compiler-at-2002`, by the brief's words).
CORRECT, WRONG, NOT_PROBED = "correct", "wrong", "not probed"
COMPILER = re.compile(r"\A\s*(correct|wrong|not probed)\b", re.I)
#: Why a SAME read upheld by its refuter is still refused a kept record.
NO_REFUTER, FLAG_UNANSWERED, NO_OWN_WITNESS = "no refuter record", "flag unanswered", "no own-edition witness"
#: The work items of the reads that are not kept.
NEEDS_OWN_WITNESS, DEFECT, UNDECIDED, UNINTERPRETABLE = "needs-own-witness", "defect", "undecided", "uninterpretable"
KEPT = "KEPT"
SLICE = re.compile(r"\A[a-z0-9][a-z0-9-]*?\Z")
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


@dataclasses.dataclass(frozen=True)
class Refute:
    rule_id: str
    slice: str
    verdict: str
    reason: str
    probe: str | None
    compiler: Judgment | None


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


def _judgment(case: dict[str, Any], slice_name: str, rid: str) -> Judgment | None:
    text = _text(case, "compiler-at-2002", slice_name, rid, required=False)
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


def normal_read(slice_name: str, case: Any) -> Read:
    if not isinstance(case, dict):
        raise _fail(slice_name, None, f"a read case is a map, not {type(case).__name__}")
    rid = _text(case, "rule-id", slice_name, case.get("rule-id"))
    to = case.get("to")
    if not isinstance(to, list) or not to or not all(isinstance(t, str) and t for t in to):
        raise _fail(slice_name, rid, f"`to` must be a non-empty list of rule ids, not {to!r}")
    verdict = case.get("verdict")
    if verdict not in READ_VERDICTS:
        raise _fail(slice_name, rid, f"the reader's verdict {verdict!r} is not one of {READ_VERDICTS}")
    _citations(case, slice_name, rid)
    return Read(rid, slice_name, tuple(to), verdict, _text(case, "reason", slice_name, rid),
                _text(case, "probe", slice_name, rid, required=False), _judgment(case, slice_name, rid),
                _flag_answers(case, slice_name, rid))


def normal_refute(slice_name: str, case: Any) -> Refute:
    if not isinstance(case, dict):
        raise _fail(slice_name, None, f"a refute case is a map, not {type(case).__name__}")
    rid = _text(case, "rule-id", slice_name, case.get("rule-id"))
    verdict = case.get("verdict")
    if verdict not in REFUTE_VERDICTS:
        raise _fail(slice_name, rid, f"the refuter's verdict {verdict!r} is not one of {REFUTE_VERDICTS}")
    _citations(case, slice_name, rid)
    return Refute(rid, slice_name, verdict, _text(case, "reason", slice_name, rid),
                  _text(case, "probe", slice_name, rid, required=False), _judgment(case, slice_name, rid))


@dataclasses.dataclass
class Slice:
    """One read record: its reads and refutes by rule, the flags the slice asked, and the reads its refuter disputes."""
    name: str
    reads: dict[str, Read]
    refutes: dict[str, Refute]
    asked: dict[str, dict[str, str]]
    disputed: set[str]


def normal_slice(name: str, doc: dict[str, Any]) -> Slice:
    """A read record (the tracked file's content) as a `Slice`; a RecordError names the slice and rule of the first case
    it cannot interpret. A rule read twice, a refute of a rule the slice did not read, or a refute of a read that is not
    SAME is uninterpretable too."""
    read = doc.get("read")
    if not isinstance(read, dict) or not isinstance(read.get("cases"), list):
        raise _fail(name, None, "the record holds no reader's `cases`")
    reads: dict[str, Read] = {}
    for c in read["cases"]:
        r = normal_read(name, c)
        if r.rule_id in reads:
            raise _fail(name, r.rule_id, "the reader decides the rule twice")
        reads[r.rule_id] = r
    refutes: dict[str, Refute] = {}
    disputed: set[str] = set()
    if (refute := doc.get("refute")) is not None:
        if not isinstance(refute, dict) or not isinstance(refute.get("cases"), list):
            raise _fail(name, None, "the refuter's record holds no `cases`")
        for c in refute["cases"]:
            f = normal_refute(name, c)
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
    return Slice(name, reads, refutes, {k: dict(v) for k, v in flags.items()}, disputed)


def record_path(year: int, slice_name: str, directory: pathlib.Path | None = None) -> pathlib.Path:
    return (directory or reads_dir(year)) / f"{slice_name}.json"


def load(year: int, directory: pathlib.Path | None = None) -> list[Slice]:
    """Every read record of `year`, normalized; a SystemExit lists every record that cannot be interpreted, and a rule
    two slices read."""
    d = directory or reads_dir(year)
    out, errs, seen = [], [], {}
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
        for rid in s.reads:
            if rid in seen:
                errs.append(f"{s.name}: {rid}: read again (first read in {seen[rid]})")
            seen[rid] = s.name
        out.append(s)
    if errs:
        raise SystemExit(f"{TOOL}: {len(errs)} read record(s) cannot be interpreted:\n  " + "\n  ".join(errs))
    return out


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def import_records(year: int, reads: pathlib.Path, asked: pathlib.Path | None, out: pathlib.Path) -> list[str]:
    """Copy every slice of `reads` (`<slice>-read.json`, with its `<slice>-refute.json` when it exists, and the slice's
    `asked`/<slice>.json when `asked` is given) into the read record `out`/<slice>.json, verbatim, with the digest of each
    source. Whichever slices exist are imported; a refute with no read is refused. Nothing is written unless every slice
    normalizes (`normal_slice`): the errors are listed by slice and rule. Returns the slices written or rewritten."""
    if not reads.is_dir():
        raise SystemExit(f"{TOOL}: {reads} is not a directory of read files")
    names = sorted(p.name[:-len("-read.json")] for p in reads.glob("*-read.json"))
    if orphans := sorted(p.name for p in reads.glob("*-refute.json") if not (reads / p.name.replace("-refute", "-read")).exists()):
        raise SystemExit(f"{TOOL}: refuter records with no reader's record: {orphans}")
    docs, errs = {}, []
    for name in names:
        if not SLICE.match(name):
            errs.append(f"{name}: a slice is named by lower-case letters, digits and dashes")
            continue
        sources: dict[str, pathlib.Path] = {"read": reads / f"{name}-read.json"}
        if (f := reads / f"{name}-refute.json").exists():
            sources["refute"] = f
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
               "refute": loaded.get("refute"), "imported": {k: digest(b) for k, b in sorted(raw.items())}}
        try:
            normal_slice(name, doc)
        except RecordError as e:
            errs.append(str(e))
            continue
        docs[name] = doc
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
    sys.path.insert(0, str(SUITE / "harness"))
    import generate  # noqa: PLC0415  (THE one reader of the suite's headers and its band check)
    return generate


@dataclasses.dataclass(frozen=True)
class CaseInfo:
    """One conformance-suite case as the generator reads it: its test-ref key (`cases/<folder>/<id>` or
    `wiseowl/cases/<folder>/<id>`), the editions it runs at (`accept` and its flag's `at`), the editions it is ACCEPTED
    at, whether it has a flag, the rules it cites and its control."""
    key: str
    path: pathlib.Path
    runs: tuple[str, ...]
    accept: tuple[str, ...]
    flag_at: tuple[str, ...]
    rules: tuple[str, ...]
    control: str | None


class Witnesses:
    """The next edition's rules' conformance-suite witnesses and where each runs. A case runs at an edition when its header
    names it (the generator places it in that edition's manifest only then, and refuses it there when a rule it cites does
    not exist at the edition: `generate.check_cases`); the gate runs every case of every edition's manifest, so a case
    that runs at the edition on a green tree passes there."""

    def __init__(self, cases: dict[str, CaseInfo], refs: dict[str, list[str]], bands: dict[str, list[str]]):
        self.cases, self.refs, self.bands = cases, refs, bands

    @classmethod
    def of(cls, newer: int) -> "Witnesses":
        """The repository's: the generator's reading of every case (both trees), and the test-refs and bands of the rows
        of the edition `newer` (the next held edition the records carry into)."""
        g = _generator()
        inputs, _problems = g.repository_inputs()
        reader = g.Reader(g.SUITE, inputs)
        reader.read_tree()
        cases = {}
        for c in reader.cases:
            key = pathlib.PurePath(c.path).with_suffix("").as_posix()
            at = tuple(c.flag.at) if c.flag is not None else ()
            cases[key] = CaseInfo(key, g.SUITE / c.path, tuple(dict.fromkeys((*c.accept, *at))), tuple(c.accept), at,
                                  tuple(c.rules), c.control)
        schema = inventory_schema.load_schema()
        rows = inventory_view.inventory(newer, schema)
        refs = {r["rule-id"]: schema.split(r.get("test-ref", "") or "", schema.test_ref_sep) for r in rows}
        bands = {r["rule-id"]: schema.split(r.get("editions", "") or "", schema.editions_sep) for r in rows}
        return cls(cases, refs, bands)

    def suite_cases(self, rule: str) -> list[str]:
        """The keys of the conformance-suite cases the rule's test-ref names (`conformance:<key>`), in its order."""
        return [ref.partition(":")[2].strip() for ref in self.refs.get(rule, []) if ref.partition(":")[0] == "conformance"]

    def at(self, rule: str, spelling: str) -> list[str]:
        """The rule's suite witnesses that run at the edition spelled `spelling`."""
        return [k for k in self.suite_cases(rule) if k in self.cases and spelling in self.cases[k].runs]

    def missing(self, ed: Edition, to: Iterable[str]) -> list[str]:
        """The rules of `to` with no suite witness that runs at `ed`: empty when the evidence is at the row's own edition."""
        return [t for t in to if not self.at(t, ed.spelling)]


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


def decide(ed: Edition, slices: Iterable[Slice], screen: dict[str, Any] | None,
           witnesses: Witnesses | None) -> list[Decision]:
    """Each read rule's `Decision` (module docstring). `witnesses` None leaves the evidence unchecked (the audit run's
    import, whose writer checks it); the screen is the edition's committed one."""
    flags = (screen or {}).get("marks", {})
    out = []
    for s in slices:
        for rid, r in sorted(s.reads.items()):
            base = dict(rule_id=rid, slice=s.name, to=r.to, source=source_of(ed.year, s.name))
            f = s.refutes.get(rid)
            if r.verdict == "DOUBTFUL":
                out.append(Decision(**base, work=UNDECIDED, probe=r.probe, why="the reader is DOUBTFUL: " + r.reason))
            elif r.verdict == "DIFFERENT":
                kind, why = work_of(r.compiler, r.probe, "reader")
                if rid in s.disputed:
                    kind, why = UNDECIDED, "the refuter disputes the reader's DIFFERENT"
                out.append(Decision(**base, work=kind, probe=r.probe or (r.compiler.text if r.compiler else None), why=why))
            elif f is None:
                out.append(Decision(**base, refused=((NO_REFUTER, "no refuter has attacked the SAME"),)))
            elif f.verdict == "DOUBTFUL":
                out.append(Decision(**base, work=UNDECIDED, probe=f.probe, why="the refuter is DOUBTFUL: " + f.reason))
            elif f.verdict == "OVERTURNED":
                kind, why = work_of(f.compiler or r.compiler, f.probe or r.probe, "refuter" if f.compiler else "reader")
                out.append(Decision(**base, work=kind, probe=f.probe or r.probe, why="the refuter OVERTURNED the SAME; " + why))
            else:
                answers, unanswered = answers_for(r, s.asked.get(rid, {}), flags.get(rid, {}))
                refused = [(FLAG_UNANSWERED, u) for u in unanswered]
                if witnesses is not None:
                    refused += [(NO_OWN_WITNESS, f"{t}: no conformance-suite witness runs at {ed.spelling}")
                                for t in witnesses.missing(ed, r.to)]
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

def widen_plan(ed: Edition, decisions: list[Decision], w: Witnesses) -> dict[str, Any]:
    """For each read rule that is refused ONLY for its evidence (a SAME, upheld, every flag answered), each next-edition
    rule it carries into: its suite cases and what each can do at `ed` (`runs` there; `widen`, a case accepted or
    flagged elsewhere whose every cited rule exists at `ed`, the generator's own band check; `band`, a case citing a
    rule whose band lacks `ed`; `refusal`, a case with no accepted or flagged edition; `missing`, a test-ref naming no
    case). A row is `has-witness`, `widenable` (each rule it lacks evidence for has a case to widen) or `needs-own-witness`."""
    sp = ed.spelling
    cases: dict[str, dict[str, Any]] = {}
    rows = []
    for d in decisions:
        if d.work or not d.refused or any(r != NO_OWN_WITNESS for r, _ in d.refused):
            continue
        per_rule = {}
        for t in d.to:
            got = []
            for k in w.suite_cases(t):
                c = w.cases.get(k)
                if c is None:
                    status = "missing"
                elif sp in c.runs:
                    status = "runs"
                elif not c.runs:
                    status = "refusal"
                elif any(sp not in w.bands.get(r, [sp]) for r in c.rules):
                    status = "band"
                else:
                    status = "widen"
                cases.setdefault(k, {"status": status, "rules": list(c.rules) if c else []})
                got.append(k)
            per_rule[t] = got
        statuses = {t: {cases[k]["status"] for k in ks} for t, ks in per_rule.items()}
        lacking = [t for t, st in statuses.items() if "runs" not in st]
        state = ("has-witness" if not lacking else "widenable" if all("widen" in statuses[t] for t in lacking)
                 else "needs-own-witness")
        rows.append({"rule-id": d.rule_id, "to": list(d.to), "state": state, "cases": per_rule})
    return {"edition": ed.year, "rows": rows, "cases": cases,
            "counts": {"rows": dict(sorted(collections.Counter(r["state"] for r in rows).items())),
                       "cases": dict(sorted(collections.Counter(c["status"] for c in cases.values()).items()))}}


def add_edition(value: str, spelling: str, order: list[str]) -> str:
    """A header's edition list `[a, b]` with `spelling` added, in the editions' order (the spelling the header keeps)."""
    eds = [e.strip() for e in value.strip().strip("[]").split(",") if e.strip()]
    return "[" + ", ".join(sorted(set(eds) | {spelling}, key=order.index)) + "]"


HEADER_FIELD = re.compile(r"^(?P<lead>\s*\*>\s*)(?P<key>accept|flag):(?P<value>.*)$")


def widen_case(path: pathlib.Path, spelling: str, order: list[str]) -> list[tuple[str, str]]:
    """Add `spelling` to the case's `accept` list and to its flag's `at` list (the S1 method, kb/Work PB5574's
    measurement): the header lines changed, as (old, new). The file's bytes are otherwise kept."""
    raw = path.read_text(encoding="utf-8")
    lines = raw.split("\n")
    edits = []
    for i, line in enumerate(lines[:80]):
        m = HEADER_FIELD.match(line)
        if not m:
            continue
        if m["key"] == "accept":
            new = f"{m['lead']}accept:{' ' if m['value'].startswith(' ') else ''}{add_edition(m['value'], spelling, order)}"
        else:
            new = re.sub(r"at:\s*\[[^\]]*\]", lambda a: "at: " + add_edition(a.group(0)[3:], spelling, order), line)
        if new != line:
            edits.append((line, new))
            lines[i] = new
    if edits:
        path.write_text("\n".join(lines), encoding="utf-8", newline="")
    return edits


def revert_case(path: pathlib.Path, edits: list[tuple[str, str]]) -> None:
    raw = path.read_text(encoding="utf-8")
    for old, new in reversed(edits):
        if new not in raw:
            raise SystemExit(f"{TOOL}: {path}: the widened line {new!r} is no longer there to revert")
        raw = raw.replace(new, old, 1)
    path.write_text(raw, encoding="utf-8", newline="")


def widen_apply(plan: dict[str, Any], w: Witnesses, ed: Edition, compiler: pathlib.Path, scratch: pathlib.Path) -> dict[str, Any]:
    """Widen every `widen` case of `plan` to `ed`, then keep only those the generator accepts and the harness passes at
    `ed` with `compiler`: the others are reverted, each with why. Writes nothing but case headers in the suite."""
    order = inventory_schema.load_schema().editions
    widened: dict[str, list[tuple[str, str]]] = {}
    for k, c in sorted(plan["cases"].items()):
        if c["status"] == "widen":
            info = w.cases[k]
            edits = widen_case(info.path, ed.spelling, order)
            if info.flag_at and info.control and (ctl := w.cases.get(f"{k.rsplit('/', 1)[0]}/{info.control}")):
                edits_ctl = widen_case(ctl.path, ed.spelling, order) if ed.spelling not in ctl.accept else []
                widened[ctl.key] = edits_ctl
            widened[k] = edits
    reverted: dict[str, str] = {}
    gen = scratch / "suites"
    run = subprocess.run([sys.executable, str(SUITE / "harness" / "generate.py"), "--out", str(gen)],
                         capture_output=True, text=True, encoding="utf-8", cwd=REPO)
    refused = {m.group(1) for m in re.finditer(r"REFUSED\s+((?:wiseowl/)?cases/[^:]+?)\.(?:cob|cbl):", run.stdout)}
    for k in sorted(refused & set(widened)):
        revert_case(w.cases[k].path, widened.pop(k))
        reverted[k] = "the generator refuses it at the edition"
    if refused - set(widened) - set(reverted):
        raise SystemExit(f"{TOOL}: the generator refuses cases this run did not widen: {sorted(refused - set(reverted))[:5]}")
    if run.returncode != 0 and not refused:
        raise SystemExit(f"{TOOL}: generate.py failed:\n{run.stdout[-2000:]}{run.stderr[-2000:]}")
    if reverted:
        run = subprocess.run([sys.executable, str(SUITE / "harness" / "generate.py"), "--out", str(gen)],
                             capture_output=True, text=True, encoding="utf-8", cwd=REPO)
        if run.returncode != 0:
            raise SystemExit(f"{TOOL}: generate.py still refuses after the reverts:\n{run.stdout[-2000:]}")
    manifest = json.loads((gen / "wiseowl" / "manifest.json").read_text(encoding="utf-8"))
    ids = {pathlib.PurePath(k).name for k in widened}
    entries = [t for t in manifest["editions"][ed.spelling] if t["id"] in ids]
    suites = scratch / "run-suites" / ed.spelling
    suites.mkdir(parents=True, exist_ok=True)
    (suites / "manifest.json").write_text(json.dumps({"edition": ed.spelling, "generator": manifest["generator"],
                                                      "tests": entries}, indent=1), encoding="utf-8")
    results = scratch / "results"
    subprocess.run([sys.executable, str(SUITE / "harness" / "run.py"), "--compiler", "wiseowl", "--set",
                    f"cobol={compiler}", "--suites", str(scratch / "run-suites"), "--editions", ed.spelling,
                    "--out", str(results)], cwd=REPO, check=False)
    verdicts = {t["id"]: t["verdict"] for t in json.loads((results / "results.json").read_text(encoding="utf-8"))["tests"]
                if t.get("edition") == ed.spelling}
    for k in sorted(widened):
        v = verdicts.get(pathlib.PurePath(k).name)
        if v != "PASS":
            revert_case(w.cases[k].path, widened.pop(k))
            reverted[k] = f"the harness's verdict at {ed.spelling} is {v}"
    return {"widened": sorted(widened), "reverted": reverted}


# -- the audit ----------------------------------------------------------------------------------------------------------

def stored_errors(ed: Edition, rows: Iterable[dict[str, Any]], slices: list[Slice], w: Witnesses | None) -> list[str]:
    """Why a STORED change record whose source names a read record of `ed` does not stand: the record is missing, its read
    is not a SAME its refuter UPHELD, it carries into other rules than the read, or (with `w`) a rule it carries into has
    no witness at the edition."""
    by_slice = {s.name: s for s in slices}
    prefix = f"lineage-reads-{ed.year}/"
    errs = []
    for r in rows:
        nxt = r.get("next") or {}
        src = nxt.get("source") or ""
        if not nxt.get("kind") or not src.startswith(prefix):
            continue
        rid, name = r["rule-id"], src[len(prefix):].removesuffix(".json")
        s = by_slice.get(name)
        read = s.reads.get(rid) if s else None
        f = s.refutes.get(rid) if s else None
        if read is None:
            errs.append(f"{rid}: its source {src} holds no read of the rule")
        elif nxt["kind"] != KEPT:
            errs.append(f"{rid}: a {nxt['kind']} record from a read: a read writes KEPT records only")
        elif read.verdict != "SAME" or f is None or f.verdict != "UPHELD":
            errs.append(f"{rid}: its read is {read.verdict} and its refuter {f.verdict if f else 'absent'}: a KEPT record "
                        "needs a SAME the refuter UPHELD")
        elif list(read.to) != list(nxt.get("to") or []):
            errs.append(f"{rid}: it carries into {nxt.get('to')}, its read into {list(read.to)}")
        elif w is not None and (gone := w.missing(ed, nxt["to"])):
            errs.append(f"{rid}: no conformance-suite witness of {gone} runs at {ed.spelling} any more: the evidence at "
                        "the row's own edition is gone (widen the band again or give the rule a witness of its own)")
    return errs


def read_years(repo: pathlib.Path = REPO) -> list[int]:
    return [y for y in inventory_schema.HELD if reads_dir(y, repo).is_dir()]


def check(years: list[int]) -> int:
    red = []
    for y in years:
        ed = edition(y)
        slices = load(y)
        rows = inventory_view.inventory(y)
        from_reads = [r for r in rows if ((r.get("next") or {}).get("source") or "").startswith(f"lineage-reads-{y}/")]
        errs = stored_errors(ed, rows, slices, Witnesses.of(ed.next_held) if from_reads else None)
        print(f"{y}: {len(slices)} read record(s), {sum(len(s.reads) for s in slices)} rule(s) read; {len(from_reads)} "
              f"stored record(s) from reads")
        for e in errs:
            print(f"  ⛔ {e}")
        if errs:
            red.append(y)
    print(f"=== LINEAGE READS: {'GREEN' if not red else f'RED ({red})'} ===")
    return 1 if red else 0


# -- the command line -----------------------------------------------------------------------------------------------------

def own_edition(year: int, out: pathlib.Path) -> int:
    """The stored KEPT records of `year` measured against the own-edition evidence rule (a measurement: it changes
    nothing): how many carry into rules of which at least one, and each, has a suite witness that runs at the edition."""
    ed = edition(year)
    w = Witnesses.of(ed.next_held)
    rows = [r for r in inventory_view.inventory(year) if (r.get("next") or {}).get("kind") == KEPT]
    some = [r["rule-id"] for r in rows if len(w.missing(ed, r["next"]["to"])) < len(r["next"]["to"])]
    every = [r["rule-id"] for r in rows if not w.missing(ed, r["next"]["to"])]
    none = sorted(set(r["rule-id"] for r in rows) - set(some))
    doc = {"edition": year, "kept": len(rows), "some-target-witnessed": len(some), "every-target-witnessed": len(every),
           "none": len(none), "rules-with-none": none,
           "rules-some-not-every": sorted(set(some) - set(every))}
    out.write_text(json.dumps(doc, indent=1) + "\n", encoding="utf-8")
    print(f"{year}: {len(rows)} stored KEPT record(s); a witness at {ed.spelling} for at least one target: {len(some)}; for "
          f"every target: {len(every)}; for none: {len(none)} -> {out}")
    return 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The read records of an older edition's rules (kb/Work PB5597).")
    ap.add_argument("--check", type=int, nargs="?", const=0, metavar="YEAR",
                    help="every read record interprets and every stored record from a read stands; with no YEAR every "
                         "held edition with read records: the gate's LINEAGE READS audit")
    sub = ap.add_subparsers(dest="cmd")
    wd = sub.add_parser("widen", help="plan (and with --apply, make) the band widening of the kept rows' witnesses")
    wd.add_argument("--edition", type=int, required=True)
    wd.add_argument("--state", required=True, help="the audit run import-reads wrote (its read records)")
    wd.add_argument("--out", required=True, help="the plan (JSON) to write")
    wd.add_argument("--apply", action="store_true", help="edit the suite's case headers, keeping what passes at the edition")
    wd.add_argument("--compiler", help="with --apply: the cobol executable the harness runs")
    wd.add_argument("--scratch", help="with --apply: a scratch directory for the generated suites and the run")
    oe = sub.add_parser("own-edition", help="measure the stored KEPT records' witnesses at their own edition")
    oe.add_argument("--edition", type=int, required=True)
    oe.add_argument("--out", required=True)
    a = ap.parse_args(argv)
    if a.cmd == "own-edition":
        return own_edition(a.edition, pathlib.Path(a.out))
    if a.cmd == "widen":
        ed = edition(a.edition)
        state = json.loads((pathlib.Path(a.state) / "reads.json").read_text(encoding="utf-8"))
        slices = load(a.edition, pathlib.Path(state["record"]))
        w = Witnesses.of(ed.next_held)
        plan = widen_plan(ed, decide(ed, slices, lineage_screen.committed(a.edition), w), w)
        if a.apply:
            if not a.compiler or not a.scratch:
                raise SystemExit(f"{TOOL}: widen --apply needs --compiler and --scratch")
            plan["applied"] = widen_apply(plan, w, ed, pathlib.Path(a.compiler), pathlib.Path(a.scratch))
        pathlib.Path(a.out).write_text(json.dumps(plan, indent=1) + "\n", encoding="utf-8")
        print(f"widen {a.edition}{'' if a.apply else ' (dry run: nothing edited)'}: rows {plan['counts']['rows']}; cases "
              f"{plan['counts']['cases']} -> {a.out}")
        if a.apply:
            print(f"  widened {len(plan['applied']['widened'])}, reverted {len(plan['applied']['reverted'])}")
        return 0
    if a.check is None:
        ap.print_help()
        return 2
    return check([a.check] if a.check else read_years())


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
