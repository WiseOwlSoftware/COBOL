"""conformance_1985.py — THE COBOL-85 CONFORMANCE STATEMENT'S COMPUTED PART: docs/CONFORMANCE.md section 9 (kb/Work PB4997,
PB4885; DESIGN-spec-conformance-review.md section 10.2, slice 6).

`--std 85` claims the HIGH subset of ANSI X3.23-1985 (as amended by X3.23a-1989 and X3.23b-1993) and the highest level of
each of its five optional modules (§1985:I.1.5.1, §1985:I.1.5.2; the owner's decision kb/Work PB4885, extended to the fifth
optional module that Amendment 1 added by kb/Work PB4997). That claim is the frame, written once in
`docs/conformance/9/index.md`. Everything the statement says the compiler MEETS is computed here from the 1985 rows and
their chains, never typed: each rule's state is the inventory view's (`inventory_view.inventory(1985)`: a rule's own
verdict, or, for a rule KEPT into ISO/IEC 1989:2023, its chain's), so the statement cannot claim a rule the view does not
close, and a rule that opens names itself here the moment it does.

`render(root)` returns the section's computed text and its findings. `conformance_source.read_sources` appends the text
to section 9's preamble (`conformance_source.GENERATED`), so every reader of the document, the rendered view and the
package's copy see one statement, and `conformance_doc.py --check` (the RENDERED VIEWS and DOC AUDITS gate audits) fails
on a finding. The findings hold the hand-written parts to the rows, both ways:
  - section 9.1 names, with its ground, every rule the claim does not support: a rule whose own verdict is
    DOCUMENTED-NON-SUPPORT, or a rule KEPT into a 2023 rule that is. A rule in that set and not in 9.1, a 9.1 row of a
    rule not in it, and a 9.1 row whose citation or subject is not its rule's are each a finding;
  - every module of the rows is one of the standard's required or optional modules (§1985:I.1.5.1).

The obsolete elements (§1985:I.1.5.2.3: "Documentation associated with an implementation must identify all obsolete
language elements in the implementation") are the list's own entries (`spec-element-standing-1985.json`, their names read
from the text by `element_standing.listed`), each with the rules its catalog marks obsolete and their states, and the
warning the same paragraph requires is read from the construct catalogue (a construct obsolete at 85, `obsoleteIn`).

NOTHING HERE IS IMPORTED BY A MODULE THE COMPILER'S TESTS RUN (the rule of `conformance_source`).
"""
from __future__ import annotations

import functools
import json
import pathlib
import sys
from collections import Counter
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402
import element_standing  # noqa: E402
import inventory_view  # noqa: E402
import lineage  # noqa: E402
from inventory_schema import REPO, Tree, load_schema  # noqa: E402

YEAR = 1985
#: The section this module computes, and the section whose rows are the claim's stated exceptions.
SECTION = "9"
EXCEPTIONS_SECTION = "9.1"
#: How the computed text names section 9.1 (the document's own section sign, not a clause of the standard).
EXCEPTIONS_REF = "§" + EXCEPTIONS_SECTION
#: The standard's modules (§1985:I.1.5.1, as amended: "Each subset is composed of a level of the seven required modules:
#: Nucleus, Sequential I-O, Relative I-O, Indexed I-O, Inter-Program Communication, Sort-Merge, and Source Text
#: Manipulation"; "The five optional modules (Report Writer, Communication, Debug, Segmentation, and Intrinsic
#: Function)"), in the order the text lists them. A row's `module` is the catalog's; a module in neither list is a finding.
REQUIRED = ("Nucleus", "Sequential I-O", "Relative I-O", "Indexed I-O", "Inter-Program Communication", "Sort-Merge",
            "Source Text Manipulation")
OPTIONAL = ("Report Writer", "Communication", "Debug", "Segmentation", "Intrinsic Function")
NOT_SUPPORTED = "DOCUMENTED-NON-SUPPORT"
STANDING_REL = "docs/rearchitecture/spec-element-standing-1985.json"
SPEC_REL = "specs/ANSI_COBOL_1985.md"
CONSTRUCTS_REL = "tests/version-matrix/constructs"
SOURCE_REL = "docs/conformance"


def _n(v: int) -> str:
    return f"{v:,}"


def not_supported_through(row: dict[str, Any], schema: Any, targets: dict[str, dict[str, Any]]) -> list[str] | None:
    """Why `row` is outside what the claim supports: `[]` when its own verdict is DOCUMENTED-NON-SUPPORT, the 2023 rules
    with that verdict it is KEPT into when it is chained to one, None when it is supported."""
    if row.get("verdict") == NOT_SUPPORTED:
        return []
    if lineage.chained(row, schema):
        through = [t for t in (row["next"].get("to") or []) if (targets.get(t) or {}).get("verdict") == NOT_SUPPORTED]
        return through or None
    return None


def _exception_rows(root: pathlib.Path, findings: list[str]) -> dict[str, list[str]]:
    """Section 9.1's rows by rule id (each row's cells), read from its source files."""
    import conformance_source  # noqa: PLC0415  (the one reader of the sources; imported late, it imports this module)
    out: dict[str, list[str]] = {}
    directory = root / SOURCE_REL / EXCEPTIONS_SECTION
    for path in sorted(directory.glob("*.md")) if directory.is_dir() else []:
        if path.name in (conformance_source.INDEX_FILE, conformance_source.EPILOGUE_FILE):
            continue
        try:
            meta, _ = conformance_source.frontmatter(path.read_text(encoding="utf-8"), f"{SOURCE_REL}/{EXCEPTIONS_SECTION}/{path.name}")
        except ValueError as e:
            findings.append(str(e))
            continue
        cells = meta.get("cells")
        if isinstance(cells, list) and cells and all(isinstance(c, str) for c in cells):
            out[cells[0].strip("`")] = cells
    return out


def _obsolete_names(root: pathlib.Path) -> dict[int, str]:
    lines = (root / SPEC_REL).read_text(encoding="utf-8").split("\n")
    return {n: element_standing.plain(name) for n, name in
            element_standing.listed(lines, citation.regions_1985(lines), "C.1").items()}


def _obsolete_constructs(root: pathlib.Path) -> list[str]:
    """The constructs the catalogue makes obsolete at 1985 (`obsoleteIn` 85): what draws the obsolete-element warning at
    `--std 85`, by the name the diagnostic gives each."""
    out = []
    for path in sorted((root / CONSTRUCTS_REL).glob("*.json")):
        row = json.loads(path.read_text(encoding="utf-8"))
        if row.get("obsoleteIn") == 85:
            out.append(row.get("display") or row["id"])
    return out


@functools.lru_cache(maxsize=4)
def render(root: pathlib.Path = REPO) -> tuple[str, tuple[str, ...]]:
    """Section 9's computed text at the repository `root`, and its findings (see the module docstring)."""
    findings: list[str] = []
    tree = Tree(root)
    schema = load_schema(tree)
    rows = inventory_view.inventory(YEAR, schema, tree)
    targets = {r["rule-id"]: r for r in inventory_view.inventory(schema.edition(YEAR).next_held, schema, tree)}
    out_of_claim = {r["rule-id"]: through for r in rows
                    if (through := not_supported_through(r, schema, targets)) is not None}
    by_id = {r["rule-id"]: r for r in rows}

    modules = list(REQUIRED + OPTIONAL)
    stray = sorted({r["module"] for r in rows if r["module"] is not None} - set(modules))
    findings += [f"{SOURCE_REL}/{SECTION}: the 1985 rows name the module {m!r}, which is neither required nor optional in "
                 f"§1985:I.1.5.1 (conformance_1985.REQUIRED, OPTIONAL)" for m in stray]

    def tally(group: list[dict[str, Any]]) -> list[str]:
        return [_n(len(group)), _n(sum(1 for r in group if r["level"] == 2)), _n(sum(1 for r in group if r["state"] == "OK")),
                _n(sum(1 for r in group if r["state"] == "GAP")), _n(sum(1 for r in group if r["verdict"])),
                _n(sum(1 for r in group if not r["verdict"] and lineage.chained(r, schema))),
                _n(sum(1 for r in group if r["rule-id"] in out_of_claim))]

    table = ["| Module | Kind | Level claimed (its highest) | Rules | Of them level 2 | OK | GAP | By its own verdict | "
             f"KEPT into ISO/IEC 1989:2023 | Not supported ({EXCEPTIONS_REF}) |", "|---|---|---|---|---|---|---|---|---|---|"]
    for m in modules:
        group = [r for r in rows if r["module"] == m]
        if not group:
            findings.append(f"{SOURCE_REL}/{SECTION}: the 1985 rows hold no rule of the {m} module")
            continue
        kind = "required" if m in REQUIRED else "optional"
        table.append("| " + " | ".join([m, kind, str(max(r["level"] or 1 for r in group))] + tally(group)) + " |")
    loose = [r for r in rows if r["module"] is None]
    if loose:
        sections = sorted({r["section"].split(".")[0] for r in loose}, key=lambda s: inventory_view._roman(s))
        table.append("| " + " | ".join([f"(no module: Sections {', '.join(sections)})", "every subset", "—"] + tally(loose)) + " |")
    table.append("| " + " | ".join(["**All**", "", ""] + [f"**{c}**" for c in tally(rows)]) + " |")

    gaps = [r for r in rows if r["state"] == "GAP"]
    parts = [f"**The claim, computed from the {_n(len(rows))} rules of the 1985 inventory** (`python "
             f"scripts/spec/inventory_view.py --summary`; a rule's state is its own verdict's, or, for a rule KEPT into ISO/IEC "
             f"1989:2023, its chain's with a witness of its own at `--std 85`: DESIGN-spec-conformance-review.md section 10.2). "
             f"Level 2 is the level the standard boxes (§1985:I.1.3); a module with one level is claimed at level 1. OK counts "
             f"every rule its verdict closes, DOCUMENTED-NON-SUPPORT included, and the last column counts those the claim "
             f"does not support.",
             "\n".join(table)]
    if gaps:
        lines = ["| Rule | Citation | Subject | Module |", "|---|---|---|---|"]
        lines += [f"| `{r['rule-id']}` | {r['citation']} | {r['subject']} | {r['module'] or '—'} |" for r in gaps]
        parts.append(f"**The claim does not hold yet: {_n(len(gaps))} rule(s) are a GAP**, each open until its row is "
                     f"decided (`python scripts/spec/work.py next` names the work):\n\n" + "\n".join(lines))
    else:
        parts.append(f"**Every rule of every claimed module level is OK: no rule is a GAP.** The {_n(len(out_of_claim))} rules "
                     f"of {EXCEPTIONS_REF} are the claim's stated exceptions, each with its ground.")

    # Section 9.1 is the claim's exceptions: exactly the rules outside it, each row's citation and subject its rule's.
    declared = _exception_rows(root, findings)
    for rid in sorted(set(out_of_claim) - set(declared)):
        r = by_id[rid]
        via = f"KEPT into {', '.join(out_of_claim[rid])}" if out_of_claim[rid] else "its own verdict"
        findings.append(f"{SOURCE_REL}/{EXCEPTIONS_SECTION}: no row for {rid} ({r['citation']}, {r['subject']}), which the "
                        f"claim does not support ({via} is {NOT_SUPPORTED}): add `{rid}.md` with its ground")
    for rid in sorted(set(declared) - set(out_of_claim)):
        findings.append(f"{SOURCE_REL}/{EXCEPTIONS_SECTION}/{rid}.md: {rid} is "
                        + ("no rule of the 1985 catalog" if rid not in by_id else "supported (no DOCUMENTED-NON-SUPPORT verdict "
                                                                                  "of its own or through its chain)")
                        + ": remove the row")
    for rid in sorted(set(declared) & set(out_of_claim)):
        cells, r = declared[rid], by_id[rid]
        if len(cells) < 3 or cells[1] != r["citation"] or cells[2] != r["subject"]:
            findings.append(f"{SOURCE_REL}/{EXCEPTIONS_SECTION}/{rid}.md: its citation and subject cells must be the rule's "
                            f"({r['citation']!r}, {r['subject']!r})")

    # The obsolete elements (§1985:I.1.5.2.3), the list's own entries.
    names = _obsolete_names(root)
    standing = element_standing.load(root / STANDING_REL)
    entries = next(lst for lst in standing if lst.locator == "C.1").entries
    marked: dict[int, list[dict[str, Any]]] = {}
    for r in rows:
        for _, locator, n in element_standing.entries_of(r["status"]):
            if locator == "C.1":
                marked.setdefault(n, []).append(r)
    obs = [f"| §1985:C.1 item | Element (the list's words) | Rules it marks | OK | Not supported ({EXCEPTIONS_REF}) |", "|---|---|---|---|---|"]
    for e in entries:
        group = marked.get(e.n, [])
        if not group:
            obs.append(f"| {e.n} | {names.get(e.n, '?')} | none: {e.no_rule or 'no rule names it'} | — | — |")
            continue
        obs.append(f"| {e.n} | {names.get(e.n, '?')} | {_n(len(group))} | {_n(sum(1 for r in group if r['state'] == 'OK'))} | "
                   f"{_n(sum(1 for r in group if r['rule-id'] in out_of_claim))} |")
    missing = sorted(set(names) - {e.n for e in entries})
    findings += [f"{STANDING_REL}: §1985:C.1 lists item {n}, which the standing data does not hold" for n in missing]
    warned = _obsolete_constructs(root)
    warning = (f"At `--std 85` the obsolete-element warning names each of {len(warned)} constructs the construct catalogue "
               f"marks obsolete at 1985 (`obsoleteIn` 85): {'; '.join(warned)}." if warned else
               "**No construct of the construct catalogue is marked obsolete at 1985** (`obsoleteIn` 85), so at `--std 85` no "
               "warning names an obsolete element, and the warning mechanism §1985:I.1.5.2.3 requires is not provided yet: "
               "until it is, the claim does not meet that paragraph.")
    parts.append(f"**The obsolete elements** (§1985:I.1.5.2.3; the list of §1985:C.1, {len(entries)} items). Each is supported "
                 f"where the claim supports its rules: the counts are the rules the 1985 catalog marks obsolete for the item, "
                 f"and their states.\n\n" + "\n".join(obs) + "\n\n" + warning)
    return "\n\n".join(parts), tuple(findings)
