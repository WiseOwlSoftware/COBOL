"""docchecks/common.py — the shapes every documentation check shares (kb/Work PB4453).

A documentation check is a PURE FUNCTION of the text it is given: `name`, `inputs(root) -> inputs` reads the tree, and
`judge(inputs) -> findings` decides. Keeping the two apart is what lets each module's self-test plant a violation in
fabricated inputs and require the finding back (a guard that cannot fail is no evidence), with no scratch repository.
`doc_audits.py` runs every check of every module over the committed tree.
"""
from __future__ import annotations

import json
import re
import subprocess
import sys
from collections.abc import Callable, Iterable
from dataclasses import dataclass
from pathlib import Path

#: scripts/spec holds the ONE parser of docs/CONFORMANCE.md's registers (inventory_schema.register_section).
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "spec"))

#: The compiler-side facts the documents state (`tests/doc-facts/compiler-facts.json`, written by the Unit test
#: DocumentedFactsDriftTests and held equal to the compiler there): the Markdown is checked against THIS FILE, so a
#: compiler test never opens a Markdown file (kb/Work PB4453).
FACTS_PATH = "tests/doc-facts/compiler-facts.json"
CONFORMANCE = "docs/CONFORMANCE.md"
VCR = "docs/VERSION_CHANGE_REFERENCE.md"
README = "README.md"


@dataclass(frozen=True)
class Check:
    """One documentation check: `run(root)` returns its findings, an empty list when the documents agree."""
    name: str
    run: Callable[[Path], list[str]]


def text(root: Path, rel: str) -> str:
    """A tracked file's text, LF line endings (universal newlines), UTF-8."""
    return (root / rel).read_text(encoding="utf-8")


def lines(root: Path, rel: str) -> list[str]:
    return text(root, rel).split("\n")


def facts(root: Path) -> dict:
    return json.loads(text(root, FACTS_PATH))


def tracked(root: Path, *pathspecs: str) -> list[str]:
    """Tracked paths under the pathspecs that exist in the working tree (`git ls-files`)."""
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z", "--", *pathspecs], capture_output=True, check=True)
    return sorted(p for p in out.stdout.decode("utf-8").split("\0") if p and (root / p).is_file())


def section(body: str, heading: str, next_heading: str = "\n## ") -> str:
    """The text from `heading` to the next `## ` heading ("" when the heading is absent)."""
    at = body.find(heading)
    if at < 0:
        return ""
    rest = body[at + len(heading):]
    end = rest.find(next_heading)
    return rest if end < 0 else rest[:end]


def table_row(body: str, key: str) -> str | None:
    """The line of a register row whose first cell is `key` (`| DOC-A.1-62 |`), or None."""
    prefix = f"| {key} |"
    return next((ln for ln in body.split("\n") if ln.startswith(prefix)), None)


def need(findings: list[str], ok: bool, message: str) -> None:
    if not ok:
        findings.append(message)


def self_test_arms(arms: Iterable[tuple[str, object, object]]) -> list[str]:
    """`(name, got, want)` arms: the names of those whose result differs."""
    return [f"{name}: got {got!r}, wanted {want!r}" for name, got, want in arms if got != want]


def words(pattern: str, body: str) -> set[str]:
    return set(re.findall(pattern, body))
