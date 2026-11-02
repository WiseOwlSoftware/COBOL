"""THE ONCE RULE: a dispatch spec states each rule once (kb/Work PB3777, PB4419).

The implementer's instruction file is read on every turn, so a sentence it states twice is paid for twice and can
drift from its twin. `check_practices.py` holds every rendered spec to it (a SENTENCE of SENTENCE_MIN characters or
more, two words or more, whitespace collapsed, may not appear twice in one file); the planner's pred builders
(groups.py) meet it when one group takes in another (Group.absorb), because each pred carries the sentences its builder
always writes. Both read the same sentences, so this module is the rule's ONE home: the unit, the test and the repair.

A sentence is the text between the marks `. ! ? : ;` (a lone token is a path the spec names in two roles, never a rule:
kb/Work PB4005, hence the two-word floor).
"""
from __future__ import annotations

import re

#: A sentence shorter than this is never counted: a heading, a short label, a path.
SENTENCE_MIN = 60

_SPLIT = re.compile(r"(?<=[.!?:;])\s+")


def _counted(sentence: str) -> bool:
    return len(sentence) >= SENTENCE_MIN and " " in sentence


def sentences(text: str) -> list[str]:
    """The sentences of `text`, whitespace collapsed, in order."""
    return _SPLIT.split(" ".join(text.split()))


def repeated_sentences(text: str) -> list[str]:
    """The counted sentences `text` states more than once, in first-seen order."""
    seen: set[str] = set()
    again: list[str] = []
    for s in sentences(text):
        if _counted(s):
            if s in seen and s not in again:
                again.append(s)
            seen.add(s)
    return again


def without_repeats(text: str) -> str:
    """`text` with the first copy of every counted sentence kept and each later copy dropped (whitespace collapsed):
    `repeated_sentences` of the result is empty, and nothing but a copy is lost."""
    seen: set[str] = set()
    kept: list[str] = []
    for s in sentences(text):
        if _counted(s):
            if s in seen:
                continue
            seen.add(s)
        kept.append(s)
    return " ".join(kept)
