#!/usr/bin/env python3
"""fuzz_batch.py — write a differential-fuzz run's programs as ONE Message Batch, then judge them locally (kb/Work PB4506).

    python scripts/fuzz_batch.py submit  --out DIR --count 6000 --api-key-file KEYFILE [--model claude-sonnet-5-5] [--clauses ...] [--seed N]
    python scripts/fuzz_batch.py collect --out DIR --api-key-file KEYFILE [--no-wait]
    python scripts/differential_fuzz.py --writer files --replies DIR/replies --count 6000 --out DIR/judged --cobol JUDGE
    python scripts/fuzz_batch.py --self-test

WHY THREE STAGES (the owner, 2026-11-03: "I'm not sure why this is a serial task. We could generate, e.g. 6000 fuzz test
programs, then compile/run them, then analyze"). differential_fuzz.py interleaves the stages per program because its first
writer, a local model, often needed correction rounds. A Claude writer needed none in the 20-program calibration of
2026-11-03 (0 of 20 refused by both compilers), so the programs are written all at once through the Message Batches API,
which the API docs price at half the standard rate and which needs no client-side concurrency, and the judge then runs
over the written programs with every core of this machine.

ONE PLAN, TWO READERS. `submit` builds request i from `differential_fuzz.prompt_plan`, `seed_at` and `first_messages`, the
same functions the judge's row i reads, so a batch's `fz00042` and the judge's `fz00042` name the same clause and the same
prompt. The plan's settings (`--count`, `--clauses`, `--seed`, `--in-order`, the variations) are recorded in
`DIR/batch.json`, and `collect` prints the judge command that repeats them.

THE FILES. `DIR/batch.json` (the batch id, the model and the plan), `DIR/replies/<id>.json` (`text`, `stop_reason`, `usage`,
one per request that succeeded; an errored, canceled or expired request writes none, and the judge records its row as a
RUNNER_ERROR naming why) and `DIR/collect.json` (the counts and the token totals).

BILLING. The API key is read from `--api-key-file` (its first line) and handed to the SDK client only; it is never printed
or written. The system prompt of each clause family is marked for prompt caching, so the requests of one family share it.
"""
from __future__ import annotations

import argparse
import json
import sys
import tempfile
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable, Iterable

sys.path.insert(0, str(Path(__file__).resolve().parent))
from standard_streams import ensure_utf8  # noqa: E402  (scripts/: UTF-8 standard streams)

import differential_fuzz as df  # noqa: E402
import fuzz_claude  # noqa: E402  (THE reader of an API key file, read_api_key)

#: The default writer: the model of the 2026-11-03 calibration, chosen by the owner for the batch run.
DEFAULT_MODEL = "claude-sonnet-5-5"
#: Output tokens per program: thinking counts toward it, and a program of a few hundred lines fits well inside.
MAX_TOKENS = 16000
#: Programs asked per clause before the walk moves on (differential_fuzz.Settings.variations).
VARIATIONS = df.Settings.__dataclass_fields__["variations"].default
#: Seconds between status polls while a batch runs.
POLL_SECONDS = 60


def program_ids(count: int) -> list[str]:
    """The ids the judge gives programs 0 .. count-1 (differential_fuzz.Fuzz.one)."""
    return [f"fz{i:05d}" for i in range(count)]


def build_requests(count: int, model: str, prefixes: tuple[str, ...], seed: int, in_order: bool) -> list[dict[str, Any]]:
    """One Message Batches request per program, in the judge's order: the clause family's system prompt (cached) and the
    seed turn, exactly as the judge's first request would carry them."""
    seeds, _declined, system_prompts = df.prompt_plan(prefixes, seed, in_order)
    if not seeds:
        raise SystemExit(f"fuzz_batch: no clauses under {prefixes}")
    out = []
    for i, name in enumerate(program_ids(count)):
        number, title, body, variation = df.seed_at(seeds, VARIATIONS, i)
        _family, messages = df.first_messages(system_prompts, number, title, body, variation)
        system = [{"type": "text", "text": messages[0]["content"], "cache_control": {"type": "ephemeral"}}]
        out.append({"custom_id": name, "params": {"model": model, "max_tokens": MAX_TOKENS, "system": system,
                                                  "messages": [messages[1]]}})
    return out


def write_replies(results: Iterable[Any], replies: Path) -> dict[str, Any]:
    """Write each succeeded result's text to `replies`/<custom_id>.json and return the counts and token totals."""
    replies.mkdir(parents=True, exist_ok=True)
    counts: dict[str, int] = {}
    tokens = {"input_tokens": 0, "output_tokens": 0, "cache_creation_input_tokens": 0, "cache_read_input_tokens": 0}
    for r in results:
        kind = r.result.type
        counts[kind] = counts.get(kind, 0) + 1
        if kind != "succeeded":
            continue
        msg = r.result.message
        text = "".join(b.text for b in msg.content if getattr(b, "type", "") == "text")
        usage = {k: int(getattr(msg.usage, k, 0) or 0) for k in tokens}
        for k, v in usage.items():
            tokens[k] += v
        (replies / f"{r.custom_id}.json").write_text(
            json.dumps({"text": text, "stop_reason": msg.stop_reason, "usage": usage}, ensure_ascii=False),
            encoding="utf-8", newline="\n")
    return {"counts": counts, "tokens": tokens}


def client_for(key_file: str) -> Any:
    import anthropic  # noqa: PLC0415  (only the network commands need the SDK)
    try:
        key = fuzz_claude.read_api_key(key_file)
    except RuntimeError as e:
        raise SystemExit(f"fuzz_batch: {e}") from None
    return anthropic.Anthropic(api_key=key)


def submit(a: argparse.Namespace, client: Any = None) -> int:
    out = Path(a.out)
    if (out / "batch.json").exists():
        raise SystemExit(f"fuzz_batch: {out / 'batch.json'} exists: one batch per run directory")
    prefixes = tuple(p.strip() for p in a.clauses.split(",") if p.strip())
    requests = build_requests(a.count, a.model, prefixes, a.seed, a.in_order)
    record = {"batch_id": None, "model": a.model, "count": a.count, "clauses": list(prefixes), "seed": a.seed,
              "in_order": a.in_order, "variations": VARIATIONS, "max_tokens": MAX_TOKENS,
              "plan": df.plan_entries(prefixes, a.seed, a.in_order, VARIATIONS, a.count)}
    # The directory and the record exist BEFORE the billed batch does, and the id is written and printed the moment it
    # exists, so a failure after the create cannot lose the paid batch's id (review R1-3).
    out.mkdir(parents=True, exist_ok=True)
    (out / "batch.json").write_text(json.dumps(record, indent=1), encoding="utf-8", newline="\n")
    batch = (client or client_for(a.api_key_file)).messages.batches.create(requests=requests)
    print(f"submitted batch {batch.id}: {a.count} requests, model {a.model}, status {batch.processing_status}", flush=True)
    record.update(batch_id=batch.id, submitted_at=datetime.now(timezone.utc).isoformat(timespec="seconds"))
    (out / "batch.json").write_text(json.dumps(record, indent=1), encoding="utf-8", newline="\n")
    print(f"record {out / 'batch.json'}")
    return 0


def judge_command(out: Path) -> str:
    """The judge's command line: the plan (count, clauses, seed, order) comes from batch.json, never from the command."""
    return f"python scripts/differential_fuzz.py --writer files --replies \"{out / 'replies'}\" --out \"{out / 'judged'}\" --cobol <judge>"


def collect(a: argparse.Namespace, sleep: Callable[[float], None] = time.sleep, client: Any = None) -> int:
    out = Path(a.out)
    rec = json.loads((out / "batch.json").read_text(encoding="utf-8"))
    if not rec.get("batch_id"):
        raise SystemExit(f"fuzz_batch: {out / 'batch.json'} names no batch: its submit did not reach the API")
    client = client or client_for(a.api_key_file)
    while True:
        batch = client.messages.batches.retrieve(rec["batch_id"])
        c = batch.request_counts
        print(f"batch {batch.id}: {batch.processing_status} (processing {c.processing}, succeeded {c.succeeded}, "
              f"errored {c.errored}, canceled {c.canceled}, expired {c.expired})", flush=True)
        if batch.processing_status == "ended":
            break
        if a.no_wait:
            return 3
        sleep(POLL_SECONDS)
    summary = write_replies(client.messages.batches.results(rec["batch_id"]), out / "replies")
    summary["collected_at"] = datetime.now(timezone.utc).isoformat(timespec="seconds")
    (out / "collect.json").write_text(json.dumps(summary, indent=1), encoding="utf-8", newline="\n")
    print(f"collected: {summary['counts']}; tokens {summary['tokens']}")
    print("judge it with:\n  " + judge_command(out))
    return 0


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(f"  {'ok  ' if cond else 'FAIL'} {name}")
        if not cond:
            failures.append(name)

    prefixes = tuple(df.DEFAULT_CLAUSES)
    reqs = build_requests(7, DEFAULT_MODEL, prefixes, 20261020, False)
    seeds, _d, prompts = df.prompt_plan(prefixes, 20261020, False)
    same = True
    for i, r in enumerate(reqs):
        number, title, body, variation = df.seed_at(seeds, VARIATIONS, i)
        _f, msgs = df.first_messages(prompts, number, title, body, variation)
        same &= (r["custom_id"] == f"fz{i:05d}" and r["params"]["system"][0]["text"] == msgs[0]["content"]
                 and r["params"]["messages"] == [msgs[1]])
    check("request i carries the judge's id, system prompt and seed turn for program i", same)
    check("every request names the model, the token cap and a cached system prompt",
          all(r["params"]["model"] == DEFAULT_MODEL and r["params"]["max_tokens"] == MAX_TOKENS
              and r["params"]["system"][0]["cache_control"] == {"type": "ephemeral"} for r in reqs))

    class Obj:
        def __init__(self, **kw: Any) -> None:
            self.__dict__.update(kw)

    usage = Obj(input_tokens=10, output_tokens=200, cache_creation_input_tokens=0, cache_read_input_tokens=900)
    ok = Obj(custom_id="fz00000", result=Obj(type="succeeded", message=Obj(
        content=[Obj(type="thinking", thinking=""), Obj(type="text", text="PROGRAM")], stop_reason="end_turn", usage=usage)))
    bad = Obj(custom_id="fz00001", result=Obj(type="errored"))
    with tempfile.TemporaryDirectory() as tmp:
        replies = Path(tmp) / "replies"
        summary = write_replies([ok, bad], replies)
        got = json.loads((replies / "fz00000.json").read_text(encoding="utf-8"))
        check("a succeeded result is written with its text (thinking blocks skipped), stop reason and usage",
              got["text"] == "PROGRAM" and got["stop_reason"] == "end_turn" and got["usage"]["cache_read_input_tokens"] == 900)
        check("an errored result writes no reply and is counted", not (replies / "fz00001.json").exists()
              and summary["counts"] == {"succeeded": 1, "errored": 1} and summary["tokens"]["output_tokens"] == 200)
        s = types_ns(writer="files", replies=str(replies), model="m", temperature=0.3)
        reply = df.writer_reply(s, {}, [], "fz00000")
        check("the judge's files writer reads that reply back", reply == ("PROGRAM", 0.0, "stop", 0))
        try:
            df.writer_reply(s, {}, [], "fz00001")
            check("a request with no reply is a RuntimeError for the judge", False)
        except RuntimeError:
            check("a request with no reply is a RuntimeError for the judge", True)
        class FakeBatches:
            def __init__(self) -> None:
                self.polls = 0

            def create(self, requests: list[Any]) -> Any:
                assert (Path(tmp) / "run" / "batch.json").exists(), "the record exists before the billed batch"
                return Obj(id="msgbatch_test", processing_status="in_progress")

            def retrieve(self, _id: str) -> Any:
                self.polls += 1
                done = self.polls >= 2
                return Obj(id=_id, processing_status="ended" if done else "in_progress",
                           request_counts=Obj(processing=0 if done else 2, succeeded=1 if done else 0, errored=1 if done else 0,
                                              canceled=0, expired=0))

            def results(self, _id: str) -> list[Any]:
                return [ok, bad]

        fake = Obj(messages=Obj(batches=FakeBatches()))
        args = types_ns(out=str(Path(tmp) / "run"), count=2, model=DEFAULT_MODEL, clauses=",".join(df.DEFAULT_CLAUSES),
                        seed=20261020, in_order=False, api_key_file="unused", no_wait=False)
        submit(args, client=fake)
        rec = json.loads((Path(tmp) / "run" / "batch.json").read_text(encoding="utf-8"))
        check("submit records the batch id and every program's plan entry", rec["batch_id"] == "msgbatch_test"
              and sorted(rec["plan"]) == ["fz00000", "fz00001"] and not df.batch_plan_problems(rec))
        slept: list[float] = []
        check("collect polls until the batch ends, then writes the replies and the summary",
              collect(args, sleep=slept.append, client=fake) == 0 and slept == [POLL_SECONDS]
              and (Path(tmp) / "run" / "replies" / "fz00000.json").exists() and (Path(tmp) / "run" / "collect.json").exists())
        moved = dict(rec, seed=rec["seed"] + 1)
        check("a plan this tree builds differently is named (the judge refuses to start on it)", bool(df.batch_plan_problems(moved)))
        check("the judge command carries no plan of its own", "--seed" not in judge_command(Path(tmp)) and "--clauses" not in judge_command(Path(tmp)))
        refusal = Path(tmp) / "run" / "replies" / "fz00009.json"
        refusal.write_text(json.dumps({"text": "IDENTIFICATION DIVISION.", "stop_reason": "refusal"}), encoding="utf-8", newline="\n")
        try:
            df.writer_reply(types_ns(writer="files", replies=str(refusal.parent)), {}, [], "fz00009")
            check("a refused reply is no program", False)
        except RuntimeError:
            check("a refused reply is no program", True)
    print(f"=== FUZZ BATCH SELF-TEST: {'GREEN' if not failures else 'RED — ' + ', '.join(failures)} ===")
    return 1 if failures else 0


def types_ns(**kw: Any) -> Any:
    import types  # noqa: PLC0415
    return types.SimpleNamespace(**kw)


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("submit")
    s.add_argument("--out", required=True)
    s.add_argument("--count", type=int, required=True)
    s.add_argument("--api-key-file", required=True)
    s.add_argument("--model", default=DEFAULT_MODEL)
    s.add_argument("--clauses", default=",".join(df.DEFAULT_CLAUSES))
    s.add_argument("--seed", type=int, default=20261020)
    s.add_argument("--in-order", action="store_true")
    c = sub.add_parser("collect")
    c.add_argument("--out", required=True)
    c.add_argument("--api-key-file", required=True)
    c.add_argument("--no-wait", action="store_true", help="report the status and exit 3 while the batch still runs")
    a = ap.parse_args(argv)
    return submit(a) if a.cmd == "submit" else collect(a)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
