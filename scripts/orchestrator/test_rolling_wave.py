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
  3. a predecessor whose review ended FINDINGS at the cap is not ready, and its successor (and the successor's reviewer)
     are told those findings belong to the predecessor's next-wave finisher; a predecessor whose review FINISHER came
     back unfinished has no stamp holding its successor, so the successor's reviewer reviews the predecessor's content
     and records again each finding that still holds (review R1-1), never "do not record them again";
  4. the reviews' leads reach the wave's result.
No clock decides anything: each stub agent yields to the event loop once and returns its scripted result.
"""
import json
import pathlib
import shutil
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
WAVE = HERE.parents[1] / ".claude" / "skills" / "workstream" / "templates" / "wf_rolling_wave.js"

# The runtime the Workflow tool gives a script, as stubs. A label with no scripted result throws, as a dead agent does.
HARNESS = r"""
import { readFileSync } from 'node:fs'
const src = readFileSync(process.argv[2], 'utf8').replace(/^export const meta/m, 'const meta')
const scenario = JSON.parse(readFileSync(process.argv[3], 'utf8'))
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


def run(tmp: pathlib.Path, name: str, scenario: dict) -> dict:
    (tmp / f"{name}.json").write_text(json.dumps(scenario), encoding="utf-8", newline="\n")
    p = subprocess.run(["node", str(tmp / "harness.mjs"), str(tmp / "wave.js"), str(tmp / f"{name}.json")],
                       capture_output=True, text=True, encoding="utf-8")
    if p.returncode != 0:
        fails.append(f"{name}: node exited {p.returncode}: {p.stderr.strip()[-800:]}")
        return {"out": {"ready": [], "results": [], "leads": []}, "events": [], "prompts": {}}
    return json.loads(p.stdout.strip().splitlines()[-1])


def at(events: list[str], prefix: str) -> int:
    """The index of the first event starting with `prefix`, or a large number when none does."""
    return next((i for i, e in enumerate(events) if e.startswith(prefix)), 10 ** 9)


def main() -> int:
    if shutil.which("node") is None:
        print("FAIL: node is not on PATH (the wave script runs under node here; this file declares the platforms that carry it)")
        return 1
    tmp = pathlib.Path(tempfile.mkdtemp(prefix="rolling-wave-test-"))
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
              "git merge bA" in bp and "head a1" in bp and "lands ON ITS OWN" in bp and "landed-via-predecessor" not in bp, bp)
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
              "git merge bA2" in bp and "head a2" in bp, bp)
        r2 = (pr.get("w9a-review-r2") or [""])[0]
        check("2: round 2 reviews only the finisher's diff from the head round 1 reviewed",
              "git diff a1..HEAD" in r2 and "not re-opened unless the diff touches it" in r2, r2)
        check("2: round 2 is not the capped round (review_fix_rounds 2): it blocks on medium too",
              "--blocking critical,high,medium" in r2, r2)
        check("2: the finished predecessor (its finisher's branch) and the successor are ready",
              [x["branch"] for x in r["out"]["ready"]] == ["bA2", "bB"], r["out"]["ready"])

        # 3. at the cap: medium stops blocking; a predecessor still FINDINGS is not ready, and its findings stay its own
        r = run(tmp, "cap", {"args": args(groups, review_fix_rounds=1), "agents": {
            "impl-A-PB1": [impl("bA", "a1")], "w9a-review-r1": [review("FINDINGS", "a1", 1)],
            "fix-A-r1": [impl("bA2", "a2")], "w9a-review-r2": [review("FINDINGS", "a2", 1)],
            "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}})
        ev, pr = r["events"], r["prompts"]
        r2 = (pr.get("w9a-review-r2") or [""])[0]
        check("3: the round after the last finisher blocks on critical,high only",
              "--blocking critical,high`" in r2 and "--blocking critical,high,medium" not in r2, r2)
        check("3: a predecessor still FINDINGS at the cap is not ready; its successor is",
              [x["branch"] for x in r["out"]["ready"]] == ["bB"], r["out"]["ready"])
        bp = (pr.get("impl-B-PB2") or [""])[0]
        check("3: the successor is told the predecessor's findings belong to its next-wave finisher",
              "git merge bA2" in bp and "FINDINGS 1" in bp and "NEXT-WAVE FINISHER" in bp, bp)
        check("3: the successor's reviewer does not record the predecessor's findings again",
              "do not record them again" in (pr.get("w9b-review-r1") or [""])[0], pr.get("w9b-review-r1"))
        res = {x["letter"]: x for x in r["out"]["results"]}
        check("3: the predecessor's result says FINDINGS after its review rounds",
              (res["A"].get("review"), res["A"].get("review_rounds")) == ("FINDINGS 1", 2), res.get("A"))

        # 3a. kb/Work PB3381 review R1-1: a predecessor whose review FINISHER came back unfinished (SPLIT on its own
        #     branch, or with no new commit) ended FINDINGS before the cap. The finisher rewrote the report for its own
        #     branch with no Review line, so no stamp holds the successor (prune_worktrees.carrying_findings reads only a
        #     stamp): the successor's review must cover the predecessor's content and record those findings again.
        for name, fix in (("unanswered-split", impl("bA2", "a2", "SPLIT")), ("unanswered-nocommit", impl("bA", "a1", "BLOCKED"))):
            r = run(tmp, name, {"args": args(groups), "agents": {
                "impl-A-PB1": [impl("bA", "a1")], "w9a-review-r1": [review("FINDINGS", "a1", 2)],
                "fix-A-r1": [fix], "impl-B-PB2": [impl("bB", "b1")], "w9b-review-r1": [review("CLEAN", "b1")]}})
            ev, pr = r["events"], r["prompts"]
            res = {x["letter"]: x for x in r["out"]["results"]}
            check(f"3a {name}: the predecessor is not ready and ended FINDINGS with its finisher's status",
                  [x["branch"] for x in r["out"]["ready"]] == ["bB"] and res["A"].get("review") == "FINDINGS 2"
                  and res["A"].get("finisher") == fix["status"], (r["out"]["ready"], res.get("A")))
            bp = (pr.get("impl-B-PB2") or [""])[0]
            check(f"3a {name}: the successor merges the predecessor's final branch and is told the findings are UNANSWERED, "
                  "not its next-wave finisher's",
                  f"git merge {fix['branch']}" in bp and "UNANSWERED" in bp and "review-w9-a.jsonl" in bp
                  and "NEXT-WAVE FINISHER" not in bp and "prune_worktrees.py holds" not in bp, bp)
            rp = (pr.get("w9b-review-r1") or [""])[0]
            check(f"3a {name}: the successor's reviewer reviews the predecessor's content and records those findings again",
                  "record again here each of those findings that still holds" in rp and "review-w9-a.jsonl" in rp
                  and "do not record them again" not in rp and "not re-opened" not in rp, rp)

        # 3b. an unfinished predecessor settles when it returns: its successor starts at once and merges what it left
        r = run(tmp, "split", {"args": args(groups), "agents": {
            "impl-A-PB1": [impl("bA", "a1", "SPLIT")], "impl-B-PB2": [impl("bB", "b1")],
            "w9b-review-r1": [review("CLEAN", "b1")]}})
        bp = (r["prompts"].get("impl-B-PB2") or [""])[0]
        check("3b: a SPLIT predecessor is never reviewed, and its successor merges its branch",
              "start:w9a-review-r1" not in r["events"] and "git merge bA" in bp and "unfinished" in bp, bp)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    for f in fails:
        print("FAIL:", f)
    print(f"rolling wave self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
