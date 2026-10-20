#!/usr/bin/env python3
"""differential_fuzz.py — a local model writes COBOL programs; WiseOwl and GnuCOBOL both compile and run each one; only
the disagreements survive to a triage report (kb/Work PB3463; the experiment record is
docs/rearchitecture/evidence/fleet-optimization/2026-10-20-differential-fuzz.md).

    python scripts/differential_fuzz.py --model qwen3-coder:30b --count 40 [--jobs 3] [--clauses 14.9,15] [--out DIR]
    python scripts/differential_fuzz.py --model qwen3-coder:30b --hours 8 --jobs 4           # an overnight run
    python scripts/differential_fuzz.py --bake-off qwen3-coder:30b,north-mini-code-1.0:q4_K_M --count 20
    python scripts/differential_fuzz.py --report DIR                                           # re-render DIR/report.md
    python scripts/differential_fuzz.py --self-test

WHAT THE MODEL IS FOR, AND WHAT IT IS NOT. The owner's rule (2026-10-03, kept by every local-model experiment since): the
ISO spec decides (CLAUDE.md rule 1); a local model produces LEADS, and nothing it writes enters `kb/Work/`, a golden or the
conformance suite without a Claude or human check against the standard. Here the model needs no judgment at all: it only
has to write plausible standard COBOL. Two compilers judge the program, and GnuCOBOL 3.2 is a NET, never authority — an
OUTPUT_DIFFERS row says the compilers disagree, not which one is right. The 2026-10-09 one-shot (kb/Work PB2886) sank on a
test program that used four constructs the standard does not have; here such a program is simply one GnuCOBOL refuses
(GNU_REJECTS), counted against the model and dropped.

THE MECHANICS ARE THE CONFORMANCE SUITE'S, NOT A SECOND COPY. Each program is compiled and run through
`conformance-suite/adapters/<compiler>.json` by the harness's own `Run.operation` (conformance-suite/harness/run.py), so the
command lines, the crash exit codes, the locale pin and the hang guard are the suite's. On Windows the GnuCOBOL steps cross
into WSL through the adapter's hook interface (`WslBridge`), as the adapter's own comment prescribes ("run it where cobc is:
on Windows, under WSL"). Both compilers run the 2014 edition, the latest GnuCOBOL 3.2 has, in free reference format.

THE VERDICT CLASSES, per program (one JSON line each in `results.jsonl`, appended as the run goes so a kill loses nothing):
  GNU_REJECTS      GnuCOBOL did not compile it: the model's program, not a lead. Counted against the model.
  AGREE            both compiled and ran, same normalized output (CutRunner.Normalize). Dropped.
  WE_REJECT        GnuCOBOL compiled it, WiseOwl refused: a candidate reject-legal-source lead — ours only where the
                   construct is ISO (GnuCOBOL's default dialect carries extensions, so `-std=cobol2014` is pinned).
  OUTPUT_DIFFERS   both ran, outputs differ: a candidate wrong answer on one side; the spec decides which.
  WE_CRASH         WiseOwl's compiler crashed (the adapter's crash exit codes), or an unhandled exception at run time.
  WE_HANG          WiseOwl's program exceeded the hang guard while GnuCOBOL's finished.
  THEY_FAIL_RUN    GnuCOBOL compiled it but its program failed at run time while ours ran: low value, kept for the record.
  THEY_HANG        GnuCOBOL's program exceeded the hang guard.
  RUNNER_ERROR     a compiler did not start or the harness could not observe it: never a compiler verdict (plan §11 A12e).
Survivors are grouped by SIGNATURE (class + WiseOwl's diagnostic codes, or class + clause) so one defect hit forty times
is one triage row. `report.md` is rendered at the end and every REPORT_EVERY programs.

Standard library only. Writes under `--out` (default conformance-suite/results/fuzz-<stamp>/, git-ignored); never touches
the repository's tracked files. The model is unloaded when the run ends (the GPU is the owner's and shared).
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import os
import re
import shutil
import sys
import threading
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO = Path(__file__).resolve().parent.parent
SUITE = REPO / "conformance-suite"
sys.path.insert(0, str(SUITE / "harness"))
import config as suite_config  # noqa: E402
import run as suite_run  # noqa: E402

SPEC = REPO / "specs" / "ISO_COBOL.md"
OLLAMA = os.environ.get("OLLAMA_HOST", "http://localhost:11434")
EDITION = "2014"            # the latest edition GnuCOBOL 3.2 has a dialect for; pinned on both compilers
SOURCE_FORMAT = "free"
HANG_GUARD = 90.0           # seconds per process: a generous limit, never a time expectation
NUM_PREDICT = 6000          # output tokens per reply; an 80-line free-format program is well under 2000
REPORT_EVERY = 25
DEFAULT_CLAUSES = ("14.9", "15", "13.18", "8.8")   # statements, intrinsic functions, data description clauses, arithmetic
#: the spec's own heading shape (one definition for the markdown transcription; the PDF's is clause_page.index)
HEADING = re.compile(r"^#{2,6} ((?:\d+|[A-Z])(?:\.\d+)+|\d+) (.*)$")
#: any info string: a model was seen to write "```cob注释"; the program is the fenced block that has a PROCEDURE DIVISION
CODE_FENCE = re.compile(r"```[^\n]*\n(.*?)(?:```|\Z)", re.DOTALL)   # a truncated reply never closes its fence
#: the structural sub-clauses every statement, function and clause has; each folds into its parent seed
STRUCTURAL = re.compile(r"^(General|General formats?|General forms?|Syntax rules|General rules|Format \d+.*|Arguments|Returned values?)\s*$",
                        re.IGNORECASE)
DIAG_CODE = re.compile(r"\b((?:COBOLNET|COBOL|CBL)\d{4})\b")

SYSTEM_PROMPT = """You write small, standalone COBOL test programs for a compiler differential. Rules:
- Standard COBOL only, as in ISO/IEC 1989:2014. No vendor extensions, no COPY, no CALL to other programs, no EXEC.
- Free reference format: EVERY line starts in column 1 with no indentation, one statement per line. At most 80 lines, and no repetitive filler data items: a few well-chosen items exercised many ways.
- The program must compile and run with no input: no ACCEPT from the console or standard input, no command-line arguments.
- Every result the program computes is shown with DISPLAY, so two compilers' outputs can be compared byte for byte.
- Output must be deterministic: no CURRENT-DATE, WHEN-COMPILED, RANDOM, SEED, time, locale or environment dependent values.
- No files unless the clause itself is about file handling; then ASSIGN each file to a plain name in the working directory, create it in the program, and DELETE FILE it before STOP RUN.
- Exercise the clause named below in several distinct ways: edge values, both branches of every optional phrase, size and sign boundaries, truncation, ROUNDED, nested forms. Prefer cases a compiler could get subtly wrong.
- Reply with exactly one COBOL program inside one ```cobol fenced block and nothing else. PROGRAM-ID is FUZZ."""


# ── the seeds: spec clauses ───────────────────────────────────────────────────────────────────────────────────────────

def clauses(text: str, prefixes: tuple[str, ...]) -> list[tuple[str, str, str]]:
    """(number, title, body) for every clause of the markdown whose number is under one of `prefixes` and whose body
    carries a general format or rules worth a program. A STRUCTURAL sub-clause (14.9.25.2 "General formats", 14.9.25.4
    "General rules", a function's "Arguments") is not a seed of its own: its text folds into its parent, so one seed is
    one statement, function or clause with its formats and rules together. A bare heading with no body is not a seed."""
    out: list[list[Any]] = []        # [number, title, [body lines]]
    cur: list[Any] | None = None

    def under(number: str) -> bool:
        return any(number == p or number.startswith(p + ".") for p in prefixes)

    for line in text.splitlines():
        m = HEADING.match(line)
        if m:
            number, title = m.group(1), m.group(2).strip()
            if cur is not None and STRUCTURAL.match(title) and number.startswith(cur[0] + "."):
                cur[2].append(f"\n{title}\n")
                continue
            cur = [number, title, []]
            if under(number):
                out.append(cur)
        elif cur is not None and not line.startswith("<a id="):
            cur[2].append(line)
    seeds = []
    for number, title, lines in out:
        body = "\n".join(lines).strip()
        if len(body) >= 200:
            seeds.append((number, title, body))
    return seeds


def seed_text(number: str, title: str, body: str, limit: int = 7000) -> str:
    return f"Clause {number} {title}\n\n{body[:limit]}"


# ── the model ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def ollama_chat(model: str, messages: list[dict[str, str]], temperature: float, keep_alive: str = "10m",
                timeout: float = 600.0) -> tuple[str, float, str]:
    """One chat completion over `messages` → (content, seconds, done_reason). `done_reason` is Ollama's: "stop" for a
    finished reply, "length" when NUM_PREDICT cut it off (the first smoke runs lost every program that way: a 3000-token cap
    and fixed-form indentation). A model that rejects the `think` field is asked again without it (the 2026-10-03 spike)."""
    body: dict[str, Any] = {"model": model, "stream": False, "think": False, "keep_alive": keep_alive,
                            "messages": messages,
                            "options": {"temperature": temperature, "num_ctx": 16384, "num_predict": NUM_PREDICT}}
    for attempt in (0, 1):
        start = time.monotonic()
        req = urllib.request.Request(f"{OLLAMA}/api/chat", json.dumps(body).encode("utf-8"),
                                     {"Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(req, timeout=timeout) as r:
                reply = json.loads(r.read())
            return reply["message"]["content"].replace("<|eot|>", ""), time.monotonic() - start, reply.get("done_reason", "stop")
        except urllib.error.HTTPError as e:
            msg = e.read()[:300]
            if attempt == 0 and b"think" in msg:
                body.pop("think")
                continue
            raise RuntimeError(f"ollama HTTP {e.code}: {msg!r}") from None
    raise AssertionError("unreachable")


def unload(model: str) -> None:
    """Free the GPU: a request with keep_alive 0 unloads the model (the GPU is shared with the owner's other work)."""
    try:
        req = urllib.request.Request(f"{OLLAMA}/api/generate", json.dumps({"model": model, "keep_alive": 0}).encode(),
                                     {"Content-Type": "application/json"})
        urllib.request.urlopen(req, timeout=60).read()
    except (urllib.error.URLError, OSError):
        pass


def extract_program(reply: str) -> str | None:
    """The fenced block of a reply that holds a PROCEDURE DIVISION (the longest, when several do), or the reply itself
    when it has one and no fence does."""
    blocks = [b for b in CODE_FENCE.findall(reply) if "PROCEDURE DIVISION" in b.upper()]
    text = max(blocks, key=len) if blocks else reply
    text = text.strip("\n")
    if "PROCEDURE DIVISION" not in text.upper():
        return None
    return text + "\n"


# ── the two compilers, through the suite's adapters ──────────────────────────────────────────────────────────────────

class WslBridge:
    """The GnuCOBOL adapter's hook on Windows: every operation runs inside WSL in the same working directory, which wsl.exe
    maps from the Windows path (E:\\x → /mnt/e/x); the run step addresses the program relative to it."""

    def __init__(self, distro: str = "Ubuntu") -> None:
        self.distro = distro

    def _execute(self, argv: tuple[str, ...], ctx: suite_config.StepContext) -> suite_config.Step:
        return suite_run.execute(("wsl", "-d", self.distro, "-e", *argv), ctx.work, ctx.environment, ctx.timeout)

    def compile(self, ctx: suite_config.StepContext) -> suite_config.Step:
        assert ctx.argv is not None
        return self._execute(ctx.argv, ctx)

    def flag(self, ctx: suite_config.StepContext) -> suite_config.Step:
        assert ctx.argv is not None
        return self._execute(ctx.argv, ctx)

    def run(self, ctx: suite_config.StepContext) -> suite_config.Step:
        return self._execute(("./" + ctx.placeholders["program"],), ctx)


def load_compiler(name: str, root: Path, work: Path, variables: dict[str, str], keep: bool) -> suite_run.Run:
    adapter, problems = suite_config.load_adapter(SUITE / "adapters" / f"{name}.json")
    if adapter is None:
        raise SystemExit("adapter problems: " + "; ".join(problems))
    profile, problems = suite_config.load_profile(SUITE / "profiles" / f"{adapter.profile}.json")
    if profile is None:
        raise SystemExit("profile problems: " + "; ".join(problems))
    if name == "gnucobol" and os.name == "nt":
        adapter = dataclasses.replace(adapter, hook=WslBridge())
    return suite_run.Run(root=root, adapter=adapter, profile=profile, variables={**adapter.variables, **variables},
                         tests={}, work=work, timeout=HANG_GUARD, keep=keep)


@dataclasses.dataclass
class Observation:
    """What one compiler did with one program."""
    compiled: bool | None          # None: not observed (RUNNER_ERROR)
    compile_rc: int | None
    diagnostics: str
    codes: list[str]
    ran: bool | None = None        # None: not attempted or not observed
    run_rc: int | None = None
    crashed: bool = False
    hung: bool = False
    output: str = ""
    runner_error: str = ""


def observe(run: suite_run.Run, entry: dict[str, Any], work: Path) -> Observation:
    """Compile, then run, one program through one compiler's adapter, as the suite's `accept` would, without judging."""
    source, program = run.stage(entry, work)
    step = run.operation("compile", EDITION, entry, work, source, program)
    if step.error is not None or step.timed_out:
        return Observation(None, step.returncode, step.text(), [], runner_error=run.unjudgeable(step, "the compiler") or "")
    diag = step.text()
    codes = sorted(set(DIAG_CODE.findall(diag)))
    if run.crashed(step):
        return Observation(False, step.returncode, diag, codes, crashed=True)
    if step.returncode != 0:
        return Observation(False, step.returncode, diag, codes)
    ran = run.operation("run", EDITION, entry, work, source, program)
    if ran.error is not None:
        return Observation(True, 0, diag, codes, ran=None, runner_error=f"the program did not start: {ran.error}")
    if ran.timed_out:
        return Observation(True, 0, diag, codes, ran=False, hung=True)
    out = suite_run.normalize(ran.stdout.decode("utf-8", errors="replace"))
    return Observation(True, 0, diag, codes, ran=True, run_rc=ran.returncode, crashed=run.crashed(ran), output=out)


# ── the verdict ───────────────────────────────────────────────────────────────────────────────────────────────────────

def classify(we: Observation, they: Observation) -> str:
    """The class of one program from the two observations. GnuCOBOL's refusal is judged first: a program it does not
    compile is the model's failure, whatever WiseOwl did with it."""
    if they.compiled is None:
        return "RUNNER_ERROR"
    if they.compiled is False:
        return "GNU_REJECTS"
    if we.compiled is None or (we.compiled and we.ran is None):
        return "RUNNER_ERROR"
    if we.crashed:
        return "WE_CRASH"
    if we.compiled is False:
        return "WE_REJECT"
    if we.hung:
        return "WE_HANG"
    if they.hung:
        return "THEY_HANG"
    if they.ran is None:
        return "RUNNER_ERROR"
    if they.run_rc != 0 and we.run_rc == 0:
        return "THEY_FAIL_RUN"
    if we.output == they.output:
        return "AGREE"
    return "OUTPUT_DIFFERS"


LEAD_CLASSES = ("WE_CRASH", "WE_REJECT", "OUTPUT_DIFFERS", "WE_HANG", "THEY_HANG", "THEY_FAIL_RUN")


def signature(verdict: str, clause: str, we: Observation) -> str:
    if verdict == "WE_REJECT" or verdict == "WE_CRASH":
        return verdict + ":" + (",".join(we.codes) or "no-code")
    return verdict + ":" + clause


# ── the run ───────────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclasses.dataclass
class Settings:
    model: str
    out: Path
    jobs: int
    count: int | None
    hours: float | None
    prefixes: tuple[str, ...]
    temperature: float
    cobol_exe: str
    keep: bool
    repairs: int = 1       # corrections offered after a GnuCOBOL refusal
    variations: int = 3    # programs asked per clause before the seed list wraps


class Fuzz:
    def __init__(self, s: Settings) -> None:
        self.s = s
        self.out = s.out
        self.programs = self.out / "programs"
        self.programs.mkdir(parents=True, exist_ok=True)
        self.results_path = self.out / "results.jsonl"
        self.lock = threading.Lock()
        self.seeds = clauses(SPEC.read_text(encoding="utf-8"), s.prefixes)
        if not self.seeds:
            raise SystemExit(f"no clauses under {s.prefixes} in {SPEC}")
        work = self.out / "work"
        self.we = load_compiler("wiseowl", self.out, work / "wiseowl", {"cobol": s.cobol_exe}, s.keep)
        self.they = load_compiler("gnucobol", self.out, work / "gnucobol", {}, s.keep)
        self.deadline = time.monotonic() + s.hours * 3600 if s.hours else None
        self.done = 0

    def seed_for(self, i: int) -> tuple[str, str, str, int]:
        n = len(self.seeds)
        number, title, body = self.seeds[(i // self.s.variations) % n]
        return number, title, body, i % self.s.variations

    def one(self, i: int) -> dict[str, Any]:
        number, title, body, variation = self.seed_for(i)
        name = f"fz{i:05d}"
        rec: dict[str, Any] = {"id": name, "clause": number, "title": title, "model": self.s.model,
                               "at": datetime.now(timezone.utc).isoformat(timespec="seconds")}
        user = seed_text(number, title, body) + f"\n\nThis is program {variation + 1} for this clause: exercise an aspect the previous programs would not."
        messages = [{"role": "system", "content": SYSTEM_PROMPT}, {"role": "user", "content": user}]
        src = self.programs / f"{name}.cob"
        rec["source"] = str(src.relative_to(self.out)).replace("\\", "/")
        entry = {"id": name, "source": rec["source"], "files": [], "source-format": SOURCE_FORMAT}
        rec["model_seconds"] = 0.0
        rec["repairs"] = 0
        they = Observation(None, None, "", [])
        # The first reply, then up to `repairs` corrections: a program GnuCOBOL refuses goes back to the model with the
        # diagnostics (a syntax slip is cheap to fix and costs a whole generation to discard). The verdict is the last
        # program's; every earlier one is kept as <name>.r<k>.cob for diagnosis.
        for attempt in range(self.s.repairs + 1):
            try:
                reply, secs, done = ollama_chat(self.s.model, messages, self.s.temperature)
            except (RuntimeError, urllib.error.URLError, OSError, TimeoutError) as e:
                rec.update(verdict="RUNNER_ERROR", reason=f"model: {e}")
                return rec
            rec["model_seconds"] = round(rec["model_seconds"] + secs, 1)
            (self.programs / f"{name}.reply{attempt}.txt").write_text(reply, encoding="utf-8", newline="\n")
            program = extract_program(reply)
            if program is None or done == "length":
                why = f"the reply was cut off at {NUM_PREDICT} tokens" if done == "length" else "no COBOL program in the reply"
                if attempt == self.s.repairs:
                    rec.update(verdict="GNU_REJECTS", reason=why, truncated=done == "length")
                    return rec
                ask = ("Your reply was cut off before the program ended. Reply with a SHORTER complete program: at most 50 lines, "
                       "no indentation, a few data items, one ```cobol fenced block." if done == "length" else
                       "That reply had no COBOL program. Reply with exactly one complete program in one ```cobol fenced block.")
                messages += [{"role": "assistant", "content": reply}, {"role": "user", "content": ask}]
                continue
            if attempt:
                rec["repairs"] = attempt
                shutil.copyfile(src, self.programs / f"{name}.r{attempt - 1}.cob")
            src.write_text(program, encoding="utf-8", newline="\n")
            shutil.rmtree(self.they.work / name, ignore_errors=True)
            they = observe(self.they, entry, self.they.work / name)
            if they.compiled is not False or attempt == self.s.repairs:
                break
            messages += [{"role": "assistant", "content": reply},
                         {"role": "user", "content": "GnuCOBOL 3.2 (-std=cobol2014, free format) refused that program:\n\n"
                          + "\n".join(they.diagnostics.strip().splitlines()[:30])
                          + "\n\nFix every error using only standard COBOL 2014 (do not drop the clause under test; replace a "
                            "construct the standard lacks with one it has). Reply with the whole corrected program in one ```cobol fenced block."}]
        if they.compiled is False:
            we = Observation(None, None, "", [])        # not compiled by us: GnuCOBOL already refused it
        else:
            we = observe(self.we, entry, self.we.work / name)
        verdict = classify(we, they)
        rec.update(verdict=verdict, signature=signature(verdict, number, we),
                   we={"compiled": we.compiled, "codes": we.codes,
                       "first_error": first_error(we.diagnostics) if we.compiled is False or we.crashed else "",
                       "run_rc": we.run_rc, "crashed": we.crashed, "hung": we.hung, "output": we.output[:4000],
                       "runner_error": we.runner_error},
                   they={"compiled": they.compiled,
                         "first_error": first_error(they.diagnostics) if they.compiled is False else "",
                         "run_rc": they.run_rc, "hung": they.hung, "output": they.output[:4000],
                         "runner_error": they.runner_error})
        if not self.s.keep:
            for r in (self.we, self.they):
                shutil.rmtree(r.work / name, ignore_errors=True)
        return rec

    def record(self, rec: dict[str, Any]) -> None:
        with self.lock:
            with self.results_path.open("a", encoding="utf-8", newline="\n") as f:
                f.write(json.dumps(rec, ensure_ascii=False) + "\n")
            self.done += 1
            print(f"[{self.done}] {rec['id']} {rec['clause']:<10} {rec['verdict']:<14} "
                  f"{rec.get('signature', rec.get('reason', ''))[:70]}", flush=True)
            if self.done % REPORT_EVERY == 0:
                write_report(self.out)

    def run(self) -> None:
        (self.out / "run.json").write_text(json.dumps({**dataclasses.asdict(self.s), "out": str(self.out),
                                                       "seeds": len(self.seeds), "edition": EDITION,
                                                       "started": datetime.now(timezone.utc).isoformat(timespec="seconds")},
                                                      indent=1), encoding="utf-8", newline="\n")
        total = self.s.count if self.s.count is not None else 10 ** 9
        print(f"model {self.s.model} · {len(self.seeds)} seed clauses under {self.s.prefixes} · jobs {self.s.jobs} · "
              f"{'count ' + str(self.s.count) if self.s.count is not None else f'{self.s.hours} h'} · out {self.out}", flush=True)
        try:
            with ThreadPoolExecutor(max_workers=self.s.jobs) as pool:
                pending = set()
                i = 0
                while i < total and (self.deadline is None or time.monotonic() < self.deadline):
                    while len(pending) < self.s.jobs and i < total:
                        pending.add(pool.submit(self.one, i))
                        i += 1
                    done_now = next(as_completed(pending))
                    pending.discard(done_now)
                    self.record(done_now.result())
                for fut in as_completed(pending):
                    self.record(fut.result())
        finally:
            unload(self.s.model)
            write_report(self.out)


def first_error(diag: str) -> str:
    for line in diag.splitlines():
        if "error" in line.lower():
            return line.strip()[:300]
    return diag.strip().splitlines()[0][:300] if diag.strip() else ""


# ── the report ────────────────────────────────────────────────────────────────────────────────────────────────────────

def read_results(out: Path) -> list[dict[str, Any]]:
    path = out / "results.jsonl"
    if not path.exists():
        return []
    rows = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.strip():
            rows.append(json.loads(line))
    return rows


def render_report(rows: list[dict[str, Any]], out: Path) -> str:
    counts: dict[str, int] = {}
    for r in rows:
        counts[r["verdict"]] = counts.get(r["verdict"], 0) + 1
    n = len(rows)
    judged = n - counts.get("RUNNER_ERROR", 0)
    rejected = counts.get("GNU_REJECTS", 0)
    model_secs = [r["model_seconds"] for r in rows if "model_seconds" in r]
    repaired = sum(1 for r in rows if r.get("repairs") and r["verdict"] != "GNU_REJECTS")
    lines = [f"# Differential fuzz — triage report", "",
             f"Programs: {n} · judged by GnuCOBOL: {judged} · GnuCOBOL accepted: {judged - rejected}"
             f" ({(judged - rejected) / judged:.0%} of judged; {repaired} of them after a repair round)" if judged else f"Programs: {n}",
             f"Model seconds per program (median): {sorted(model_secs)[len(model_secs) // 2]:.0f}" if model_secs else "",
             "", "| Class | Count |", "|---|---|"]
    lines += [f"| {k} | {v} |" for k, v in sorted(counts.items(), key=lambda kv: -kv[1])]
    lines += ["", "⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the",
              "compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A",
              "confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a",
              "suite case with its expected output derived from the standard, never copied from either compiler.", ""]
    groups: dict[str, list[dict[str, Any]]] = {}
    for r in rows:
        if r["verdict"] in LEAD_CLASSES:
            groups.setdefault(r.get("signature", r["verdict"]), []).append(r)
    lines.append(f"## Survivors: {sum(len(g) for g in groups.values())} programs in {len(groups)} signatures")
    for sig, g in sorted(groups.items(), key=lambda kv: (-len(kv[1]), kv[0])):
        r = g[0]
        lines += ["", f"### `{sig}` × {len(g)} — first: {r['id']} (clause {r['clause']} {r['title']})",
                  f"- programs: {', '.join(x['id'] for x in g[:12])}{' …' if len(g) > 12 else ''}",
                  f"- source: `{r.get('source', '')}`"]
        we, they = r.get("we", {}), r.get("they", {})
        if we.get("first_error"):
            lines.append(f"- WiseOwl: `{we['first_error']}`")
        if we.get("runner_error") or they.get("runner_error"):
            lines.append(f"- runner: {we.get('runner_error') or they.get('runner_error')}")
        if r["verdict"] in ("OUTPUT_DIFFERS", "THEY_FAIL_RUN"):
            lines.append("- outputs (WiseOwl | GnuCOBOL), first differing lines:")
            a, b = we.get("output", "").splitlines(), they.get("output", "").splitlines()
            shown = 0
            for k in range(max(len(a), len(b))):
                x, y = (a[k] if k < len(a) else "<end>"), (b[k] if k < len(b) else "<end>")
                if x != y:
                    lines.append(f"  - line {k + 1}: `{x[:120]}` | `{y[:120]}`")
                    shown += 1
                    if shown == 5:
                        break
            if they.get("run_rc") not in (None, 0):
                lines.append(f"  - GnuCOBOL run exit {they['run_rc']}: `{they.get('first_error', '')}`")
    runner = [r for r in rows if r["verdict"] == "RUNNER_ERROR"]
    if runner:
        lines += ["", f"## Runner errors: {len(runner)} (not compiler verdicts)"]
        lines += [f"- {r['id']}: {r.get('reason') or (r.get('we') or {}).get('runner_error') or (r.get('they') or {}).get('runner_error')}"
                  for r in runner[:20]]
    return "\n".join(line for line in lines if line is not None) + "\n"


def write_report(out: Path) -> Path:
    path = out / "report.md"
    path.write_text(render_report(read_results(out), out), encoding="utf-8", newline="\n")
    return path


# ── the bake-off ──────────────────────────────────────────────────────────────────────────────────────────────────────

def bake_off(models: list[str], base: Settings) -> None:
    """Each model writes `count` programs from the same seeds; the score is the share GnuCOBOL accepts and the
    seconds per program. The winner generates the overnight run."""
    table = []
    for m in models:
        s = dataclasses.replace(base, model=m, out=base.out / re.sub(r"[^A-Za-z0-9_.-]", "_", m))
        Fuzz(s).run()
        rows = read_results(s.out)
        judged = [r for r in rows if r["verdict"] != "RUNNER_ERROR"]
        accepted = [r for r in judged if r["verdict"] != "GNU_REJECTS"]
        secs = sorted(r["model_seconds"] for r in rows if "model_seconds" in r)
        leads = sum(1 for r in rows if r["verdict"] in LEAD_CLASSES)
        table.append((m, len(rows), len(accepted), len(judged), secs[len(secs) // 2] if secs else float("nan"), leads))
    lines = ["# Bake-off", "", "| Model | Programs | GnuCOBOL accepts | Median model s | Lead rows |", "|---|---|---|---|---|"]
    for m, n, acc, judged, med, leads in table:
        lines.append(f"| {m} | {n} | {acc}/{judged} ({acc / judged:.0%}) | {med:.0f} | {leads} |" if judged
                     else f"| {m} | {n} | 0/0 | - | {leads} |")
    text = "\n".join(lines) + "\n"
    (base.out / "bake-off.md").write_text(text, encoding="utf-8", newline="\n")
    print(text)


# ── self-test ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    rc = 0

    def check(name: str, ok: bool, got: object = None) -> None:
        nonlocal rc
        print(("  ok: " if ok else "  SELF-TEST FAILED: ") + name + ("" if ok else f" — got {got!r}"))
        rc |= 0 if ok else 1

    print("=== differential_fuzz.py --self-test ===")
    md = ("## 14 Procedure division\n<a id=\"x\"></a>\n#### 14.9.25 MOVE statement\n" + "body " * 60 +
          "\n##### 14.9.25.1 General\nshort\n#### 15.3 ABS function\n" + "rules " * 60 + "\n### 13.18.1 ALIGNED clause\n" + "x " * 150)
    got = clauses(md, ("14.9", "15"))
    check("seeds: clauses under the prefixes with a body, bare and short headings skipped",
          [c[0] for c in got] == ["14.9.25", "15.3"] and got[0][1] == "MOVE statement", got)
    check("seeds: a prefix matches whole components only (13.1 is not under 13.18 and 13.18 is not under 13.1)",
          [c[0] for c in clauses(md, ("13.1",))] == [] and [c[0] for c in clauses(md, ("13.18",))] == ["13.18.1"])
    folded = ("#### 14.9.25 MOVE statement\nintro\n##### 14.9.25.1 General\n" + "g " * 40 + "\n##### 14.9.25.2 General formats\n"
              + "f " * 40 + "\n###### 14.9.25.2.1 Format 1 (simple)\nfmt\n##### 14.9.25.4 General rules\n" + "r " * 40
              + "\n#### 14.9.26 MULTIPLY statement\n" + "m " * 120)
    got = clauses(folded, ("14.9",))
    check("seeds: the structural sub-clauses (General, General formats, Format n, General rules) fold into the statement's seed",
          [c[0] for c in got] == ["14.9.25", "14.9.26"] and "General rules" in got[0][2] and "fmt" in got[0][2]
          and got[0][1] == "MOVE statement", [(c[0], c[1]) for c in got])
    prog = extract_program("Here:\n```cobol\nIDENTIFICATION DIVISION.\nPROGRAM-ID. FUZZ.\nPROCEDURE DIVISION.\n STOP RUN.\n```\n")
    check("extract: the fenced program, newline-terminated", prog is not None and prog.startswith("IDENTIFICATION") and prog.endswith("STOP RUN.\n"), prog)
    odd = extract_program("不经.fromJson\n```cob注释\nIDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.\n```\n\n```text\nnotes\n```\n")
    check("extract: any fence info string (a model wrote one with CJK characters after cob); the block with the PROCEDURE DIVISION is taken, prose before it dropped",
          odd == "IDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.\n", odd)
    cut = extract_program("```cobol\nIDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nDISPLAY 1\nADD 1 TO")
    check("extract: a truncated reply's unterminated fence yields its program (the fence line itself is never part of it)",
          cut is not None and cut.startswith("IDENTIFICATION") and "```" not in cut, cut)
    check("extract: a reply without a PROCEDURE DIVISION is no program", extract_program("I cannot do that.") is None)
    check("extract: an unfenced program is accepted", extract_program("IDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.") is not None)

    ok_run = Observation(True, 0, "", [], ran=True, run_rc=0, output="A\n1")
    check("classify: GnuCOBOL's refusal is the model's failure, whatever we did",
          classify(Observation(None, None, "", []), Observation(False, 1, "x.cob:3: error: syntax", [])) == "GNU_REJECTS")
    check("classify: same output agrees", classify(ok_run, ok_run) == "AGREE")
    check("classify: different output differs", classify(dataclasses.replace(ok_run, output="A\n2"), ok_run) == "OUTPUT_DIFFERS")
    check("classify: our refusal of a program GnuCOBOL compiled", classify(Observation(False, 1, "e: error COBOL1234", ["COBOL1234"]), ok_run) == "WE_REJECT")
    check("classify: our compiler crash outranks the refusal", classify(Observation(False, -1, "", [], crashed=True), ok_run) == "WE_CRASH")
    check("classify: our run-time crash", classify(dataclasses.replace(ok_run, crashed=True, run_rc=3762504530), ok_run) == "WE_CRASH")
    check("classify: our hang", classify(Observation(True, 0, "", [], ran=False, hung=True), ok_run) == "WE_HANG")
    check("classify: their hang", classify(ok_run, Observation(True, 0, "", [], ran=False, hung=True)) == "THEY_HANG")
    check("classify: their run-time failure while ours ran", classify(ok_run, dataclasses.replace(ok_run, run_rc=1)) == "THEY_FAIL_RUN")
    check("classify: an unobserved compiler is never a verdict (plan §11 A12e)",
          classify(Observation(None, None, "", [], runner_error="did not start"), ok_run) == "RUNNER_ERROR"
          and classify(ok_run, Observation(None, None, "", [], runner_error="x")) == "RUNNER_ERROR"
          and classify(Observation(True, 0, "", [], ran=None, runner_error="x"), ok_run) == "RUNNER_ERROR")
    check("signature: a refusal groups by our codes, a difference by clause",
          signature("WE_REJECT", "14.9.25", Observation(False, 1, "", ["COBOL1234", "COBOLNET4001"])) == "WE_REJECT:COBOL1234,COBOLNET4001"
          and signature("OUTPUT_DIFFERS", "14.9.25", ok_run) == "OUTPUT_DIFFERS:14.9.25")

    rows = [{"id": "fz00000", "clause": "14.9.25", "title": "MOVE", "verdict": "AGREE", "model_seconds": 10},
            {"id": "fz00001", "clause": "14.9.25", "title": "MOVE", "verdict": "OUTPUT_DIFFERS", "signature": "OUTPUT_DIFFERS:14.9.25",
             "source": "programs/fz00001.cob", "model_seconds": 12,
             "we": {"output": "A\n1", "first_error": "", "codes": []}, "they": {"output": "A\n2", "run_rc": 0}},
            {"id": "fz00002", "clause": "15.3", "title": "ABS", "verdict": "GNU_REJECTS", "model_seconds": 8},
            {"id": "fz00003", "clause": "15.3", "title": "ABS", "verdict": "RUNNER_ERROR", "reason": "model: down"}]
    rep = render_report(rows, Path("."))
    check("report: counts, acceptance over judged programs only, the survivor group and the differing line",
          "GnuCOBOL accepted: 2 (67% of judged; 0 of them after a repair round)" in rep and "| AGREE | 1 |" in rep and "### `OUTPUT_DIFFERS:14.9.25` × 1" in rep
          and "line 2: `1` | `2`" in rep and "Runner errors: 1" in rep, rep)
    check("report: the triage rule is printed", "the spec decides" in rep)
    adapter, problems = suite_config.load_adapter(SUITE / "adapters" / "gnucobol.json")
    check("adapters: the suite's gnucobol adapter loads and has the 2014 edition this script pins",
          adapter is not None and EDITION in adapter.editions, problems)
    adapter_w, problems = suite_config.load_adapter(SUITE / "adapters" / "wiseowl.json")
    check("adapters: the suite's wiseowl adapter loads and names its `cobol` variable",
          adapter_w is not None and "cobol" in adapter_w.variables and EDITION in adapter_w.editions, problems)
    bridge = WslBridge("Ubuntu")
    ctx = suite_config.StepContext("run", ("E:/w/fz00001",), Path("."), {}, 1.0, {"program": "fz00001", "work": "E:/w"})
    seen: list[tuple[str, ...]] = []
    orig = suite_run.execute
    try:
        suite_run.execute = lambda argv, work, env, timeout: (seen.append(argv), suite_config.Step(0))[1]  # type: ignore[assignment]
        bridge.run(ctx)
        bridge.compile(suite_config.StepContext("compile", ("cobc", "-x", "fz00001.cob"), Path("."), {}, 1.0, {}))
    finally:
        suite_run.execute = orig
    check("wsl bridge: the run addresses the program relative to the mapped working directory; compile passes the argv through",
          seen == [("wsl", "-d", "Ubuntu", "-e", "./fz00001"), ("wsl", "-d", "Ubuntu", "-e", "cobc", "-x", "fz00001.cob")], seen)
    print("self-test " + ("PASSED" if rc == 0 else "FAILED"))
    return rc


# ── main ──────────────────────────────────────────────────────────────────────────────────────────────────────────────

def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--model", help="the Ollama model tag that writes the programs")
    ap.add_argument("--bake-off", help="comma-separated model tags; each writes --count programs, scored by GnuCOBOL acceptance")
    ap.add_argument("--count", type=int, help="programs to write (per model in a bake-off)")
    ap.add_argument("--hours", type=float, help="stop starting new programs after this many hours")
    ap.add_argument("--jobs", type=int, default=2, help="programs in flight at once (Ollama queues the generations)")
    ap.add_argument("--clauses", default=",".join(DEFAULT_CLAUSES), help="comma-separated clause prefixes to seed from")
    ap.add_argument("--temperature", type=float, default=0.6)
    ap.add_argument("--repairs", type=int, default=1, help="corrections offered to the model after a GnuCOBOL refusal (0: none)")
    ap.add_argument("--cobol", default=str(REPO / "src/Cobol.Net.Cli/bin/Debug/net10.0" / ("cobol.exe" if os.name == "nt" else "cobol")),
                    help="the WiseOwl compiler to test")
    ap.add_argument("--out", help="the run directory (default conformance-suite/results/fuzz-<stamp>)")
    ap.add_argument("--keep", action="store_true", help="keep every program's working directories")
    ap.add_argument("--report", help="re-render DIR/report.md from DIR/results.jsonl and exit")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.report:
        print(write_report(Path(a.report)))
        return 0
    if not (a.model or a.bake_off):
        ap.error("--model or --bake-off is required")
    if a.count is None and a.hours is None:
        ap.error("--count or --hours is required")
    if not Path(a.cobol).exists():
        ap.error(f"compiler not found: {a.cobol} (build src/Cobol.Net.Cli first, or pass --cobol)")
    out = Path(a.out) if a.out else SUITE / "results" / f"fuzz-{datetime.now().strftime('%Y%m%d-%H%M%S')}"
    s = Settings(model=a.model or "", out=out, jobs=a.jobs, count=a.count, hours=a.hours,
                 prefixes=tuple(p.strip() for p in a.clauses.split(",") if p.strip()), temperature=a.temperature,
                 cobol_exe=str(Path(a.cobol).resolve()), keep=a.keep, repairs=max(0, a.repairs))
    if a.bake_off:
        bake_off([m.strip() for m in a.bake_off.split(",") if m.strip()], s)
    else:
        Fuzz(s).run()
    print(f"report: {out / 'report.md'}")
    return 0


if __name__ == "__main__":
    # A Windows console in cp1252 cannot print the report's and a model's non-Latin characters; never die on a print.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    sys.exit(main(sys.argv[1:]))
