"""docchecks/tree.py — the byte-level facts about every tracked Markdown file (kb/Work PB4453).

NulByteDriftTests swept the text kinds of src/, tests/ and conformance-suite/ for a NUL byte (kb/Work PB927: an agent's
tool write once left NULs in a file), `.md` among them. A compiler test no longer reads a Markdown file, so the Markdown
half of that sweep is HERE, over EVERY tracked `*.md` rather than the three trees. A file encoded as UTF-16 or UTF-32 holds
NULs by construction; its byte-order mark says so, and it is set aside structurally.
"""
from __future__ import annotations

from pathlib import Path

from . import common
from .common import Check

BOMS = (b"\xff\xfe", b"\xfe\xff", b"\x00\x00\xfe\xff")
#: A listing that finds fewer Markdown files than this is a broken listing, never a clean tree.
MIN_MARKDOWN = 500


#: The tracked Markdown files that legitimately hold a NUL, each with why. A file is here only when no agent's tool write
#: produced its bytes: RECORD DATA a program wrote and a report quotes byte for byte. A stale entry (the file no longer holds a
#: NUL) is a finding, so the list cannot rot into a blanket exemption.
EXEMPT = {
    "docs/rearchitecture/evidence/fleet-optimization/2026-10-21-differential-fuzz-overnight/report.md":
        "the overnight differential-fuzz report quotes the two compilers' output for a FORMAT2 edited-numeric program byte for "
        "byte, and WiseOwl's output holds NUL characters in the zero-suppressed positions (the finding it reports); a frozen "
        "evidence record, never edited",
}


def nul_findings(files: dict[str, bytes], exempt: dict[str, str] = EXEMPT) -> list[str]:
    out = [] if len(files) >= MIN_MARKDOWN else [f"only {len(files)} tracked Markdown file(s) found: the listing is broken"]
    for path, data in sorted(files.items()):
        holds = b"\0" in data and not data.startswith(BOMS)
        if holds and path not in exempt:
            out.append(f"{path} holds a NUL byte at offset {data.index(b'\0')} (an agent tool write can leave one, kb/Work PB927)")
        if not holds and path in exempt:
            out.append(f"{path} is exempt for holding a NUL byte and no longer does: remove the exemption")
    out += [f"the exemption {p} names no tracked Markdown file: remove it" for p in sorted(exempt) if p not in files]
    return out


def _markdown(root: Path) -> dict[str, bytes]:
    return {p: (root / p).read_bytes() for p in common.tracked(root, "*.md")}


CHECKS = (Check("MARKDOWN FILES hold no NUL byte", lambda r: nul_findings(_markdown(r))),)


def self_test() -> list[str]:
    many = {f"d/{n}.md": b"text" for n in range(MIN_MARKDOWN)}
    return common.self_test_arms([
        ("a clean tree has no finding", nul_findings(many, {}), []),
        ("a NUL is named with its offset", nul_findings({**many, "a.md": b"ab\0c"}, {}), ["a.md holds a NUL byte at offset 2 (an agent tool write can leave one, kb/Work PB927)"]),
        ("a UTF-16 file is set aside by its byte-order mark", nul_findings({**many, "a.md": b"\xff\xfea\0b\0"}, {}), []),
        ("a broken listing is a finding", len(nul_findings({"a.md": b"x"}, {})), 1),
        ("an exempt file holding a NUL is clean", nul_findings({**many, "a.md": b"a\0"}, {"a.md": "why"}), []),
        ("an exemption the file outgrew is a finding", len(nul_findings({**many, "a.md": b"a"}, {"a.md": "why"})), 1),
        ("an exemption naming no file is a finding", len(nul_findings(many, {"gone.md": "why"})), 1),
    ])
