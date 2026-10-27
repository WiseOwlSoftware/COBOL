#!/usr/bin/env python3
"""fuzz_ollama.py — THE MODEL'S SIDE OF THE DIFFERENTIAL FUZZ (scripts/differential_fuzz.py): the streaming chat to Ollama,
the watch that ends a reply which has degenerated into an enumeration, and the PAUSE file through which the run gives the
GPU away and takes it back without losing a program (kb/Work PB3537, PB3561).

    from fuzz_ollama import ollama_chat, PauseGate, lossless       # differential_fuzz.py's imports
    python scripts/fuzz_ollama.py --self-test                      # a stub Ollama over a local socket, then the unit cases

⛔ A DEGENERATE REPLY IS CUT, NOT PAID FOR (kb/Work PB3537). The third overnight run (2026-10-22, nemotron-3.5-bolt, IQ2_XXS)
lost 19 of its first 90 replies to the 6,000-token cap: data items WS-ARG1 … WS-ARG309, hundreds of lines differing only by
a counter. `ollama_chat` therefore STREAMS the reply and feeds every piece to an `EnumerationWatch`; when the last
DEGENERATE_LINES non-blank lines show at most DEGENERATE_FORMS distinct forms (a form is a line with its digit runs
replaced by one mark), the harness closes the connection, which makes Ollama stop generating, and returns the reply cut
KEPT_WINDOW_LINES lines into that window with `done_reason` "degenerate" (Ollama's own "token repeat limit reached" abort
comes back as "aborted"; DEGENERATE_REASONS names both, so the report counts one failure shape).
THE THRESHOLD IS MEASURED (2026-10-26), over the 44,737 DISTINCT replies the harness's five runs left on disk
(conformance-suite/results/fuzz-*/programs/*.reply*.txt), counting a reply COMPLETE when it ends in a closed fence and holds a
PROCEDURE DIVISION (309 replies are not): the longest run of consecutive lines with at most 3 forms is, over the complete
programs, 146 lines, and 35 of them reach 100; of the 309 incomplete replies, 77 reach 150. So a 150-line window at 3 forms
flags none of the 44,428 complete replies and 77 of the 309 that never completed; a window of 120 flagged one complete
program and 100 flagged 35 (a complete program that lists 100 near-identical items is still a program). With four forms the
150-line window flags 2 complete replies and with six, 3, so three forms it is. This class, run over the same replies, flags
the same 0 and 77, reading 441,258 of those 77 replies' 829,824 characters before it cuts (53 %); PB3537's own repro,
fz00088, is cut after 5,061 of its 10,540 characters.

⛔ A PAUSE LOSES NOTHING (kb/Work PB3561). The owner's GPU also plays games; scripts/fuzz_night.ps1 watches for one and, to
free the GPU at once, ENDS Ollama's runner process. Every generation in flight then fails (`ollama HTTP 500: an error was
encountered while running the model`, a refused connection, a stream cut mid-reply), and until PB3561 the harness recorded
each as a terminal RUNNER_ERROR row (three programs at 22:05 on 2026-10-22). The protocol is a file, `<run dir>/PAUSE`:
  - the wrapper CREATES it before it ends the runner, REWRITES it on every poll while the pause lasts (a lease: a PAUSE not
    rewritten for PAUSE_STALE_SECONDS belongs to a wrapper that died, so the run goes on and says so) and REMOVES it on resume.
    Only the wrapper's PAUSE is a lease: its first line starts LEASE_MARK ("lease:"). A PAUSE made by hand (an empty file
    will do) has no wrapper to die, so it holds until it is removed;
  - a worker WAITS for it to clear before every generation (`PauseGate.wait`), so no new request starts during a pause;
  - a generation that fails while it exists is the pause's doing, not the model's: it is asked AGAIN after the pause, as
    often as pauses end it (`lossless`; every ask waits for the PAUSE to clear first, so a pause costs no retry budget). A
    failure outside a pause whose text says Ollama's runner failed or restarted (`runner_failed`) is asked again ONCE, after
    RUNNER_SETTLE_SECONDS for the runner to restart, whatever pauses came before; a failure outside a pause that is not a
    runner failure, or a second runner failure, is the terminal RUNNER_ERROR it always was;
  - every pause and resume is appended to `<run dir>/pauses.jsonl`, so the report counts paused time and re-queued
    generations as pauses, not as the model's failures (`read_pauses`).
Nothing is suspended: the compiles and runs of programs already generated are CPU work and finish.

Standard library only.
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import http.client
import http.server
import json
import os
import re
import sys
import tempfile
import threading
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

from standard_streams import ensure_utf8

HOST = os.environ.get("OLLAMA_HOST", "http://localhost:11434")
NUM_PREDICT = 6000          # output tokens per reply; an 80-line free-format program is well under 2000
NUM_CTX = 16384             # the context Ollama gives the model: the whole exchange, every correction included

#: A reply is degenerate when its last DEGENERATE_LINES non-blank lines hold at most DEGENERATE_FORMS distinct forms
#: (module docstring: measured over 44,737 replies).
DEGENERATE_LINES = 150
DEGENERATE_FORMS = 3
#: `done_reason` of a reply the harness cut for it, and of one Ollama's own repeat limit aborted: one failure shape.
DEGENERATE = "degenerate"
ABORTED = "aborted"
DEGENERATE_REASONS = (DEGENERATE, ABORTED)
#: A reply the harness cuts keeps everything before the window that tripped the watch and the window's first lines: the
#: model's slip stays visible in the recorded reply and in the context of the correction, without the hundreds of lines
#: (the window may open with up to DEGENERATE_FORMS - 1 lines that are not part of the repetition).
KEPT_WINDOW_LINES = 4

#: The Ollama options a run may set for one model (`--model-option MODEL:KEY=VALUE`), and the type each takes. Adding a
#: sampler option is one line here.
MODEL_OPTION_TYPES: dict[str, Callable[[str], float | int]] = {"repeat_penalty": float, "repeat_last_n": int}

#: The wrapper's file (module docstring); the same name in fuzz_night.ps1, which its self-test holds to this one.
PAUSE_FILE = "PAUSE"
PAUSE_LOG = "pauses.jsonl"
PAUSE_POLL_SECONDS = 2.0
#: The wrapper rewrites PAUSE every poll (10 s by default); a file this old is abandoned.
PAUSE_STALE_SECONDS = 300.0
#: The first line of the wrapper's PAUSE starts with this; fuzz_night_lib.ps1 spells it the same (its self-test holds the two).
LEASE_MARK = "lease:"
#: A runner failure with no pause (Ollama's runner crashed and restarts on the next request): the wait before the one new try.
RUNNER_SETTLE_SECONDS = 15.0

#: What a model call raises: our OllamaError (a RuntimeError), a refused or reset connection, a timeout (all OSError).
MODEL_ERRORS = (RuntimeError, OSError)

_DIGITS = re.compile(r"\d+")
_SPACES = re.compile(r"\s+")


# ── the reply watch ───────────────────────────────────────────────────────────────────────────────────────────────────

def line_form(line: str) -> str:
    """A line with its digit runs replaced by one mark and its white space collapsed: 'WS-ARG17 PIC X.' and
    'WS-ARG309 PIC X.' are one form. '' for a blank line."""
    return _SPACES.sub(" ", _DIGITS.sub("#", line.strip()))


class EnumerationWatch:
    """Reads a reply as it streams and says when it has degenerated into an enumeration (module docstring). `feed(piece)`
    returns None, or the character offset in the whole text so far at which the harness cuts the reply: the start of the
    tripping window's KEPT_WINDOW_LINES + 1st line. O(1) per line; pieces may split a line anywhere."""

    def __init__(self, window: int = DEGENERATE_LINES, forms: int = DEGENERATE_FORMS) -> None:
        self.window, self.forms = window, forms
        self._pending = ""                  # the unfinished last line
        self._offset = 0                    # where the unfinished line starts in the whole text
        self._lines: collections.deque[tuple[str, int]] = collections.deque()   # (form, start offset) of the last `window` lines
        self._counts: collections.Counter[str] = collections.Counter()

    def feed(self, piece: str) -> int | None:
        self._pending += piece
        while (end := self._pending.find("\n")) >= 0:
            line, self._pending = self._pending[:end], self._pending[end + 1:]
            start, self._offset = self._offset, self._offset + end + 1
            form = line_form(line)
            if not form:
                continue
            self._lines.append((form, start))
            self._counts[form] += 1
            if len(self._lines) > self.window:
                old, _ = self._lines.popleft()
                self._counts[old] -= 1
                if not self._counts[old]:
                    del self._counts[old]
            if len(self._lines) == self.window and len(self._counts) <= self.forms:
                return self._lines[KEPT_WINDOW_LINES][1]
        return None


# ── the chat ──────────────────────────────────────────────────────────────────────────────────────────────────────────

class OllamaError(RuntimeError):
    """Ollama's failure, not the model's reply. `status` is the HTTP status, None for a failure after the reply began
    (an error line in the stream, a stream cut before Ollama said it was done)."""

    def __init__(self, message: str, status: int | None = None) -> None:
        super().__init__(message)
        self.status = status

    @property
    def server_side(self) -> bool:
        """The runner failed or restarted (a 5xx or a failure mid-reply), as against a request Ollama refused (4xx)."""
        return self.status is None or self.status >= 500


def runner_failed(e: BaseException) -> bool:
    """True when `e` says Ollama's runner failed or restarted (a 5xx, an error or a cut stream mid-reply, a refused or
    reset connection), as against a request it refused or a generation that timed out."""
    if isinstance(e, OllamaError):
        return e.server_side
    return isinstance(e, (urllib.error.URLError, ConnectionError))


#: What Ollama says when the runner PROCESS died or its connection broke, however else the message is worded.
RUNNER_DEATH_MARKERS = ("llama-server process has terminated", "forcibly closed", "unexpected EOF", "connection reset",
                        "an error was encountered while running the model")


def repeat_abort(err: str) -> bool:
    """True when `err` is Ollama stopping a reply that fell into a repeating loop ('prediction aborted, token repeat limit
    reached'): the model's degenerate reply, not a failure of the server. The caller records the message as the reply and
    treats it as a reply with no program. A message that also names the runner's death is the runner's failure, never a
    verdict on the model (one end-to-end run, 2026-10-26, recorded an aborted reply on a generation asked again after its
    runner was ended; the message was not kept, so whether Ollama words a restart that way is unknown: this rule makes the
    answer harmless, and the aborted reply's message is now the recorded reply)."""
    return "prediction aborted" in err and not any(m in err for m in RUNNER_DEATH_MARKERS)


def read_reply(lines: Any, start: float) -> tuple[str, float, str, int]:
    """The reply of Ollama's NDJSON stream (one JSON object per line, one per token) → (content, seconds, done_reason,
    thinking characters). Returns at once, closing the stream, when the EnumerationWatch cuts the reply."""
    watch = EnumerationWatch()
    parts: list[str] = []
    thinking = 0
    for raw in lines:
        if not raw.strip():
            continue
        chunk = json.loads(raw)
        if chunk.get("error"):
            err = str(chunk["error"])
            if repeat_abort(err):
                return err[:300], time.monotonic() - start, ABORTED, 0
            raise OllamaError(f"ollama error after the reply began: {err[:300]!r}")
        message = chunk.get("message") or {}
        thinking += len(message.get("thinking") or "")
        piece = message.get("content") or ""
        if piece:
            parts.append(piece)
            cut = watch.feed(piece)
            if cut is not None:
                return "".join(parts)[:cut].replace("<|eot|>", ""), time.monotonic() - start, DEGENERATE, thinking
        if chunk.get("done"):
            return "".join(parts).replace("<|eot|>", ""), time.monotonic() - start, chunk.get("done_reason", "stop"), thinking
    raise OllamaError("the reply stream ended before Ollama said it was done")


def ollama_chat(model: str, messages: list[dict[str, str]], temperature: float, keep_alive: str = "10m",
                timeout: float = 600.0, options: dict[str, float | int] | None = None,
                host: str | None = None) -> tuple[str, float, str, int]:
    """One chat completion over `messages` → (content, seconds, done_reason, thinking characters). `done_reason` is
    Ollama's: "stop" for a finished reply, "length" when NUM_PREDICT cut it off (the first smoke runs lost every program that
    way: a 3000-token cap and fixed-form indentation), plus DEGENERATE (the harness cut an enumeration, `EnumerationWatch`)
    and ABORTED (Ollama's repeat limit). The reply is STREAMED, so `timeout` bounds the silence between two tokens, not the
    whole reply. `options` are the model's extra Ollama options (MODEL_OPTION_TYPES). A model that rejects the `think` field
    is asked again without it (the 2026-10-03 spike). A model that thinks despite `think: false` (MiniMax M3.1 Flash on a
    full-size prompt, 2026-10-20: 28,000 characters of thinking, every output token spent, empty content) is visible
    through the thinking length. Raises OllamaError for an Ollama failure (`runner_failed` says which kind)."""
    body: dict[str, Any] = {"model": model, "stream": True, "think": False, "keep_alive": keep_alive, "messages": messages,
                            "options": {**(options or {}), "temperature": temperature, "num_ctx": NUM_CTX,
                                        "num_predict": NUM_PREDICT}}
    for attempt in (0, 1):
        start = time.monotonic()
        req = urllib.request.Request(f"{host or HOST}/api/chat", json.dumps(body).encode("utf-8"),
                                     {"Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(req, timeout=timeout) as r:
                return read_reply(r, start)
        except urllib.error.HTTPError as e:
            msg = e.read()[:300]
            if attempt == 0 and b"think" in msg:
                body.pop("think")
                continue
            if repeat_abort(msg.decode("utf-8", "replace")):
                return msg.decode("utf-8", "replace"), time.monotonic() - start, ABORTED, 0
            raise OllamaError(f"ollama HTTP {e.code}: {msg!r}", e.code) from None
        except http.client.HTTPException as e:
            # a chunked stream the server closed before its last chunk (the runner died mid-reply)
            raise OllamaError(f"the reply stream was cut: {e!r}") from None
    raise AssertionError("unreachable")


def unload(model: str, host: str | None = None) -> None:
    """Free the GPU: a request with keep_alive 0 unloads the model (the GPU is shared with the owner's other work)."""
    try:
        req = urllib.request.Request(f"{host or HOST}/api/generate", json.dumps({"model": model, "keep_alive": 0}).encode(),
                                     {"Content-Type": "application/json"})
        urllib.request.urlopen(req, timeout=60).read()
    except (urllib.error.URLError, OSError):
        pass


def parse_model_options(items: list[str]) -> dict[str, dict[str, float | int]]:
    """`MODEL:KEY=VALUE` (a model tag holds a colon, so the split is at the LAST one before the '=') → {model: {key: value}}.
    ValueError names the first bad item."""
    out: dict[str, dict[str, float | int]] = {}
    for item in items:
        head, eq, value = item.partition("=")
        model, colon, key = head.rpartition(":")
        if not (eq and colon and model and key in MODEL_OPTION_TYPES):
            raise ValueError(f"--model-option {item!r}: expected MODEL:KEY=VALUE with KEY one of {sorted(MODEL_OPTION_TYPES)}")
        try:
            out.setdefault(model, {})[key] = MODEL_OPTION_TYPES[key](value)
        except ValueError:
            raise ValueError(f"--model-option {item!r}: {value!r} is not a valid {key}") from None
    return out


# ── the pause ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def _iso(t: float) -> str:
    return datetime.fromtimestamp(t, timezone.utc).isoformat(timespec="seconds")


class PauseGate:
    """The run's view of its PAUSE file (module docstring). `active()` is whether a live PAUSE exists; `wait()` blocks
    while it does. Every transition is appended to PAUSE_LOG. `now` and `sleep` are injectable so a test needs no clock."""

    def __init__(self, run_dir: Path, poll: float = PAUSE_POLL_SECONDS, stale_after: float = PAUSE_STALE_SECONDS,
                 now: Callable[[], float] = time.time, sleep: Callable[[float], None] = time.sleep,
                 say: Callable[[str], None] = print) -> None:
        self.path = run_dir / PAUSE_FILE
        self.log = run_dir / PAUSE_LOG
        self.poll, self.stale_after, self.sleep, self._now, self._say = poll, stale_after, sleep, now, say
        self._lock = threading.Lock()
        self._since: float | None = None        # when this gate first saw the live PAUSE it is in
        self._warned = False
        self._unreadable_since: float | None = None

    def _live(self, now: float) -> bool:
        try:
            age = now - self.path.stat().st_mtime
        except FileNotFoundError:
            self._warned, self._unreadable_since = False, None
            return False
        except OSError as e:
            # The wrapper is replacing the file at this instant (Windows refuses a stat then): still a pause. A file that stays
            # unreadable as long as a lease lasts is as dead as a lease nobody renews.
            self._unreadable_since = self._unreadable_since if self._unreadable_since is not None else now
            if now - self._unreadable_since <= self.stale_after:
                return True
            age = now - self._unreadable_since
            phrase = f"has been unreadable for {age:.0f} s ({e})"
        else:
            self._unreadable_since = None
            phrase = f"was last rewritten {age:.0f} s ago"
            if age > self.stale_after and not self._leased():
                return True         # made by hand: nobody renews it and nobody abandoned it
        if age > self.stale_after:
            if not self._warned:
                self._warned = True
                self._say(f"{self.path} {phrase}: the wrapper that made it stopped, so it is ignored and the run goes on")
            return False
        self._warned = False
        return True

    def _leased(self) -> bool:
        """Whether the PAUSE is the wrapper's (its first line starts LEASE_MARK), the only kind that expires."""
        try:
            with self.path.open(encoding="utf-8", errors="replace") as f:
                return f.readline().startswith(LEASE_MARK)
        except OSError:
            return True             # cannot be read and was not rewritten: as dead as an abandoned lease

    def _append(self, record: dict[str, Any]) -> None:
        with self.log.open("a", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps(record) + "\n")

    def active(self) -> bool:
        with self._lock:
            now = self._now()
            live = self._live(now)
            if live and self._since is None:
                self._since = now
                self._append({"at": _iso(now), "event": "pause"})
                self._say(f"PAUSE: {self.path} exists; generations wait until it is removed")
            elif not live and self._since is not None:
                self._append({"at": _iso(now), "event": "resume", "seconds": round(now - self._since, 1)})
                self._say(f"PAUSE over after {now - self._since:.0f} s; generations resume")
                self._since = None
            return live

    def wait(self) -> None:
        while self.active():
            self.sleep(self.poll)


@dataclasses.dataclass(frozen=True)
class Pauses:
    count: int          # pauses the run saw
    seconds: float      # their total length
    open: bool          # the run ended (or was killed) inside one


def read_pauses(run_dir: Path) -> Pauses:
    """The pauses a run saw, from its PAUSE_LOG (none when the file is absent)."""
    path = run_dir / PAUSE_LOG
    if not path.exists():
        return Pauses(0, 0.0, False)
    count, seconds, inside = 0, 0.0, False
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        record = json.loads(line)
        if record["event"] == "pause":
            count, inside = count + 1, True
        else:
            seconds, inside = seconds + record["seconds"], False
    return Pauses(count, seconds, inside)


def lossless(gate: PauseGate, call: Callable[[], Any], requeued: Callable[[BaseException], None],
             settle: float = RUNNER_SETTLE_SECONDS) -> Any:
    """`call()` after the pause, asked AGAIN whenever a pause ends it, and ONCE MORE if the runner fails outside a pause
    (module docstring): `requeued(error)` is told of every failure that is asked again. A failure under a pause is the
    pause's doing and costs no retry budget (every ask waits for the PAUSE to clear first, so this is no busy loop); a runner
    failure outside a pause waits `settle` seconds for the runner to restart and is the one retry the model is allowed. A
    failure outside a pause that is no runner failure, or the second runner failure, propagates: it is a RUNNER_ERROR."""
    runner_retried = False
    while True:
        gate.wait()
        try:
            return call()
        except MODEL_ERRORS as e:
            if gate.active():
                requeued(e)
                continue
            if runner_retried or not runner_failed(e):
                raise
            runner_retried = True
            requeued(e)
            gate.sleep(settle)


# ── self-test ─────────────────────────────────────────────────────────────────────────────────────────────────────────

class _StubOllama(http.server.BaseHTTPRequestHandler):
    """A stub Ollama over a local socket: the model name picks the behavior (`server.behaviors`). It speaks chunked NDJSON
    like the real server, so a stream cut mid-reply is the real IncompleteRead."""

    protocol_version = "HTTP/1.1"

    def log_message(self, *_args: object) -> None:
        pass

    def do_POST(self) -> None:  # noqa: N802 — the http.server name
        body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        self.server.requests.append(body)  # type: ignore[attr-defined]
        try:
            self.server.behaviors[body["model"]](self, body)  # type: ignore[attr-defined]
        finally:
            # A COUNT of handlers that returned, never one shared flag: the previous request's handler can still be in this
            # finally when the next request starts, and a flag it set after the next request cleared it read as the next
            # handler's end (kb/Work PB3867: 1 run in 8 on Linux).
            with self.server.handled_cv:  # type: ignore[attr-defined]
                self.server.handled += 1  # type: ignore[attr-defined]
                self.server.handled_cv.notify_all()  # type: ignore[attr-defined]

    def begin(self) -> None:
        self.send_response(200)
        self.send_header("Content-Type", "application/x-ndjson")
        self.send_header("Transfer-Encoding", "chunked")
        self.end_headers()

    def send(self, obj: dict[str, Any]) -> bool:
        """One NDJSON line as one chunk; False once the client has closed the connection."""
        data = (json.dumps(obj) + "\n").encode("utf-8")
        try:
            self.wfile.write(f"{len(data):x}\r\n".encode("ascii") + data + b"\r\n")
            self.wfile.flush()
            return True
        except OSError:
            self.server.client_left = True  # type: ignore[attr-defined]
            self.close_connection = True
            return False

    def end(self) -> None:
        try:
            self.wfile.write(b"0\r\n\r\n")
            self.wfile.flush()
        except OSError:
            self.close_connection = True

    def fail(self, status: int, message: str) -> None:
        data = json.dumps({"error": message}).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)


def _stub_behaviors(server: http.server.HTTPServer) -> dict[str, Callable[[_StubOllama, dict[str, Any]], None]]:
    def tokens(text: str) -> list[str]:
        return [text[i:i + 3] for i in range(0, len(text), 3)]

    def ok(h: _StubOllama, _b: dict[str, Any]) -> None:
        h.begin()
        for piece in tokens("IDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.\n<|eot|>"):
            h.send({"message": {"role": "assistant", "content": piece}, "done": False})
        h.send({"message": {"role": "assistant", "content": ""}, "done": True, "done_reason": "stop"})
        h.end()

    def endless(h: _StubOllama, _b: dict[str, Any]) -> None:
        # more than any socket buffer holds: the loop ends only because the client closed the connection
        h.begin()
        h.send({"message": {"content": "```cobol\nWORKING-STORAGE SECTION.\n"}, "done": False})
        for k in range(1, 200_001):
            if not h.send({"message": {"content": f"01 WS-ARG{k} PIC X(10) VALUE \"ARG {k}\".\n"}, "done": False}):
                return
        server.sent_all = True  # type: ignore[attr-defined]

    def length(h: _StubOllama, _b: dict[str, Any]) -> None:
        h.begin()
        h.send({"message": {"content": "partial", "thinking": "hmm"}, "done": False})
        h.send({"message": {"content": ""}, "done": True, "done_reason": "length"})
        h.end()

    def error_line(h: _StubOllama, _b: dict[str, Any]) -> None:
        h.begin()
        h.send({"message": {"content": "IDENT"}, "done": False})
        h.send({"error": "an error was encountered while running the model: unexpected EOF"})
        h.end()

    def repeat_limit(h: _StubOllama, _b: dict[str, Any]) -> None:
        h.begin()
        h.send({"message": {"content": "A"}, "done": False})
        h.send({"error": "prediction aborted, token repeat limit reached"})
        h.end()

    def aborted_runner_died(h: _StubOllama, _b: dict[str, Any]) -> None:
        h.begin()
        h.send({"error": "prediction aborted: llama-server process has terminated: exit status 0xffffffff"})
        h.end()

    def aborted_http(h: _StubOllama, _b: dict[str, Any]) -> None:
        h.fail(500, "prediction aborted, token repeat limit reached")

    def cut(h: _StubOllama, _b: dict[str, Any]) -> None:
        h.begin()
        h.send({"message": {"content": "IDENT"}, "done": False})
        h.close_connection = True               # no last chunk: the runner died mid-reply

    def think_refused(h: _StubOllama, body: dict[str, Any]) -> None:
        if "think" in body:
            h.fail(400, "\"think-refused\" does not support thinking")
        else:
            ok(h, body)

    return {"ok": ok, "endless": endless, "length": length, "error_line": error_line, "repeat_limit": repeat_limit,
            "aborted_runner_died": aborted_runner_died, "aborted_http": aborted_http, "cut": cut, "think_refused": think_refused,
            "http500": lambda h, _b: h.fail(500, "an error was encountered while running the model"),
            "http400": lambda h, _b: h.fail(400, "invalid options")}


def reference_cut(lines: list[str]) -> int | None:
    """The same answer as EnumerationWatch by brute force, for the self-test: the first moment the last DEGENERATE_LINES
    non-blank lines hold at most DEGENERATE_FORMS forms, and the offset where that window's KEPT_WINDOW_LINES + 1st line
    starts."""
    kept = [(line_form(line), sum(len(x) + 1 for x in lines[:i])) for i, line in enumerate(lines) if line_form(line)]
    for end in range(DEGENERATE_LINES, len(kept) + 1):
        window = kept[end - DEGENERATE_LINES:end]
        if len({form for form, _ in window}) <= DEGENERATE_FORMS:
            return window[KEPT_WINDOW_LINES][1]
    return None


def self_test() -> int:
    rc = 0

    def check(name: str, ok: bool, got: object = None) -> None:
        nonlocal rc
        print(("  ok: " if ok else "  SELF-TEST FAILED: ") + name + ("" if ok else f" — got {got!r}"))
        rc |= 0 if ok else 1

    print("=== fuzz_ollama.py --self-test ===")

    # ── the watch (kb/Work PB3537): every case against the brute-force reference, whole and in pieces
    def watched(lines: list[str], piece: int = 0) -> int | None:
        watch, text = EnumerationWatch(), "".join(line + "\n" for line in lines)
        step = piece or len(text) or 1
        for i in range(0, len(text), step):
            cut = watch.feed(text[i:i + step])
            if cut is not None:
                return cut
        return None

    head = ["IDENTIFICATION DIVISION.", "PROGRAM-ID. FUZZ.", "DATA DIVISION.", "WORKING-STORAGE SECTION.", "01 WS-HEAD PIC 9."]
    enumeration = [f"01 WS-ARG{k} PIC X(10) VALUE \"ARG {k}\"." for k in range(1, DEGENERATE_LINES + 40)]
    pure = enumeration[:DEGENERATE_LINES]
    check(f"watch: {DEGENERATE_LINES} lines that differ only by a counter are degenerate, one fewer is not; the cut keeps the "
          f"window's first {KEPT_WINDOW_LINES} lines",
          watched(pure) == len("".join(x + "\n" for x in pure[:KEPT_WINDOW_LINES])) and watched(pure[:-1]) is None,
          (watched(pure), watched(pure[:-1])))
    check("watch: after a program's head the enumeration trips it as soon as the window holds at most "
          f"{DEGENERATE_FORMS} forms (the head's last lines may still be in it), and the answer is the reference's",
          watched(head + enumeration) is not None and watched(head + enumeration) == reference_cut(head + enumeration)
          and watched(head + enumeration[:DEGENERATE_LINES - len(head)]) is None,
          (watched(head + enumeration), reference_cut(head + enumeration)))
    check("watch: the answer does not depend on how the stream splits the text (a character at a time, seven at a time)",
          watched(head + enumeration, 1) == watched(head + enumeration) == watched(head + enumeration, 7))
    templates = [f"MOVE {k} TO WS-{'ABC'[k % 3]}{k}." if k % 3 else f"DISPLAY \"R{k}\"." for k in range(1, 400)]
    cycle4 = [f"{'ABCD'[k % 4]} STEP {k} OF THE LOOP" for k in range(1, 400)]
    check(f"watch: a cycle of {DEGENERATE_FORMS} templates is the same enumeration, a cycle of {DEGENERATE_FORMS + 1} is not "
          "(and both agree with the reference)",
          watched(templates) is not None and watched(templates) == reference_cut(templates)
          and watched(cycle4) is None and reference_cut(cycle4) is None)
    program = head + [f"01 WS-{n} PIC 9(4) VALUE {k}." for k, n in enumerate("ALPHA BRAVO CHARLIE DELTA ECHO FOXTROT".split())] + \
        ["", "PROCEDURE DIVISION."] + [f"    DISPLAY \"STEP {k}\" WS-ALPHA" for k in range(60)] + ["    STOP RUN."]
    check("watch: an ordinary program, blank lines and a 60-line run of DISPLAYs included, is never degenerate",
          watched(program) is None and reference_cut(program) is None)
    spaced = [line for e in enumeration for line in (e, "", "   ")]
    check("watch: blank lines carry no form, so they neither fill nor reset the window",
          watched(spaced) is not None and watched(spaced) == reference_cut(spaced))
    check("watch: the thresholds are the measured ones (module docstring)", (DEGENERATE_LINES, DEGENERATE_FORMS) == (150, 3))

    # ── the chat, against a stub Ollama
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), _StubOllama)
    server.requests = []  # type: ignore[attr-defined]
    server.handled = 0  # type: ignore[attr-defined]
    server.handled_cv = threading.Condition()  # type: ignore[attr-defined]
    server.client_left = False  # type: ignore[attr-defined]
    server.sent_all = False  # type: ignore[attr-defined]
    server.behaviors = _stub_behaviors(server)  # type: ignore[attr-defined]
    server.daemon_threads = True
    host = f"http://127.0.0.1:{server.server_address[1]}"
    threading.Thread(target=server.serve_forever, daemon=True).start()

    def chat(model: str, **kw: Any) -> tuple[str, float, str, int] | OllamaError:
        try:
            return ollama_chat(model, [{"role": "user", "content": "x"}], 0.3, host=host, timeout=30, **kw)
        except OllamaError as e:
            return e

    try:
        got = chat("ok", options={"repeat_penalty": 1.1, "repeat_last_n": 64})
        sent = server.requests[-1]  # type: ignore[attr-defined]
        check("chat: a streamed reply is joined, its <|eot|> marker dropped, its done_reason Ollama's; the request streams and "
              "carries the model's extra options beside the harness's own",
              isinstance(got, tuple) and got[0] == "IDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.\n" and got[2] == "stop"
              and got[3] == 0 and sent["stream"] is True and sent["options"]["repeat_penalty"] == 1.1
              and sent["options"]["repeat_last_n"] == 64 and sent["options"]["num_predict"] == NUM_PREDICT
              and sent["options"]["num_ctx"] == NUM_CTX and sent["options"]["temperature"] == 0.3, (got, sent))
        got = chat("length")
        check("chat: a reply Ollama cut at the token cap says so, with the thinking it spent counted",
              isinstance(got, tuple) and got[0] == "partial" and got[2] == "length" and got[3] == 3, got)
        got = chat("endless")
        # Every request so far, this one included (its body was recorded before it answered), has a handler that returned:
        # the stub then knows whether the client left. A hang guard for the stub's thread, never a verdict.
        with server.handled_cv:  # type: ignore[attr-defined]
            server.handled_cv.wait_for(lambda: server.handled >= len(server.requests), 30)  # type: ignore[attr-defined]
        check("chat: an enumeration is cut by the harness (done_reason degenerate, the head and the window's first lines kept) "
              "and the connection is closed, so Ollama stops generating instead of running to the cap",
              isinstance(got, tuple) and got[2] == DEGENERATE
              and got[0] == "```cobol\nWORKING-STORAGE SECTION.\n01 WS-ARG1 PIC X(10) VALUE \"ARG 1\".\n01 WS-ARG2 PIC X(10) VALUE \"ARG 2\".\n"
              and server.client_left and not server.sent_all,  # type: ignore[attr-defined]
              (got, server.client_left, server.sent_all))  # type: ignore[attr-defined]
        got, got_http = chat("repeat_limit"), chat("aborted_http")
        died = chat("aborted_runner_died")
        check("chat: Ollama's own repeat-limit abort, as an error line or an HTTP error, is a reply with no program, done_reason "
              "aborted, its message the recorded reply; one that names the runner's death is a runner failure, not a verdict "
              "on the model",
              isinstance(got, tuple) and "token repeat limit" in got[0] and got[2] == ABORTED
              and isinstance(got_http, tuple) and "token repeat limit" in got_http[0] and got_http[2] == ABORTED
              and isinstance(died, OllamaError) and runner_failed(died), (got, got_http, died))
        check("chat: the two cut shapes are one failure shape", set(DEGENERATE_REASONS) == {DEGENERATE, ABORTED})
        err_line, err_cut, err500, err400 = chat("error_line"), chat("cut"), chat("http500"), chat("http400")
        check("chat: an error line after the reply began, a stream cut before its last chunk and an HTTP 500 are runner failures "
              "(OllamaError, server side, runner_failed); an HTTP 400 is Ollama refusing the request, not a runner failure",
              isinstance(err_line, OllamaError) and err_line.status is None and "unexpected EOF" in str(err_line)
              and isinstance(err_cut, OllamaError) and err_cut.status is None and err_cut.server_side and runner_failed(err_cut)
              and isinstance(err500, OllamaError) and err500.status == 500 and runner_failed(err500) and "HTTP 500" in str(err500)
              and isinstance(err400, OllamaError) and err400.status == 400 and not err400.server_side and not runner_failed(err400),
              (err_line, err_cut, err500, err400))
        before = len(server.requests)  # type: ignore[attr-defined]
        got = chat("think_refused")
        check("chat: a model that refuses the `think` field is asked again without it",
              isinstance(got, tuple) and got[2] == "stop" and len(server.requests) == before + 2  # type: ignore[attr-defined]
              and "think" in server.requests[-2] and "think" not in server.requests[-1], got)  # type: ignore[attr-defined]
    finally:
        server.shutdown()
        server.server_close()

    check("runner_failed: a refused or reset connection and a URL error are runner failures; a timeout and an unrelated error are not",
          runner_failed(ConnectionRefusedError()) and runner_failed(ConnectionResetError())
          and runner_failed(urllib.error.URLError("refused")) and not runner_failed(TimeoutError())
          and not runner_failed(RuntimeError("x")) and not runner_failed(ValueError("x")))
    check("unload: a model that cannot be reached is no error (the stub's closed port refuses)", unload("nothing", host=host) is None)

    # ── the model options
    check("options: MODEL:KEY=VALUE, the model tag's own colon kept, each typed; a repeat gathers under the model",
          parse_model_options(["nemotron-3.5-bolt:latest:repeat_penalty=1.15", "nemotron-3.5-bolt:latest:repeat_last_n=128",
                               "qwen3.6:35b:repeat_penalty=1"])
          == {"nemotron-3.5-bolt:latest": {"repeat_penalty": 1.15, "repeat_last_n": 128}, "qwen3.6:35b": {"repeat_penalty": 1.0}})
    bad = []
    for item in ("repeat_penalty=1.1", "m:top_k=3", "m:repeat_last_n=1.5", "m:repeat_penalty", ":repeat_penalty=1"):
        try:
            parse_model_options([item])
        except ValueError as e:
            bad.append(str(e))
    check("options: a missing model, an unknown key, a value of the wrong type and a missing value are each refused, naming the item",
          len(bad) == 5 and all("--model-option" in b for b in bad), bad)

    # ── the pause gate (kb/Work PB3561), on an injected clock
    with tempfile.TemporaryDirectory() as td:
        run = Path(td)
        clock = [1_000_000.0]
        said: list[str] = []
        slept: list[float] = []

        def touch(g: PauseGate) -> None:
            """What the wrapper does on every poll: create or rewrite PAUSE (its mtime is the lease)."""
            g.path.write_text(LEASE_MARK + " paused\n", encoding="utf-8", newline="\n")
            os.utime(g.path, (clock[0], clock[0]))

        def sleeper(seconds: float) -> None:
            slept.append(seconds)
            clock[0] += seconds
            if len(slept) == 3:
                gate.path.unlink()      # the wrapper's resume

        gate = PauseGate(run, poll=2.0, stale_after=60.0, now=lambda: clock[0], sleep=sleeper, say=said.append)
        check("pause: no PAUSE file, no pause, and nothing logged", gate.active() is False and not gate.log.exists())
        gate.wait()
        check("pause: wait() returns at once when there is no pause", slept == [])
        touch(gate)
        gate.wait()
        log = [json.loads(x) for x in gate.log.read_text(encoding="utf-8").splitlines()]
        check("pause: wait() blocks, polling, while PAUSE exists and returns when it is removed; the pause and the resume are "
              "logged, the resume with the seconds this gate saw",
              slept == [2.0, 2.0, 2.0] and [r["event"] for r in log] == ["pause", "resume"] and log[1]["seconds"] == 6.0
              and read_pauses(run) == Pauses(1, 6.0, False) and any("PAUSE:" in s for s in said), (slept, log))
        touch(gate)
        clock[0] += 61
        check("pause: a PAUSE its wrapper stopped rewriting is stale: ignored, with one message however often it is asked",
              gate.active() is False and gate.active() is False and sum("stopped" in s for s in said) == 1, said)
        touch(gate)
        check("pause: the wrapper's next rewrite makes it live again", gate.active() is True)
        gate.path.unlink()
        check("pause: the log counts the second pause, ended by the removal", gate.active() is False and read_pauses(run).count == 2,
              read_pauses(run))
        (run / PAUSE_LOG).write_text(json.dumps({"at": "x", "event": "pause"}) + "\n", encoding="utf-8", newline="\n")
        check("pause: a run killed inside a pause reads as open", read_pauses(run) == Pauses(1, 0.0, True))
        check("pause: a run that never paused reads as none", read_pauses(run / "nowhere") == Pauses(0, 0.0, False))

        class Locked:
            """A PAUSE that cannot be statted (the wrapper is replacing it; or a permission that never comes right)."""
            def stat(self) -> os.stat_result:
                raise PermissionError("locked")

        said3: list[str] = []
        gate3 = PauseGate(run / "g3", poll=1.0, stale_after=60.0, now=lambda: clock[0], sleep=lambda _s: None, say=said3.append)
        (run / "g3").mkdir()
        gate3.path = Locked()  # type: ignore[assignment]
        first = gate3.active()
        clock[0] += 30
        second = gate3.active()
        clock[0] += 31
        third = gate3.active()
        check("pause: a PAUSE that cannot be read counts as a pause for as long as a lease lasts, then as a dead wrapper's, said once",
              (first, second, third) == (True, True, False) and gate3.active() is False
              and sum("unreadable" in s for s in said3) == 1 and read_pauses(run / "g3") == Pauses(1, 61.0, False),
              (first, second, third, said3, read_pauses(run / "g3")))

        # ── lossless (kb/Work PB3561 item 1): the second gate's PAUSE is created by the first call's failure, as the wrapper
        # creates it before it ends the runner, and removed when the gate has polled once
        (run / "g2").mkdir()
        polls: list[float] = []
        gate2 = PauseGate(run / "g2", poll=1.0, stale_after=1e9, now=lambda: clock[0],
                          sleep=lambda s: (polls.append(s), gate2.path.unlink(missing_ok=True))[0], say=lambda _m: None)

        def scripted(*outcomes: object, pause_on: tuple[int, ...] = ()) -> tuple[Callable[[], Any], list[str]]:
            calls: list[str] = []

            def call() -> Any:
                calls.append("call")
                if len(calls) in pause_on:
                    touch(gate2)
                outcome = outcomes[len(calls) - 1]
                if isinstance(outcome, BaseException):
                    raise outcome
                return outcome
            return call, calls

        def outcome_of(call: Callable[[], Any], requeues: list[BaseException]) -> Any:
            try:
                return lossless(gate2, call, requeues.append)
            except MODEL_ERRORS as e:
                return e

        runner_down = OllamaError("ollama HTTP 500: an error was encountered while running the model", 500)
        requeues: list[BaseException] = []
        call, calls = scripted(runner_down, ("reply", 1.0, "stop", 0), pause_on=(1,))
        got = outcome_of(call, requeues)
        check("lossless: a generation that fails while PAUSE exists is asked again once the pause is over, and its second "
              "answer is the result; the re-queue is reported",
              got == ("reply", 1.0, "stop", 0) and len(calls) == 2 and requeues == [runner_down] and polls == [1.0],
              (got, calls, requeues, polls))
        timed_out = TimeoutError("timed out")
        requeues.clear()
        call, calls = scripted(timed_out, "again", pause_on=(1,))
        check("lossless: any failure while PAUSE exists is re-queued, a timeout too",
              outcome_of(call, requeues) == "again" and requeues == [timed_out])
        requeues.clear()
        polls.clear()
        call, calls = scripted(runner_down, "again")
        check("lossless: a runner failure outside a pause is also asked again once, after the settle for the runner to restart",
              outcome_of(call, requeues) == "again" and len(calls) == 2 and requeues == [runner_down]
              and polls == [RUNNER_SETTLE_SECONDS], (calls, requeues, polls))
        for name, failure in (("a timeout", TimeoutError("t")), ("a refused request", OllamaError("ollama HTTP 400: x", 400)),
                              ("an unrelated RuntimeError", RuntimeError("x"))):
            requeues.clear()
            call, calls = scripted(failure, "never")
            check(f"lossless: {name} outside a pause is terminal on the first call (a RUNNER_ERROR row in the harness)",
                  outcome_of(call, requeues) is failure and len(calls) == 1 and requeues == [], (calls, requeues))
        requeues.clear()
        second = OllamaError("ollama HTTP 500: again", 500)
        call, calls = scripted(runner_down, second)
        check("lossless: a second runner failure outside a pause is terminal, after exactly one re-queue",
              outcome_of(call, requeues) is second and len(calls) == 2 and requeues == [runner_down])
        # Review R1-1: a pause costs no retry budget, so a retry that a SECOND pause kills is asked again too
        requeues.clear()
        call, calls = scripted(runner_down, runner_down, ("reply", 1.0, "stop", 0), pause_on=(1, 2))
        check("lossless: a generation killed by a pause and again by a second pause is asked a third time and keeps its program "
              "(review R1-1: the first pause used up the only retry, the second recorded a RUNNER_ERROR)",
              outcome_of(call, requeues) == ("reply", 1.0, "stop", 0) and len(calls) == 3 and requeues == [runner_down] * 2,
              (calls, requeues))
        requeues.clear()
        polls.clear()
        call, calls = scripted(runner_down, runner_down, "again", pause_on=(1,))
        check("lossless: the runner's own one retry survives an earlier pause (a pause, then a runner failure outside it, then the answer)",
              outcome_of(call, requeues) == "again" and len(calls) == 3 and requeues == [runner_down] * 2
              and polls == [1.0, RUNNER_SETTLE_SECONDS], (calls, requeues, polls))
        requeues.clear()
        call, calls = scripted(runner_down, runner_down, second, pause_on=(1,))
        check("lossless: after a pause the second runner failure outside a pause is still terminal",
              outcome_of(call, requeues) is second and len(calls) == 3 and requeues == [runner_down] * 2, (calls, requeues))

        # Review R1-3: only the wrapper's PAUSE is a lease; one made by hand holds until it is removed
        (run / "g4").mkdir()
        gate4 = PauseGate(run / "g4", poll=1.0, stale_after=60.0, now=lambda: clock[0], sleep=lambda _s: None, say=lambda _m: None)
        gate4.path.write_text("", encoding="utf-8", newline="\n")
        os.utime(gate4.path, (clock[0], clock[0]))
        clock[0] += 3600
        by_hand = gate4.active()
        touch_path = gate4.path
        touch_path.write_text(LEASE_MARK + " paused for a game\n", encoding="utf-8", newline="\n")
        os.utime(touch_path, (clock[0] - 3600, clock[0] - 3600))
        check("pause: a PAUSE made by hand (empty, an hour old) still pauses, and the wrapper's (first line 'lease:') of the same age is "
              "dead (review R1-3: the printed 'create PAUSE' line promised a pause that ended after five minutes)",
              by_hand is True and gate4.active() is False, (by_hand,))
    print("self-test " + ("PASSED" if rc == 0 else "FAILED"))
    return rc


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--self-test", action="store_true", help="a stub Ollama over a local socket, then the unit cases")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    ap.print_usage()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
