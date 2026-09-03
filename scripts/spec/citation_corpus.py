#!/usr/bin/env python3
"""The ONE definition of "the files that carry spec citations", shared by the citation audits.

⛔ WHY THIS IS A MODULE AND NOT A COPIED GLOB. There are two citation audits and they own DIFFERENT rules —
`audit_doc_citations.py` checks a QUOTED FRAGMENT against the clause it is filed under, `audit_code_citations.py`
checks the CLAUSE NUMBER against the construct the comment is about — but they must look at the SAME files, or a
citation is covered by one rule and not the other purely by where it happens to live. `audit_doc_citations.py`
already learned this once the hard way: it scanned only `docs/` until PB3's fabricated "GR7 k3" turned up in
THIRTEEN places across five source files (DEVLOG 13557/13606). One glob, one place.
"""
from __future__ import annotations

import pathlib

REPO = pathlib.Path(__file__).resolve().parents[2]

#: Directories that hold FROZEN evidence — an agent's raw output, recorded as it was said. Repairing a citation
#: inside one would rewrite the record of what was found, which is the opposite of what these files are for.
#: (The findings they contain are actioned through `kb/Work/`, never by editing the transcript.)
FROZEN = (
    "docs/rearchitecture/spec-reconciliation",
    "docs/rearchitecture/evidence",
)

#: ⛔ BUILD OUTPUT IS NOT SOURCE, AND A CITATION AUDIT THAT READS IT CANNOT EVER GO GREEN. The ANTLR
#: build task copies every `.g4` into `src/<project>/obj/antlr-lib/`, so a rebuilt tree carries a SECOND
#: copy of every grammar comment: each grammar finding was reported TWICE, and — worse — the copy is the
#: grammar as it stood at the LAST BUILD, so a repaired citation stayed red until somebody rebuilt, and a
#: clean checkout and a built one disagreed about whether the gate passed. A generated file cannot be
#: repaired at all, only regenerated (feedback: generated_parser_is_a_build_output). Matched on PATH
#: SEGMENTS, not on a prefix, so it holds for every project layout rather than the two that exist today.
BUILD_OUTPUT = frozenset({"obj", "bin", "Generated"})


def _keep(p: pathlib.Path) -> bool:
    rel = p.relative_to(REPO)
    if BUILD_OUTPUT & set(rel.parts[:-1]):
        return False
    return not any(rel.as_posix().startswith(f) for f in FROZEN)


def prose_files() -> list[pathlib.Path]:
    """Design docs, the process SSOT, and the source doc-comments that carry citations at the same density."""
    out = sorted(REPO.joinpath("docs").rglob("*.md")) + [REPO / "CLAUDE.md"]
    out += sorted(REPO.joinpath("src").rglob("*.cs")) + sorted(REPO.joinpath("src").rglob("*.g4"))
    return [p for p in out if _keep(p)]


def declaration_files() -> list[pathlib.Path]:
    """The files whose comments follow the DEFINITION-HEADER convention — "name the construct, then cite its
    clause": the ANTLR grammars (`// MOVE (§14.9.25)` above `moveStatement`) and the golden program headers
    (`*> ISO §14.7.4 + §14.9.2 — ROUNDED MODE on ADD CORRESPONDING`).

    ⛔ C# IS DELIBERATELY NOT HERE, AND IT WAS MEASURED, NOT ASSUMED. Running the construct-vs-clause check over
    `src/**/*.cs` reported 1224 candidates, and the sample was almost entirely correct citations: a doc-comment
    cites a clause to SUPPORT a rule rather than to LABEL a construct, so the construct's name is legitimately
    absent ("as though it were moved to an alphanumeric data item (§14.9.25.4 GR6)" names no MOVE). A check that
    is wrong 95% of the time cannot gate anything, and gating on it would train the reader to skip it.
    C# doc-comments are still covered — by `audit_doc_citations.py`, whose quoted-fragment rule works there, and
    by this file's PHANTOM check, which needs no convention at all."""
    out = sorted(REPO.joinpath("src").rglob("*.g4"))
    out += sorted(REPO.joinpath("tests").joinpath("conformance").rglob("*.cob"))
    return [p for p in out if _keep(p)]


def all_files() -> list[pathlib.Path]:
    """Every file that carries a citation — grammars, C#, goldens, design docs, the work register and the vault
    notes. Used by the checks that need no convention at all (does this clause EXIST), which are exactly the
    ones that can be run over prose without a false-positive problem."""
    out = declaration_files() + prose_files()
    out += sorted(REPO.joinpath("tests").rglob("*.cs"))
    out += sorted(REPO.joinpath("kb").rglob("*.md"))
    return [p for p in dict.fromkeys(out) if _keep(p)]
