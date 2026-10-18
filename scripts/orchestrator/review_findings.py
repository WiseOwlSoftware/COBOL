#!/usr/bin/env python3
"""The branch review's record (kb/Work PB3027; DESIGN-test-build-ci.md section 3.17.2 step 2): one JSON line per
decision in `<scratch>/review-w<wave>-<letter>.jsonl`, the verdict computed from it, and the stamp in the report.

    python scripts/orchestrator/review_findings.py start   <file> --round 1 --branch B --head SHA --reviewer LABEL
    python scripts/orchestrator/review_findings.py finding <file> --round 1 '<one JSON object>'
    python scripts/orchestrator/review_findings.py verdict <file> --round 1 [--blocking critical,high,medium]
                                                                                    # CLEAN | FINDINGS n, from the file
    python scripts/orchestrator/review_findings.py stamp   <report> --findings <file>
    python scripts/orchestrator/review_findings.py show    <file> [--round N]     # the findings, for the finisher
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
recorded before severities were required carries none and BLOCKS: an unknown severity is never waved through.

`stamp` writes the verdict into the implementer's report header as the line plan_wave.REVIEW reads (CLEAN <reviewer>
<head>, or FINDINGS <n> <reviewer> <head> <file>), and reads it back through plan_wave.parse_review, so the writer and
the reader cannot drift. prune_worktrees.py lists a DONE branch WAITING TO LAND only under a CLEAN stamp at its tip;
plan_wave.py plans a finisher for a FINDINGS stamp.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import re
import shutil
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import plan_wave  # noqa: E402  (the one reader of the stamp, and the one definition of a stamp line: PB3077)

KINDS = ("start", "finding", "verdict")
FINDING_FIELDS = ("file", "line", "category", "severity", "summary", "failure_scenario")
#: The severities a finding may carry, most severe first (the review skill's branch form, step 3).
SEVERITIES = ("critical", "high", "medium", "low", "info")
#: The blocking set of every round a review finisher can still answer, and of the round after the last one.
BLOCKING_DEFAULT = ("critical", "high", "medium")
BLOCKING_AT_CAP = ("critical", "high")
REVIEW_LINE = plan_wave.REVIEW_LINE   # the planner skips these lines when it reads the branch (kb/Work PB3077)


class RecordError(ValueError):
    pass


def now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")


def load(path: pathlib.Path) -> list[dict]:
    """Every record, in order. A malformed line is an error, never skipped: a lost finding would read CLEAN."""
    if not path.is_file():
        return []
    out = []
    for i, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        try:
            rec = json.loads(line)
        except ValueError as e:
            raise RecordError(f"{path}:{i}: not JSON ({e})") from e
        if not isinstance(rec, dict) or rec.get("kind") not in KINDS:
            raise RecordError(f"{path}:{i}: not a review record (kind must be one of {', '.join(KINDS)})")
        out.append(rec)
    return out


def append(path: pathlib.Path, rec: dict) -> dict:
    path.parent.mkdir(parents=True, exist_ok=True)
    rec = {**rec, "at": now()}
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
    if not isinstance(body["line"], int) or body["line"] < 1:
        raise RecordError("`line` is the 1-based line the finding anchors to (an integer)")
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
                if sev is None:   # a record from before severities were required: written as such, never via finding()
                    append(path, {**body, "kind": "finding", "round": 1, "id": f"R1-{k}"})
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
        _, v = one_round("x", ["high", "medium", "low", None])
        check(f"arm: the count is the blocking findings only; a finding with no severity blocks: {v}",
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
        bad = tmp / "bad.jsonl"
        bad.write_text('{"kind":"start","round":1}\nnot json\n', encoding="utf-8", newline="\n")
        check("arm: a malformed line is an error, never skipped (a lost finding would read CLEAN)", refused(load, bad))
        bad.unlink()
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
        else:
            print(json.dumps(summary(a.paths), indent=1))
    except RecordError as e:
        print(f"REFUSED: {e}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
