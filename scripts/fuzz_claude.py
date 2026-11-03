#!/usr/bin/env python3
"""fuzz_claude.py — the CLAUDE program writer of the differential fuzz (kb/Work PB4504): the twin of fuzz_ollama.py's
`ollama_chat`, for a run where a Claude model, not a local one, writes the programs (the owner, 2026-11-03: "I would
expect the cloud to not use a local model but use an appropriate Claude model").

    python scripts/differential_fuzz.py --writer claude --model sonnet --count 40 --jobs 3     # see differential_fuzz.py
    python scripts/fuzz_claude.py --self-test

ONE CALL, THE SAME SHAPE. `claude_chat(model, messages, temperature)` returns `(reply, seconds, done, thinking)` exactly as
`ollama_chat` does, so `differential_fuzz.Fuzz` asks either writer through one line and every later step (the program
extraction, the repair rounds, the verdict) is unchanged. `done` is "stop", or "length" when the reply hit the model's
token cap (`stop_reason` "max_tokens"), which the fuzz treats as a reply cut short; `thinking` is 0, because the CLI
returns no separate reasoning text.

HOW IT CALLS CLAUDE. Through the Claude Code CLI in print mode, `claude -p`, which a cloud session (claude.ai/code) and
an attended host both have signed in: no API key is handled here, and the call bills the account that runs it. The CLI
is made a plain text generator: `--safe-mode` (no CLAUDE.md, skills, plugins, hooks, MCP servers or other
customizations), `--tools ""` (no tools), `--no-session-persistence`, `--setting-sources ""`, `--strict-mcp-config`,
`--system-prompt` (the fuzz's own, replacing Claude Code's), `--output-format json` (one record: `result`,
`stop_reason`, `is_error`, `total_cost_usd`) and `--max-budget-usd` (a per-call ceiling), and it runs in an EMPTY
temporary directory, so nothing about the directory a run starts in reaches the model. Measured 2026-11-03 (kb/Work
PB4504 review R1-2): without the two, a call from a worktree carried 3,781 input tokens of the repository's agent
instructions; with them, 693, the CLI's own minimum.

BILLING. The call bills whatever the CLI is signed in with; with `ANTHROPIC_API_KEY` in its environment it bills that
key (its init record then reports `apiKeySource: ANTHROPIC_API_KEY`). `--api-key-file` (differential_fuzz.py) loads the
key from a file into the CLI's environment only, so it is never on a command line or in a log. The exchange so far travels on stdin as one prompt: the CLI takes one user
turn, so an earlier reply and the correction that followed it are written into that turn, labelled.

TEMPERATURE. The CLI has no sampling option, so `temperature` is accepted for the signature and not sent; the run's
`run.json` records the writer, which says so.

COST. Every call's `total_cost_usd` is added to a process-wide meter (`spent()`), so a run can report what it cost and
stop at a ceiling (differential_fuzz.py `--max-usd`).
"""
from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading
import time
from pathlib import Path

from standard_streams import ensure_utf8  # noqa: E402  (scripts/: UTF-8 standard streams)

#: The executable; WISEOWL_COBOL_FUZZ_CLAUDE overrides it (the self-test points it at a stub).
EXE_ENV = "WISEOWL_COBOL_FUZZ_CLAUDE"
#: The most one call may spend, in US dollars (`--max-budget-usd`): a program and its correction are a few cents.
CALL_BUDGET_USD = 0.50
#: Seconds one call may take before it is a model error (a hung CLI must not hold a job slot for the night).
CALL_TIMEOUT_SECONDS = 600

_meter_lock = threading.Lock()
_spent_usd = 0.0
#: The API key the CLI is given, when a run names one (`use_api_key_file`); None: the CLI's own sign-in.
_api_key: str | None = None


def use_api_key_file(path: str | os.PathLike[str]) -> None:
    """Bill every later call to the API key held in the file `path` (its first line). The key goes only into the CLI's
    environment; a missing or empty file is an error before any call is made."""
    global _api_key
    key = Path(path).read_text(encoding="utf-8").strip().splitlines()[0].strip() if Path(path).exists() else ""
    if not key:
        raise RuntimeError(f"no API key in {path}")
    _api_key = key


def spent() -> float:
    """What every claude_chat call of this process has cost so far, in US dollars."""
    with _meter_lock:
        return _spent_usd


def _charge(usd: float) -> None:
    global _spent_usd
    with _meter_lock:
        _spent_usd += usd


def executable() -> str:
    """The Claude Code CLI to run: the override, else `claude` on PATH. A missing CLI is a model error the row records."""
    exe = os.environ.get(EXE_ENV) or shutil.which("claude")
    if not exe:
        raise RuntimeError("the Claude Code CLI (claude) is not on PATH")
    return exe


def transcript(messages: list[dict[str, str]]) -> tuple[str, str]:
    """(system prompt, the one user turn) for an exchange in the Ollama chat shape: the first system message is the system
    prompt; the first user message opens the turn, and each later assistant reply and the user message after it are
    written in, labelled, so the model reads the correction against the program it wrote."""
    system = next((m["content"] for m in messages if m["role"] == "system"), "")
    rest = [m for m in messages if m["role"] != "system"]
    if not rest or rest[0]["role"] != "user":
        raise ValueError("an exchange opens with a user message")
    parts = [rest[0]["content"]]
    for m in rest[1:]:
        label = "Your earlier reply was:" if m["role"] == "assistant" else "The answer to that reply:"
        parts.append(f"{label}\n\n{m['content']}")
    return system, "\n\n".join(parts)


def claude_chat(model: str, messages: list[dict[str, str]], temperature: float,
                budget_usd: float = CALL_BUDGET_USD, timeout: float = CALL_TIMEOUT_SECONDS) -> tuple[str, float, str, int]:
    """One reply from the Claude model `model` (an alias such as `sonnet`, or a full model id) to `messages`.
    Raises RuntimeError for anything that is not a reply (the CLI missing, failing, timing out or reporting an error),
    which the fuzz records as the row's RUNNER_ERROR, as it does an Ollama failure."""
    del temperature     # the CLI has no sampling option (module docstring)
    system, prompt = transcript(messages)
    cmd = [executable(), "-p", "--safe-mode", "--model", model, "--system-prompt", system, "--tools", "", "--output-format", "json",
           "--no-session-persistence", "--setting-sources", "", "--strict-mcp-config", "--max-budget-usd", f"{budget_usd:.2f}"]
    env = {**os.environ, **({"ANTHROPIC_API_KEY": _api_key} if _api_key else {})}
    start = time.monotonic()
    with tempfile.TemporaryDirectory(prefix="fuzz-claude-") as empty:
        try:
            proc = subprocess.run(cmd, input=prompt, capture_output=True, text=True, encoding="utf-8", timeout=timeout,
                                  cwd=empty, env=env)
        except OSError as e:
            raise RuntimeError(f"claude -p could not be started: {e}") from e
        except subprocess.TimeoutExpired as e:
            # A call that timed out may have spent up to its --max-budget-usd, which no record reports: the meter charges
            # that ceiling, so a run's --max-usd stays a bound (review R1-5).
            _charge(budget_usd)
            raise RuntimeError(f"claude -p did not answer in {timeout:.0f} s (charged its {budget_usd:.2f} USD ceiling)") from e
    secs = time.monotonic() - start
    try:
        record = json.loads(proc.stdout)
    except json.JSONDecodeError as e:
        raise RuntimeError(f"claude -p exited {proc.returncode} without a JSON record: {(proc.stderr or proc.stdout).strip()[:300]}") from e
    _charge(float(record.get("total_cost_usd") or 0.0))
    if record.get("is_error") or record.get("subtype") != "success":
        raise RuntimeError(f"claude -p reported {record.get('subtype')}: {str(record.get('result') or record.get('api_error_status'))[:300]}")
    done = "length" if record.get("stop_reason") == "max_tokens" else "stop"
    return str(record.get("result") or ""), secs, done, 0


# ── self-test ──────────────────────────────────────────────────────────────────────────────────────────────────────────

_STUB = '''import json, sys
args = sys.argv[1:]
prompt = sys.stdin.read()
mode = open(sys.argv[0] + ".mode").read().strip()
if mode == "garbage":
    print("not json"); sys.exit(3)
rec = {"type": "result", "subtype": "success", "is_error": False, "stop_reason": "end_turn", "total_cost_usd": 0.25,
       "result": "ARGS=" + json.dumps(args) + " PROMPT=" + json.dumps(prompt)}
if mode == "cut":
    rec["stop_reason"] = "max_tokens"
if mode == "error":
    rec.update(subtype="error_during_execution", is_error=True, result="overloaded")
if mode == "cwd":
    import os
    rec["result"] = "CWD=" + os.getcwd() + " EMPTY=" + str(not os.listdir("."))
if mode == "key":
    import os
    rec["result"] = "KEY=" + os.environ.get("ANTHROPIC_API_KEY", "")
print(json.dumps(rec))
'''


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(f"  {'ok  ' if cond else 'FAIL'} {name}")
        if not cond:
            failures.append(name)

    with tempfile.TemporaryDirectory() as tmp:
        stub = Path(tmp) / "claude_stub.py"
        stub.write_text(_STUB, encoding="utf-8", newline="\n")
        mode = Path(str(stub) + ".mode")
        launcher = Path(tmp) / ("claude.cmd" if os.name == "nt" else "claude")
        if os.name == "nt":
            launcher.write_text(f'@"{sys.executable}" "{stub}" %*\n', encoding="utf-8", newline="\n")
        else:
            launcher.write_text(f'#!/bin/sh\nexec "{sys.executable}" "{stub}" "$@"\n', encoding="utf-8", newline="\n")
            launcher.chmod(0o755)
        saved = os.environ.get(EXE_ENV)
        os.environ[EXE_ENV] = str(launcher)
        try:
            msgs = [{"role": "system", "content": "SYS"}, {"role": "user", "content": "write a program"},
                    {"role": "assistant", "content": "PROGRAM-1"}, {"role": "user", "content": "both refused: fix it"}]
            mode.write_text("ok", encoding="utf-8", newline="\n")
            before = spent()
            reply, secs, done, thinking = claude_chat("sonnet", msgs, 0.3)
            after_first = spent()
            args = json.loads(reply.split(" PROMPT=")[0][len("ARGS="):])
            prompt = json.loads(reply.split(" PROMPT=", 1)[1])
            check("the reply is the record's result, done is stop, no thinking", done == "stop" and thinking == 0 and secs >= 0)
            check("the model and the system prompt are passed", args[args.index("--model") + 1] == "sonnet"
                  and args[args.index("--system-prompt") + 1] == "SYS")
            check("safe mode, no tools, no persistence, no settings, no MCP", "--safe-mode" in args
                  and args[args.index("--tools") + 1] == "" and "--no-session-persistence" in args
                  and args[args.index("--setting-sources") + 1] == "" and "--strict-mcp-config" in args)
            mode.write_text("cwd", encoding="utf-8", newline="\n")
            cwd = claude_chat("sonnet", msgs[:2], 0.3)[0]
            check("the CLI runs in an empty temporary directory, not the run's", cwd.startswith("CWD=")
                  and "fuzz-claude-" in cwd and cwd.endswith("EMPTY=True"))
            mode.write_text("ok", encoding="utf-8", newline="\n")
            check("the exchange is one labelled turn on stdin", prompt.startswith("write a program")
                  and "Your earlier reply was:\n\nPROGRAM-1" in prompt and "The answer to that reply:\n\nboth refused: fix it" in prompt)
            check("the call's cost is metered", abs(after_first - before - 0.25) < 1e-9)
            mode.write_text("cut", encoding="utf-8", newline="\n")
            check("max_tokens is a reply cut short (length)", claude_chat("sonnet", msgs[:2], 0.3)[2] == "length")
            for bad in ("error", "garbage"):
                mode.write_text(bad, encoding="utf-8", newline="\n")
                try:
                    claude_chat("sonnet", msgs[:2], 0.3)
                    check(f"a {bad} reply raises RuntimeError", False)
                except RuntimeError:
                    check(f"a {bad} reply raises RuntimeError", True)
            keyfile = Path(tmp) / "key.txt"
            keyfile.write_text("sk-test-key\n", encoding="utf-8", newline="\n")
            use_api_key_file(keyfile)
            mode.write_text("key", encoding="utf-8", newline="\n")
            check("an API key file reaches the CLI's environment only", claude_chat("sonnet", msgs[:2], 0.3)[0] == "KEY=sk-test-key")
            globals()["_api_key"] = None
            os.environ[EXE_ENV] = str(Path(tmp) / "no-such-claude")
            saved_path = os.environ.get("PATH", "")
            os.environ["PATH"] = ""
            try:
                claude_chat("sonnet", msgs[:2], 0.3)
                check("a CLI that cannot be started is a RuntimeError", False)
            except RuntimeError:
                check("a CLI that cannot be started is a RuntimeError", True)
            finally:
                os.environ["PATH"] = saved_path
                os.environ[EXE_ENV] = str(launcher)
            try:
                transcript([{"role": "system", "content": "S"}, {"role": "assistant", "content": "A"}])
                check("an exchange not opened by the user is refused", False)
            except ValueError:
                check("an exchange not opened by the user is refused", True)
        finally:
            if saved is None:
                os.environ.pop(EXE_ENV, None)
            else:
                os.environ[EXE_ENV] = saved
    print(f"=== FUZZ CLAUDE SELF-TEST: {'GREEN' if not failures else 'RED — ' + ', '.join(failures)} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    print(__doc__)
    sys.exit(2)
