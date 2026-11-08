#!/usr/bin/env python3
"""message_batches.py - THE restartable state of a run of Message Batches API requests (kb/Work PB4816, PB4886).

Two spec tools bill the Message Batches API: the standard's transcriber (transcribe_pdf.py, one request per PDF page) and
the edition audit's judge (audit_batch.py, one request per rule or claim). They share this one implementation of what makes
a run restartable with minimal loss, so neither carries a copy:

  - `state.json` is written atomically (a temporary file, then a rename) BEFORE a batch is created, as an intent (when,
    which members), and AFTER, with the batch's id; a crash between the two is reconciled on the next run from the
    server's batch list (`adopt_pending`: the batch with the same request count, created after the intent);
  - `collect` takes each batch once it has ENDED and hands every succeeded reply that stopped at `end_turn` to the
    caller's `take`; a reply that errored, expired or was cut off, and one `take` refuses, is recorded as bad, and a
    member no answer covers and no uncollected batch holds is the caller's to retry;
  - the usage of EVERY succeeded reply is recorded (a cut-off reply is billed too), keyed by its batch AND its custom id
    (`usage_key`): a retried member's earlier billed reply keeps its own entry beside the retry's, and a collect repeated
    after a crash rewrites the same entries, so `cost_so_far` is what was spent;
  - the API key goes to the SDK client only (`load_key`): `state.json` records the NAME of the account the run bills
    (api_key.read_run_key keeps a later command to it), never a key.

A batch's MEMBERS are what its requests stand for: a page number for the transcriber (state key `pages`, custom id
`p0012`), a request's custom id for the audit (state key `items`). The state key and the two conversions are the caller's,
so a run directory that exists keeps its shape.

    python scripts/spec/test_transcribe_pdf.py   # and test_audit_batch.py: a fake client, no API call
"""
from __future__ import annotations

import json
import pathlib
import sys
import time
from typing import Any, Callable, Generic, Iterable, TypeVar

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/
from api_key import ApiKey, read_run_key  # noqa: E402  (THE reader of an API key file, kb/Work PB4816, PB4916)

#: The model the 1985 pilot chose (2026-11-06): Sonnet 5.5 got the words right but drew general formats wrongly.
DEFAULT_MODEL = "claude-opus-5-5"
#: Standard (non-batch) US dollars per million tokens, (input, output), by model; a batch costs half.
STANDARD_PRICE_PER_MTOK: dict[str, tuple[float, float]] = {"claude-opus-5-5": (4.0, 20.0)}
BATCH_DISCOUNT = 0.5
#: THE PROMPT CACHE OF A BATCH (Anthropic's notice of 2026-11-08: a low cache hit rate on this account's direct API
#: traffic). Every request of a pass shares its instructions, so they are a cached `system` block (`cached_system`). A
#: batch runs for minutes to hours and its cache hits are best-effort, so the entry lives an hour: a write of that TTL
#: costs CACHE_WRITE_MULTIPLIER times the input price, and a read CACHE_READ_MULTIPLIER[model] times it.
CACHE_TTL = "1h"
CACHE_WRITE_MULTIPLIER = 2.0
CACHE_READ_MULTIPLIER: dict[str, float] = {"claude-opus-5-5": 0.05}
#: The fewest requests that must share a block for caching it to pay: the first writes it at CACHE_WRITE_MULTIPLIER and
#: each later one reads it, so n requests cost 2 + 0.05 (n - 1) times the input against n uncached: from 3 on (review
#: w20261922c R1-3). A block fewer requests share is sent uncached.
CACHE_MIN_SHARERS = 3
#: A reply's usage counts, in the order `usd` takes them (THE one reader of them: `usage_of`).
USAGE_FIELDS = ("input_tokens", "output_tokens", "cache_creation_input_tokens", "cache_read_input_tokens")
#: One batch is capped at 256 MB by the API; a submit splits under this budget.
BATCH_BYTES = 180 * 1024 * 1024
#: The SDK retries a network error or a 5xx this many times, and waits this long for one reply.
CLIENT_RETRIES = 8
CLIENT_TIMEOUT_SECONDS = 900

M = TypeVar("M")


def write_atomic(path: pathlib.Path, text: str) -> None:
    """LF text, a temporary file then a rename: a crash never leaves half a file."""
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_text(text, encoding="utf-8", newline="\n")
    tmp.replace(path)


def make_client(key: str) -> Any:
    import anthropic  # noqa: PLC0415  (only the network commands need the SDK)
    return anthropic.Anthropic(api_key=key, max_retries=CLIENT_RETRIES, timeout=CLIENT_TIMEOUT_SECONDS)


def usd(model: str, input_tokens: int, output_tokens: int, cache_write_tokens: int = 0, cache_read_tokens: int = 0) -> float:
    """The batch price of one reply's tokens: its uncached input, its output, and the cache it wrote and read."""
    price_in, price_out = STANDARD_PRICE_PER_MTOK[model]
    cached = cache_write_tokens * CACHE_WRITE_MULTIPLIER + cache_read_tokens * CACHE_READ_MULTIPLIER[model]
    return ((input_tokens + cached) * price_in + output_tokens * price_out) / 1e6 * BATCH_DISCOUNT


def usage_of(message: Any) -> list[int]:
    """A reply's usage as `usd` takes it (USAGE_FIELDS), a count the reply does not report as none."""
    return [int(getattr(message.usage, f, 0) or 0) for f in USAGE_FIELDS]


def cached_text(text: str) -> dict[str, Any]:
    """`text`, a part many requests of a batch share, as a text block the prompt cache keeps for CACHE_TTL: the cache
    reads it for every later request whose prefix up to this block is the same."""
    return {"type": "text", "text": text, "cache_control": {"type": "ephemeral", "ttl": CACHE_TTL}}


def cached_system(text: str) -> list[dict[str, Any]]:
    """`text`, the part every request of a batch shares, as its cached `system`."""
    return [cached_text(text)]


def reply_text(message: Any) -> str:
    return "".join(c.text for c in message.content if c.type == "text")


class BatchRun(Generic[M]):
    """One run's `state.json`: `model`, `batches` (each `{id, <members key>: [...], collected?, bad?}` plus the caller's
    fields), `pending` (the intent of a create not yet recorded), `usage` (`usage_key` -> [input, output] tokens, one
    entry per billed reply) and `api_account`."""

    def __init__(self, state_file: pathlib.Path, members: str, member_of: Callable[[str], M],
                 custom_id: Callable[[M], str], tool: str):
        self.state_file, self.members, self.member_of, self.custom_id, self.tool = (
            state_file, members, member_of, custom_id, tool)

    def load(self, **fresh: Any) -> dict[str, Any]:
        """The recorded state, or a new one with `fresh`'s fields (never written until `save`)."""
        if self.state_file.exists():
            return json.loads(self.state_file.read_text(encoding="utf-8"))
        return {"model": DEFAULT_MODEL, "batches": [], "pending": None, **fresh}

    def save(self, st: dict[str, Any]) -> None:
        self.state_file.parent.mkdir(parents=True, exist_ok=True)
        write_atomic(self.state_file, json.dumps(st, indent=1))

    def load_key(self, key_file: str, account: str | None, st: dict[str, Any]) -> ApiKey:
        """The key of `account` in `key_file`, kept to the account `st` records (`read_run_key`), which it then records; a
        file with no such key, or another account than the state's, exits naming the path (never a key)."""
        try:
            chosen = read_run_key(key_file, account, st.get("api_account"), self.state_file)
        except RuntimeError as e:
            raise SystemExit(f"{self.tool}: {e}") from None
        st["api_account"] = chosen.account
        return chosen

    def covered(self, st: dict[str, Any]) -> set[M]:
        """Every member some recorded batch holds."""
        return {m for b in st["batches"] for m in b[self.members]}

    def in_flight(self, st: dict[str, Any]) -> list[M]:
        """The members of every batch not yet collected."""
        return [m for b in st["batches"] if not b.get("collected") for m in b[self.members]]

    @staticmethod
    def usage_key(batch_id: str, custom_id: str) -> str:
        """One billed reply. A member retried in a later batch is billed again, so its custom id alone would overwrite the
        earlier reply's usage and drop it from `cost_so_far`."""
        return f"{batch_id}/{custom_id}"

    def cost_so_far(self, st: dict[str, Any]) -> float:
        """The batch price of every reply `usage` records: what the run has been billed."""
        return sum(usd(st["model"], *u) for u in st.get("usage", {}).values())

    def adopt_pending(self, st: dict[str, Any], client: Any) -> None:
        """A crash between `create` and the state write: find the batch the server accepted (the same request count,
        created after the recorded intent, and not yet in the state) and record it; the intent is then cleared."""
        pend = st.get("pending")
        if not pend:
            return
        known = {b["id"] for b in st["batches"]}
        for b in client.messages.batches.list(limit=50):
            rc = b.request_counts
            total = rc.processing + rc.succeeded + rc.errored + rc.canceled + rc.expired
            if b.id not in known and total == len(pend[self.members]) and b.created_at.timestamp() >= pend["t"] - 60:
                st["batches"].append({"id": b.id, **{k: v for k, v in pend.items() if k != "t"}})
                print("adopted", b.id, "from an interrupted submit", flush=True)
                break
        st["pending"] = None
        self.save(st)

    def submit(self, st: dict[str, Any], client: Any, requests: Iterable[dict[str, Any]], **fields: Any) -> list[str]:
        """Batches of at most BATCH_BYTES (`requests` is read lazily, so a request is built only when its batch is), each
        recorded as an intent before it is created and as a batch after; `fields` go on each batch record. The new ids."""
        chunk: list[dict[str, Any]] = []
        size = 0
        created: list[str] = []

        def flush() -> None:
            nonlocal chunk, size
            if not chunk:
                return
            members = [self.member_of(r["custom_id"]) for r in chunk]
            st["pending"] = {"t": time.time(), self.members: members, **fields}
            self.save(st)
            b = client.messages.batches.create(requests=chunk)
            st["batches"].append({"id": b.id, self.members: members, **fields})
            st["pending"] = None
            self.save(st)
            created.append(b.id)
            print("submitted", b.id, f"{chunk[0]['custom_id']}-{chunk[-1]['custom_id']}", len(chunk),
                  f"{size / 1e6:.1f} MB", flush=True)
            chunk, size = [], 0

        for req in requests:
            n = len(json.dumps(req))
            if size + n > BATCH_BYTES:
                flush()
            chunk.append(req)
            size += n
        flush()
        return created

    def collect(self, st: dict[str, Any], client: Any, take: Callable[[M, Any, dict[str, Any]], str | None]) -> None:
        """Take every batch that has ENDED and is not yet collected. `take(member, message, batch)` stores one reply that
        stopped at `end_turn` and returns None, or returns why it refused it; every other result is recorded as bad."""
        for b in st["batches"]:
            bb = client.messages.batches.retrieve(b["id"])
            b["status"] = bb.processing_status
            print(b["id"], len(b[self.members]), bb.processing_status, flush=True)
            if bb.processing_status != "ended" or b.get("collected"):
                continue
            bad: list[list[str]] = []
            for res in client.messages.batches.results(b["id"]):
                if res.result.type != "succeeded":
                    bad.append([res.custom_id, res.result.type])
                    continue
                m = res.result.message
                st.setdefault("usage", {})[self.usage_key(b["id"], res.custom_id)] = usage_of(m)
                if m.stop_reason != "end_turn":
                    bad.append([res.custom_id, m.stop_reason])
                    continue
                why = take(self.member_of(res.custom_id), m, b)
                if why is not None:
                    bad.append([res.custom_id, why])
            b["bad"], b["collected"] = bad, True
            self.save(st)
            print("  collected; bad:", bad[:10], flush=True)
        self.save(st)
