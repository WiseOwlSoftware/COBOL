#!/usr/bin/env python3
"""ci_workflow.py — ⭐ THE ONE READER OF A CI WORKFLOW'S SHAPE for the script self-tests (kb/Work PB3819, PB4395).

    from ci_workflow import jobs, job, steps     # a self-test under scripts/ (sys.path holds scripts/ for it)
    python scripts/ci_workflow.py --self-test

The jobs under a workflow's top-level `jobs:` key, each job's steps and the comments between them, for every check that
audits `.github/workflows/*.yml` (or a composite action's `steps:`). Ten C# drift classes once split the workflow their
own way, and a workflow edit one reader tolerated another misread (kb/Work PB3819), so there is one reader. It is a port
of the C# reader `tests/_shared/CiWorkflow.cs`, which the compiler's tests that still read a workflow keep until the
pipeline's checks have all left the compiler's test assembly (kb/Work PB4395 item 1: they become the self-tests of the
scripts they check, and the C# reader is deleted with its last caller).

The shape it reads is the one GitHub's workflow syntax gives these files: a top-level `jobs:` key; job ids two spaces
deep; a job's `steps:` list whose `- ` items sit deeper than the key. It is a LINE reader, not a YAML parser: block
scalars (`run: |`) are kept as the lines they are, and a flow-style job or step list is not understood: it is REFUSED
(`ValueError`) where it can be told apart, never read as empty, because a scan that looked at nothing must not report
clean.

Comments: a comment or blank line is attributed to what FOLLOWS it. A banner above a job key documents that job, and a
comment above a step documents that step; the trailing comments of a job or step belong to the next. A comment between
two lines of a job or step stays in its `lines`, and `code` drops it.
"""
from __future__ import annotations

import re
import sys
from collections.abc import Iterable, Iterator
from dataclasses import dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from standard_streams import ensure_utf8  # noqa: E402

JOBS_KEY = re.compile(r"^jobs:[ \t]*(?:#.*)?$")
JOB_HEADER = re.compile(r"^  (?P<id>[A-Za-z0-9_.-]+):[ \t]*(?:#.*)?$")
STEPS_KEY = re.compile(r"^\s*steps:[ \t]*(?:#.*)?$")
INLINE_STEPS = re.compile(r"^\s*steps:[ \t]*[^\s#]")
KEY = re.compile(r"^(?P<key>[A-Za-z0-9_.-]+):(?:[ \t]+(?P<value>.*))?$")
TRAILING_COMMENT = re.compile(r"[ \t]+#.*$")


def lines_of(text: str) -> list[str]:
    return text.replace("\r\n", "\n").replace("\r", "\n").split("\n")


def is_trivia(line: str) -> bool:
    t = line.lstrip()
    return not t or t[0] == "#"


def indent(line: str) -> int:
    return len(line) - len(line.lstrip())


def joined(lines: Iterable[str]) -> str:
    return "".join(f"{x}\n" for x in lines)


def own_keys(lines: Iterable[str], mapping_indent: int) -> Iterator[tuple[str, str]]:
    """The `key: value` pairs of a mapping whose keys sit `mapping_indent` deep (a `- ` item's first key counts at the
    item's indentation plus two), each value without its trailing comment."""
    for raw in lines:
        line, at = raw, indent(raw)
        if line.lstrip().startswith("- "):
            line, at = line.lstrip()[2:], at + 2
        if at != mapping_indent:
            continue
        m = KEY.match(line.lstrip())
        if m:
            yield m.group("key"), TRAILING_COMMENT.sub("", m.group("value") or "").strip()


def _code(lines: Iterable[str]) -> list[str]:
    return [x for x in lines if not x.lstrip().startswith("#")]


@dataclass(frozen=True)
class Step:
    lines: tuple[str, ...]
    item_indent: int

    @property
    def text(self) -> str:
        return joined(self.lines)

    @property
    def code_lines(self) -> list[str]:
        return _code(self.lines)

    @property
    def code(self) -> str:
        return joined(self.code_lines)

    @property
    def keys(self) -> list[tuple[str, str]]:
        return list(own_keys(self.lines, self.item_indent + 2))

    def value(self, key: str) -> str | None:
        return next((v for k, v in self.keys if k == key), None)

    def input(self, name: str) -> str | None:
        """The value of `with: <name>:` of this step."""
        in_with = False
        for raw in self.code_lines:
            line = (" " * (indent(raw) + 2) + raw.lstrip()[2:]) if raw.lstrip().startswith("- ") else raw
            at = indent(line)
            if at == self.item_indent + 2:
                in_with = line.strip().startswith("with:")
            elif in_with and at == self.item_indent + 4:
                key = next(own_keys([line], at), None)
                if key and key[0] == name:
                    return key[1]
        return None

    @property
    def label(self) -> str:
        return next((v for k, v in self.keys if k in ("name", "id", "uses")), self.lines[0].strip())


@dataclass(frozen=True)
class Job:
    id: str
    banner: tuple[str, ...]
    lines: tuple[str, ...]

    @property
    def text(self) -> str:
        return joined(self.lines)

    @property
    def code_lines(self) -> list[str]:
        return _code(self.lines)

    @property
    def code(self) -> str:
        return joined(self.code_lines)

    @property
    def steps(self) -> list[Step]:
        return steps(list(self.lines))

    def value(self, key: str) -> str | None:
        return next((v for k, v in own_keys(self.lines, 4) if k == key), None)


def has_jobs(text: str) -> bool:
    """Whether `text` has a top-level `jobs:` key: a workflow, as against a composite action."""
    return any(JOBS_KEY.match(x) for x in lines_of(text))


def jobs(workflow: str) -> list[Job]:
    """The jobs under the workflow's top-level `jobs:` key, in file order. Refuses a text with no `jobs:` key, or with
    one that holds no job: an empty answer would be indistinguishable from "every job is clean"."""
    ls = lines_of(workflow)
    start = next((i for i, x in enumerate(ls) if JOBS_KEY.match(x)), -1)
    if start < 0:
        raise ValueError("the workflow has no top-level `jobs:` key, so no job reader can have found anything")
    out: list[Job] = []
    jid: str | None = None
    banner: list[str] = []
    body: list[str] = []
    pending: list[str] = []
    for i in range(start + 1, len(ls)):
        line = ls[i]
        if is_trivia(line):
            pending.append(line)
            continue
        if line[0] != " ":
            break  # a top-level key after `jobs:` ends the jobs block
        header = JOB_HEADER.match(line)
        if header:
            if jid is not None:
                out.append(Job(jid, tuple(banner), tuple(body)))
            jid, banner, body, pending = header.group("id"), pending, [], []
            continue
        if jid is None:
            raise ValueError(f"line {i + 1} of the workflow sits under `jobs:` before any job id: `{line.strip()}` "
                             "(only block-style job ids two spaces deep are read)")
        body += pending
        pending = []
        body.append(line)
    if jid is not None:
        out.append(Job(jid, tuple(banner), tuple(body)))
    if not out:
        raise ValueError("the workflow's `jobs:` key holds no job")
    return out


def job(workflow: str, jid: str) -> Job:
    found = jobs(workflow)
    for j in found:
        if j.id == jid:
            return j
    raise ValueError(f"the workflow has no `{jid}` job (jobs: {', '.join(j.id for j in found)})")


def steps(lines: list[str] | str) -> list[Step]:
    """The `- ` items of the first block-style `steps:` list in `lines` (a job's lines, or a composite action's text);
    none when there is no `steps:` key. A flow-style list is refused."""
    if isinstance(lines, str):
        lines = lines_of(lines)
    at = -1
    for i, x in enumerate(lines):
        if INLINE_STEPS.match(x):
            raise ValueError(f"`{x.strip()}` is a flow-style step list (only block-style `steps:` lists are read)")
        if STEPS_KEY.match(x):
            at = i
            break
    if at < 0:
        return []
    key, item = indent(lines[at]), -1
    out: list[Step] = []
    current: list[str] = []
    pending: list[str] = []
    for line in lines[at + 1:]:
        if is_trivia(line):
            pending.append(line)
            continue
        at_ = indent(line)
        is_item = line.lstrip().startswith("- ") or line.strip() == "-"
        if at_ < key or (at_ == key and not (is_item and (item < 0 or item == key))):
            break  # as shallow as `steps:` itself (a list may sit at the key's own indentation): the list ended
        if is_item and (item < 0 or at_ == item):
            item = at_
            if current:
                out.append(Step(tuple(current), item))
            current = [line]
            pending = []  # the comments above this item document it, and are not part of its lines
            continue
        if not current:
            raise ValueError(f"`steps:` is followed by `{line.strip()}`, not a `- ` item (only block-style step lists "
                             "are read)")
        current += pending
        pending = []
        current.append(line)
    if current:
        out.append(Step(tuple(current), item))
    return out


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────

PLANTED = """name: x
on:
  push:
# banner of a
jobs:
  # banner of a
  a:
    runs-on: ubuntu-24.04   # pinned
    if: github.event_name == 'merge_group'
    steps:
      # documents step one
      - name: one
        # inside step one
        run: |
          echo one   # kept: a block scalar is its lines
        # trailing: it documents the next step
      - uses: actions/cache@v4
        with:
          key: k1
          path: p
  # banner of b
  b:
    runs-on: windows-2025
    steps:
    - id: two
      run: echo two
"""


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    js = jobs(PLANTED)
    expect("the jobs are the ids two spaces under `jobs:`, in file order", [j.id for j in js], ["a", "b"])
    a, b = js
    expect("a job's banner is the comment ABOVE its key, never the one above the job before it",
           (a.banner, b.banner), (("  # banner of a",), ("  # banner of b",)))
    expect("a job's own keys, their trailing comments dropped", (a.value("runs-on"), a.value("if"), b.value("if")),
           ("ubuntu-24.04", "github.event_name == 'merge_group'", None))
    sa = a.steps
    expect("a job's steps are its `- ` items, the comment above an item documenting it, not in the item before",
           ([s.label for s in sa], "# documents step one" in sa[0].text, "# documents" in a.text),
           (["one", "actions/cache@v4"], False, True))
    expect("a comment inside a step stays in its lines and leaves its code; a block scalar is kept as its lines; a "
           "trailing comment belongs to the next step",
           ("# inside step one" in sa[0].text, "# inside step one" in sa[0].code, "echo one   # kept" in sa[0].code,
            "# trailing" in sa[0].text), (True, False, True, False))
    expect("a step's `with:` input, and none it does not have", (sa[1].input("key"), sa[1].input("nope")), ("k1", None))
    expect("a step list at its key's own indentation is read", [s.label for s in b.steps], ["two"])
    expect("job() names the jobs it has when the one asked for is missing",
           _raises(lambda: job(PLANTED, "zz")), "the workflow has no `zz` job (jobs: a, b)")
    expect("a text with no `jobs:` key is refused, never read as no jobs", _raises(lambda: jobs("on:\n  push:\n")) != "",
           True)
    expect("a flow-style step list is refused", _raises(lambda: steps("    steps: [a, b]\n")) != "", True)
    expect("a composite action (no `jobs:` key) is told apart, and its steps are read",
           (has_jobs("runs:\n  steps:\n    - run: x\n"), [s.label for s in steps("runs:\n  steps:\n    - run: x\n")]),
           (False, ["- run: x"]))
    print(f"=== CI WORKFLOW READER SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def _raises(f) -> str:
    try:
        f()
    except ValueError as e:
        return str(e)
    return ""


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    print(__doc__.splitlines()[0])
    sys.exit(2)
