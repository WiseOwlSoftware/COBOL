#!/usr/bin/env python3
# SELF-TEST-PLATFORM: windows — it runs the wave script under node, which the Windows hosts (where Claude Code's Workflow runtime runs it) and CI's Windows job carry and the WSL distribution scripts/wsl/setup-wsl.sh provisions does not
"""THE ROLLING WAVE, RUN: wf_rolling_wave.js executed under node with stub agents (kb/Work PB3381).

    python scripts/orchestrator/test_rolling_wave.py

The wave script had no harness: its rules were pinned only as source text (test_plan_wave.py), so the hold that kept a
finished, CLEAN predecessor off main behind its same-file successor (wave 1118: six DONE groups, none landed in seven
hours) was never seen to run. This runs the script itself, as the Workflow runtime does (its body as an async function
of `args`, `agent`, `parallel`, `log` and `phase`), against scripted agent results keyed by each agent's label, and
checks the ORDER of what happened. Expected results come from the rules (MANDATORY-PRACTICES O2 and I10, the workstream
skill section 2), never from what the script does:
  1. a predecessor reviewed CLEAN is in `ready` before its successor's implementer starts, and the successor is handed
     the predecessor's FINAL branch and head;
  2. a successor waits for the predecessor's whole review loop, and merges the review FINISHER's branch when one ran;
     round 2 reviews only the finisher's diff from the head round 1 reviewed; the blocking set is critical,high,medium
     until the round after the last finisher, which blocks on critical,high only;
  3. a FINDINGS end is read back from the predecessor's FINDINGS FILE through the one reader the survey's hold reads
     too (review_findings.py last-verdict, run for real here on files the review's own tool writes; review R3-1): a
     predecessor CAPPED at the round after its last finisher is not ready, and its successor (and the successor's
     reviewer) are told those findings belong to the predecessor's next-wave finisher; a predecessor whose review
     finisher came back without answering them (SPLIT on a head of its own; dead, STALLED or BLOCKED with no commit
     after rewriting the report), or whose findings file is TORN, is UNANSWERED: the successor's reviewer reviews the
     predecessor's content and records again each finding that still holds (review R1-1), never "do not record them
     again", and both are told the survey holds the successor. A TORN file (review R4-1, R5-1, R6-1) is one the reader
     cannot open (gone, a directory in its place) or whose content is not records (a torn last line, a byte-order mark,
     a record SCHEMA tears): no verdict is read from it, the CLI exits 0 with verdict TORN, and it is never read as
     capped;
  4. the reviews' leads reach the wave's result;
  5. a GATE HANDED TO THE WAVE (kb/Work PB3536): an agent that returns GATE-PENDING with its gate's run directory is
     awaited through the gate's own reader (`run_gate_legs.py --await`, run for real here on planted run directories):
     GREEN continues as DONE (the finished test, then the review), RED goes to a gate finisher whose branch is then
     reviewed, a run that never writes a verdict ends STALLED after the await windows, and a review finisher's
     GATE-PENDING settles the same way before the next review round.
No clock decides anything: each stub agent yields to the event loop once and returns its scripted result.
"""
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import review_findings  # noqa: E402  (the findings file's own writer and its one reader, kb/Work PB3381 review R3-1)

WAVE = HERE.parents[1] / ".claude" / "skills" / "workstream" / "templates" / "wf_rolling_wave.js"

# The runtime the Workflow tool gives a script, as stubs. A label with no scripted result throws, as a dead agent does.
HARNESS = r"""
import { readFileSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
const src = readFileSync(process.argv[2], 'utf8').replace(/^export const meta/m, 'const meta')
const scenario = JSON.parse(readFileSync(process.argv[3], 'utf8'))
const [python, reviewFindings, runGateLegs] = [process.argv[4], process.argv[5], process.argv[6]]
// the one mechanical agent of awaitGate runs the REAL `run_gate_legs.py --await` on the run directory its prompt names
// (kb/Work PB3536): the wave and every other reader of a gate read one verdict file through one function
function gateAwaitAgent(prompt) {
  const [, run, worktree, wait] = /--await "([^"]+)" --worktree "([^"]*)" --wait-s (\S+)/.exec(prompt)
  // run from another directory than the worktree, as the wave's agent runs from the main checkout (review R1-1)
  const p = spawnSync(python, [runGateLegs, '--await', run, '--worktree', worktree, '--wait-s', wait],
                      { encoding: 'utf8', cwd: process.env.HARNESS_ELSEWHERE })
  return { exit: p.status, line: (p.stdout || p.stderr || '').trim().split(/\r?\n/).pop() || '' }
}
// the one mechanical agent of lastVerdict runs the REAL reader on the findings file its prompt names (kb/Work PB3381
// review R3-1): the wave and the survey read one file through one function, so the harness does not script it
function lastVerdictAgent(prompt) {
  const file = /last-verdict "([^"]+)"/.exec(prompt)[1]
  const p = spawnSync(python, [reviewFindings, 'last-verdict', file], { encoding: 'utf8' })
  return { exit: p.status, line: (p.stdout || p.stderr || '').trim().split(/\r?\n/).pop() || '' }
}
const events = []
const prompts = {}
const queues = Object.fromEntries(Object.entries(scenario.agents).map(([k, v]) => [k, [...v]]))
async function agent(prompt, opts) {
  const label = opts.label
  events.push('start:' + label)
  ;(prompts[label] = prompts[label] || []).push(prompt)
  await new Promise(r => setImmediate(r))
  let res
  if (label.startsWith('finished-')) res = { exit: 0, line: '=== REPORT: FINISHED ===' }
  else if (label.startsWith('last-verdict-')) res = lastVerdictAgent(prompt)
  else if (label.startsWith('gate-await-')) res = gateAwaitAgent(prompt)
  else {
    const q = queues[label]
    if (!q || !q.length) throw new Error('no scripted result for ' + label)
    res = q.shift()
  }
  events.push('end:' + label)
  return res
}
const parallel = fns => Promise.all(fns.map(f => f()))
const log = m => events.push('log:' + m)
const phase = () => {}
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor
const out = await new AsyncFunction('args', 'agent', 'parallel', 'log', 'phase', src)(scenario.args, agent, parallel, log, phase)
console.log(JSON.stringify({ out, events, prompts }))
"""

fails: list[str] = []
checked: list[str] = []


def check(name: str, ok: bool, detail: object = "") -> None:
    checked.append(name)
    if not ok:
        fails.append(f"{name}: {detail}")


def args(groups: list[dict], **extra) -> dict:
    return {"scratch": "S", "wave": "9", "stop_file": "S/STOP-w9", "global_stop": "C/STOP", "concurrency": 2,
            "implementer_ceiling_min": 0, "reviewer_ceiling_min": 0, "groups": groups, **extra}


def impl(branch: str, head: str, status: str = "DONE") -> dict:
    return {"status": status, "branch": branch, "worktree": f"W/{branch}", "base": "b0", "head": head,
            "report": f"R/{branch}-report.md", "gate_verdict": "GREEN", "summary": "s"}


def review(verdict: str, head: str, findings: int = 0, leads: list[str] | None = None) -> dict:
    return {"verdict": verdict, "findings": findings, "head": head, "stamp": f"**Review:** {verdict}",
            "leads": leads or []}


def findings_file(path: pathlib.Path, rounds: list[tuple[str, str, list[str]]], fix_rounds: int = 2) -> None:
    """The findings file the scripted reviews would have written, through the review's own tool: one round per
    (branch, head, severities), each verdict under the blocking set the wave passes for that round."""
    for k, (branch, head, severities) in enumerate(rounds, 1):
        review_findings.start(path, k, branch, head, f"w9a-review-r{k}")
        for n, sev in enumerate(severities, 1):
            review_findings.finding(path, k, json.dumps({"file": "x.py", "line": n, "category": "correctness",
                                                         "summary": "s", "severity": sev, "failure_scenario": "x"}))
        review_findings.verdict(path, k, review_findings.BLOCKING_AT_CAP if k > fix_rounds else review_findings.BLOCKING_DEFAULT)


def run(tmp: pathlib.Path, name: str, scenario: dict, findings: list | None = None, torn: bool = False,
        damage: str = "") -> dict:
    """Run one scenario; `findings` are group A's review rounds, written to the scenario's own scratch. `torn`: a next
    round is started after them and its first finding is cut off mid-append (a reviewer killed while writing).
    `damage`: the written file is given a UTF-8 byte-order mark (`bom`, a hand edit) or replaced by a directory
    (`dir`, a file the reader cannot open)."""
    scratch = tmp / name
    scratch.mkdir()
    scenario["args"]["scratch"] = str(scratch)
    if findings:
        findings_file(scratch / "review-w9-a.jsonl", findings, scenario["args"].get("review_fix_rounds", 2))
    if torn:
        branch, head, _ = findings[-1]
        review_findings.start(scratch / "review-w9-a.jsonl", len(findings) + 1, branch, head, "w9a-review-torn")
        with (scratch / "review-w9-a.jsonl").open("a", encoding="utf-8", newline="\n") as h:
            h.write('{"kind": "finding", "round": ' + str(len(findings) + 1) + ', "file": "x.py", "li')
    if damage == "bom":
        (scratch / "review-w9-a.jsonl").write_bytes(review_findings.BOM + (scratch / "review-w9-a.jsonl").read_bytes())
    elif damage == "dir":
        (scratch / "review-w9-a.jsonl").unlink()
        (scratch / "review-w9-a.jsonl").mkdir()
    (tmp / f"{name}.json").write_text(json.dumps(scenario), encoding="utf-8", newline="\n")
    p = subprocess.run(["node", str(tmp / "harness.mjs"), str(tmp / "wave.js"), str(tmp / f"{name}.json"),
                        sys.executable, str(HERE / "review_findings.py"), str(HERE.parent / "run_gate_legs.py")],
                       capture_output=True, text=True, encoding="utf-8",
                       env={**os.environ, "HARNESS_ELSEWHERE": str(scratch)})   # the gate's reader runs from here
    if p.returncode != 0:
        fails.append(f"{name}: node exited {p.returncode}: {p.stderr.strip()[-800:]}")
        return {"out": {"ready": [], "results": [], "leads": []}, "events": [], "prompts": {}}
    return json.loads(p.stdout.strip().splitlines()[-1])


def at(events: list[str], prefix: str) -> int:
    """The index of the first event starting with `prefix`, or a large number when none does."""
    return next((i for i, e in enumerate(events) if e.startswith(prefix)), 10 ** 9)


def main() -> int:
    sys.path.insert(0, str(HERE.parent))
    from scratch_repo import remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    if shutil.which("node") is None:
        print("FAIL: node is not on PATH (the wave script runs under node here; this file declares the platforms that carry it)")
        return 1
    tmp = scratch_dir("rolling-wave-test-")
    try:
        (tmp / "harness.mjs").write_text(HARNESS, encoding="utf-8", newline="\n")
        (tmp / "wave.js").write_text(WAVE.read_text(encoding="utf-8"), encoding="utf-8", newline="\n")
        groups = [{"letter": "A", "lead": "PB1", "notes": "PB1", "codes": "C1"},
                  {"letter": "B", "lead": "PB2", "notes": "PB2", "codes": "C2", "after": "A"}]

        # 1. a predecessor reviewed CLEAN lands at once: it is in `ready` before its successor even starts
        r = run(tmp, "clean", {"args": args(groups), "agents": {
            "impl-A-PB1": [impl("bA", "a1")], "w9a-review-r1": [review("CLEAN", "a1", leads=["R1-1 LEAD (low): x.py:3 s"])],
            "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}})
        ev, pr = r["events"], r["prompts"]
        check("1: the predecessor is reviewed before its successor starts",
              at(ev, "end:w9a-review-r1") < at(ev, "start:impl-B-PB2"), ev)
        check("1: the predecessor is READY for the landing lane before its successor starts (no hold)",
              at(ev, "log:A: DONE; 1 branch(es) reviewed CLEAN") < at(ev, "start:impl-B-PB2"), ev)
        check("1: both branches are ready, the predecessor first",
              [x["branch"] for x in r["out"]["ready"]] == ["bA", "bB"], r["out"]["ready"])
        bp = (pr.get("impl-B-PB2") or [""])[0]
        check("1: the successor merges the predecessor's reviewed head and is told it lands on its own",
              "merge_drivers.py merge bA" in bp and "head a1" in bp and "lands ON ITS OWN" in bp and "landed-via-predecessor" not in bp, bp)
        check("1: round 1 blocks on critical,high,medium and reviews the whole branch",
              "--blocking critical,high,medium" in (pr.get("w9a-review-r1") or [""])[0] and "git diff" not in (pr.get("w9a-review-r1") or [""])[0],
              (pr.get("w9a-review-r1") or [""])[0])
        check("1: the successor's reviewer does not re-open the predecessor's reviewed content",
              "reviewed CLEAN" in (pr.get("w9b-review-r1") or [""])[0] and "not re-opened" in (pr.get("w9b-review-r1") or [""])[0], (pr.get("w9b-review-r1") or [""])[0])
        check("4: the reviews' leads reach the wave's result",
              r["out"].get("leads") == ["A (bA): R1-1 LEAD (low): x.py:3 s"], r["out"].get("leads"))

        # 2. the successor waits for the predecessor's whole review loop and merges the review finisher's branch
        r = run(tmp, "finisher", {"args": args(groups), "agents": {
            "impl-A-PB1": [impl("bA", "a1")], "w9a-review-r1": [review("FINDINGS", "a1", 2)],
            "fix-A-r1": [impl("bA2", "a2")], "w9a-review-r2": [review("CLEAN", "a2")],
            "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}})
        ev, pr = r["events"], r["prompts"]
        check("2: the successor starts only after the predecessor's last review round",
              at(ev, "end:w9a-review-r2") < at(ev, "start:impl-B-PB2"), ev)
        bp = (pr.get("impl-B-PB2") or [""])[0]
        check("2: the successor merges the review finisher's branch, the reviewed head",
              "merge_drivers.py merge bA2" in bp and "head a2" in bp, bp)
        r2 = (pr.get("w9a-review-r2") or [""])[0]
        check("2: round 2 reviews only the finisher's diff from the head round 1 reviewed",
              "git diff a1..HEAD" in r2 and "not re-opened unless the diff touches it" in r2, r2)
        check("2: round 2 is not the capped round (review_fix_rounds 2): it blocks on medium too",
              "--blocking critical,high,medium" in r2, r2)
        check("2: the finished predecessor (its finisher's branch) and the successor are ready",
              [x["branch"] for x in r["out"]["ready"]] == ["bA2", "bB"], r["out"]["ready"])

        A1, A2 = "a1a1a1a", "a2a2a2a"   # commit-shaped heads: the findings file records them (review_findings.start)
        # 3. at the cap: medium stops blocking; a predecessor still FINDINGS is not ready, and its findings stay its own.
        #    The findings file's last verdict is the round after the last finisher (round 2 of review_fix_rounds 1):
        #    nobody was sent to answer it, so it is CAPPED, and the survey holds the successor through that file.
        cap_rounds = [("bA", A1, ["high"]), ("bA2", A2, ["high"])]
        r = run(tmp, "cap", {"args": args(groups, review_fix_rounds=1), "agents": {
            "impl-A-PB1": [impl("bA", A1)], "w9a-review-r1": [review("FINDINGS", A1, 1)],
            "fix-A-r1": [impl("bA2", A2)], "w9a-review-r2": [review("FINDINGS", A2, 1)],
            "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}}, cap_rounds)
        ev, pr = r["events"], r["prompts"]
        r2 = (pr.get("w9a-review-r2") or [""])[0]
        check("3: the round after the last finisher blocks on critical,high only",
              "--blocking critical,high`" in r2 and "--blocking critical,high,medium" not in r2, r2)
        check("3: a predecessor still FINDINGS at the cap is not ready; its successor is",
              [x["branch"] for x in r["out"]["ready"]] == ["bB"], r["out"]["ready"])
        check("3: the predecessor's FINDINGS end is read back from its findings file before its successor starts",
              at(ev, "end:last-verdict-A") < at(ev, "start:impl-B-PB2") < 10 ** 9, ev)
        bp = (pr.get("impl-B-PB2") or [""])[0]
        check("3: the successor is told the predecessor's findings belong to its next-wave finisher, held through the file",
              "merge_drivers.py merge bA2" in bp and "FINDINGS 1" in bp and "NEXT-WAVE FINISHER" in bp and "prune_worktrees.py holds" in bp
              and "review-w9-a.jsonl" in bp and "UNANSWERED" not in bp, bp)
        check("3: the successor's reviewer does not record the predecessor's findings again",
              "do not record them again" in (pr.get("w9b-review-r1") or [""])[0], pr.get("w9b-review-r1"))
        res = {x["letter"]: x for x in r["out"]["results"]}
        check("3: the predecessor's result says FINDINGS after its review rounds, with the file's last verdict",
              (res["A"].get("review"), res["A"].get("review_rounds"), (res["A"].get("last_verdict") or {}).get("round"))
              == ("FINDINGS 1", 2, 2), res.get("A"))
        # ... and a last verdict that cannot be read (no findings file) is never taken for the cap: unanswered
        r = run(tmp, "cap-unread", {"args": args(groups, review_fix_rounds=1), "agents": {
            "impl-A-PB1": [impl("bA", A1)], "w9a-review-r1": [review("FINDINGS", A1, 1)],
            "fix-A-r1": [impl("bA2", A2)], "w9a-review-r2": [review("FINDINGS", A2, 1)],
            "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}})
        bp = (r["prompts"].get("impl-B-PB2") or [""])[0]
        lv = {x["letter"]: x for x in r["out"]["results"]}.get("A", {}).get("last_verdict") or {}
        check("3: with no findings file the reader answers TORN (cannot be read) and the successor is told UNANSWERED, "
              "never capped", (lv.get("verdict"), "cannot be read" in (lv.get("reason") or ""), "UNANSWERED" in bp,
                               "NEXT-WAVE FINISHER" in bp) == ("TORN", True, True, False), (lv, bp))
        # ... and a findings file the survey's discovery once dropped (review R6-1): a byte-order mark, a directory in
        # its place. The CLI exits 0 with verdict TORN for each (never exit 1), and the wave reads it UNANSWERED
        for damage, why in (("bom", "byte-order mark"), ("dir", "cannot be read")):
            r = run(tmp, f"cap-{damage}", {"args": args(groups, review_fix_rounds=1), "agents": {
                "impl-A-PB1": [impl("bA", A1)], "w9a-review-r1": [review("FINDINGS", A1, 1)],
                "fix-A-r1": [impl("bA2", A2)], "w9a-review-r2": [review("FINDINGS", A2, 1)],
                "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}}, cap_rounds, damage=damage)
            bp = (r["prompts"].get("impl-B-PB2") or [""])[0]
            lv = {x["letter"]: x for x in r["out"]["results"]}.get("A", {}).get("last_verdict") or {}
            check(f"3: a findings file with {why} reads TORN through the CLI (exit 0), and the successor is told "
                  "UNANSWERED", (lv.get("verdict"), why in (lv.get("reason") or ""), "UNANSWERED" in bp,
                                 "NEXT-WAVE FINISHER" in bp) == ("TORN", True, True, False), (lv, bp))
        # ... and a TORN findings file (review R4-1, R5-1): the reader reads no verdict from it (verdict TORN, `torn`
        # set), and the wave never reads it capped: the successor is told UNANSWERED
        r = run(tmp, "cap-torn", {"args": args(groups, review_fix_rounds=1), "agents": {
            "impl-A-PB1": [impl("bA", A1)], "w9a-review-r1": [review("FINDINGS", A1, 1)],
            "fix-A-r1": [impl("bA2", A2)], "w9a-review-r2": [review("FINDINGS", A2, 1)],
            "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}}, cap_rounds, torn=True)
        bp = (r["prompts"].get("impl-B-PB2") or [""])[0]
        lv = {x["letter"]: x for x in r["out"]["results"]}.get("A", {}).get("last_verdict") or {}
        check("3: a torn findings file reads TORN, never its capped verdict, and the successor is told UNANSWERED",
              (lv.get("verdict"), lv.get("unanswered"), bool(lv.get("torn")), "UNANSWERED" in bp, "NEXT-WAVE FINISHER" in bp)
              == ("TORN", True, True, True, False), (lv, bp))
        check("1, 2: a CLEAN end reads no findings file (no extra agent)",
              all(not e.startswith("start:last-verdict") for e in run(tmp, "clean-lv", {"args": args(groups), "agents": {
                  "impl-A-PB1": [impl("bA", A1)], "w9a-review-r1": [review("CLEAN", A1)],
                  "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}})["events"]))

        # 3a. kb/Work PB3381 review R1-1, R3-1: a predecessor whose review FINISHER was sent to answer the findings and
        #     came back without answering them. Its findings file's last verdict is still round 1's FINDINGS, and the
        #     finisher rewrote the group's one report for its own branch with no Review line. Whatever it left (a SPLIT
        #     head of its own; or NO head of its own: killed, STALLED, BLOCKED with no commit), the predecessor is
        #     UNANSWERED: the successor's review covers the predecessor's content and records those findings again, and
        #     the successor is told the survey holds it, because the survey reads the same findings file.
        one_round = [("bA", A1, ["high", "high"])]
        r = run(tmp, "unanswered-split", {"args": args(groups), "agents": {
            "impl-A-PB1": [impl("bA", A1)], "w9a-review-r1": [review("FINDINGS", A1, 2)],
            "fix-A-r1": [impl("bA2", A2, "SPLIT")], "impl-B-PB2": [impl("bB", "b1")],
            "w9b-review-r1": [review("CLEAN", "b1")]}}, one_round)
        ev, pr = r["events"], r["prompts"]
        res = {x["letter"]: x for x in r["out"]["results"]}
        check("3a unanswered-split: the predecessor is not ready and ended FINDINGS on its finisher's head",
              [x["branch"] for x in r["out"]["ready"]] == ["bB"] and res["A"].get("review") == "FINDINGS 2"
              and res["A"].get("finisher") == "SPLIT" and res["A"].get("head") == A2, (r["out"]["ready"], res.get("A")))
        bp = (pr.get("impl-B-PB2") or [""])[0]
        check("3a unanswered-split: the successor merges the finisher's branch and is told the findings are UNANSWERED "
              "and that the survey holds it through the findings file",
              "merge_drivers.py merge bA2" in bp and "UNANSWERED" in bp and "review-w9-a.jsonl" in bp
              and "NEXT-WAVE FINISHER" not in bp and "prune_worktrees.py holds your branch" in bp, bp)
        rp = (pr.get("w9b-review-r1") or [""])[0]
        check("3a unanswered-split: the successor's reviewer reviews the predecessor's content and records those findings again",
              "record again here each of those findings that still holds" in rp and "review-w9-a.jsonl" in rp
              and "do not record them again" not in rp and "not re-opened" not in rp, rp)

        # 3a'. the same with a finisher that left NO head of its own (review R2-1, R3-1): it died (no scripted result:
        #     NO-RESULT), it was recorded STALLED, or it returned BLOCKED with no new commit after REWRITING the report
        #     for its own branch (its contract). The result is the reviewed head, but no report stamps the findings any
        #     more: deciding "capped" from the head (R2-1's fix) told the successor the survey held it while the
        #     survey, reading the report, held nothing. Both now read the findings file: UNANSWERED, held.
        for name, fix in (("dead-finisher", None), ("stalled-finisher", {"status": "STALLED"}),
                          ("nocommit-finisher", impl("bA", A1, "BLOCKED"))):
            agents = {"impl-A-PB1": [impl("bA", A1)], "w9a-review-r1": [review("FINDINGS", A1, 2)],
                      "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}
            if fix:
                agents["fix-A-r1"] = [fix]
            r = run(tmp, name, {"args": args(groups), "agents": agents}, one_round)
            ev, pr = r["events"], r["prompts"]
            res = {x["letter"]: x for x in r["out"]["results"]}
            want = fix["status"] if fix else "NO-RESULT"
            check(f"3a' {name}: the predecessor is not ready and ended FINDINGS at its reviewed head",
                  [x["branch"] for x in r["out"]["ready"]] == ["bB"] and res["A"].get("review") == "FINDINGS 2"
                  and res["A"].get("finisher") == want and res["A"].get("head") == A1, (r["out"]["ready"], res.get("A")))
            bp = (pr.get("impl-B-PB2") or [""])[0]
            check(f"3a' {name}: the successor merges the reviewed branch and is told the findings are UNANSWERED and held "
                  "through the findings file, never its next-wave finisher's alone",
                  "merge_drivers.py merge bA" in bp and "head a1" in bp and "UNANSWERED" in bp and "prune_worktrees.py holds your branch" in bp
                  and want in bp and "review-w9-a.jsonl" in bp and "NEXT-WAVE FINISHER" not in bp, bp)
            rp = (pr.get("w9b-review-r1") or [""])[0]
            check(f"3a' {name}: the successor's reviewer reviews the predecessor's content and records those findings again",
                  "record again here each of those findings that still holds" in rp and "do not record them again" not in rp, rp)

        # 5. A GATE HANDED TO THE WAVE (kb/Work PB3536). Planted run directories: a GREEN verdict, a RED one, and one
        #    whose gate process (this test's own, alive throughout) never writes a verdict.
        runs = tmp / "gate-runs"
        sys.path.insert(0, str(HERE.parent))
        import run_gate_legs  # noqa: PLC0415  (the gate's reader: its PENDING exit is the one the wave waits through)
        js_pending = re.search(r"^const AWAIT_PENDING_EXIT = (\d+)$", WAVE.read_text(encoding="utf-8"), re.M)
        check("5: the wave's AWAIT_PENDING_EXIT is run_gate_legs.AWAIT_PENDING_EXIT, never BUSY's exit",
              bool(js_pending) and int(js_pending.group(1)) == run_gate_legs.AWAIT_PENDING_EXIT != run_gate_legs.EXIT_BUSY,
              js_pending and js_pending.group(1))

        def gate_run(name: str, verdict: str | None, exit_code: int = 0) -> str:
            run = runs / name
            run.mkdir(parents=True)
            (run / "gate.pid").write_text(json.dumps({"pid": os.getpid()}), encoding="utf-8", newline="\n")
            if verdict:
                (run / "verdict.json").write_text(json.dumps({"verdict": verdict, "line": f"=== BUILD-LOCAL GATE: {verdict} — planted ===",
                                                             "exit_code": exit_code}), encoding="utf-8", newline="\n")
            return str(run)

        def pending(branch: str, head: str, run: str) -> dict:
            return {**impl(branch, head, "GATE-PENDING"), "gate_run": run, "gate_verdict": "pending"}

        solo = [{"letter": "A", "lead": "PB1", "notes": "PB1", "codes": "C1"}]
        gate_args = {"gate_await_wait_s": 0, "gate_await_windows": 2}
        green_run = gate_run("green", "LEG 1 ONLY (batched gating, PB2515): GREEN")
        # the form an agent returns (review R1-1 of wave 1152): the run RELATIVE to its worktree, read from elsewhere
        r = run(tmp, "gate-green", {"args": args(solo, **gate_args), "agents": {
            "impl-A-PB1": [{**pending("bA", "a1", "green"), "worktree": str(runs)}],
            "w9a-review-r1": [review("CLEAN", "a1")]}})
        ev, res = r["events"], {x["letter"]: x for x in r["out"]["results"]}
        check("5: GATE-PENDING -> GREEN: the wave awaits the handed gate's verdict file (named relative to the agent's "
              "worktree, read from another directory), then the finished test and the review run as for DONE, and the "
              "branch is ready",
              at(ev, "end:gate-await-implementer-1") < at(ev, "start:finished-implementer") < at(ev, "start:w9a-review-r1") < 10 ** 9
              and [x["branch"] for x in r["out"]["ready"]] == ["bA"] and res["A"].get("status") == "DONE"
              and res["A"].get("claimed") == "GATE-PENDING" and "GREEN — planted" in (res["A"].get("gate_verdict") or ""),
              (ev, res.get("A")))
        red_run = gate_run("red", "LEG 1 ONLY (batched gating, PB2515): RED", 1)
        r = run(tmp, "gate-red", {"args": args(solo, **gate_args), "agents": {
            "impl-A-PB1": [pending("bA", "a1", red_run)], "gatefix-A-r1": [impl("bA3", "a3")],
            "w9a-review-r1": [review("CLEAN", "a3")]}})
        ev, pr = r["events"], r["prompts"]
        gp = (pr.get("gatefix-A-r1") or [""])[0]
        check("5: GATE-PENDING -> RED: a gate finisher merges the branch and is told the gate's verdict, and ITS branch "
              "is reviewed (never the red head)",
              at(ev, "end:gate-await-implementer-1") < at(ev, "start:gatefix-A-r1") < at(ev, "start:w9a-review-r1") < 10 ** 9
              and "merge_drivers.py merge bA" in gp and red_run in gp and "RED — planted" in gp
              and [x["branch"] for x in r["out"]["ready"]] == ["bA3"], (ev, gp))
        stuck_run = gate_run("stuck", None)
        r = run(tmp, "gate-stuck", {"args": args(solo, **gate_args), "agents": {
            "impl-A-PB1": [pending("bA", "a1", stuck_run)]}})
        ev, res = r["events"], {x["letter"]: x for x in r["out"]["results"]}
        check("5: a handed gate that never writes a verdict ends STALLED after the await windows: never reviewed, "
              "never re-gated by the wave",
              res["A"].get("status") == "STALLED" and sum(e.startswith("start:gate-await-") for e in ev) == 2
              and not any(e.startswith("start:w9a-review") or e.startswith("start:gatefix") for e in ev)
              and not r["out"]["ready"], (ev, res.get("A")))
        r = run(tmp, "gate-review-finisher", {"args": args(solo, **gate_args), "agents": {
            "impl-A-PB1": [impl("bA", "a1")], "w9a-review-r1": [review("FINDINGS", "a1", 1)],
            "fix-A-r1": [pending("bA2", "a2", green_run)], "w9a-review-r2": [review("CLEAN", "a2")]}})
        ev = r["events"]
        check("5: a review finisher's GATE-PENDING settles the same way before the next review round",
              at(ev, "end:gate-await-review finisher round 1-1") < at(ev, "start:w9a-review-r2") < 10 ** 9
              and [x["branch"] for x in r["out"]["ready"]] == ["bA2"], ev)

        # 3b. an unfinished predecessor settles when it returns: its successor starts at once and merges what it left
        r = run(tmp, "split", {"args": args(groups), "agents": {
            "impl-A-PB1": [impl("bA", "a1", "SPLIT")], "impl-B-PB2": [impl("bB", "b1")],
            "w9b-review-r1": [review("CLEAN", "b1")]}})
        bp = (r["prompts"].get("impl-B-PB2") or [""])[0]
        check("3b: a SPLIT predecessor is never reviewed, and its successor merges its branch",
              "start:w9a-review-r1" not in r["events"] and "merge_drivers.py merge bA" in bp and "unfinished" in bp, bp)
    finally:
        remove_tree(tmp)
    for f in fails:
        print("FAIL:", f)
    print(f"rolling wave self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
