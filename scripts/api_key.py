#!/usr/bin/env python3
"""api_key.py - THE reader of an API key file (kb/Work PB4816).

Three tools bill a Claude call to a key named by `--api-key-file`: the fuzz's CLI writer (fuzz_claude.py), its batch
driver (fuzz_batch.py) and the standard's transcriber (spec/transcribe_pdf.py). They share this one reader, so the key's
file format (first non-empty line, byte-order mark tolerated) and the rule that an error names the PATH and never the key
are written once.

    python scripts/api_key.py --self-test
"""
from __future__ import annotations

import os
import sys
import tempfile
from pathlib import Path

from standard_streams import ensure_utf8  # noqa: E402  (scripts/: UTF-8 standard streams)


def read_api_key(path: str | os.PathLike[str]) -> str:
    """The key held in the file `path`: its first non-empty line, stripped, a byte-order mark dropped (a file saved by
    Notepad carries one). A missing or empty file is a RuntimeError naming the path, never the key."""
    lines = [ln.strip() for ln in Path(path).read_text(encoding="utf-8-sig").splitlines() if ln.strip()] if Path(path).exists() else []
    if not lines:
        raise RuntimeError(f"no API key in {path}")
    return lines[0]


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(f"  {'ok  ' if cond else 'FAIL'} {name}")
        if not cond:
            failures.append(name)

    with tempfile.TemporaryDirectory() as tmp:
        keyfile = Path(tmp) / "key.txt"
        keyfile.write_text(chr(0xFEFF) + chr(10) + "  sk-test-key  " + chr(10) + "second", encoding="utf-8", newline="\n")
        check("the key file's first non-empty line is read, stripped, a byte-order mark dropped", read_api_key(keyfile) == "sk-test-key")
        empty = Path(tmp) / "empty.txt"
        empty.write_text(chr(10), encoding="utf-8", newline="\n")
        for name, bad in (("an empty key file", empty), ("a missing key file", Path(tmp) / "none.txt")):
            try:
                read_api_key(bad)
                check(f"{name} is a RuntimeError", False)
            except RuntimeError as e:
                check(f"{name} is a RuntimeError naming the path", str(bad) in str(e))
    print(f"=== API KEY SELF-TEST: {'GREEN' if not failures else 'RED - ' + ', '.join(failures)} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    print(__doc__)
    sys.exit(2)
