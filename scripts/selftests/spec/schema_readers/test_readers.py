#!/usr/bin/env python3
"""The inventory schema's one reader (DESIGN-architecture-review §8.2 rule 7, kb/Work PB6098): a planted source for each
finding (a path to the schema file, a second parse, a hand split on a separator), each prose shape the rule leaves alone,
the one reader's own calls, and the rule over this tree."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

C = selftest_cases.subject(__file__)   # scripts/spec/schema_readers.py


def _rules(source: str) -> list[str]:
    """The rule word of each finding in the planted `source`."""
    return [f.split(": ", 1)[1].split()[0] for f in C.scan("planted.py", source)]


def case_00_each_hand_copy_is_a_finding(expect):
    expect.equal("a path to the schema file", _rules('P = REPO / "tests" / "version-matrix" / "inventory-schema.json"\n'),
                 ["PATH"])
    expect.equal("the path written whole", _rules('P = "tests/version-matrix/inventory-schema.json"\n'), ["PATH"])
    expect.equal("a second parse with json.loads", _rules("raw = json.loads((REPO / SCHEMA_REL).read_text())\n"), ["PARSE"])
    expect.equal("a second parse through a tree", _rules("raw = tree.json(inventory_schema.SCHEMA_REL)\n"), ["PARSE"])
    expect.equal("a hand split on each separator the schema has",
                 sorted(r for sep in sorted(C.SEPARATORS) for r in _rules(f"parts = value.split(schema.{sep}.strip())\n")),
                 ["SPLIT"] * len(C.SEPARATORS))
    expect.equal("an rsplit and a sep= keyword", _rules("a = v.rsplit(s.test_ref_sep)\nb = v.split(sep=s.editions_sep)\n"),
                 ["SPLIT", "SPLIT"])


def case_01_prose_and_the_one_reader_are_not(expect):
    source = ('"""Reads tests/version-matrix/inventory-schema.json through inventory_schema."""\n'
              'def f(tree, schema, row):\n'
              '    """The schema file inventory-schema.json, named in a docstring."""\n'
              '    msg = "inventory-schema.json declares no `derivation`"\n'
              '    label = f"inventory-schema.json{row}"\n'
              '    raw = inventory_schema.raw_schema(tree)\n'
              '    refs = schema.split(row["test-ref"], schema.test_ref_sep)\n'
              '    text = schema.test_ref_sep.join(refs)\n'
              '    repo.commit("copy", {SCHEMA_REL: WORKING_TREE.text(SCHEMA_REL)})\n'
              '    return label.split(",")\n')
    expect.equal("a docstring, a message, an f-string piece, raw_schema, Schema.split, a join, a text copy, a literal split",
                 _rules(source), [])


def case_02_the_separators_are_the_schemas(expect):
    expect("every separator the schema's three list fields carry is held", {"test_ref_sep", "code_location_sep",
                                                                          "editions_sep"} <= C.SEPARATORS,
           f"got {sorted(C.SEPARATORS)}")


def case_03_this_tree_keeps_the_rule(expect):
    found, files = C.findings()
    expect("the scan read the live Python files", len(files) > 100, f"read {len(files)}")
    expect("the one reader is not scanned", C.READER not in files)
    expect.equal("no script outside inventory_schema.py keeps a copy of the schema's reading", found, [])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
