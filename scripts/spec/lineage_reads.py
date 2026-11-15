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
#: ... and why a read's targets cannot be a KEPT record's (kb/Work PB5597, branch review R1-1, R1-2).
NO_LICENCE, NOT_THE_PAIRING = "no 2002 licence stated", "targets are not the pairing's"
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
    #: The read's `licence-2002`: the citation of the older text that permits declining the facility, or None (absent,
    #: or the brief's `none`).
    licence: str | None = None


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
    if not isinstance(to, list) or not all(isinstance(t, str) and t for t in to):
        raise _fail(slice_name, rid, f"`to` must be a list of rule ids (empty for a rule with no pointer), not {to!r}")
    verdict = case.get("verdict")
    if verdict not in READ_VERDICTS:
        raise _fail(slice_name, rid, f"the reader's verdict {verdict!r} is not one of {READ_VERDICTS}")
    _citations(case, slice_name, rid)
    licence = _text(case, "licence-2002", slice_name, rid, required=False)
    return Read(rid, slice_name, tuple(to), verdict, _text(case, "reason", slice_name, rid),
                _text(case, "probe", slice_name, rid, required=False), _judgment(case, slice_name, rid),
                _flag_answers(case, slice_name, rid),
                None if licence is None or not licence.strip() or licence.strip().lower().startswith("none") else licence)


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
        if bad := digest_errors(path.stem, doc):
            errs += bad
            continue
        for rid in s.reads:
            if rid in seen:
                errs.append(f"{s.name}: {rid}: read again (first read in {seen[rid]})")
            seen[rid] = s.name
        out.append(s)
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
    return [f"{name}: its {k} no longer matches the digest it was imported with" for k in ("read", "refute", "asked")
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
    if orphans := sorted(p.name for p in reads.glob("*-refute.json") if not (reads / p.name.replace("-refute", "-read")).exists()):
        raise SystemExit(f"{TOOL}: refuter records with no reader's record: {orphans}")
    docs, errs = {}, []
    seen: dict[str, str] = {}
    if out.is_dir():   # the records already there and not re-imported now: their rules are read too
        for path in sorted(out.glob("*.json")):
            if path.stem not in names:
                for c in (json.loads(path.read_text(encoding="utf-8")).get("read") or {}).get("cases", []):
                    seen.setdefault(c.get("rule-id"), path.stem)
    for name in names:
        if not SLICE.match(name):
            errs.append(f"{name}: a slice's name pairs <name>-read.json with <name>-refute.json; this one is no file label")
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
               "refute": loaded.get("refute"), "imported": {k: digest(v) for k, v in sorted(loaded.items())}}
        try:
            s = normal_slice(name, doc)
        except RecordError as e:
            errs.append(str(e))
            continue
        if rules is not None and (unknown := sorted(rid for rid in s.reads if rid not in rules)):
            errs += [f"{name}: {rid}: no rule of the {year} catalog" for rid in unknown]
            continue
        if twice := sorted(rid for rid in s.reads if rid in seen):   # refused BEFORE any write (review R1-5)
            errs += [f"{name}: {rid}: read again (first read in {seen[rid]})" for rid in twice]
            continue
        seen.update(dict.fromkeys(s.reads, name))
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


@dataclasses.dataclass(frozen=True)
class EvidenceTable:
    """What a witness of each kind shows (`EVIDENCE_TABLE`, data): a DIAGNOSIS (a refusal test's `refuse-at`, a flagged
    case's `at`) witnesses a rule only when what the rule asks of a compiler IS a diagnosis (its catalog `kind` is one
    of `diagnosed`: a syntax rule or a general format, which a program can violate) or the next-edition rule's facility
    is DECLINED (its verdict is `declined`, and then the read must state the older text's licence to decline it). Every
    other rule needs a case that RUNS and passes at the edition."""
    diagnosed: frozenset[str]
    declined: str

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
        return cls(frozenset(diagnosed), doc["declined-verdict"])


@dataclasses.dataclass(frozen=True)
class Lineage:
    """What a read's rule is in its own edition and its pairing: the catalog's `kind` of each rule, the candidate
    pairing's targets of each paired rule (docs/rearchitecture/spec-lineage-candidates-<year>.json), and the evidence
    table. A KEPT record carries exactly the pairing's targets (review R1-2)."""
    kinds: dict[str, str]
    pairs: dict[str, tuple[str, ...]]
    table: EvidenceTable

    @classmethod
    def of(cls, ed: Edition) -> "Lineage":
        import lineage  # noqa: PLC0415  (the candidate pairing's file)
        kinds = {r["id"]: r["kind"] for r in ed.rules()}
        pairs = {p["rule-id"]: tuple(p["to"]) for p in
                 json.loads(lineage.candidates_path(ed.year).read_text(encoding="utf-8"))["pairs"]}
        return cls(kinds, pairs, EvidenceTable.load(kinds=kinds.values()))


class Witnesses:
    """The next edition's rules' conformance-suite witnesses and where each runs. A case runs at an edition when its header
    names it (the generator places it in that edition's manifest only then, and refuses it there when a rule it cites does
    not exist at the edition: `generate.check_cases`); the gate runs every case of every edition's manifest, so a case
    that runs at the edition on a green tree passes there."""

    def __init__(self, cases: dict[str, CaseInfo], refs: dict[str, list[str]], bands: dict[str, list[str]],
                 verdicts: dict[str, str] | None = None):
        self.cases, self.refs, self.bands, self.verdicts = cases, refs, bands, verdicts or {}

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
            refusal = inputs.diagnostics.get(c.test_id)
            refuse_at = tuple(str(e) for e in refusal["refuse-at"]) if refusal else ()
            cases[key] = CaseInfo(key, g.SUITE / c.path, tuple(dict.fromkeys((*c.accept, *at, *refuse_at))),
                                  tuple(c.accept), at, tuple(c.rules), c.control, refuse_at,
                                  g.SUITE / g.sidecar_path(g.DIAGNOSTICS_DIR, c.test_id) if refusal else None,
                                  refusal["expect"] if refusal else "", c.source_format)
        schema = inventory_schema.load_schema()
        rows = inventory_view.inventory(newer, schema)
        refs = {r["rule-id"]: schema.split(r.get("test-ref", "") or "", schema.test_ref_sep) for r in rows}
        bands = {r["rule-id"]: schema.split(r.get("editions", "") or "", schema.editions_sep) for r in rows}
        return cls(cases, refs, bands, {r["rule-id"]: r.get("verdict") or "" for r in rows})

    def suite_cases(self, rule: str) -> list[str]:
        """The keys of the conformance-suite cases the rule's test-ref names (`conformance:<key>`), in its order."""
        return [ref.partition(":")[2].strip() for ref in self.refs.get(rule, []) if ref.partition(":")[0] == "conformance"]

    def at(self, rule: str, spelling: str) -> list[str]:
        """The rule's suite witnesses that run at the edition spelled `spelling`, of any kind."""
        return [k for k in self.suite_cases(rule) if k in self.cases and spelling in self.cases[k].runs]

    def accepted_at(self, rule: str, spelling: str) -> list[str]:
        """The rule's suite witnesses ACCEPTED at the edition: a program that runs there and whose output is compared."""
        return [k for k in self.suite_cases(rule) if k in self.cases and spelling in self.cases[k].accept]

    def evidence(self, ed: Edition, rule_kind: str, to: Iterable[str], licence: str | None,
                 table: EvidenceTable) -> list[tuple[str, str]]:
        """Why each rule of `to` has no witness at `ed` of the kind the older rule (catalog `rule_kind`) needs (review
        R1-1): a case accepted there always is; a diagnosis there only for a kind the table's `diagnosed` names, or for
        a declined facility with the read's `licence` stated. Empty when the evidence is at the row's own edition."""
        sp, out = ed.spelling, []
        for t in to:
            if self.accepted_at(t, sp):
                continue
            diagnoses = self.at(t, sp)
            if diagnoses and rule_kind in table.diagnosed:
                continue
            if diagnoses and self.verdicts.get(t) == table.declined:
                if licence is None:
                    out.append((NO_LICENCE, f"{t} is {table.declined} and only a diagnosis at {sp} shows the decline: "
                                            f"the read states no {ed.year} licence (licence-{ed.year}) to decline it"))
                continue
            out.append((NO_OWN_WITNESS, f"{t}: no conformance-suite case runs and passes at {sp}" + (
                f" (only a diagnosis, which shows a {rule_kind} rule nothing)" if diagnoses else "")))
        return out

    def missing(self, ed: Edition, to: Iterable[str], rule_kind: str, table: EvidenceTable,
                licence: str | None = None) -> list[str]:
        """The rules of `to` lacking own-edition evidence (`evidence`), by id."""
        return [d.split(" ", 1)[0].rstrip(":") for _r, d in self.evidence(ed, rule_kind, to, licence, table)]


def kept_problems(ed: Edition, rid: str, to: Iterable[str], licence: str | None, lin: Lineage,
                  w: Witnesses | None) -> list[tuple[str, str]]:
    """THE ONE CHECK of a KEPT record's targets (the writer, the import's decisions and the audit share it): they are
    exactly the candidate pairing's for the rule (review R1-2), and, for an edition whose records close on own-edition
    evidence and with `w`, each has a witness at the edition of the kind the rule needs (review R1-1)."""
    out = []
    want = lin.pairs.get(rid)
    if want is None:
        out.append((NOT_THE_PAIRING, f"{rid} has no candidate pair: a KEPT record carries only a pairing's targets"))
    elif sorted(want) != sorted(to):
        out.append((NOT_THE_PAIRING, f"the read carries into {sorted(to)}, the pairing into {sorted(want)}"))
    if ed.own_evidence and w is not None:
        out += w.evidence(ed, lin.kinds.get(rid, ""), to, licence, lin.table)
    return out


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
           witnesses: Witnesses | None, lin: Lineage) -> list[Decision]:
    """Each read rule's `Decision` (module docstring). The evidence is checked for an edition whose records close on
    own-edition evidence (`Edition.own_evidence`) when `witnesses` is given; None leaves it to the writer (the audit
    run's import). `screen` is the edition's committed lineage screen; `lin` the catalog's kinds and the pairing. A SAME
    of a rule with no candidate pair is the `OWN_VERDICT_FROM_POINTER` work item, never a KEPT record."""
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
            elif rid not in lin.pairs:
                out.append(Decision(**base, work=OWN_VERDICT_FROM_POINTER, probe=r.probe,
                                    why=f"a SAME for a rule with no candidate pair (its `to` {list(r.to)} is a pointer); "
                                        f"refuter: {f.verdict if f else 'none'}: record its own verdict, citing the "
                                        "pointer's witness once it runs at the edition"))
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
                refused += kept_problems(ed, rid, r.to, r.licence, lin, witnesses)
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
#: What a widening that does not hold leaves: the row needs a witness of its own at the edition.
NEEDS_OWN = "needs an own witness"


def case_status(c: CaseInfo | None, sp: str, bands: dict[str, list[str]]) -> str:
    """`RUNS` at the edition spelled `sp`; `WIDEN` (a case accepted or flagged at other editions) or `WIDEN_REFUSAL` (a
    refusal test refused at other editions) when every rule it cites exists at `sp` (the generator's own band check,
    `generate.check_cases`), else `BAND`; `NO_EDITION` for a case placed at no edition; `MISSING` for a test-ref naming
    no case."""
    if c is None:
        return MISSING
    if sp in c.runs:
        return RUNS
    if not c.runs:
        return NO_EDITION
    if any(sp not in bands.get(r, [sp]) for r in c.rules):
        return BAND
    return WIDEN if (c.accept or c.flag_at) else WIDEN_REFUSAL


def widen_plan(ed: Edition, decisions: list[Decision], w: Witnesses, lin: Lineage) -> dict[str, Any]:
    """For each read rule refused ONLY for its evidence (a SAME, upheld, every flag answered, the pairing's targets), each
    target that lacks a witness of the kind the rule needs: its suite cases and what each can do at `ed`
    (`case_status`). A widening helps a target when it would give it such a witness: an ACCEPTED case always; a
    diagnosis (a flag or a refusal) only for a kind the evidence table names, or a declined facility (review R1-1). A row
    is `widenable` (every lacking target has a case that helps) or `needs-own-witness`."""
    sp = ed.spelling
    cases: dict[str, dict[str, Any]] = {}
    rows = []
    for d in decisions:
        if d.work or not d.refused or any(r != NO_OWN_WITNESS for r, _ in d.refused):
            continue
        kind = lin.kinds.get(d.rule_id, "")
        lacking = [t for t in d.to if w.evidence(ed, kind, [t], None, lin.table)]
        per_rule, helps = {}, {}
        for t in lacking:
            ks = w.suite_cases(t)
            for k in ks:
                c = w.cases.get(k)
                cases.setdefault(k, {"status": case_status(c, sp, w.bands), "rules": list(c.rules) if c else []})
            diagnosis_counts = kind in lin.table.diagnosed or w.verdicts.get(t) == lin.table.declined
            helps[t] = any(cases[k]["status"] == WIDEN and (w.cases[k].accept or diagnosis_counts)
                           or cases[k]["status"] == WIDEN_REFUSAL and diagnosis_counts for k in ks)
            per_rule[t] = ks
        rows.append({"rule-id": d.rule_id, "to": list(d.to), "kind": kind, "cases": per_rule,
                     "state": "widenable" if lacking and all(helps.values()) else "needs-own-witness"})
    return {"edition": ed.year, "rows": rows, "cases": cases,
            "counts": {"rows": dict(sorted(collections.Counter(r["state"] for r in rows).items())),
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
#: crash) means the results are not this run's, and every case it was to judge is reverted (review R1-3).
RUN_RAN = (0, 1)


def widen_apply(plan: dict[str, Any], w: Witnesses, ed: Edition, compiler: pathlib.Path, scratch: pathlib.Path,
                run: Any = subprocess.run) -> dict[str, Any]:
    """Widen every `WIDEN` and `WIDEN_REFUSAL` case of `plan` to `ed` (a flagged case's control with it), and KEEP only a
    case run at the edition IN THIS INVOCATION that passed there: the generator must accept the tree; an accepted or
    flagged case must PASS in the results the harness (run.py) writes now, into a directory this call makes fresh, from
    a run that exited 0 or 1 (`RUN_RAN`); a refusal test must be refused, with its expected diagnostic, by its
    compilation now. Every other case, and EVERY case when anything fails or raises on the way, is restored and listed
    as needing an own witness: the tree ends fully consistent (the kept widenings all proven) or untouched, never in
    between (review R1-3). `run` is subprocess.run, a parameter so the self-test plants the harness."""
    order = inventory_schema.load_schema().editions
    sp = ed.spelling
    if scratch.exists() and any(scratch.iterdir()):
        raise SystemExit(f"{TOOL}: widen --apply needs an empty --scratch, so no result it reads is an earlier run's")
    widened: dict[str, list[tuple[pathlib.Path, list[tuple[str, str]]]]] = {}
    reverted: dict[str, str] = {}

    def undo(k: str, why: str) -> None:
        for path, edits in reversed(widened.pop(k)):
            revert(path, edits)
        reverted[k] = f"{NEEDS_OWN}: {why}"

    try:
        for k, c in sorted(plan["cases"].items()):
            info = w.cases.get(k)
            if c["status"] == WIDEN:
                widened[k] = [(info.path, widen_case(info.path, sp, order))]
                if info.flag_at and info.control and (ctl := w.cases.get(f"{k.rsplit('/', 1)[0]}/{info.control}")) \
                        and sp not in ctl.accept:
                    widened[k].append((ctl.path, widen_case(ctl.path, sp, order)))
            elif c["status"] == WIDEN_REFUSAL:
                widened[k] = [(info.refusal, widen_refusal(info.refusal, sp, order))]
        gen = scratch / "suites"

        def generate() -> Any:
            return run([sys.executable, str(SUITE / "harness" / "generate.py"), "--out", str(gen)],
                       capture_output=True, text=True, encoding="utf-8", cwd=REPO)

        g = generate()
        refused = {m.group(1) for m in re.finditer(r"REFUSED\s+((?:wiseowl/)?cases/[^:]+?)\.(?:cob|cbl):", g.stdout or "")}
        if stray := sorted(refused - set(widened)):
            raise SystemExit(f"{TOOL}: the generator refuses cases this run did not widen: {stray[:5]}")
        for k in sorted(refused):
            undo(k, f"the generator refuses it at {sp}")
        if refused:
            g = generate()
        if g.returncode != 0:
            raise SystemExit(f"{TOOL}: generate.py refuses the tree:\n{(g.stdout or '')[-2000:]}")
        accepted = [k for k in widened if plan["cases"][k]["status"] == WIDEN]
        if accepted:
            manifest = json.loads((gen / "wiseowl" / "manifest.json").read_text(encoding="utf-8"))
            ids = {pathlib.PurePath(k).name for k in accepted}
            suites = scratch / "run-suites" / sp
            suites.mkdir(parents=True)
            (suites / "manifest.json").write_text(json.dumps(
                {"edition": sp, "generator": manifest["generator"],
                 "tests": [t for t in manifest["editions"][sp] if t["id"] in ids]}, indent=1),
                encoding="utf-8", newline="\n")
            results = scratch / "results"          # made by this run only: the scratch was empty
            r = run([sys.executable, str(SUITE / "harness" / "run.py"), "--compiler", "wiseowl", "--set",
                     f"cobol={compiler}", "--suites", str(scratch / "run-suites"), "--editions", sp, "--out", str(results)],
                    cwd=REPO, check=False)
            file = results / "results.json"
            verdicts: dict[str, str] = {}
            if r.returncode in RUN_RAN and file.is_file():
                verdicts = {t["id"]: t["verdict"] for t in json.loads(file.read_text(encoding="utf-8"))["tests"]
                            if t.get("edition") == sp}
            for k in accepted:
                if (v := verdicts.get(pathlib.PurePath(k).name)) != "PASS":
                    undo(k, f"the harness's verdict at {sp} is {v}" if verdicts else
                         f"the harness exited {r.returncode} and wrote no results of this run")
        for k in [k for k in widened if plan["cases"][k]["status"] == WIDEN_REFUSAL]:
            if why := refused_at(w.cases[k], sp, compiler, scratch, run):
                undo(k, why)
    except BaseException:
        for k in list(widened):
            undo(k, "the widening stopped before it was proven")
        raise
    return {"widened": sorted(widened), "reverted": reverted}


def refused_at(c: CaseInfo, spelling: str, compiler: pathlib.Path, scratch: pathlib.Path, run: Any = subprocess.run) -> str | None:
    """Why the refusal test `c` does not refuse at the edition (None when it does): compiled at `--std` with `compiler`,
    the compilation must fail and report the entry's code or text, as the gate's refusal row requires."""
    out = scratch / "refusals" / c.key.replace("/", "_")
    out.mkdir(parents=True, exist_ok=True)
    r = run([str(compiler), str(c.path), "--std", spelling, "--source-format", c.source_format, "-o", str(out / "p.dll")],
            capture_output=True, text=True, encoding="utf-8", errors="replace", cwd=c.path.parent, timeout=600)
    if r.returncode == 0:
        return f"accepted at {spelling}"
    return None if c.expect and c.expect in (r.stdout or "") + (r.stderr or "") else f"refused at {spelling} without {c.expect!r}"


# -- the audit ----------------------------------------------------------------------------------------------------------

def stored_errors(ed: Edition, rows: Iterable[dict[str, Any]], slices: list[Slice], w: Witnesses | None,
                  lin: Lineage) -> list[str]:
    """Why a STORED change record of `ed` does not stand. One whose source names a read record is that record's decision:
    the record (whose digests `load` verified against its content) holds a read of the rule, the read is a SAME its
    refuter UPHELD, and the record is KEPT. For an edition whose records close on own-edition evidence, every stored KEPT
    record, whatever its source, also passes `kept_problems`: the pairing's targets, each witnessed at the edition (with
    `w`) by a case of the kind the rule needs."""
    by_slice = {s.name: s for s in slices}
    prefix = f"lineage-reads-{ed.year}/"
    errs = []
    for r in rows:
        nxt = r.get("next") or {}
        src = nxt.get("source") or ""
        if not nxt.get("kind"):
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
        if nxt["kind"] == KEPT and ed.own_evidence:
            errs += [f"{rid}: {why}: {d}" for why, d in kept_problems(ed, rid, nxt.get("to") or [], licence, lin, w)]
    return errs


def read_years(repo: pathlib.Path = REPO) -> list[int]:
    """The held editions this audit holds: those with read records, and those whose records close on own-edition evidence."""
    return [e.year for e in inventory_schema.load_schema().all_editions
            if e.held and not e.newest and (reads_dir(e.year, repo).is_dir() or e.own_evidence)]


def check(years: list[int]) -> int:
    red = []
    for y in years:
        ed = edition(y)
        slices = load(y)
        rows = inventory_view.inventory(y)
        from_reads = [r for r in rows if ((r.get("next") or {}).get("source") or "").startswith(f"lineage-reads-{y}/")]
        kept = [r for r in rows if (r.get("next") or {}).get("kind") == KEPT]
        w = Witnesses.of(ed.next_held) if ed.own_evidence and kept else None
        errs = stored_errors(ed, rows, slices, w, Lineage.of(ed))
        print(f"{y}: {len(slices)} read record(s), {sum(len(s.reads) for s in slices)} rule(s) read, each record's "
              f"digests verified; {len(from_reads)} stored record(s) from reads; {len(kept)} KEPT record(s)"
              + (", each held to the pairing and a witness at its own edition" if ed.own_evidence else ""))
        for e in errs:
            print(f"  ⛔ {e}")
        if errs:
            red.append(y)
    print(f"=== LINEAGE READS: {'GREEN' if not red else f'RED ({red})'} ===")
    return 1 if red else 0


# -- the command line -----------------------------------------------------------------------------------------------------

def own_edition(year: int, out: pathlib.Path) -> int:
    """The stored KEPT records of `year` measured against the own-edition evidence rule, the kind the rule needs
    included (a measurement: it changes nothing): how many carry into rules of which at least one, and each, has such a
    witness at the edition."""
    ed = edition(year)
    w = Witnesses.of(ed.next_held)
    lin = Lineage(kinds={r["id"]: r["kind"] for r in ed.rules()}, pairs={}, table=EvidenceTable.load())
    rows = [r for r in inventory_view.inventory(year) if (r.get("next") or {}).get("kind") == KEPT]

    def lacking(r: dict[str, Any]) -> list[str]:
        return w.missing(ed, r["next"]["to"], lin.kinds.get(r["rule-id"], ""), lin.table)

    some = [r["rule-id"] for r in rows if len(lacking(r)) < len(r["next"]["to"])]
    every = [r["rule-id"] for r in rows if not lacking(r)]
    none = sorted(set(r["rule-id"] for r in rows) - set(some))
    doc = {"edition": year, "kept": len(rows), "some-target-witnessed": len(some), "every-target-witnessed": len(every),
           "none": len(none), "rules-with-none": none, "rules-some-not-every": sorted(set(some) - set(every))}
    out.write_text(json.dumps(doc, indent=1) + "\n", encoding="utf-8", newline="\n")
    print(f"{year}: {len(rows)} stored KEPT record(s); a witness of the kind the rule needs at {ed.spelling} for at least "
          f"one target: {len(some)}; for every target: {len(every)}; for none: {len(none)} -> {out}")
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
    wd.add_argument("--scratch", help="with --apply: an EMPTY scratch directory for the generated suites and the run")
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
        w, lin = Witnesses.of(ed.next_held), Lineage.of(ed)
        plan = widen_plan(ed, decide(ed, slices, lineage_screen.committed(a.edition), w, lin), w, lin)
        if a.apply:
            if not a.compiler or not a.scratch:
                raise SystemExit(f"{TOOL}: widen --apply needs --compiler and --scratch")
            plan["applied"] = widen_apply(plan, w, ed, pathlib.Path(a.compiler), pathlib.Path(a.scratch))
        pathlib.Path(a.out).write_text(json.dumps(plan, indent=1) + "\n", encoding="utf-8", newline="\n")
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
