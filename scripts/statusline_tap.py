#!/usr/bin/env python3
"""statusline_tap.py — the status line command that keeps the account's live quota on disk (kb/Work PB4505).

    "statusLine": {"type": "command", "command": "python E:/COBOL/scripts/statusline_tap.py -- npx -y ccstatusline@latest"}
    python scripts/statusline_tap.py --self-test

WHY. Claude Code hands its status line command a JSON record on stdin at every refresh, and that record carries
`rate_limits`: `five_hour` and `seven_day` (and the per-model weekly rows), each `{used_percentage, resets_at}` with
`resets_at` in epoch seconds. That is the meter the claude.ai usage page shows, and nothing else in a session can read
it: the orchestrator used to open the usage page in a browser (the owner, 2026-11-03: "The statusline plugin reads the
current weekly quota. Why can't you read it however the statusline does?").

WHEN THE NUMBERS WERE MEASURED, NOT WHEN THEY WERE SEEN (branch review R1-1). Claude Code updates `rate_limits` only when
its session gets an API response, but the status line re-runs every `refreshInterval` seconds even while the session is
idle. So the time the tap runs says nothing about the numbers' age: an idle session repeats hours-old figures every ten
seconds, and spend since then (another session's, or a headless `claude -p` fleet's, which has no status line) is not in
them. The tap therefore keeps, per session, the figures and the time they last CHANGED (`measured_at`), and publishes the
session whose figures changed most recently. `budget.py` anchors on `measured_at` and counts telemetry after it.

THE FILE. `<config dir>/rate-limits.json`: `measured_at`, `session_id` and `rate_limits` of the most recently measured
session, and `sessions` (each session's `rate_limits`, `measured_at` and `seen_at`; a session unseen for eight days is
dropped). The config dir is the session's own, through account.py's one rule (`account.config_dir`), so account 1's
sessions write `~/.claude/rate-limits.json` and account 2's `~/.claude-acct2/rate-limits.json`. Nothing here reads a
credential or makes a request.

IT NEVER BREAKS THE STATUS LINE. Anything that goes wrong in the tap (no stdin, not JSON, no `rate_limits`, an unwritable
or locked file, account.py unimportable) is skipped and the status line command still runs. The file is written to a
temporary file renamed over it, and the temporary file is removed if the rename fails. The status line command's stdin
and output pass through as BYTES, unchanged.
"""
from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parent))
from standard_streams import ensure_utf8  # noqa: E402  (scripts/: UTF-8 standard streams)

#: The file name, in the session's config dir.
FILE_NAME = "rate-limits.json"
#: A session not seen for this long is dropped from the file (longer than a week: a quota window).
SESSION_KEEP = timedelta(days=8)


def config_dir() -> Path:
    """The session's Claude config dir, by account.py's one rule (CLAUDE_CONFIG_DIR when set, else ~/.claude)."""
    sys.path.insert(0, str(Path(__file__).resolve().parent / "orchestrator"))
    import account  # noqa: PLC0415  (scripts/orchestrator: the one resolver of the config dir)
    return Path(account.config_dir())


def _parse(ts: str) -> datetime:
    return datetime.fromisoformat(ts)


def tap(raw: bytes, directory: Path, now: datetime | None = None) -> bool:
    """Record the `rate_limits` of the status line record `raw` for its session in `directory`/rate-limits.json, and
    publish the most recently measured session. True when written; False (nothing written) when the record has none or
    anything fails."""
    tmp: str | None = None
    try:
        record = json.loads(raw.decode("utf-8"))
        limits = record.get("rate_limits") if isinstance(record, dict) else None
        if not isinstance(limits, dict) or not limits:
            return False
        now = now or datetime.now(timezone.utc)
        sid = str(record.get("session_id") or "")
        path = directory / FILE_NAME
        # Only a file that does not exist yet means "no history". Any other failure to read it (on Windows, a read that
        # overlaps another session's rename is refused) skips this refresh: starting from an empty history would stamp an
        # idle session's old figures as measured now and drop every other session (review R2-1). The next refresh tries again.
        try:
            text = path.read_text(encoding="utf-8")
        except FileNotFoundError:
            text = None
        old = json.loads(text) if text is not None else {}
        sessions = old.get("sessions") if isinstance(old, dict) and isinstance(old.get("sessions"), dict) else {}
        prev = sessions.get(sid)
        measured = prev["measured_at"] if isinstance(prev, dict) and prev.get("rate_limits") == limits else now.isoformat(timespec="seconds")
        sessions[sid] = {"rate_limits": limits, "measured_at": measured, "seen_at": now.isoformat(timespec="seconds")}
        sessions = {k: v for k, v in sessions.items() if now - _parse(v["seen_at"]) <= SESSION_KEEP}
        best_id = max(sessions, key=lambda k: _parse(sessions[k]["measured_at"]))
        best = sessions[best_id]
        out = {"measured_at": best["measured_at"], "session_id": best_id, "rate_limits": best["rate_limits"], "sessions": sessions}
        directory.mkdir(parents=True, exist_ok=True)
        fd, tmp = tempfile.mkstemp(prefix=".rate-limits.", dir=directory)
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            json.dump(out, f, indent=1)
        os.replace(tmp, path)
        tmp = None
        return True
    except Exception:  # noqa: BLE001 — the status line must render whatever the tap meets (module docstring)
        return False
    finally:
        if tmp is not None:
            try:
                os.unlink(tmp)
            except OSError:
                pass


def run_downstream(command: list[str], raw: bytes) -> int:
    """Run the status line command with the same stdin; its stdout and stderr pass through as bytes. No command, no output."""
    if not command:
        return 0
    exe = shutil.which(command[0]) or command[0]
    proc = subprocess.run([exe, *command[1:]], input=raw, capture_output=True)
    sys.stdout.buffer.write(proc.stdout)
    sys.stdout.buffer.flush()
    sys.stderr.buffer.write(proc.stderr)
    return proc.returncode


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(f"  {'ok  ' if cond else 'FAIL'} {name}")
        if not cond:
            failures.append(name)

    def rec(sid: str, weekly: int) -> bytes:
        return json.dumps({"session_id": sid, "rate_limits": {"seven_day": {"used_percentage": weekly, "resets_at": 1762596000}}}).encode()

    with tempfile.TemporaryDirectory() as tmp:
        d = Path(tmp) / "acct2"
        t0 = datetime(2026, 11, 3, 20, 0, tzinfo=timezone.utc)
        read = lambda: json.loads((d / FILE_NAME).read_text(encoding="utf-8"))  # noqa: E731
        check("a record with rate_limits is written", tap(rec("a", 29), d, t0))
        check("its measured_at is when it was first seen", read()["measured_at"] == t0.isoformat(timespec="seconds"))
        tap(rec("a", 29), d, t0 + timedelta(hours=3))
        check("an idle session repeating the same figures keeps the time they were measured (review R1-1)",
              read()["measured_at"] == t0.isoformat(timespec="seconds")
              and read()["sessions"]["a"]["seen_at"] == (t0 + timedelta(hours=3)).isoformat(timespec="seconds"))
        tap(rec("b", 33), d, t0 + timedelta(hours=1))
        tap(rec("a", 29), d, t0 + timedelta(hours=4))
        check("the most recently MEASURED session is published, whichever wrote last",
              read()["session_id"] == "b" and read()["rate_limits"]["seven_day"]["used_percentage"] == 33)
        tap(rec("a", 35), d, t0 + timedelta(hours=5))
        check("a session whose figures change is measured again", read()["session_id"] == "a"
              and read()["measured_at"] == (t0 + timedelta(hours=5)).isoformat(timespec="seconds"))
        tap(rec("c", 1), d, t0 + timedelta(days=9))
        check("a session unseen for eight days is dropped", set(read()["sessions"]) == {"c"})
        for name, raw in (("not JSON", b"{oops"), ("no rate_limits", json.dumps({"session_id": "s2"}).encode()),
                          ("empty rate_limits", json.dumps({"rate_limits": {}}).encode()), ("a JSON list", b"[1]"),
                          ("not UTF-8", b"\xff\xfe")):
            check(f"{name}: nothing is written and the file stays", not tap(raw, d) and read()["session_id"] == "c")
        blocked = Path(tmp) / "file-not-dir"
        blocked.write_text("x", encoding="utf-8", newline="\n")
        check("an unwritable directory is skipped, not raised", not tap(rec("a", 1), blocked))
        before = (d / FILE_NAME).read_bytes()
        saved_read = Path.read_text
        Path.read_text = lambda self, *a, **k: (_ for _ in ()).throw(PermissionError("locked by another session's rename"))
        try:
            check("a file that exists but cannot be read is skipped, never reset (review R2-1)",
                  not tap(rec("idle", 50), d, t0 + timedelta(days=9, hours=1)))
        finally:
            Path.read_text = saved_read
        check("... and the history it could not read is left as it was", (d / FILE_NAME).read_bytes() == before)
        saved_replace = os.replace
        os.replace = lambda *_a: (_ for _ in ()).throw(PermissionError("in use"))
        try:
            check("a failed rename is skipped", not tap(rec("c", 2), d, t0 + timedelta(days=9, hours=2)))
        finally:
            os.replace = saved_replace
        check("... and its temporary file is removed (review R2-2)",
              not any(p.name.startswith(".rate-limits.") for p in d.iterdir()))
        saved = os.environ.get("CLAUDE_CONFIG_DIR")
        try:
            os.environ["CLAUDE_CONFIG_DIR"] = str(d)
            check("the config dir is account.py's: CLAUDE_CONFIG_DIR when set", config_dir() == d)
            os.environ.pop("CLAUDE_CONFIG_DIR")
            check("else ~/.claude", config_dir() == Path.home() / ".claude")
        finally:
            if saved is not None:
                os.environ["CLAUDE_CONFIG_DIR"] = saved
        echo = [sys.executable, "-c", "import sys; d = sys.stdin.buffer.read(); sys.stdout.buffer.write(b'LINE ' + str(len(d)).encode() + b'\\n\\xe2\\x96\\x88')"]
        stdin = rec("e2e", 7)
        proc = subprocess.run([sys.executable, __file__, "--", *echo], input=stdin, capture_output=True,
                              env={**os.environ, "CLAUDE_CONFIG_DIR": str(Path(tmp) / "acct1")})
        check("end to end: the downstream command gets the same stdin, its bytes pass through unchanged (LF kept, UTF-8 kept), "
              "and the account's file is written",
              proc.returncode == 0 and proc.stdout == f"LINE {len(stdin)}\n".encode() + b"\xe2\x96\x88"
              and (Path(tmp) / "acct1" / FILE_NAME).exists())
    print(f"=== STATUSLINE TAP SELF-TEST: {'GREEN' if not failures else 'RED — ' + ', '.join(failures)} ===")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    command = argv[argv.index("--") + 1:] if "--" in argv else argv
    raw = sys.stdin.buffer.read()
    try:
        tap(raw, config_dir())
    except Exception:  # noqa: BLE001 — account.py unimportable: the status line still renders
        pass
    return run_downstream(command, raw)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
