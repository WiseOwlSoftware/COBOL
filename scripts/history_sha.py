"""Translate a commit SHA from before the history rewrite of 2026-11-05 to its commit today.

WHY. On 2026-11-05 the owner had the repository's whole history rewritten (git filter-repo) to remove the reseller's per-copy
licence stamp — the purchaser's name and order number — that had been committed with the spec work. Every commit from the first
such one onward got a new SHA. The current tree's citations were moved to the new SHAs in the same landing, but a SHA in an old
commit's files, in a pull request, in a gate record or in a mailbox message still names the old commit. This tool reads the
rewrite's own commit map, `docs/history/commit-map-2026-11-05.txt` (old new, one pair per line, written by filter-repo), and
answers for a full SHA or any unique prefix.

    python scripts/history_sha.py <old-sha-or-prefix> [...]
    python scripts/history_sha.py --self-test
"""
from __future__ import annotations

import bisect
import pathlib
import sys

sys.path.append(str(pathlib.Path(__file__).resolve().parent))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

MAP = pathlib.Path(__file__).resolve().parents[1] / "docs" / "history" / "commit-map-2026-11-05.txt"


def load(path: pathlib.Path = MAP) -> dict[str, str]:
    lines = path.read_text(encoding="utf-8").splitlines()
    return dict(line.split() for line in lines[1:] if line.strip())


def translate(prefix: str, mapping: dict[str, str], olds: list[str] | None = None) -> str | None:
    """The new SHA of the one old commit `prefix` names, or None when it names none or more than one."""
    olds = olds if olds is not None else sorted(mapping)
    p = prefix.lower()
    i = bisect.bisect_left(olds, p)
    hits = [o for o in olds[i:i + 2] if o.startswith(p)]
    return mapping[hits[0]] if len(hits) == 1 else None


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(("  ok    " if cond else "  FAIL  ") + name)
        if not cond:
            failures.append(name)

    m = {"aaaa1111" + "0" * 32: "bbbb2222" + "0" * 32, "aaaa1199" + "0" * 32: "cccc3333" + "0" * 32}
    check("a unique prefix translates", translate("aaaa1111", m) == "bbbb2222" + "0" * 32)
    check("an ambiguous prefix translates to nothing", translate("aaaa11", m) is None)
    check("an unknown SHA translates to nothing", translate("ffff", m) is None)
    check("upper case is accepted", translate("AAAA1199", m) == "cccc3333" + "0" * 32)
    real = load()
    check("the committed map loads and is a full rewrite's (over 10,000 commits, 40-hex pairs)",
          len(real) > 10000 and all(len(k) == 40 and len(v) == 40 for k, v in list(real.items())[:50]))
    print(f"=== HISTORY SHA SELF-TEST: {'FAIL - ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    if not argv:
        print(__doc__)
        return 2
    mapping = load()
    olds = sorted(mapping)
    status = 0
    for a in argv:
        new = translate(a, mapping, olds)
        print(f"{a} -> {new or 'NOT FOUND (not a pre-rewrite commit, or the prefix is ambiguous)'}")
        status |= new is None
    return status


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
