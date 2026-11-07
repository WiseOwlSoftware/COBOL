"""fake_batches.py - THE fake Message Batches client of the batch tools' self-tests (test_transcribe_pdf.py,
test_audit_batch.py): it makes no API call. It keeps every accepted batch server-side, ends a batch when a test says so,
serves the results a test plants, lists batches newest first as the API does, and can die between the server accepting a
batch and the caller recording it (`crash_after_create`), the window message_batches.BatchRun.adopt_pending closes.
"""
from __future__ import annotations

import datetime
from types import SimpleNamespace
from typing import Any


class Crash(Exception):
    """A process death between the server accepting a batch and the state file recording it."""


class FakeBatches:
    def __init__(self) -> None:
        self.created: list[dict] = []      # server side: every accepted batch
        self.results_by_id: dict[str, list] = {}
        self.crash_after_create = False
        self.ended: set[str] = set()

    def create(self, requests):
        bid = f"msgbatch_{len(self.created) + 1}"
        self.created.append({"id": bid, "ids": [r["custom_id"] for r in requests], "requests": requests,
                             "at": datetime.datetime.now(datetime.timezone.utc)})
        if self.crash_after_create:
            self.crash_after_create = False
            raise Crash
        return self._view(bid)

    def _view(self, bid):
        b = next(x for x in self.created if x["id"] == bid)
        n = len(b["ids"])
        ended = bid in self.ended
        counts = SimpleNamespace(processing=0 if ended else n, succeeded=n if ended else 0, errored=0, canceled=0, expired=0)
        return SimpleNamespace(id=bid, processing_status="ended" if ended else "in_progress", request_counts=counts,
                               created_at=b["at"])

    def retrieve(self, bid):
        return self._view(bid)

    def list(self, limit):
        return [self._view(b["id"]) for b in reversed(self.created)][:limit]

    def results(self, bid):
        return iter(self.results_by_id[bid])


def client(fake: FakeBatches) -> Any:
    """An SDK-shaped client (`client.messages.batches`) over `fake`."""
    return SimpleNamespace(messages=SimpleNamespace(batches=fake))


def message(text: str, stop: str = "end_turn", input_tokens: int = 5000, output_tokens: int = 900) -> Any:
    return SimpleNamespace(stop_reason=stop, content=[SimpleNamespace(type="text", text=text)],
                           usage=SimpleNamespace(input_tokens=input_tokens, output_tokens=output_tokens))


def succeeded(custom_id: str, text: str, stop: str = "end_turn", **usage: int) -> Any:
    return SimpleNamespace(custom_id=custom_id, result=SimpleNamespace(type="succeeded", message=message(text, stop, **usage)))


def errored(custom_id: str) -> Any:
    return SimpleNamespace(custom_id=custom_id, result=SimpleNamespace(type="errored"))
