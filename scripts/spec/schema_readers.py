#!/usr/bin/env python3
"""schema_readers.py — THE INVENTORY SCHEMA HAS ONE READER: the drift assertion of DESIGN-architecture-review §8.2 rule 7
(kb/Work PB6098; PB3206, PB3210 made the reader one).

    python scripts/spec/schema_readers.py --check    # the rule below over every live Python file (live_tools.ROOTS)

The inventory schema (`tests/version-matrix/inventory-schema.json`) owns the verdicts, the test-ref forms, the list
separators, the retired-witness mark and every other vocabulary the inventory's scripts share. `scripts/spec/
inventory_schema.py` is its ONE reader: `raw_schema()` is the one parse of the file, `load_schema()` the one `Schema` of
it, and `Schema.split` the one splitter of a list field. A second parse, or a hand split on a separator, is a copy of the
schema's rule that drifts the day the schema changes (PB3206's sweep missed `rename_diagnostic_prefix.py`'s own parse of
the retired-witness mark, and `normalize_batch.py` kept an inline `Schema.split` after PB3210 de-duplicated the file).

THE RULE (--check), over every tracked .py file under live_tools.ROOTS but `inventory_schema.py` itself, read on the
parse tree so that prose naming the file (a docstring, a message, a comment) is no finding:
  1. PATH    no string literal is a path to the schema file (`…/inventory-schema.json`, written whole): a script reaches
             the file through `inventory_schema.SCHEMA_REL` and reads it through `raw_schema` or `load_schema`. A docstring
             and a piece of an f-string are prose, not a path.
  2. PARSE   no `json.load(…)`, `json.loads(…)` or `<tree>.json(…)` call names `SCHEMA_REL` in its arguments: that is
             the second parse; `raw_schema(tree)` is the one (it gives a fresh dict to plant in).
  3. SPLIT   no `.split(…)` or `.rsplit(…)` call is handed one of the schema's separators as str.split's separator (its
             first argument or `sep=`; each `Schema` attribute whose name ends in `_sep`, read from a real `Schema`, so a
             separator the schema gains is held at once): a list field is split by `Schema.split(value, sep)` (on the
             stripped separator, each part stripped, empties dropped), whose separator is its second argument.
THE RESIDUE, named: the scan reads one call at a time, so a script that binds SCHEMA_REL to another name, or reads the
file's text on one line and parses it on another, escapes rule 2; the review of a change that adds such a reader is the
net for it.
`scripts/selftests/spec/schema_readers/` plants a source for each finding and for each prose shape the rule leaves alone,
then runs --check over this tree. The scan reads only the scripts, so it is the self-test's to run (every gate of a
change that touches a script, and CI's `audits` job), not a gate audit of its own.
"""
from __future__ import annotations

import argparse
import ast
import pathlib
import re
import sys
from collections.abc import Iterable

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams and live_tools
from standard_streams import ensure_utf8  # noqa: E402
import live_tools  # noqa: E402  (the one listing of the live tools' trees)
from inventory_schema import REPO, SCHEMA_REL, load_schema  # noqa: E402

#: The one reader, the only file the rule does not scan.
READER = "scripts/spec/inventory_schema.py"
#: A string literal that IS a path to the schema file (rule 1): the file's name, alone or after a directory.
SCHEMA_FILE = re.compile(r"(?:^|[/\\])" + re.escape(SCHEMA_REL.rsplit("/", 1)[-1]) + r"$")
#: The names of the schema's path constant a parse would be handed (rule 2).
PATH_NAMES = frozenset({"SCHEMA_REL"})
#: The `Schema` attributes that are list separators (rule 3), read from the real schema.
SEPARATORS = frozenset(a for a in vars(load_schema()) if a.endswith("_sep"))
#: The words one of which every finding's source spells (`scan`'s filter).
WORDS = (SCHEMA_REL.rsplit("/", 1)[-1], *PATH_NAMES, *SEPARATORS)


def _names(node: ast.AST) -> set[str]:
    """Every bare name and attribute name inside `node`."""
    return {n.id if isinstance(n, ast.Name) else n.attr for n in ast.walk(node) if isinstance(n, (ast.Name, ast.Attribute))}


def _prose(tree: ast.AST) -> set[int]:
    """The ids of the string constants that are prose: a docstring (an expression statement that is a string) and each
    literal piece of an f-string."""
    out: set[int] = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Expr) and isinstance(node.value, ast.Constant) and isinstance(node.value.value, str):
            out.add(id(node.value))
        elif isinstance(node, ast.JoinedStr):
            out.update(id(v) for v in node.values if isinstance(v, ast.Constant))
    return out


def scan(rel: str, source: str) -> list[str]:
    """Rule 1-3's findings in the Python file `rel` holding `source`, each `<rel>:<line>: <RULE> <why>`. A source that
    spells none of the words a finding needs (the file's name, a path name, a separator) is not parsed: every finding
    names one of them, so the filter drops no finding and spares parsing the hundreds of scripts that name none."""
    if not any(word in source for word in WORDS):
        return []
    tree = ast.parse(source, rel)
    prose = _prose(tree)
    found: list[str] = []
    for node in ast.walk(tree):
        if (isinstance(node, ast.Constant) and isinstance(node.value, str) and id(node) not in prose
                and SCHEMA_FILE.search(node.value.strip())):
            found.append(f"{rel}:{node.lineno}: PATH names the schema file {node.value!r}: reach it through "
                         f"inventory_schema.SCHEMA_REL and read it through raw_schema() or load_schema()")
        if not isinstance(node, ast.Call):
            continue
        func = node.func
        called = func.attr if isinstance(func, ast.Attribute) else func.id if isinstance(func, ast.Name) else ""
        args: Iterable[ast.AST] = [*node.args, *(k.value for k in node.keywords)]
        named = set().union(*(_names(a) for a in args)) if node.args or node.keywords else set()
        is_json = called in ("load", "loads") and isinstance(func, ast.Attribute) and _names(func.value) == {"json"}
        if (is_json or called == "json") and named & PATH_NAMES:
            found.append(f"{rel}:{node.lineno}: PARSE parses the schema file a second time: call "
                         f"inventory_schema.raw_schema(tree), the one parse")
        # str.split's separator is its first argument (or `sep=`); `Schema.split(value, sep)` takes the value first.
        sep_arg = node.args[0] if node.args else next((k.value for k in node.keywords if k.arg == "sep"), None)
        if (called in ("split", "rsplit") and isinstance(func, ast.Attribute) and sep_arg is not None
                and (seps := sorted(_names(sep_arg) & SEPARATORS))):
            found.append(f"{rel}:{node.lineno}: SPLIT splits on the schema's {', '.join(seps)} by hand: call "
                         f"Schema.split(value, sep), the one splitter")
    return found


def findings(repo: pathlib.Path = REPO) -> tuple[list[str], list[str]]:
    """(every finding, the files scanned) over `repo`'s live Python files but the reader itself."""
    files = [rel for rel in live_tools.tracked(repo, (".py",)) if rel != READER]
    return [f for rel in files for f in scan(rel, (repo / rel).read_text(encoding="utf-8"))], files


def check(repo: pathlib.Path = REPO) -> int:
    found, files = findings(repo)
    for f in found:
        print(f"  {f}")
    if not files:
        print("=== SCHEMA READERS: FAIL — the scan read no file under live_tools.ROOTS: a net over nothing ===")
        return 1
    if not SEPARATORS:
        print("=== SCHEMA READERS: FAIL — Schema has no `_sep` attribute: rule 3 holds nothing ===")
        return 1
    print(f"=== SCHEMA READERS: {'FAIL' if found else 'PASS'} — {len(files)} live Python files scanned, "
          f"{len(found)} finding(s) (DESIGN-architecture-review §8.2 rule 7) ===")
    return 1 if found else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    ap.add_argument("--check", action="store_true", required=True, help="the rule over every live Python file")
    ap.parse_args()
    return check()


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
