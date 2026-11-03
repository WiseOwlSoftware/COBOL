#!/usr/bin/env python3
"""statusline_tap.py — the status line command that keeps the account's live quota on disk (kb/Work PB4505).

    "statusLine": {"type": "command", "command": "python E:/COBOL/scripts/statusline_tap.py -- npx -y ccstatusline@latest"}
    python scripts/statusline_tap.py --self-test

WHY. Claude Code hands its status line command a JSON record on stdin at every refresh, and that record carries
`rate_limits`: `five_hour` and `seven_day` (and the per-model weekly rows), each `{used_percentage, resets_at}` with
`resets_at` in epoch seconds. That is the meter the claude.ai usage page shows, current to the refresh interval, and
nothing else in a session can read it: the orchestrator used to open the usage page in a browser (the owner,
2026-11-03: "The statusline plugin reads the current weekly quota. Why can't you read it however the statusline does?").

WHAT IT DOES. It reads stdin, writes the `rate_limits` it found (with the time it saw them and the session id) to
`<config dir>/rate-limits.json`, then runs the status line command after `--` with the same stdin and passes its output
through unchanged. The config dir is the session's own (`CLAUDE_CONFIG_DIR`, else `~/.claude`), so each account's
sessions write that account's file: account 1 and account 2 (scripts/orchestrator/model_rules.json `accounts`) each get
their own, and `budget.py` reads the file of the account it estimates (`live_reading`). Nothing here reads a credential
or makes a request.

IT NEVER BREAKS THE STATUS LINE. Anything that goes wrong in the tap (no stdin, not JSON, no `rate_limits`, an
unwritable file) is skipped silently and the status line command still runs; the file is written atomically (a
temporary file renamed over it), so a reader never sees half of one.
"""
from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

#: The file name, in the session's config dir.
FILE_NAME = "rate-limits.json"


def config_dir() -> Path:
    """The session's Claude config dir: CLAUDE_CONFIG_DIR when set, else ~/.claude (account.py's rule)."""
    env = os.environ.get("CLAUDE_CONFIG_DIR")
    return Path(env).expanduser() if env else Path.home() / ".claude"


def tap(raw: str, directory: Path, now: datetime | None = None) -> bool:
    """Write the `rate_limits` of the status line record `raw` to `directory`/rate-limits.json. True when written;
    False (and nothing written) when the record has none or anything fails."""
    try:
        record = json.loads(raw)
        limits = record.get("rate_limits") if isinstance(record, dict) else None
        if not isinstance(limits, dict) or not limits:
            return False
        out = {"captured_at": (now or datetime.now(timezone.utc)).isoformat(timespec="seconds"),
               "session_id": record.get("session_id"), "rate_limits": limits}
        directory.mkdir(parents=True, exist_ok=True)
        fd, tmp = tempfile.mkstemp(prefix=".rate-limits.", dir=directory)
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            json.dump(out, f, indent=1)
        os.replace(tmp, directory / FILE_NAME)
        return True
    except Exception:  # noqa: BLE001 — the status line must render whatever the tap meets (module docstring)
        return False


def run_downstream(command: list[str], raw: str) -> int:
    """Run the status line command with the same stdin; its stdout and stderr pass through. An empty command prints nothing."""
    if not command:
        return 0
    exe = shutil.which(command[0]) or command[0]
    proc = subprocess.run([exe, *command[1:]], input=raw, capture_output=True, text=True, encoding="utf-8")
    sys.stdout.write(proc.stdout)
    sys.stderr.write(proc.stderr)
    return proc.returncode


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(f"  {'ok  ' if cond else 'FAIL'} {name}")
        if not cond:
            failures.append(name)

    limits = {"five_hour": {"used_percentage": 12, "resets_at": 1762210800},
              "seven_day": {"used_percentage": 29, "resets_at": 1762596000}}
    with tempfile.TemporaryDirectory() as tmp:
        d = Path(tmp) / "acct2"
        when = datetime(2026, 11, 3, 20, 0, tzinfo=timezone.utc)
        check("a record with rate_limits is written", tap(json.dumps({"session_id": "s1", "rate_limits": limits}), d, when))
        got = json.loads((d / FILE_NAME).read_text(encoding="utf-8"))
        check("the file holds the limits, the time seen and the session",
              got == {"captured_at": "2026-11-03T20:00:00+00:00", "session_id": "s1", "rate_limits": limits})
        for name, raw in (("not JSON", "{oops"), ("no rate_limits", json.dumps({"session_id": "s2"})),
                          ("empty rate_limits", json.dumps({"rate_limits": {}})), ("a JSON list", "[1]")):
            check(f"{name}: nothing is written and the earlier file stays", not tap(raw, d) and
                  json.loads((d / FILE_NAME).read_text(encoding="utf-8"))["session_id"] == "s1")
        blocked = Path(tmp) / "file-not-dir"
        blocked.write_text("x", encoding="utf-8", newline="\n")
        check("an unwritable directory is skipped, not raised", not tap(json.dumps({"rate_limits": limits}), blocked))
        saved = os.environ.get("CLAUDE_CONFIG_DIR")
        try:
            os.environ["CLAUDE_CONFIG_DIR"] = str(d)
            check("the config dir is CLAUDE_CONFIG_DIR when set", config_dir() == d)
            os.environ.pop("CLAUDE_CONFIG_DIR")
            check("else ~/.claude", config_dir() == Path.home() / ".claude")
        finally:
            if saved is not None:
                os.environ["CLAUDE_CONFIG_DIR"] = saved
        echo = [sys.executable, "-c", "import sys; data = sys.stdin.read(); print('LINE ' + str(len(data)))"]
        proc = subprocess.run([sys.executable, __file__, "--", *echo], input=json.dumps({"rate_limits": limits}),
                              capture_output=True, text=True, encoding="utf-8", env={**os.environ, "CLAUDE_CONFIG_DIR": str(Path(tmp) / "acct1")})
        check("end to end: the downstream command gets the same stdin and its output passes through, and the account's file is written",
              proc.returncode == 0 and proc.stdout.strip() == f"LINE {len(json.dumps({'rate_limits': limits}))}"
              and (Path(tmp) / "acct1" / FILE_NAME).exists())
    print(f"=== STATUSLINE TAP SELF-TEST: {'GREEN' if not failures else 'RED — ' + ', '.join(failures)} ===")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    command = argv[argv.index("--") + 1:] if "--" in argv else argv
    raw = sys.stdin.read()
    tap(raw, config_dir())
    return run_downstream(command, raw)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
