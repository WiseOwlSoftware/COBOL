"""docchecks/design.py — the design docs and user documents against the compiler and against each other (kb/Work PB4453).

Each check was a Unit drift test that read a Markdown file: BooleanStoreDeterminationDriftTests (one determination, word
for word, in RuntimeApi.cs, DATA_MODEL_DESIGN.md and CONFORMANCE.md), OperandClassScreenDriftTests (the binder design
names every OperandPositions row), SortRuntimeDocShapeDriftTests (a design doc writes only CobolSort calls the runtime
ships) and OneHostSurfaceDriftTests' document arm (no user document names an untyped activation route). The compiler's side
of the last three is `tests/doc-facts/compiler-facts.json` and `tests/doc-facts/untyped-host-routes.json`.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

from . import common
from .common import Check, need

ROUTES_PATH = "tests/doc-facts/untyped-host-routes.json"
RUNTIME_API = "src/WiseOwl.Cobol.Compiler/CodeGen/Roslyn/RuntimeApi.cs"
DATA_MODEL = "docs/DATA_MODEL_DESIGN.md"
BINDER_DESIGN = "docs/rearchitecture/DESIGN-binder-bound-tree.md"
#: kb/Work PB3697: which stores convert a boolean item's characters (determination D-B5, PB3666) and which content no
#: store can reach (§14.6.13.2 1)) is ONE text, carried word for word by these three places. A fourth bypass is added to
#: all three or to none.
BOOLEAN_STORE_DETERMINATION = (
    "Every STORE into a boolean item converts its characters. That includes a GR4 group move (the decoded "
    "characters of a national or display receiver; a USAGE BIT receiver's bits are all valid) and a READ or RETURN into an "
    "elementary record area that no other description shares. A boolean item can still hold a character other than 0 or 1 "
    "only through another description of the same storage, which no store into the item reaches: a group receiver (GR4 moves "
    "the group's bytes), a REDEFINES view, a record area shared with another record of its file or with a SAME RECORD AREA "
    "peer (a READ or RETURN fills the area's bytes), an APPLY COMMIT restore (ROLLBACK returns the saved image), or storage "
    "shared through a BY REFERENCE argument, EXTERNAL, BASED or ADDRESS OF. That content is undefined "
    "(§14.6.13.2 1)), and the boolean operators read such a position as zero.")
SUPERSEDED_BYPASS_WORDING = re.compile("Two things bypass|Two exemptions|The exemptions:")
MENTION = re.compile(r"`CobolSort\.(?P<member>[A-Z]\w*)\((?P<args>[^`()]*)\)`")


def boolean_store_findings(places: dict[str, str]) -> list[str]:
    findings: list[str] = []
    for place, body in places.items():
        flat = re.sub(r"\s+", " ", re.sub(r"///|</?para>|`", " ", body))
        need(findings, BOOLEAN_STORE_DETERMINATION in flat,
             f"{place} no longer carries the boolean-store determination word for word (kb/Work PB3697)")
        need(findings, not SUPERSEDED_BYPASS_WORDING.search(body),
             f"{place} spells the superseded 'two bypasses / two exemptions' wording (kb/Work PB3697)")
    return findings


def operand_position_findings(design: str, names: list[str]) -> list[str]:
    findings: list[str] = []
    need(findings, len(names) >= 5, f"only {len(names)} OperandPositions row(s) in the compiler facts: the facts are broken")
    return findings + [f"{BINDER_DESIGN} §3.6 does not name OperandPositions.{n}" for n in names
                       if f"OperandPositions.{n}" not in design]


def takes(parameters: list[dict], args: list[str]) -> bool:
    """A shape is the arity, and an argument spelled as one of the member's parameter names stands at that parameter's
    position (the removed `Init(name, collation, national)` has an arity Init still takes, but puts `collation` where
    `keys` is)."""
    names = [p["name"] for p in parameters]
    required = sum(1 for p in parameters if not p["optional"])
    return required <= len(args) <= len(names) and all(a not in names or names.index(a) == i for i, a in enumerate(args))


def sort_call_findings(docs: dict[str, str], members: list[dict]) -> list[str]:
    held, stale = [], []
    for path, body in docs.items():
        for m in MENTION.finditer(body):
            args = m.group("args")
            if "…" in args or "..." in args:
                continue  # a prose call whose arguments are elided names no shape
            given = [a.strip() for a in args.split(",")] if args.strip() else []
            where = f"{path}: {m.group(0)}"
            held.append(where)
            if not any(x["name"] == m.group("member") and takes(x["parameters"], given) for x in members):
                stale.append(where)
    findings = [] if held else ["no design doc writes a CobolSort call with arguments: the sweep is vacuous"]
    return findings + [f"{w}: a CobolSort call no shipped member takes (CLAUDE.md rule 6)" for w in stale]


def route_findings(documents: dict[str, str], pattern: re.Pattern[str]) -> list[str]:
    return [f"{path}:{n}: {m.group(0)}: the user documentation describes one host interface, the generated host entry "
            "(DESIGN-external-repository §4.4, §9.4), and this line names an untyped route"
            for path, body in documents.items() for n, line in enumerate(body.split("\n"), 1)
            if (m := pattern.search(line))]


def user_documents(root: Path) -> dict[str, str]:
    """The documents a user reads: README.md, CONFORMANCE.md, the manual and the samples, whole."""
    paths = [common.README, common.CONFORMANCE] + [
        p for p in common.tracked(root, "docs/manual", "samples") if p.endswith((".md", ".cs", ".cob", ".cbl", ".txt"))]
    return {p: common.text(root, p) for p in paths}


def _boolean(root: Path) -> list[str]:
    return boolean_store_findings({"RuntimeApi.cs (StrStoreBoolean)": common.text(root, RUNTIME_API),
                                   "DATA_MODEL_DESIGN.md (D19)": common.text(root, DATA_MODEL),
                                   "CONFORMANCE.md (D-B5)": common.text(root, common.CONFORMANCE)})


def _operand(root: Path) -> list[str]:
    return operand_position_findings(common.text(root, BINDER_DESIGN), common.facts(root)["operandPositionRows"])


def _sort(root: Path) -> list[str]:
    docs = {p: common.text(root, p) for p in common.tracked(root, "docs") if p.endswith(".md")}
    return sort_call_findings(docs, common.facts(root)["cobolSortMembers"])


def _routes(root: Path) -> list[str]:
    pattern = re.compile(json.loads(common.text(root, ROUTES_PATH))["pattern"])
    return route_findings(user_documents(root), pattern)


CHECKS = (
    Check("BOOLEAN-STORE DETERMINATION is one text in three places", _boolean),
    Check("BINDER DESIGN names every OperandPositions row", _operand),
    Check("DESIGN DOCS write only CobolSort calls the runtime ships", _sort),
    Check("USER DOCUMENTS name no untyped activation route", _routes),
)


def self_test() -> list[str]:
    ok = {"a": "x " + BOOLEAN_STORE_DETERMINATION.replace(" ", "\n  ") + " y"}
    pattern = re.compile(r"\bProgramRegistry\s*\.\s*CallProgram\b")
    members = [{"name": "Init", "parameters": [{"name": "name", "optional": False}, {"name": "keys", "optional": False}, {"name": "collation", "optional": True}]},
               {"name": "Sort", "parameters": [{"name": "a", "optional": False}, {"name": "b", "optional": True}]}]
    return common.self_test_arms([
        ("the determination survives comment markers, backticks and line breaks", boolean_store_findings(ok), []),
        ("a place that lost a word is a finding", len(boolean_store_findings({"a": "Every STORE into a boolean item"})), 1),
        ("the superseded wording is a finding", len(boolean_store_findings({"a": BOOLEAN_STORE_DETERMINATION + " Two exemptions"})), 1),
        ("a design doc naming every row is clean", operand_position_findings("OperandPositions.A OperandPositions.B", list("ABABAB")), []),
        ("a row the design omits is named", operand_position_findings("OperandPositions.A", list("ABCDEFGHIJK")),
         [f"{BINDER_DESIGN} §3.6 does not name OperandPositions.{n}" for n in "BCDEFGHIJK"]),
        ("a CobolSort call of a shipped shape is clean", sort_call_findings({"d": "`CobolSort.Init(name, keys)`"}, members), []),
        ("the removed Init(name, collation) puts collation where keys is", len(sort_call_findings(
            {"d": "`CobolSort.Init(name, collation)` `CobolSort.Init(name, keys)`"}, members)), 1),
        ("a call with elided arguments names no shape", sort_call_findings({"d": "`CobolSort.Init(name, …)`"}, members),
         ["no design doc writes a CobolSort call with arguments: the sweep is vacuous"]),
        ("an optional parameter may be omitted", takes(members[1]["parameters"], ["a"]), True),
        ("too many arguments is not a shape", takes(members[1]["parameters"], ["a", "b", "c"]), False),
        ("a route in a user document is a finding with its line", route_findings({"README.md": "ok\nProgramRegistry.CallProgram(x)"}, pattern),
         ["README.md:2: ProgramRegistry.CallProgram: the user documentation describes one host interface, the generated host "
          "entry (DESIGN-external-repository §4.4, §9.4), and this line names an untyped route"]),
    ])
