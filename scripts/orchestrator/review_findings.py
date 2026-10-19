#!/usr/bin/env python3
"""The branch review's record (kb/Work PB3027; DESIGN-test-build-ci.md section 3.17.2 step 2): one JSON line per
decision in `<scratch>/review-w<wave>-<letter>.jsonl`, the verdict computed from it, and the stamp in the report.

    python scripts/orchestrator/review_findings.py start   <file> --round 1 --branch B --head SHA --reviewer LABEL
    python scripts/orchestrator/review_findings.py finding <file> --round 1 '<one JSON object>'
    python scripts/orchestrator/review_findings.py verdict <file> --round 1 [--blocking critical,high,medium]
                                                                                    # CLEAN | FINDINGS n, from the file
    python scripts/orchestrator/review_findings.py stamp   <report> --findings <file>
    python scripts/orchestrator/review_findings.py show    <file> [--round N]     # the findings, for the finisher
    python scripts/orchestrator/review_findings.py last-verdict <file>             # the newest verdict: does it hold?
    python scripts/orchestrator/review_findings.py repair  <file>                 # mend a torn file (operator only)
    python scripts/orchestrator/review_findings.py summary <file or directory>... # the measurement (PB3027 item 5)
    python scripts/orchestrator/review_findings.py --self-test

WHY A RECORD AND A TOOL. Until PB3027 the review of finished work ran inside the lander, between its gates: review
shards were 23 % of the enqueue units' subagent tokens on 2026-10-10 and sat on the landing's critical path, and every
finding fixed in the train re-gated every cluster (kb/Work PB3015, its evidence table 6). The review now runs on the
BRANCH when its implementer returns DONE: ONE read-only `cobol-reviewer` (the `review` skill's branch form), inside the
rolling wave or dispatched by hand for an attended branch. The reviewer cannot write in a git tree (its hook), and a
kill can come at any moment, so every decision is a line on disk, appended through this tool the moment it is made:

  start    {round, branch, head, reviewer}: the review of one head began (its minutes are measured from here).
  finding  {round, file, line, category, severity, summary, failure_scenario}: ONE confirmed finding (the reviewer
           verified it, running the scenario where that is cheap; a candidate it could not confirm is not recorded).
           Its `severity` is one of SEVERITIES. The tool gives it an id `R<round>-<k>`, by which the finisher answers it.
  verdict  computed here, never claimed: CLEAN when the round recorded no finding of a BLOCKING severity, else
           FINDINGS n (n = the blocking ones). Every finding stays recorded; the ones below the threshold are the
           round's LEADS, which the stamp writes into the report's `## Review findings` section and the reviewer
           returns, so the enqueue unit's clerk files them as notes (units/enqueue.md step 5).

THE SEVERITY THRESHOLD (kb/Work PB3381, owner 2026-10-18: "We should not be holding finished work this long,
continually postponing it"). `verdict --blocking` names the severities that block (default BLOCKING_DEFAULT:
critical,high,medium). The rolling wave passes the default for every round a review finisher can still answer and
BLOCKING_AT_CAP (critical,high) for the round after its last finisher: a medium finding blocks while a finisher can
answer it and becomes a lead at the cap; a low or info finding never blocks; a critical or high one always does, and at
the cap its FINDINGS stamp stays for the next wave's finisher. Wave 1118 measured why: PB3331's third whole-branch round
turned one LOW finding (a test theory's length class) into a DECISION row, and the branch never landed. A finding
recorded before SEVERITIES were required carries a free word (`minor`, `MAJOR`) and BLOCKS: an unknown severity is
never waved through.

`stamp` writes the verdict into the implementer's report header as the line plan_wave.REVIEW reads (CLEAN <reviewer>
<head>, or FINDINGS <n> <reviewer> <head> <file>), and reads it back through plan_wave.parse_review, so the writer and
the reader cannot drift. prune_worktrees.py lists a DONE branch WAITING TO LAND only under a CLEAN stamp at its tip;
plan_wave.py plans a finisher for a FINDINGS stamp.

UNANSWERED FINDINGS ARE READ FROM THIS FILE, NEVER FROM A REPORT (kb/Work PB3381 review R3-1). `last_verdict(file)` is
the ONE answer to "does this group's branch carry unanswered blocking findings": the newest verdict the file records
(its round, branch, head, verdict and `blocking`, the number of findings of a blocking severity under the thresholds
that round used), `unanswered` when it is FINDINGS with blocking > 0. The rolling wave reads it (the CLI's
`last-verdict`, through one mechanical agent) to tell a predecessor capped at the round cap from one whose review
finisher came back without answering, and prune_worktrees.py reads it (`unanswered()` over `findings_files()`) to hold
a branch that carries another note's unanswered findings. The report stamp is only their human-readable rendering: a
review finisher rewrites the group's one report for its own branch with no Review line, whatever it returns (BLOCKED,
SPLIT, STALLED, killed), so a hold read from the stamp let a successor reviewed CLEAN carry the predecessor's findings
to main. A finisher that answers them is reviewed again and records its round's verdict here; nothing else changes
what this file says.

EVERY CONTENT PROBLEM IS A TEAR, AND A TORN FILE HOLDS EVERY BRANCH IT NAMES (kb/Work PB3381 review R4-1, R5-1,
R6-1, R6-2). The survey finds the findings files BY NAME ONLY (findings_files: every `review-*.jsonl` where they live,
and every file a report stamp names); it never opens one to decide whether it is one, so nothing the reader would hold
is dropped before the reader sees it. The reader, `last_verdict`, is the ONE answer to both callers (the CLI's
`last-verdict`, which the wave reads, and the survey's `unanswered`), and it reads a file as TORN when the file cannot
be opened or read (gone, a directory in its place, a read denied), starts with a byte-order mark, holds bytes that are
not UTF-8, does not end with a newline, holds a line that is not a record (JSON that passes SCHEMA, the record schema
the writers share; a blank line is skipped), or holds no record: a writer killed mid-append, a record that lost its
final newline, a hand edit. A torn file is never read for a verdict, whatever its records say: the records before a
tear can read more permissive than the file, since a FINDINGS verdict that lost only its newline, with the next record
glued onto its line, leaves the CLEAN round before it as the last whole verdict (R5-1). Its reading is verdict TORN,
`unanswered` true, `torn` naming the file, line, byte offset and reason (`reason` and `line` apart, `line` None when
the file could not be read), and `branches` and `heads`, every branch and head its parseable lines name (or the branch
its name names). `last_verdict` never raises, and the CLI prints that reading and exits 0, so the wave reads UNANSWERED
from the reading, never from an exit code. The survey holds each branch the file names and each branch containing a
head it names (a file that names none, by the group its name or a stamp binds it to), the wave reads it UNANSWERED,
and the landing lane lands none of them. Every writer (`append`, so `start`, `finding` and `verdict`; `stamp` reads the
file first) refuses a torn file BEFORE writing, naming `repair <file>`, so nothing is ever glued onto a torn line, and
`append` refuses a record that fails SCHEMA, so no writer writes what the reader would tear. `repair` prints what it
mends: a byte-order mark is removed; a last line that is a record but lost its newline is terminated and kept;
otherwise the torn line and every line after it are removed, bytes that are not UTF-8 shown as bytes; a file left with
no record is deleted; a file it cannot read is refused; an intact file is left unchanged. Only the operator runs it, by
hand; no survey or wave does. `--self-test` proves the rule by cutting a findings file at every byte offset (THE
PROPERTY), by each discovery case at the file's first read (DISCOVERY) and by each schema mutation (SCHEMA).
"""
from __future__ import annotations

import argparse
import dataclasses
import datetime as dt
import json
import os
import pathlib
import re
import shutil
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import plan_wave  # noqa: E402  (the one reader of the stamp, and the one definition of a stamp line: PB3077)

#: The fields a finding's reviewer supplies (`finding`'s JSON object); the tool adds `kind`, `round`, `id` and `at`.
FINDING_FIELDS = ("file", "line", "category", "severity", "summary", "failure_scenario")
#: The severities a finding may carry, most severe first (the review skill's branch form, step 3).
SEVERITIES = ("critical", "high", "medium", "low", "info")
#: The blocking set of every round a review finisher can still answer, and of the round after the last one.
BLOCKING_DEFAULT = ("critical", "high", "medium")
BLOCKING_AT_CAP = ("critical", "high")
REVIEW_LINE = plan_wave.REVIEW_LINE   # the planner skips these lines when it reads the branch (kb/Work PB3077)


SHA = re.compile(r"[0-9a-f]{7,40}")


def _integer_from(low: int):
    return lambda v: isinstance(v, int) and not isinstance(v, bool) and v >= low


#: The tests a field's value passes, each with what it says when the value fails it.
POSITIVE = ("a positive integer", _integer_from(1))
COUNT = ("an integer >= 0", _integer_from(0))
TEXT = ("a non-empty string", lambda v: isinstance(v, str) and v != "")
COMMIT = ("a commit sha", lambda v: isinstance(v, str) and SHA.fullmatch(v) is not None)
TEXTS = ("a list of strings", lambda v: isinstance(v, list) and all(isinstance(x, str) for x in v))
VERDICTS = ("CLEAN", "FINDINGS")
#: THE RECORD SCHEMA (kb/Work PB3381 review R6-2): every field each kind of record carries and the test its value
#: passes, the ONE statement of what a record is. The writers share it (`append` refuses to write a record that fails
#: it) and so does the reader (`scan` reads a line that fails it as a TEAR). A field it does not name is allowed (a
#: finding's `fix` or `dimension`); a verdict's `blocking` and `leads` are OPTIONAL (verdicts recorded before kb/Work
#: PB3381's threshold carry neither) and tested when present. A finding's `severity` is any word: a file from before
#: SEVERITIES carries free words (`minor`, `MAJOR`), which block (`blocks`); `finding` writes only SEVERITIES.
SCHEMA = {
    "start": {"round": POSITIVE, "branch": TEXT, "head": COMMIT, "reviewer": TEXT, "at": TEXT},
    "finding": {"round": POSITIVE, "id": TEXT, **{k: POSITIVE if k == "line" else TEXT for k in FINDING_FIELDS},
                "at": TEXT},
    "verdict": {"round": POSITIVE, "branch": TEXT, "head": COMMIT, "reviewer": TEXT,
                "verdict": ("CLEAN or FINDINGS", lambda v: isinstance(v, str) and v in VERDICTS), "findings": COUNT,
                "at": TEXT},
}
OPTIONAL = {"verdict": {"blocking": TEXTS, "leads": TEXTS}}
KINDS = tuple(SCHEMA)


class RecordError(ValueError):
    pass


def malformed(rec: object) -> str:
    """'' when `rec` is a well-formed review record (SCHEMA), else what is wrong with it: not an object of a kind of
    KINDS, a required field missing, a field that fails its test, or a verdict whose `verdict` and `findings` disagree
    (FINDINGS when, and only when, `findings` > 0: what `verdict` computes)."""
    if not isinstance(rec, dict) or rec.get("kind") not in SCHEMA:
        return f"not a review record (a JSON object whose `kind` is one of {', '.join(KINDS)})"
    kind = rec["kind"]
    missing = [k for k in SCHEMA[kind] if k not in rec]
    if missing:
        return f"a {kind} record that lacks {', '.join(missing)}"
    tests = {**SCHEMA[kind], **{k: f for k, f in OPTIONAL.get(kind, {}).items() if k in rec}}
    bad = [f"`{k}` is {rec[k]!r}, not {what}" for k, (what, ok) in tests.items() if not ok(rec[k])]
    if bad:
        return f"a {kind} record whose {'; '.join(bad)}"
    if kind == "verdict" and (rec["verdict"] == "FINDINGS") != (rec["findings"] > 0):
        return f"a verdict {rec['verdict']} with findings {rec['findings']} (FINDINGS when, and only when, findings > 0)"
    return ""


def now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")


NEWLINE = b"\n"
BOM = b"\xef\xbb\xbf"


@dataclasses.dataclass(frozen=True)
class Scan:
    """One findings file read against the tear rule (the module docstring, EVERY CONTENT PROBLEM IS A TEAR). `records`
    is every record when the file is intact. When it is torn, `torn` describes the tear (`<file>:<line> (byte
    <offset>): <reason>`, or `<file>: <reason>` when it could not be read: `line` is then None), `reason` is why,
    `offset` is the byte offset where the torn line starts, and `keep` says that line is the unterminated last line and
    is a record (repair terminates it); `records` then holds the records before the tear, which only `repair` reads: no
    reader takes a verdict from a torn file."""
    records: list[dict]
    torn: str = ""
    reason: str = ""
    line: int | None = None
    offset: int = 0
    keep: bool = False


def scan(data: bytes, path: pathlib.Path) -> Scan:
    """The ONE parser of a findings file's bytes: `load` (every writer's read, the finisher's), `last_verdict` (the
    reader) and `repair` all call it. Every content problem is a tear: a byte-order mark, bytes that are not UTF-8, a
    line that is not JSON or not a record (malformed: SCHEMA), no newline at the end, no record at all."""
    recs: list[dict] = []

    def tear(why: str, line: int, at: int, keep: bool = False) -> Scan:
        return Scan(recs, f"{path}:{line} (byte {at}): {why}", why, line, at, keep)

    if data.startswith(BOM):
        return tear("starts with a UTF-8 byte-order mark (a hand edit: the writers write none)", 1, 0)
    try:
        data.decode("utf-8")
        bad = -1
    except UnicodeDecodeError as e:
        bad = e.start
    at, line = 0, 0
    while at < len(data):
        line += 1
        end = data.find(NEWLINE, at)
        stop = len(data) if end < 0 else end
        if at <= bad < stop:
            return tear(f"not UTF-8 (byte {bad} is 0x{data[bad]:02X})", line, at)
        text = data[at:stop].decode("utf-8")
        if text.strip():
            try:
                rec = json.loads(text)
            except ValueError as e:
                return tear(f"not JSON ({e})", line, at)
            why = malformed(rec)
            if why:
                return tear(why, line, at)
            if end < 0:
                return tear("the last record has no newline at the end", line, at, keep=True)
            recs.append(rec)
        elif end < 0:
            return tear("no newline at the end", line, at)
        at = stop + 1
    if not recs:
        return tear("holds no record (a findings file starts with its round-1 `start`)", 1, 0)
    return Scan(recs)


def examine(path: pathlib.Path) -> tuple[Scan, bytes]:
    """The findings file at `path` read and scanned, with its bytes. It never raises: a file that cannot be opened or
    read (gone, a directory in its place, a read denied, a lock) is a tear of its own (`line` None, no bytes)."""
    try:
        data = path.read_bytes()
    except OSError as e:
        why = f"cannot be read ({type(e).__name__}: {e.strerror or e})"
        return Scan([], f"{path}: {why}", why), b""
    return scan(data, path), data


def repair_command(path: pathlib.Path) -> str:
    return f"python scripts/orchestrator/review_findings.py repair {path}"


def remedy(path: pathlib.Path, line: int | None) -> str:
    """What the operator does about a torn file (no survey and no wave does it): `repair` it; when it could not be read
    (its tear's `line` is None) `repair` cannot either, so it is made readable first."""
    if line is None:
        return (f"make {path} readable again (restore it, or move what stands in its place aside), then run "
                f"`{repair_command(path)}` if it still reads torn")
    return f"run `{repair_command(path)}`"


def load(path: pathlib.Path) -> list[dict]:
    """Every record, in order; [] when there is no file yet (round 1's `start` creates it). A torn file is an error,
    never read past or around: a lost finding would read CLEAN, and a record glued onto a torn line swallows the one
    before it (review R5-1). The refusal names the repair, which the operator runs by hand (`repair`)."""
    if not os.path.lexists(path):
        return []
    s, _ = examine(path)
    if s.torn:
        raise RecordError(f"{s.torn}: the findings file is TORN, so no writer appends to it and every branch it names is "
                          f"held. The operator mends it: {remedy(path, s.line)} (repair prints what it mends: it keeps "
                          "a last record that only lost its newline and removes the rest)")
    return s.records


def mend(data: bytes, path: pathlib.Path) -> tuple[bytes, list[str]]:
    """A torn file's bytes mended, with what was done, one line each (`kept: …`, `removed: …`): a byte-order mark is
    removed and the rest mended; a last line that is a record but lost its newline is terminated and kept; otherwise
    the torn line and every line after it are removed, bytes that are not UTF-8 shown as bytes. Bytes that hold no
    record mend to none (repair then deletes the file). An intact file's bytes come back unchanged, with []."""
    s = scan(data, path)
    if not s.torn:
        return data, []
    if data.startswith(BOM):
        rest, did = mend(data[len(BOM):], path)
        return rest, ["removed: the UTF-8 byte-order mark", *did]
    if not data.strip():
        return b"", [f"removed: {len(data)} byte(s) that hold no record"]
    if s.keep:
        return data + NEWLINE, [f"kept: {data[s.offset:].decode('utf-8')} (its newline restored)"]
    shown = []
    for raw in data[s.offset:].split(NEWLINE):
        try:
            shown.append(f"removed: {raw.decode('utf-8')}")
        except UnicodeDecodeError:
            shown.append(f"removed (not UTF-8): {raw!r}")
    if data.endswith(NEWLINE):
        shown.pop()   # the empty piece after the last newline is no line
    return data[:s.offset], shown


def repair(path: pathlib.Path) -> list[str]:
    """Mend a torn findings file in place (mend) and return what was done; [] when it is intact, and then the file is
    left untouched. When no record is left the file is deleted (the next `start` creates it again). A file it cannot
    read is refused: the operator makes it readable first. The operator runs it by hand (the CLI's `repair`), after
    reading what it prints; no survey and no wave runs it."""
    s, data = examine(path)
    if s.torn and s.line is None:
        raise RecordError(f"{s.torn}: repair cannot mend a file it cannot read; make it readable (or move what stands "
                          "in its place aside) and run it again")
    new, did = mend(data, path)
    if did and new:
        path.write_bytes(new)
    elif did:
        path.unlink()
        did.append(f"deleted: {path} (no record is left; the next `start` creates it again)")
    return did


def append(path: pathlib.Path, rec: dict) -> dict:
    """Append one record. A torn file is refused BEFORE anything is written (`load`), so no record is ever glued onto a
    torn line (review R5-1: a FINDINGS verdict that lost its newline, then the next `start` glued on, read as the CLEAN
    round before it), and so is a record the reader would read as a tear (malformed: SCHEMA)."""
    load(path)
    rec = {**rec, "at": now()}
    why = malformed(rec)
    if why:
        raise RecordError(f"refused to write {why} (SCHEMA: the reader would read it as a tear)")
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(rec, ensure_ascii=False) + "\n")
    return rec


def of_round(recs: list[dict], rnd: int, kind: str) -> list[dict]:
    return [r for r in recs if r.get("round") == rnd and r.get("kind") == kind]


def start(path: pathlib.Path, rnd: int, branch: str, head: str, reviewer: str) -> dict:
    recs = load(path)
    if of_round(recs, rnd, "start"):
        raise RecordError(f"round {rnd} already started in {path}: a re-review of a new head is the next round")
    if rnd > 1 and not of_round(recs, rnd - 1, "verdict"):
        raise RecordError(f"round {rnd - 1} has no verdict in {path}: finish it (verdict) before round {rnd}")
    if not re.fullmatch(r"[0-9a-f]{7,40}", head):
        raise RecordError(f"--head must be a commit sha, got {head!r}")
    if not re.fullmatch(r"[^\s`*]+", reviewer):
        raise RecordError(f"--reviewer must be one word (the stamp reads it as one), got {reviewer!r}")
    return append(path, {"kind": "start", "round": rnd, "branch": branch, "head": head, "reviewer": reviewer})


def finding(path: pathlib.Path, rnd: int, raw: str) -> dict:
    recs = load(path)
    if not of_round(recs, rnd, "start"):
        raise RecordError(f"round {rnd} was never started in {path} (start first)")
    if of_round(recs, rnd, "verdict"):
        raise RecordError(f"round {rnd} already has its verdict in {path}: a later finding is the next round's")
    try:
        body = json.loads(raw)
    except ValueError as e:
        raise RecordError(f"the finding is not one JSON object: {e}") from e
    if not isinstance(body, dict):
        raise RecordError("the finding is not one JSON object")
    missing = [k for k in FINDING_FIELDS if body.get(k) in (None, "")]
    if missing:
        raise RecordError(f"the finding lacks {', '.join(missing)} (no scenario, no finding: the review skill's bar)")
    if body["severity"] not in SEVERITIES:
        raise RecordError(f"`severity` is one of {', '.join(SEVERITIES)}, got {body['severity']!r}")
    k = len(of_round(recs, rnd, "finding")) + 1
    return append(path, {**{k2: v for k2, v in body.items() if k2 not in ("kind", "round", "id", "at")},
                         "kind": "finding", "round": rnd, "id": f"R{rnd}-{k}"})


def blocking_set(text: str) -> tuple[str, ...]:
    """`--blocking`'s value: a comma list of SEVERITIES that includes critical (a critical finding always blocks)."""
    got = tuple(dict.fromkeys(x.strip() for x in text.split(",") if x.strip()))
    bad = [x for x in got if x not in SEVERITIES]
    if bad or "critical" not in got:
        raise RecordError(f"--blocking is a comma list of {', '.join(SEVERITIES)} that includes critical, got {text!r}")
    return got


def blocks(rec: dict, blocking: tuple[str, ...]) -> bool:
    """A finding blocks when its severity is in `blocking`, or when it carries none (recorded before severities were
    required): an unknown severity is never waved through."""
    return rec.get("severity") not in SEVERITIES or rec["severity"] in blocking


def verdict(path: pathlib.Path, rnd: int, blocking: tuple[str, ...] = BLOCKING_DEFAULT) -> dict:
    """The round's verdict, from the file: CLEAN when no finding of a `blocking` severity was recorded, else FINDINGS n
    (n = the blocking ones). The others are the round's `leads` (their ids), every one still recorded."""
    recs = load(path)
    started = of_round(recs, rnd, "start")
    if not started:
        raise RecordError(f"round {rnd} was never started in {path}")
    if of_round(recs, rnd, "verdict"):
        raise RecordError(f"round {rnd} already has its verdict in {path}")
    found = of_round(recs, rnd, "finding")
    n = sum(1 for r in found if blocks(r, blocking))
    s = started[0]
    return append(path, {"kind": "verdict", "round": rnd, "branch": s["branch"], "head": s["head"],
                         "reviewer": s["reviewer"], "verdict": "CLEAN" if n == 0 else "FINDINGS", "findings": n,
                         "blocking": list(blocking), "leads": [r["id"] for r in found if not blocks(r, blocking)]})


def lead_lines(path: pathlib.Path, v: dict) -> list[str]:
    """The verdict `v`'s leads as the lines the stamp writes and the reviewer returns:
    `<id> LEAD (<severity>, not blocking at round <k>): <file>:<line> <summary>`."""
    by_id = {r["id"]: r for r in load(path) if r["kind"] == "finding"}
    return [f"{i} LEAD ({by_id[i]['severity']}, not blocking at round {v['round']}): "
            f"{by_id[i]['file']}:{by_id[i]['line']} {by_id[i]['summary']}" for i in v.get("leads", []) if i in by_id]


def stamp_line(v: dict, findings_file: pathlib.Path) -> str:
    if v["verdict"] == "CLEAN":
        return f"**Review:** CLEAN {v['reviewer']} {v['head']}"
    return f"**Review:** FINDINGS {v['findings']} {v['reviewer']} {v['head']} {findings_file}"


def stamp(report: pathlib.Path, findings_file: pathlib.Path) -> str:
    """Write the newest verdict of `findings_file` into the report's header: replace its Review line, else insert one
    right under the status line (plan_wave.header_lines and plan_wave.status_line: the planner's own reading).

    A report that is not FINISHED (plan_wave.Report.finished: DONE, or DISCHARGED with branch work) is REFUSED, a
    header with no status line among them: the branch survey never admits such a report, so a stamp on it would read as
    a reviewed branch that nobody lands (kb/Work PB3181 shape 3: w1080f's header began with an empty line and no
    `Status:`, the stamp went under line 0, and the reviewed, gate-GREEN branch sat in the DECISION rows)."""
    verdicts = [r for r in load(findings_file) if r["kind"] == "verdict"]
    if not verdicts:
        raise RecordError(f"{findings_file} holds no verdict: run `verdict` first")
    if " " in str(findings_file):
        raise RecordError(f"the findings file path must have no space (the stamp reads it as one word): {findings_file}")
    rep = plan_wave.parse_report(report)
    if rep is None or not rep.finished:
        raise RecordError(f"{report} is not a finished fix-lane report ("
                          + ("not a w<wave><letter>-PB<lead>-report.md name" if rep is None else
                             f"status {rep.status or 'missing: no status line in the first six header lines'}"
                             f", head {rep.head or 'none'}, base {rep.base or 'none'}")
                          + "): its author writes `Status: DONE` (or DISCHARGED with its HEAD) first; nothing stamped")
    line = stamp_line(verdicts[-1], findings_file)
    text = report.read_text(encoding="utf-8")
    lines = with_leads(text.splitlines(), lead_lines(findings_file, verdicts[-1]))
    end = len(plan_wave.header_lines(lines))
    at = next((i for i in range(end) if REVIEW_LINE.match(lines[i].strip())), None)
    if at is not None:
        lines[at] = line
    else:
        lines.insert(plan_wave.status_line(lines[:end])[0] + 1, line)
    out = "\n".join(lines) + ("\n" if text.endswith("\n") else "")
    header = "\n".join(lines[:end + (0 if at is not None else 1)])
    got = plan_wave.parse_review(header)
    v = verdicts[-1]
    if got is None or got.verdict != v["verdict"] or got.sha != v["head"] or got.findings != v["findings"]:
        raise RecordError(f"the stamp would not read back through plan_wave.parse_review: {line!r} -> {got}")
    report.write_text(out, encoding="utf-8", newline="")
    return line


LEADS_SECTION = "## Review findings"
LEAD_ID = re.compile(r"\bR\d+-\d+\b")


def with_leads(lines: list[str], leads: list[str]) -> list[str]:
    """The report's lines with each lead listed under its `## Review findings` section (made at the end when the report
    has none), once: a lead whose id the section already names is not written again, so a re-stamp adds nothing. That
    section is where a review finisher answers findings by id and where the enqueue unit's clerk step reads the leads
    (units/enqueue.md step 5)."""
    if not leads:
        return lines
    out = list(lines)
    at = next((i for i, text in enumerate(out) if text.strip() == LEADS_SECTION), None)
    if at is None:
        out += ["", LEADS_SECTION]
        at = len(out) - 1
    end = next((i for i in range(at + 1, len(out)) if out[i].startswith("## ")), len(out))
    named = {m for text in out[at + 1:end] for m in LEAD_ID.findall(text)}
    new = [f"- {lead}" for lead in leads if LEAD_ID.match(lead).group(0) not in named]
    while end > at + 1 and not out[end - 1].strip():
        end -= 1
    return out[:end] + new + out[end:]


def show(path: pathlib.Path, rnd: int | None = None) -> list[str]:
    recs = load(path)
    rnd = rnd or max((r["round"] for r in recs if r["kind"] == "verdict"), default=None)
    v = next((r for r in recs if r["kind"] == "verdict" and r["round"] == rnd), None)
    leads = set(v.get("leads", [])) if v else set()
    return [f"{r['id']}  {'LEAD' if r['id'] in leads else 'BLOCKING'} ({r.get('severity', 'no severity')})  "
            f"{r['file']}:{r['line']}  [{r['category']}] {r['summary']}\n    scenario: {r['failure_scenario']}"
            for r in recs if r["kind"] == "finding" and r["round"] == rnd]


def named(data: bytes) -> tuple[list[str], list[str]]:
    """The branches and heads a findings file's bytes name: every line that parses as a JSON object, a torn file's
    included (a byte-order mark set aside), in order, once each. What a torn file holds (EVERY CONTENT PROBLEM IS A
    TEAR)."""
    branches: dict[str, None] = {}
    heads: dict[str, None] = {}
    for text in data.decode("utf-8", errors="replace").split("\n"):
        try:
            rec = json.loads(text.lstrip("\ufeff"))
        except ValueError:
            continue
        if isinstance(rec, dict):
            if isinstance(rec.get("branch"), str) and rec["branch"]:
                branches[rec["branch"]] = None
            if isinstance(rec.get("head"), str) and SHA.fullmatch(rec["head"]):
                heads[rec["head"]] = None
    return list(branches), list(heads)


def torn_reading(path: pathlib.Path, s: Scan, data: bytes) -> dict:
    """A torn file's reading: no verdict is read from it, it holds (`unanswered`), and it names the tear (`torn`, its
    `reason` and `line`, None when the file could not be read) and every branch and head its parseable lines name,
    which the survey holds. A file whose bytes name no branch is bound by its NAME: the enqueue unit's
    `review-<branch>.jsonl` names its branch; a wave's `review-w<wave>-<letter>.jsonl` names its group, whose report
    the survey finds (prune_worktrees.findings_lead)."""
    branches, heads = named(data)
    by_name = NAMED_FINDINGS.match(path.name)
    if not branches and by_name and not WAVE_FINDINGS.match(path.name):
        branches = [by_name["branch"]]
    return {"file": str(path), "round": None, "branch": "", "head": "", "verdict": "TORN", "blocking": None,
            "unanswered": True, "torn": s.torn, "reason": s.reason, "line": s.line, "branches": branches,
            "heads": heads}


def last_verdict(path: pathlib.Path) -> dict | None:
    """The newest verdict the findings file records (reading, over the file's bytes): the ONE answer to both callers,
    the CLI's `last-verdict` (the wave) and `unanswered` (the survey). It never raises: a file that cannot be read is a
    tear (examine), and so is any failure of the reader itself, so the two callers cannot disagree."""
    s, data = examine(path)
    try:
        return torn_reading(path, s, data) if s.torn else reading(data, path, s)
    except Exception as e:  # noqa: BLE001  (the one boundary: a reader that fails reads TORN, never a release)
        why = f"the reader failed ({type(e).__name__}: {e})"
        return torn_reading(path, Scan([], f"{path}: {why}", why), data)


def reading(data: bytes, path: pathlib.Path, s: Scan | None = None) -> dict | None:
    """The newest verdict findings-file bytes record, as {file, round, branch, head, verdict, blocking, unanswered,
    torn}, or None when no round has one yet. `blocking` is the number of findings of a blocking severity under the
    thresholds that round used (the verdict's own count); `unanswered` is FINDINGS with blocking > 0. A round started
    after it and never finished (its reviewer died) answers nothing, so the verdict before it still stands; `torn` is ''.
    TORN bytes are never read for a verdict (EVERY CONTENT PROBLEM IS A TEAR): their reading is torn_reading's, verdict
    TORN and `unanswered` true, whatever the records say, so every caller holds it and the wave never reads it capped."""
    s = s or scan(data, path)
    if s.torn:
        return torn_reading(path, s, data)
    verdicts = [r for r in s.records if r["kind"] == "verdict"]
    if not verdicts:
        return None
    v = max(verdicts, key=lambda r: r["round"])
    return {"file": str(path), "round": v["round"], "branch": v["branch"], "head": v["head"], "verdict": v["verdict"],
            "blocking": v["findings"], "unanswered": v["findings"] > 0, "torn": ""}


#: The two names a branch review's findings file takes: the rolling wave's `<scratch>/review-w<wave>-<letter>.jsonl`
#: (wf_rolling_wave.js findingsFile) and the enqueue unit's `<coord>/scratch/review-<branch>.jsonl` (units/enqueue.md).
FINDINGS_NAME = "review-*.jsonl"
WAVE_FINDINGS = re.compile(r"^review-w(?P<wave>\d+)-(?P<letter>[a-z0-9]+)\.jsonl$")
NAMED_FINDINGS = re.compile(r"^review-(?P<branch>.+)\.jsonl$")


def findings_files(scratch: pathlib.Path, also: list[str | pathlib.Path] = ()) -> list[pathlib.Path]:
    """Every branch review's findings file, BY NAME ONLY (kb/Work PB3381 review R6-1): every `review-*.jsonl` in the
    coordination scratch `scratch` and in each directory directly under it (a wave's scratch, `<coord>/scratch/w<wave>`),
    plus each path in `also` (the files report stamps name, wherever a hand review put them), once each. Nothing is
    opened, read or parsed to decide what is one: a candidate's content is the reader's business (last_verdict), which
    reads one that cannot be opened (a stamped path that is gone among them), or whose content is not records, as
    TORN, so it holds. The R2 review's
    `review-<pair>--f<k>.jsonl` shards live two levels down (`<coord>/scratch/r2-<sha>/batch-<n>`), outside this search."""
    seen: dict[str, pathlib.Path] = {}
    cands = [*scratch.glob(FINDINGS_NAME), *scratch.glob(f"*/{FINDINGS_NAME}")] if scratch.is_dir() else []
    for p in [*cands, *(pathlib.Path(a) for a in also if a)]:
        seen.setdefault(os.path.normcase(os.path.abspath(p)), p)
    return sorted(seen.values())


def unanswered(paths: list[pathlib.Path]) -> list[dict]:
    """The reading of each file that holds: its last verdict is FINDINGS with blocking findings, or it is TORN
    (torn_reading: `torn` set, which the caller says aloud until the operator repairs the file). It never raises
    (last_verdict never does), so one findings file never stops the landing lane's tick for every branch."""
    return [v for v in map(last_verdict, paths) if v and v["unanswered"]]


def minutes(a: str, b: str) -> float:
    return (dt.datetime.fromisoformat(b) - dt.datetime.fromisoformat(a)).total_seconds() / 60


def summary(paths: list[pathlib.Path]) -> dict:
    """The measurement (kb/Work PB3027 item 5): per branch review file its rounds, findings and reviewer minutes; over
    all of them, findings per branch, the re-review rate (branches reviewed more than once) and the minutes."""
    files = sorted({f for p in paths for f in ([p] if p.is_file() else p.glob("review-w*.jsonl"))})
    per = []
    for f in files:
        recs = load(f)
        rounds = []
        for v in (r for r in recs if r["kind"] == "verdict"):
            s = of_round(recs, v["round"], "start")[0]
            rounds.append({"round": v["round"], "verdict": v["verdict"], "findings": v["findings"],
                           "leads": len(v.get("leads", [])), "minutes": round(minutes(s["at"], v["at"]), 1)})
        per.append({"file": str(f), "rounds": rounds, "open": any(r["kind"] == "start" for r in recs)
                    and len(rounds) < max(r["round"] for r in recs if r["kind"] == "start")})
    done = [p for p in per if p["rounds"]]
    n = len(done)
    return {"branches": n,
            "findings_per_branch": round(sum(r["findings"] for p in done for r in p["rounds"]) / n, 2) if n else 0.0,
            "re_review_rate": round(sum(1 for p in done if len(p["rounds"]) > 1) / n, 2) if n else 0.0,
            "leads_per_branch": round(sum(r["leads"] for p in done for r in p["rounds"]) / n, 2) if n else 0.0,
            "clean": sum(1 for p in done if p["rounds"][-1]["verdict"] == "CLEAN"),
            "reviewer_minutes": round(sum(r["minutes"] for p in done for r in p["rounds"]), 1),
            "per_branch": per}


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    fails: list[str] = []

    def check(what: str, ok: bool) -> None:
        if not ok:
            fails.append(what)

    def refused(fn, *a) -> bool:
        try:
            fn(*a)
        except RecordError:
            return True
        return False

    tmp = pathlib.Path(tempfile.mkdtemp(prefix="review-findings-selftest-"))
    try:
        f = tmp / "review-w9-a.jsonl"
        report = tmp / "w9a-PB1-report.md"
        report.write_text("# PB1 — implementer report\n\n**Status:** DONE · **branch:** `b`\n**diffstat:** x\n\n"
                          "## Reproduced?\n**Review:** below the header binds nothing\n", encoding="utf-8", newline="\n")
        check("arm: a finding before its round started is refused", refused(finding, f, 1, '{"file":"a"}'))
        start(f, 1, "b", "abc1234", "w9a-review-r1")
        check("arm: a second start of one round is refused", refused(start, f, 1, "b", "abc1234", "x"))
        check("arm: a reviewer label with a space is refused", refused(start, f, 2, "b", "abc1234", "two words"))
        check("arm: a finding without its scenario is refused",
              refused(finding, f, 1, json.dumps({"file": "a.py", "line": 3, "category": "correctness", "summary": "s"})))
        check("arm: a finding whose line is not an integer is refused",
              refused(finding, f, 1, json.dumps({"file": "a.py", "line": "3", "category": "c", "summary": "s",
                                                 "severity": "high", "failure_scenario": "x"})))
        check("arm: a finding without its severity is refused (kb/Work PB3381: the threshold reads it)",
              refused(finding, f, 1, json.dumps({"file": "a.py", "line": 3, "category": "c", "summary": "s",
                                                 "failure_scenario": "x"})))
        check("arm: a finding whose severity is not one of SEVERITIES is refused",
              refused(finding, f, 1, json.dumps({"file": "a.py", "line": 3, "category": "c", "summary": "s",
                                                 "severity": "nit", "failure_scenario": "x"})))
        r = finding(f, 1, json.dumps({"file": "a.py", "line": 3, "category": "correctness", "summary": "off by one",
                                      "severity": "high",
                                      "failure_scenario": "n=0 -> IndexError", "id": "forged", "round": 7}))
        check("arm: the tool numbers the finding, whatever the record claimed", r["id"] == "R1-1" and r["round"] == 1)
        check("arm: round 2 before round 1's verdict is refused", refused(start, f, 2, "b", "def5678", "x"))
        v = verdict(f, 1)
        check("arm: the verdict is computed from the findings on disk", v["verdict"] == "FINDINGS" and v["findings"] == 1)
        check("arm: a finding after the round's verdict is refused",
              refused(finding, f, 1, json.dumps({"file": "a", "line": 1, "category": "c", "summary": "s",
                                                 "severity": "low", "failure_scenario": "x"})))
        line = stamp(report, f)
        rep = plan_wave.parse_report(report)
        check(f"arm: a FINDINGS stamp reads back through plan_wave: {line} -> {rep.review if rep else None}",
              rep is not None and rep.review == plan_wave.Review("FINDINGS", 1, "w9a-review-r1", "abc1234", str(f))
              and rep.status == "DONE")
        check("arm: the stamp goes right under the Status line",
              report.read_text(encoding="utf-8").splitlines()[3].startswith("**Review:** FINDINGS 1 "))
        check("arm: `show` lists the round's findings by id", show(f)[0].startswith("R1-1  BLOCKING (high)  a.py:3  [correctness]"))
        start(f, 2, "b2", "def5678", "w9a-review-r2")
        verdict(f, 2)
        line = stamp(report, f)
        text = report.read_text(encoding="utf-8")
        rep = plan_wave.parse_report(report)
        check("arm: a re-review REPLACES the stamp, never adds a second",
              text.count("**Review:** CLEAN") == 1 and "FINDINGS 1" not in text.split("## ")[0])
        check("arm: a CLEAN stamp covers exactly its head",
              rep.review.verdict == "CLEAN" and rep.review.covers("def5678aaaa") and not rep.review.covers("abc1234"))
        check("arm: the line below the header is untouched", "**Review:** below the header binds nothing" in text)
        check("arm: a FINDINGS stamp without its file does not parse",
              plan_wave.parse_review("**Review:** FINDINGS 2 x abc1234") is None)
        check("arm: a placeholder does not parse", plan_wave.parse_review("**Review:** pending") is None)
        # kb/Work PB3181 shape 3: a report that is not FINISHED is refused and left untouched, never stamped under line 0
        for name, body in (("w9b-PB2-report.md", "\n# w9 group B -- PB2\nBranch: b · head abc1234\n"),   # no status line
                           ("w9c-PB3-report.md", "Status: SPLIT\nBranch: b · HEAD: abc1234\n"),
                           ("w9d-PB4-report.md", "Status: DISCHARGED\nBranch: b · base abc1234 · head abc1234\n"),
                           ("notes-PB5.md", "Status: DONE\nBranch: b\n")):                         # not a report name
            unfinished = tmp / name
            unfinished.write_text(body + "\n## Rule\n", encoding="utf-8", newline="\n")
            check(f"arm: {name} (not a finished report) is refused, unchanged",
                  refused(stamp, unfinished, f) and unfinished.read_text(encoding="utf-8") == body + "\n## Rule\n")
        bare = tmp / "w9e-PB6-report.md"
        bare.write_text("# w9 group E -- PB6\nDONE\nBranch: b · head abc1234\n\n## Rule\n", encoding="utf-8", newline="\n")
        stamp(bare, f)
        check("arm: the stamp goes under a bare status word that is not the first line",
              bare.read_text(encoding="utf-8").splitlines()[2].startswith("**Review:** CLEAN "))
        # THE SEVERITY THRESHOLD (kb/Work PB3381). Expected results from the rule: a high blocks at every round, a
        # medium blocks by default and not with --blocking critical,high, a low never blocks, every finding stays
        # recorded, and the ones that do not block are the round's leads.
        def one_round(name: str, severities: list, blocking=BLOCKING_DEFAULT) -> tuple:
            path = tmp / "thresholds" / f"review-w9-{name}.jsonl"
            start(path, 1, "b", "abc1234", f"w9{name}-review-r1")
            for k, sev in enumerate(severities, 1):
                body = {"file": "a.py", "line": k, "category": "correctness", "summary": f"finding {k}",
                        "failure_scenario": "x"}
                if sev not in SEVERITIES:   # a free word from before SEVERITIES (`minor`): never written via finding()
                    append(path, {**body, "severity": sev, "kind": "finding", "round": 1, "id": f"R1-{k}"})
                else:
                    finding(path, 1, json.dumps({**body, "severity": sev}))
            return path, verdict(path, 1, blocking)

        _, v = one_round("h", ["high"], BLOCKING_AT_CAP)
        check(f"arm: a high finding blocks, at the capped round too: {v}", (v["verdict"], v["findings"]) == ("FINDINGS", 1))
        _, v = one_round("m", ["medium"])
        check(f"arm: a medium finding blocks by default: {v}",
              (v["verdict"], v["findings"], v["leads"]) == ("FINDINGS", 1, []))
        _, v = one_round("n", ["medium"], blocking_set("critical,high"))
        check(f"arm: a medium finding does not block with --blocking critical,high, and is a lead: {v}",
              (v["verdict"], v["findings"], v["leads"]) == ("CLEAN", 0, ["R1-1"]))
        lowf, v = one_round("l", ["low", "info"])
        check(f"arm: a low or info finding never blocks; both are leads: {v}",
              (v["verdict"], v["findings"], v["leads"]) == ("CLEAN", 0, ["R1-1", "R1-2"]))
        check("arm: every finding stays recorded, blocking or not",
              len([r for r in load(lowf) if r["kind"] == "finding"]) == 2)
        _, v = one_round("x", ["high", "medium", "low", "minor"])
        check(f"arm: the count is the blocking findings only; a finding with an unknown severity blocks: {v}",
              (v["verdict"], v["findings"], v["leads"]) == ("FINDINGS", 3, ["R1-3"]))
        check("arm: --blocking without critical, or naming no severity, is refused",
              refused(blocking_set, "high,medium") and refused(blocking_set, "critical,nit"))
        check("arm: `show` marks each finding BLOCKING or LEAD",
              [x.split()[1] for x in show(tmp / "thresholds" / "review-w9-x.jsonl", 1)] == ["BLOCKING", "BLOCKING", "LEAD", "BLOCKING"])
        lreport = tmp / "thresholds" / "w9l-PB7-report.md"
        lreport.write_text("Status: DONE\nBranch: b · head abc1234\n\n## Rule\nx\n", encoding="utf-8", newline="\n")
        line = stamp(lreport, lowf)
        text = lreport.read_text(encoding="utf-8")
        check(f"arm: a CLEAN round with leads stamps CLEAN and lists each lead under ## Review findings: {text!r}",
              line.startswith("**Review:** CLEAN ") and text.count("## Review findings") == 1
              and "- R1-1 LEAD (low, not blocking at round 1): a.py:1 finding 1" in text
              and "- R1-2 LEAD (info, not blocking at round 1): a.py:2 finding 2" in text)
        stamp(lreport, lowf)
        check("arm: a re-stamp writes no lead twice", lreport.read_text(encoding="utf-8") == text)
        areport = tmp / "thresholds" / "w9a-PB8-report.md"
        areport.write_text("Status: DONE\nBranch: b · head abc1234\n\n## Review findings\n- R1-1 fixed in abc1234\n\n"
                           "## For the next implementer\nx\n", encoding="utf-8", newline="\n")
        stamp(areport, lowf)
        atext = areport.read_text(encoding="utf-8")
        check(f"arm: a lead the section already answers is not added; the other goes in that section: {atext!r}",
              atext.count("R1-1") == 1 and atext.index("- R1-2 LEAD") < atext.index("## For the next implementer"))
        cli = tmp / "thresholds" / "review-w9-c.jsonl"
        start(cli, 1, "b", "abc1234", "w9c-review-r1")
        finding(cli, 1, json.dumps({"file": "a.py", "line": 1, "category": "c", "summary": "s", "severity": "medium",
                                    "failure_scenario": "x"}))
        check("arm: the CLI passes --blocking to the verdict",
              main(["verdict", str(cli), "--round", "1", "--blocking", "critical,high"]) == 0
              and [r for r in load(cli) if r["kind"] == "verdict"][0]["blocking"] == ["critical", "high"])
        # THE ONE READER OF UNANSWERED FINDINGS (kb/Work PB3381 review R3-1): the file's newest verdict, whatever any
        # report says. Expected results from the rule: no verdict reads None; a FINDINGS round with blocking findings
        # is unanswered at its head; a later CLEAN round answers it; a round started and never finished answers nothing.
        lv = tmp / "last" / "review-w9-l.jsonl"
        start(lv, 1, "bL", "abc1234", "w9l-review-r1")
        check("arm: a findings file with no verdict yet has no last verdict", last_verdict(lv) is None)
        finding(lv, 1, json.dumps({"file": "a.py", "line": 1, "category": "c", "summary": "s", "severity": "high",
                                   "failure_scenario": "x"}))
        finding(lv, 1, json.dumps({"file": "a.py", "line": 2, "category": "c", "summary": "s", "severity": "low",
                                   "failure_scenario": "x"}))
        verdict(lv, 1)
        got = last_verdict(lv)
        check(f"arm: a FINDINGS round is unanswered at its head, counting only its blocking findings: {got}",
              got == {"file": str(lv), "round": 1, "branch": "bL", "head": "abc1234", "verdict": "FINDINGS",
                      "blocking": 1, "unanswered": True, "torn": ""})
        start(lv, 2, "bL2", "def5678", "w9l-review-r2")
        check("arm: a round started and never finished answers nothing: the FINDINGS before it stands",
              (last_verdict(lv) or {}).get("round") == 1 and last_verdict(lv)["unanswered"])
        verdict(lv, 2)
        got = last_verdict(lv)
        check(f"arm: a later CLEAN round answers them: {got}",
              (got["round"], got["branch"], got["head"], got["verdict"], got["unanswered"]) == (2, "bL2", "def5678", "CLEAN", False))
        held = tmp / "last" / "w77" / "review-w77-h.jsonl"
        start(held, 1, "bH", "fed4321", "w77h-review-r1")
        finding(held, 1, json.dumps({"file": "a.py", "line": 1, "category": "c", "summary": "s", "severity": "medium",
                                     "failure_scenario": "x"}))
        verdict(held, 1)
        enq = tmp / "last" / "review-worktree-wf_x-1.jsonl"   # the enqueue unit's name for an attended branch
        start(enq, 1, "worktree-wf_x-1", "0123456", "enqueue-review-worktree-wf_x-1-r1")
        verdict(enq, 1)
        (tmp / "last" / "r2").mkdir()
        (tmp / "last" / "r2" / "review-cli--code.jsonl").write_text('{"type": "read", "file": "x.cs"}\n', encoding="utf-8", newline="\n")
        (tmp / "last" / "w77" / "deep").mkdir()
        deep = tmp / "last" / "w77" / "deep" / "review-w77-z.jsonl"
        start(deep, 1, "bZ", "9999999", "w77z-review-r1")
        outside = tmp / "elsewhere" / "review-w78-o.jsonl"
        start(outside, 1, "bO", "8888888", "w78o-review-r1")
        files = findings_files(tmp / "last", also=[str(outside), str(lv), str(tmp / "missing.jsonl")])
        check(f"arm: findings_files lists BY NAME the scratch and one level under it, plus the stamp-named files, once "
              f"each, a file of other records and a stamp-named file that is gone included (review R6-1): {files}",
              sorted(p.name for p in files) == sorted(["review-w9-l.jsonl", "review-w77-h.jsonl", "review-cli--code.jsonl",
                                                       "review-worktree-wf_x-1.jsonl", "review-w78-o.jsonl",
                                                       "missing.jsonl"]))
        got = unanswered(files)
        check(f"arm: unanswered() keeps the files whose last verdict holds blocking findings, and every torn one (another "
              f"tool's records; a stamp-named file that is gone), the torn one bound by its name: {got}",
              sorted((v["verdict"], v["branch"] or ",".join(v["branches"]), pathlib.Path(v["file"]).name) for v in got)
              == [("FINDINGS", "bH", "review-w77-h.jsonl"), ("TORN", "", "missing.jsonl"),
                  ("TORN", "cli--code", "review-cli--code.jsonl")])
        check("arm: the wave's name maps back to its wave and letter",
              WAVE_FINDINGS.match("review-w77-h.jsonl").group("wave", "letter") == ("77", "h")
              and WAVE_FINDINGS.match("review-worktree-wf_x-1.jsonl") is None)
        import contextlib  # noqa: PLC0415
        import io  # noqa: PLC0415
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            rc = main(["last-verdict", str(held)])
        line = buf.getvalue().strip()
        check(f"arm: the CLI prints the one line the wave parses: {line}",
              rc == 0 and line.startswith("=== LAST VERDICT: ") and line.endswith(" ===")
              and json.loads(line[len("=== LAST VERDICT: "):-4]) == last_verdict(held))
        bad = tmp / "bad.jsonl"
        bad.write_text('{"kind":"start","round":1}\nnot json\n', encoding="utf-8", newline="\n")
        check("arm: a malformed line is an error for every writer's read, never skipped (a lost finding would read CLEAN)",
              refused(load, bad))
        bad.unlink()
        # A TORN FILE HOLDS EVERY BRANCH IT NAMES (kb/Work PB3381 review R4-1, R5-1). Expected results from the Mythos
        # decision (the module docstring, A TORN FILE): a torn file reads TORN, held, never a verdict, its tear named
        # (file, line, byte offset, reason) with every branch and head it names; every writer refuses it BEFORE
        # writing, naming the repair; `repair` keeps a last record that only lost its newline and removes any other
        # torn tail; the survey's reader never raises.
        def refusal(fn, *args) -> str:
            try:
                fn(*args)
            except RecordError as e:
                return str(e)
            return ""

        high = json.dumps({"file": "x.py", "line": 1, "category": "c", "summary": "s", "severity": "high",
                           "failure_scenario": "x"})
        low = json.dumps({"file": "x.py", "line": 2, "category": "c", "summary": "s", "severity": "low",
                          "failure_scenario": "x"})
        # R4-1, the reviewer's probe: a capped round-3 FINDINGS, then a round-4 finding torn mid-append
        tf = tmp / "torn" / "w1125" / "review-w1125-a.jsonl"
        for k in (1, 2, 3):
            start(tf, k, "bA", "a1a1a1a", f"w1125a-review-r{k}")
            finding(tf, k, high)
            verdict(tf, k, BLOCKING_AT_CAP if k == 3 else BLOCKING_DEFAULT)
        start(tf, 4, "bA", "a1a1a1a", "w1125a-review-r4")
        good = tf.read_bytes()
        tail = b'{"kind": "finding", "round": 4, "file": "x.py", "li'
        tf.write_bytes(good + tail)
        tear_at = good.count(b"\n") + 1
        got = unanswered(findings_files(tmp / "torn"))
        check(f"arm: a torn file reads TORN, held, never the verdict before the tear, naming its branch and head: {got}",
              [(v["verdict"], v["unanswered"], v["round"], v["branches"], v["heads"]) for v in got]
              == [("TORN", True, None, ["bA"], ["a1a1a1a"])])
        check(f"arm: the tear is named by file, line, byte offset and reason: {got}",
              got and got[0]["torn"].startswith(f"{tf}:{tear_at} (byte {len(good)}): not JSON"))
        check("arm: an intact file's last verdict names no tear", last_verdict(held)["torn"] == "")
        for what, fn, args in (("start", start, (tf, 5, "bA", "a1a1a1a", "x")), ("finding", finding, (tf, 4, low)),
                               ("verdict", verdict, (tf, 4)), ("stamp", stamp, (report, tf)),
                               ("append", append, (tf, {"kind": "start", "round": 5, "branch": "bA"}))):
            said = refusal(fn, *args)
            check(f"arm: `{what}` refuses a torn file BEFORE writing and names the repair: {said!r}",
                  f"{tf}:{tear_at}" in said and f"review_findings.py repair {tf}" in said
                  and tf.read_bytes() == good + tail)
        check("arm: `repair` removes exactly the torn tail and says so",
              repair(tf) == [f"removed: {tail.decode()}"] and tf.read_bytes() == good)
        check("arm: `repair` of an intact file changes nothing", repair(tf) == [] and tf.read_bytes() == good)
        finding(tf, 4, low)
        v4 = verdict(tf, 4, BLOCKING_AT_CAP)
        check(f"arm: after `repair` the appends work again, and a later round answers the hold: {v4}",
              v4["verdict"] == "CLEAN" and unanswered(findings_files(tmp / "torn")) == [])
        # R5-1, the reviewer's probe case 3: a FINDINGS verdict that lost only its newline. The next writer refuses
        # (gluing its record on swallowed the verdict, and the round-4 reader read round 1 CLEAN); the reader holds
        sw = tmp / "swallow" / "review-w9-s.jsonl"
        start(sw, 1, "bS", "a1a1a1a", "w9s-review-r1")
        verdict(sw, 1)
        start(sw, 2, "bS", "b2b2b2b", "w9s-review-r2")
        finding(sw, 2, high)
        verdict(sw, 2)
        whole = sw.read_bytes()
        sw.write_bytes(whole[:-1])
        said = refusal(start, sw, 3, "bS", "b2b2b2b", "w9s-review-r3")
        check(f"arm: a writer refuses a file whose last record lost its newline, naming the repair: {said!r}",
              "the last record has no newline at the end" in said and f"review_findings.py repair {sw}" in said
              and sw.read_bytes() == whole[:-1])
        v = last_verdict(sw)
        check(f"arm: it reads TORN, held, naming every branch and head: {v}",
              (v["verdict"], v["unanswered"], v["branches"], v["heads"]) == ("TORN", True, ["bS"], ["a1a1a1a", "b2b2b2b"]))
        glued = whole[:-1] + json.dumps({"kind": "start", "round": 3, "branch": "bS", "head": "b2b2b2b",
                                         "reviewer": "w9s-review-r3"}).encode() + b"\n"
        v = reading(glued, sw)
        check(f"arm: a record glued onto it (probe case 3) reads TORN, held, never round 1 CLEAN: {v}",
              (v["verdict"], v["unanswered"]) == ("TORN", True))
        did = repair(sw)
        v = last_verdict(sw)
        check(f"arm: `repair` keeps the record that only lost its newline, and the FINDINGS stands: {did} {v}",
              len(did) == 1 and did[0].startswith("kept: ") and sw.read_bytes() == whole
              and (v["round"], v["verdict"], v["unanswered"]) == (2, "FINDINGS", True))
        check("arm: after `repair` the next round starts", not refusal(start, sw, 3, "bS", "b2b2b2b", "w9s-review-r3"))
        # a tear inside a multi-byte character (the reviewer's medium lead: the round-4 reader raised
        # UnicodeDecodeError out of the survey, and land_lane's tick, which does not catch it, landed nothing)
        mb = tmp / "utf8" / "review-w9-u.jsonl"
        start(mb, 1, "bU", "a1a1a1a", "w9u-review-r1")
        intact = mb.read_bytes()
        cut = json.dumps({"kind": "finding", "round": 1, "summary": "§14.9 wrong"}, ensure_ascii=False).encode()
        mb.write_bytes(intact + cut[:cut.index("§".encode()) + 1])
        got = unanswered(findings_files(tmp / "utf8"))
        check(f"arm: a file torn inside a multi-byte character is still found, and reads TORN, not UTF-8: {got}",
              [(v["verdict"], v["branches"]) for v in got] == [("TORN", ["bU"])] and "not UTF-8" in got[0]["torn"])
        check("arm: a writer refuses it, naming the repair",
              f"review_findings.py repair {mb}" in refusal(finding, mb, 1, high))
        did = repair(mb)
        check(f"arm: `repair` shows the bytes that are not UTF-8 and removes their line: {did}",
              len(did) == 1 and did[0].startswith("removed (not UTF-8): b'") and mb.read_bytes() == intact)
        # DISCOVERY (kb/Work PB3381 review R6-1, the Mythos decision's part 5). Expected results from the rule: a file
        # is found BY NAME, never by its content, so each of these is LISTED by findings_files and reads TORN through
        # the one reader, the CLI and the survey agreeing (part 4), and the next file is still read. Each case is made
        # at the file's FIRST read: a read denied is pathlib's `open` raising PermissionError (where discovery opened
        # the file before), a directory in the file's place is a real one, a BOM and a zero-byte file are real bytes.
        import subprocess  # noqa: PLC0415
        disc = tmp / "discovery" / "w31"
        start(disc / "review-w31-a.jsonl", 1, "bA31", "a1a1a1a", "w31a-review-r1")
        finding(disc / "review-w31-a.jsonl", 1, high)
        verdict(disc / "review-w31-a.jsonl", 1)
        findings_bytes = (disc / "review-w31-a.jsonl").read_bytes()
        denied = disc / "review-w31-n.jsonl"
        denied.write_bytes(findings_bytes)
        (disc / "review-w31-b.jsonl").write_bytes(BOM + findings_bytes)
        (disc / "review-w31-z.jsonl").write_bytes(b"")
        (disc / "review-w31-d.jsonl").mkdir()
        real_open = pathlib.Path.open

        def deny(self, *a, **k):
            if self == denied:
                raise PermissionError(13, "Permission denied", str(self))
            return real_open(self, *a, **k)

        import contextlib  # noqa: PLC0415
        import io  # noqa: PLC0415

        def cli(path: pathlib.Path) -> tuple[int, object]:
            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                rc = main(["last-verdict", str(path)])
            text = buf.getvalue().strip()
            prefix = "=== LAST VERDICT: "
            return rc, json.loads(text[len(prefix):-4]) if text.startswith(prefix) and text.endswith(" ===") else text

        pathlib.Path.open = deny
        try:
            listed = findings_files(tmp / "discovery")
            survey = {v["file"]: v for v in unanswered(listed)}
            answers = {name: cli(disc / name) for name in ("review-w31-n.jsonl", "review-w31-b.jsonl",
                                                           "review-w31-z.jsonl", "review-w31-d.jsonl")}
        except Exception as e:  # noqa: BLE001  (a raise is a failed check, never the self-test's end)
            listed, survey, answers = [], {}, {"raised": (1, repr(e))}
        finally:
            pathlib.Path.open = real_open
        for name, why in (("review-w31-n.jsonl", "cannot be read (PermissionError"),
                          ("review-w31-b.jsonl", "byte-order mark"), ("review-w31-z.jsonl", "holds no record"),
                          ("review-w31-d.jsonl", "cannot be read (")):
            path = disc / name
            v = survey.get(str(path)) or {}
            check(f"arm: DISCOVERY {name} ({why}) is LISTED and reads TORN, held, the tear named: {v}",
                  path in listed and (v.get("verdict"), v.get("unanswered"), v.get("file")) == ("TORN", True, str(path))
                  and why in v.get("torn", "") and why in v.get("reason", "")
                  and v.get("line") == (None if why.startswith("cannot") else 1))
            check(f"arm: DISCOVERY {name}: the CLI exits 0 with the same reading the survey holds: {answers.get(name)}",
                  answers.get(name) == (0, v) and v)
        check(f"arm: DISCOVERY the readable file beside them is still read: {survey.get(str(disc / 'review-w31-a.jsonl'))}",
              (survey.get(str(disc / "review-w31-a.jsonl")) or {}).get("verdict") == "FINDINGS")
        check(f"arm: DISCOVERY a BOM file names its branches and heads: {survey.get(str(disc / 'review-w31-b.jsonl'))}",
              ((survey.get(str(disc / "review-w31-b.jsonl")) or {}).get("branches"),
               (survey.get(str(disc / "review-w31-b.jsonl")) or {}).get("heads")) == (["bA31"], ["a1a1a1a"]))
        enq = tmp / "discovery" / "review-worktree-wf_q-9.jsonl"
        enq.mkdir()
        v = last_verdict(enq) or {}
        check(f"arm: DISCOVERY an enqueue unit's file that cannot be read is bound to its branch by its name: {v}",
              (v.get("verdict"), v.get("branches")) == ("TORN", ["worktree-wf_q-9"]))
        p = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "last-verdict",
                            str(disc / "review-w31-d.jsonl")], capture_output=True, text=True, encoding="utf-8")
        line = p.stdout.strip()
        check(f"arm: DISCOVERY the CLI as a process exits 0 with verdict TORN, reason, file and line: {p.returncode} {line}",
              p.returncode == 0 and line.startswith("=== LAST VERDICT: ")
              and json.loads(line[len("=== LAST VERDICT: "):-4]) == survey.get(str(disc / "review-w31-d.jsonl")))
        said = refusal(start, disc / "review-w31-z.jsonl", 1, "bZ", "a1a1a1a", "x")
        check(f"arm: DISCOVERY a writer refuses a zero-byte file, naming the repair: {said!r}",
              "holds no record" in said and "review_findings.py repair" in said)
        did = repair(disc / "review-w31-b.jsonl")
        check(f"arm: DISCOVERY `repair` removes a BOM and the records stand: {did}",
              did == ["removed: the UTF-8 byte-order mark"] and (disc / "review-w31-b.jsonl").read_bytes() == findings_bytes
              and last_verdict(disc / "review-w31-b.jsonl")["verdict"] == "FINDINGS")
        did = repair(disc / "review-w31-z.jsonl")
        check(f"arm: DISCOVERY `repair` deletes a file that holds no record, and round 1 starts again: {did}",
              len(did) == 2 and did[-1].startswith("deleted: ") and not (disc / "review-w31-z.jsonl").exists()
              and not refusal(start, disc / "review-w31-z.jsonl", 1, "bZ", "a1a1a1a", "x"))
        check("arm: DISCOVERY `repair` refuses a file it cannot read (a directory in its place), changing nothing",
              "cannot mend a file it cannot read" in refusal(repair, disc / "review-w31-d.jsonl")
              and (disc / "review-w31-d.jsonl").is_dir())
        # SCHEMA (kb/Work PB3381 review R6-2, the Mythos decision's part 3). Expected results from SCHEMA: a record
        # missing any required field, or with any field of the wrong type, reads TORN (held), and the writers refuse
        # to write it; a field SCHEMA does not name is no tear, and the verdict reads exactly as written.
        base = [{"kind": "start", "round": 1, "branch": "bS", "head": "a1a1a1a", "reviewer": "w9-review-r1", "at": "t"},
                {"kind": "finding", "round": 1, "id": "R1-1", "file": "x.py", "line": 3, "category": "c",
                 "severity": "high", "summary": "s", "failure_scenario": "x", "at": "t"},
                {"kind": "verdict", "round": 1, "branch": "bS", "head": "a1a1a1a", "reviewer": "w9-review-r1",
                 "verdict": "FINDINGS", "findings": 1, "blocking": ["critical", "high", "medium"], "leads": [], "at": "t"}]
        wrong = {"round": ["1", 0, True, None], "line": ["3", 0, 1.5], "findings": ["1", -1, None, True],
                 "head": ["zzz", 7, ""], "verdict": ["findings", "clean", 1], "blocking": ["high", [1]],
                 "leads": ["R1-1", [2]]}
        sf = tmp / "schema" / "review-w9-s.jsonl"
        sf.parent.mkdir(parents=True)

        def rows(recs: list[dict]) -> bytes:
            return "".join(json.dumps(r, ensure_ascii=False) + "\n" for r in recs).encode()

        mutations, missed = 0, []
        for i, rec in enumerate(base):
            for field in rec:
                if field == "kind":
                    continue
                cases = [] if field in OPTIONAL.get(rec["kind"], {}) else [("dropped", {k: v for k, v in rec.items() if k != field})]
                cases += [(f"= {bad!r}", {**rec, field: bad}) for bad in wrong.get(field, [7, "", None])]
                for what, mutant in cases:
                    mutations += 1
                    sf.write_bytes(rows([*base[:i], mutant, *base[i + 1:]]))
                    v = last_verdict(sf) or {}
                    torn_ok = (v.get("verdict"), v.get("unanswered"), v.get("line")) == ("TORN", True, i + 1)
                    writer_ok = field == "at" or bool(refusal(append, sf.parent / "fresh.jsonl",
                                                              {k: x for k, x in mutant.items() if k != "at"}))
                    if not (torn_ok and writer_ok):   # `append` stamps `at` itself, so a bad `at` never reaches it
                        missed.append((rec["kind"], field, what, v.get("verdict"), writer_ok))
        for what, recs in (("FINDINGS with findings 0", [*base[:2], {**base[2], "findings": 0}]),
                           ("CLEAN with findings 1", [*base[:2], {**base[2], "verdict": "CLEAN"}])):
            mutations += 1
            sf.write_bytes(rows(recs))
            if (last_verdict(sf) or {}).get("verdict") != "TORN":
                missed.append(("verdict", "verdict/findings", what, (last_verdict(sf) or {}).get("verdict")))
        check(f"arm: SCHEMA, {mutations} mutations (each required field dropped, each given a wrong type, a verdict that "
              f"disagrees with its count): each reads TORN at its line, held; missed {missed}",
              not missed and mutations >= 60)
        sf.write_bytes(rows([{**r, "zz": [1, {"x": None}]} for r in base]))
        v = last_verdict(sf)
        check(f"arm: SCHEMA, a field SCHEMA does not name is no tear: the verdict reads as written: {v}",
              (v["round"], v["verdict"], v["blocking"], v["unanswered"], v["torn"]) == (1, "FINDINGS", 1, True, ""))
        check("arm: SCHEMA, a writer refuses a record the reader would tear",
              "SCHEMA" in refusal(append, tmp / "schema" / "review-w9-w.jsonl",
                                  {"kind": "verdict", "round": 1, "branch": "b", "head": "a1a1a1a", "reviewer": "r",
                                   "verdict": "FINDINGS", "findings": 0})
              and not (tmp / "schema" / "review-w9-w.jsonl").exists())
        # THE PROPERTY (kb/Work PB3381 review R5-1, the Mythos decision's exhaustive proof): a findings file of seven
        # records (round 1 CLEAN; round 2 a high finding and FINDINGS; round 3 started and CLEAN; "§" in the branch and
        # the summary), cut at EVERY byte offset k from 0 to its length, alone and with one well-formed record glued
        # on without a separating newline (a start, a CLEAN verdict). The expected readings come from the rule, never
        # from the reader: after j whole records the newest verdict is SNAP[j], and a glued CLEAN round 9 at a record
        # boundary is a legitimate append. A cut that is not at a record boundary reads TORN, held, never a verdict
        # (the weaker "TORN or the whole records' verdict" passes the round-4 reader at probe case 3, whose whole
        # records ARE round 1 CLEAN). `mend` yields bytes every writer accepts (scan finds no tear: `load`'s and
        # `append`'s test) whose reading is SNAP of the whole records before the cut, plus the cut record when only
        # its newline was lost; at a boundary it changes nothing.
        pf = tmp / "property" / "review-w9-p.jsonl"
        br = "b§P"
        start(pf, 1, br, "a1a1a1a", "w9p-review-r1")
        verdict(pf, 1)
        start(pf, 2, br, "b2b2b2b", "w9p-review-r2")
        finding(pf, 2, json.dumps({"file": "x.py", "line": 1, "category": "c", "summary": "§14.9 wrong",
                                   "severity": "high", "failure_scenario": "x"}, ensure_ascii=False))
        verdict(pf, 2)
        start(pf, 3, br, "c3c3c3c", "w9p-review-r3")
        verdict(pf, 3)
        snap = [None, None, (1, "CLEAN", False), (1, "CLEAN", False), (1, "CLEAN", False), (2, "FINDINGS", True),
                (2, "FINDINGS", True), (3, "CLEAN", False)]
        data = pf.read_bytes()
        check("arm: the property's file holds its seven records, multi-byte characters among them",
              data.count(b"\n") == 7 and data.count("§".encode()) == 7)
        glue = (("alone", b"", None),
                ("start", json.dumps({"kind": "start", "round": 4, "branch": br, "head": "d4d4d4d",
                                      "reviewer": "w9p-review-r4", "at": now()}, ensure_ascii=False).encode() + b"\n",
                 None),
                ("clean", json.dumps({"kind": "verdict", "round": 9, "branch": br, "head": "d4d4d4d", "reviewer": "x",
                                      "verdict": "CLEAN", "findings": 0, "at": now()},
                                     ensure_ascii=False).encode() + b"\n",
                 (9, "CLEAN", False)))

        def said(v: dict | None) -> object:
            if v is None or not v["torn"]:
                return v and (v["round"], v["verdict"], v["unanswered"])
            return "TORN" if (v["verdict"], v["unanswered"]) == ("TORN", True) else f"TORN, released: {v}"

        cases, wrong = 0, []
        for k in range(len(data) + 1):
            pre = data[:k]
            j = pre.count(b"\n")
            boundary = k == 0 or pre.endswith(b"\n")
            for name, extra, appended in glue:
                cases += 1
                body = pre + extra
                want = (appended or snap[j]) if boundary and body else "TORN"   # an empty file holds no record: TORN
                got = said(reading(body, pf))
                mended, _ = mend(body, pf)
                removed = mended == b""   # no record is left: `repair` deletes the file, and the next `start` makes it
                want_mended = snap[j + 1] if name == "alone" and data[k:k + 1] == b"\n" else snap[j]
                if got != want or not (removed or not scan(mended, pf).torn) or (
                        mended != body if boundary else (None if removed else said(reading(mended, pf))) != want_mended):
                    wrong.append((k, name, got, want))
        check(f"arm: THE PROPERTY, {cases} cuts of {len(data)} bytes: each reads TORN or exactly its whole records' "
              f"verdict and mends to bytes every writer accepts; wrong: {len(wrong)}, first {wrong[:3]}",
              not wrong and cases == 3 * (len(data) + 1))
        s = summary([tmp])
        check(f"arm: the summary measures findings per branch and the re-review rate: {s}",
              s["branches"] == 1 and s["findings_per_branch"] == 1.0 and s["re_review_rate"] == 1.0 and s["clean"] == 1)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    for f in fails:
        print(f"FAIL  {f}")
    print(f"=== REVIEW_FINDINGS SELF-TEST: {'GREEN' if not fails else f'RED ({len(fails)})'} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if argv[:1] == ["--self-test"]:
        return self_test()
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("start"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int, required=True)
    p.add_argument("--branch", required=True); p.add_argument("--head", required=True); p.add_argument("--reviewer", required=True)
    p = sub.add_parser("finding"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int, required=True)
    p.add_argument("json")
    p = sub.add_parser("verdict"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int, required=True)
    p.add_argument("--blocking", default=",".join(BLOCKING_DEFAULT),
                   help=f"the severities that block (default {','.join(BLOCKING_DEFAULT)}; the rolling wave's round after "
                        f"its last review finisher passes {','.join(BLOCKING_AT_CAP)})")
    p = sub.add_parser("stamp"); p.add_argument("report", type=pathlib.Path); p.add_argument("--findings", type=pathlib.Path, required=True)
    p = sub.add_parser("show"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int)
    p = sub.add_parser("last-verdict"); p.add_argument("file", type=pathlib.Path)
    p = sub.add_parser("repair", help="print a torn findings file's tail and mend it (the operator, by hand)")
    p.add_argument("file", type=pathlib.Path)
    p = sub.add_parser("summary"); p.add_argument("paths", type=pathlib.Path, nargs="+")
    a = ap.parse_args(argv)
    try:
        if a.cmd == "start":
            start(a.file, a.round, a.branch, a.head, a.reviewer)
            print(f"started round {a.round} of {a.branch} at {a.head}")
        elif a.cmd == "finding":
            print(f"recorded {finding(a.file, a.round, a.json)['id']}")
        elif a.cmd == "verdict":
            v = verdict(a.file, a.round, blocking_set(a.blocking))
            print(f"=== BRANCH REVIEW: {v['verdict']}{'' if v['verdict'] == 'CLEAN' else ' ' + str(v['findings'])} "
                  f"(round {v['round']}, {v['branch']} at {v['head']}; blocking {','.join(v['blocking'])}; "
                  f"{len(v['leads'])} lead(s)) ===")
            for line in lead_lines(a.file, v):
                print(line)
        elif a.cmd == "stamp":
            print(stamp(a.report, a.findings))
        elif a.cmd == "show":
            print("\n".join(show(a.file, a.round)) or "(no findings)")
        elif a.cmd == "last-verdict":   # the line the rolling wave parses (wf_rolling_wave.js lastVerdict)
            print(f"=== LAST VERDICT: {json.dumps(last_verdict(a.file), ensure_ascii=False)} ===")
        elif a.cmd == "repair":
            did = repair(a.file)
            for line in did:
                print(line)
            print(f"=== REPAIR: {a.file}: {f'{len(did)} line(s) mended' if did else 'intact, left unchanged'} ===")
        else:
            print(json.dumps(summary(a.paths), indent=1))
    except RecordError as e:
        print(f"REFUSED: {e}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
