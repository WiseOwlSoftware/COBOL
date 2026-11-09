#!/usr/bin/env python3
"""api_key.py - THE reader of an API key file (kb/Work PB4816, PB4916).

Five tools bill a Claude call to a key named by `--api-key-file`: the fuzz's CLI writer (fuzz_claude.py, through
differential_fuzz.py), its batch driver (fuzz_batch.py), the standard's transcriber (spec/transcribe_pdf.py) and the
edition audit's judge (spec/audit_batch.py; these two read it through spec/message_batches.py, their one batch state). They
share this one reader, so the file's format, the choice of account and the rule that an error names the PATH and never a
key are written once.

THE FORMAT (kb/Work PB4916). The file holds one key per Claude account. A KEY line is one token of an API key's shape
(`KEY_SHAPE`: `sk-` and the key's characters, nothing else on the line); the line before it, when it is not a key, is
that key's LABEL, which names the account (its sign-in, or its name). Blank lines separate the entries; a byte-order mark
(a file saved by Notepad) is dropped. A file with exactly ONE key may carry no label.

THE ACCOUNT. Every caller takes `--api-account NAME` and passes it here as `account`. With more than one key in the file
it is REQUIRED. It selects the entry whose label carries, as a whole word, either the account itself or the sign-in of
the account of that name in `scripts/orchestrator/account.py`'s table (`account1`, `account2`). The sign-in is read at
run time from that account's config dir; it is never written into the repository. No match, or more than one, is an
error naming the path and the labels found. `ApiKey.account` is the ACCOUNT NAME the call bills, which a run records;
the key itself is never printed, logged or recorded. A run with an uncollected batch reads its key through
`read_run_key`, which keeps that batch's account: a batch belongs to the account that created it.

    python scripts/api_key.py --self-test
"""
from __future__ import annotations

import dataclasses
import os
import re
import sys
import tempfile
from pathlib import Path
from typing import Mapping

from standard_streams import ensure_utf8  # noqa: E402  (scripts/: UTF-8 standard streams)

#: One API key, alone on its line: `sk-` and the key's characters (an Anthropic key is `sk-ant-api03-...`).
KEY_SHAPE = re.compile(r"sk-[A-Za-z0-9_-]{4,}")
#: The characters that separate the words of a label (an address keeps its `@` and `.`).
_LABEL_SEPARATORS = re.compile(r"[\s,;:()<>\[\]\"'=|]+")
#: The help text of every caller's `--api-account` (one wording, so the four tools describe it alike).
ACCOUNT_HELP = ("the Claude account to bill when the key file holds more than one key: an account's name in "
                "scripts/orchestrator/model_rules.json (account1, account2) or the sign-in its label names (api_key.py)")


@dataclasses.dataclass(frozen=True)
class ApiKey:
    """A key read from a key file: the key (never in `repr`), its label and the account name it bills (None: the file's
    one unlabeled key, chosen with no account named)."""
    key: str = dataclasses.field(repr=False)
    label: str | None
    account: str | None


def _table_sign_ins() -> dict[str, str | None]:
    """name -> sign-in for every account of the accounts table, read at run time (account.py). When the table cannot be
    read the answer is {}, so an account is then matched by its name alone, and a label that names only a sign-in is not
    found: the error that follows names the labels, never a silent choice of key."""
    saved = list(sys.path)          # account.py puts scripts/orchestrator first on the path; the caller's path is restored
    sys.path.insert(0, str(Path(__file__).resolve().parent / "orchestrator"))
    try:
        import account as accounts  # noqa: PLC0415  (scripts/orchestrator: THE accounts table)
        return {row["name"]: accounts.by_name(row["name"]).email for row in accounts.coord.rules()["accounts"]["list"]}
    except (ImportError, OSError, LookupError, ValueError):
        return {}
    finally:
        sys.path[:] = saved


def _entries(path: Path) -> list[tuple[str | None, str]]:
    """(label, key) per key line of the file, in file order."""
    lines = [ln.strip() for ln in path.read_text(encoding="utf-8-sig").splitlines()]
    out: list[tuple[str | None, str]] = []
    for i, line in enumerate(lines):
        if KEY_SHAPE.fullmatch(line):
            before = lines[i - 1] if i > 0 else ""
            out.append((before if before and not KEY_SHAPE.fullmatch(before) else None, line))
    return out


def _words(label: str) -> set[str]:
    return {w.casefold() for w in _LABEL_SEPARATORS.split(label) if w}


def read_api_key(path: str | os.PathLike[str], account: str | None = None, *,
                 sign_ins: Mapping[str, str | None] | None = None) -> ApiKey:
    """The key of `account` in the key file `path` (module docstring: THE FORMAT, THE ACCOUNT). `sign_ins` (name -> sign-in)
    replaces the accounts table, for a test. Every failure is a RuntimeError naming the path, never a key."""
    p = Path(path)
    entries = _entries(p) if p.exists() else []
    if not entries:
        raise RuntimeError(f"no API key in {path} (a key is a line of the shape sk-... alone)")
    labels = ", ".join(repr(label) if label else "(unlabeled)" for label, _ in entries)
    if account is None:
        if len(entries) > 1:
            raise RuntimeError(f"{path} holds {len(entries)} keys ({labels}): name the account to bill with --api-account")
        label, key = entries[0]
        return ApiKey(key, label, None)
    table = _table_sign_ins() if sign_ins is None else dict(sign_ins)
    wanted = account.strip().casefold()
    # The account's name in the table, whether it was given by name or by its sign-in.
    name = next((n for n, s in table.items() if wanted in (n.casefold(), (s or "").casefold())), account.strip())
    words = {wanted, *(s.casefold() for n, s in table.items() if n == name and s)}
    found = [(label, key) for label, key in entries if label and words & _words(label)]
    if len(found) != 1:
        why = "no key whose label names" if not found else f"{len(found)} keys whose labels name"
        raise RuntimeError(f"{path} holds {why} the account {account!r} (labels: {labels})")
    label, key = found[0]
    return ApiKey(key, label, name)


def read_run_key(path: str | os.PathLike[str], account: str | None, recorded: str | None, record: str | os.PathLike[str], *,
                 sign_ins: Mapping[str, str | None] | None = None) -> ApiKey:
    """The key a run with a record bills: a batch belongs to the account that created it, so a run whose record (`record`)
    names an account (`recorded`) keeps it. `account` (`--api-account`) may restate it, by name or by sign-in; one that
    resolves to another account is a RuntimeError naming the record, never a key."""
    chosen = read_api_key(path, account if account is not None else recorded, sign_ins=sign_ins)
    if recorded is not None and chosen.account != recorded:
        raise RuntimeError(f"{record} was billed to the account {recorded!r}, not {chosen.account!r}: its batches belong to "
                           f"{recorded!r}")
    return chosen


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(f"  {'ok  ' if cond else 'FAIL'} {name}")
        if not cond:
            failures.append(name)

    def refused(name: str, path: Path, account: str | None, *needles: str) -> None:
        try:
            read_api_key(path, account, sign_ins=planted)
            check(f"{name} is a RuntimeError", False)
        except RuntimeError as e:
            check(f"{name} is a RuntimeError naming the path {'and ' + ', '.join(needles) + ' ' if needles else ''}and no key",
                  str(path) in str(e) and all(n in str(e) for n in needles) and not any(k in str(e) for k in (one, two, "sk-test-key")))

    # Invented labels and sign-ins only: no real account is named in a tracked file.
    planted = {"account1": "first.person@example.invalid", "account2": "second.person@example.invalid"}
    one, two = "sk-ant-api03-PLANTED-one_1", "sk-ant-api03-PLANTED-two_2"
    with tempfile.TemporaryDirectory() as tmp:
        bare = Path(tmp) / "bare.txt"
        bare.write_text(chr(0xFEFF) + chr(10) + "  sk-test-key  " + chr(10), encoding="utf-8", newline="\n")
        got = read_api_key(bare, sign_ins=planted)
        check("a single bare key is read, stripped, a byte-order mark dropped, with no account", got == ApiKey("sk-test-key", None, None))
        check("the key is not in the ApiKey's repr", "sk-test-key" not in repr(got))
        refused("an account named for a file with one unlabeled key", bare, "account2", "'account2'")
        pair = Path(tmp) / "pair.txt"
        pair.write_text(chr(0xFEFF) + f"First account: {planted['account1']}\n{one}\n\nSecond account: {planted['account2']}\n{two}\n",
                        encoding="utf-8", newline="\n")
        check("a labeled pair: the account selected by its name", read_api_key(pair, "account2", sign_ins=planted).key == two
              and read_api_key(pair, "account1", sign_ins=planted).key == one)
        by_sign_in = read_api_key(pair, planted["account2"].upper(), sign_ins=planted)
        check("a labeled pair: the account selected by its sign-in (any case), billing the account's NAME",
              by_sign_in.key == two and by_sign_in.account == "account2")
        refused("a labeled pair with no account given", pair, None, "2 keys", "--api-account")
        refused("an unknown account", pair, "account3", "'account3'", repr(f"First account: {planted['account1']}"))
        refused("a prefix of a sign-in (no whole-word match)", pair, "first.person", "'first.person'")
        named = Path(tmp) / "named.txt"
        named.write_text(f"account1\n{one}\naccount2 (the operator)\n{two}\n", encoding="utf-8", newline="\n")
        check("a label that carries the account's name selects it, with no table", read_api_key(named, "account2", sign_ins={}).key == two)
        twice = Path(tmp) / "twice.txt"
        twice.write_text(f"account1\n{one}\naccount1 again\n{two}\n", encoding="utf-8", newline="\n")
        refused("two labels naming one account", twice, "account1", "2 keys whose labels name")
        empty = Path(tmp) / "empty.txt"
        empty.write_text(chr(10) + "a label with no key" + chr(10), encoding="utf-8", newline="\n")
        for name, bad in (("a file with no key line", empty), ("a missing key file", Path(tmp) / "none.txt")):
            refused(name, bad, None)
        check("read_run_key: the record's account is kept, unnamed or restated by name or sign-in",
              all(read_run_key(pair, given, "account2", "r", sign_ins=planted).key == two
                  for given in (None, "account2", planted["account2"])))
        check("read_run_key: a run with no record yet bills the account named", read_run_key(pair, "account1", None, "r", sign_ins=planted).key == one)
        try:
            read_run_key(pair, "account1", "account2", "rec.json", sign_ins=planted)
            check("read_run_key: another account than the record's is a RuntimeError", False)
        except RuntimeError as e:
            check("read_run_key: another account than the record's is a RuntimeError naming the record and no key",
                  "rec.json" in str(e) and one not in str(e) and two not in str(e))
    print(f"=== API KEY SELF-TEST: {'GREEN' if not failures else 'RED - ' + ', '.join(failures)} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    print(__doc__)
    sys.exit(2)
