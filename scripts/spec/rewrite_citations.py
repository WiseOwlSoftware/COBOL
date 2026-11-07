#!/usr/bin/env python3
"""THE ONE-TIME REWRITE OF THE TREE'S BARE CITATIONS INTO THE EDITION FORM (kb/Work PB4887 item 2).

    python scripts/spec/rewrite_citations.py --dry-run     # what --apply would change: counts per directory and edition
    python scripts/spec/rewrite_citations.py --apply       # rewrite in place (idempotent: a second run changes nothing)
    python scripts/spec/rewrite_citations.py --self-test   # the planted cases

The tree writes its citations of ISO/IEC 1989:2023 bare (`§13.18.35.4 GR1`); the format (`scripts/spec/citation.py`) makes
the edition mandatory (`§2023:13.18.35.4 GR1`). This script rewrites, once, every bare `§N` that `citation.bare_standard`
classifies as a citation of the 2023 standard: its number is a clause of the 2023 transcription, the file does not head a
section of its own with that number, and no document name or other edition stands just left of it. Everything else is
left alone and counted: a repository document's own section ("plan §0", "DESIGN-x §4.4.3"), a number the standard does
not have (the forensic record of a phantom), and a free-text citation of ANOTHER edition. That last kind is converted when
its locator is certain (an ISO edition's clause number: "ISO 1989:2002 §14.8.4" -> "§2002:14.8.4") and listed for hand
repair otherwise (a 1985 citation carries no Roman section in free text, and paragraph numbers restart in each section).

⛔ PHASE 2 RUNS IT, ONCE, AND THEN DELETES IT (kb/Work PB4887 "Landing"): it touches thousands of files, so it lands alone,
merged with main after the operator lane's branches drain, in the same change that deletes `citation.BARE_MEANS_2023`,
and the citations audit (`audit_code_citations.py --check`, which then refuses a bare citation as BARE) must be GREEN on
the rewritten tree. Phase 1 proves it on a scratch copy and never commits its output.

THE POPULATION. The files the citation audits read (`citation_corpus.all_files()`: the compiler's C# and grammars, the
tests, the suite's programs, the docs, the register), and the other PROSE and DATA that cite the standard: the Markdown of
the scripts and the skills, the root documents, the GitHub templates, the suite's diagnostic expectations,
`constructs.json`, and the Characterization snapshots (the compiler's diagnostic text changes in the same landing).
⛔ THE TOOLING'S CODE IS NOT REWRITTEN (`scripts/`, `.claude/` Python, PowerShell and JavaScript): its string literals are
data its own logic reads — a self-test's planted lines, `r2_collect.py`'s design references (`§8.x` of a design document,
which are also 2023 clause numbers) — and the scratch-copy proof broke two self-tests that way. Its citations of the
standard are counted and repaired by hand as they are touched. Every other tracked file is left out and counted too: the
frozen evidence (`citation_corpus.FROZEN`), the DEVLOG (history, never rewritten), `adjudication/` (agents' recorded
output), the generated inventory (`tests/version-matrix/inventory/`, rewritten only by `record_verdicts.py`) and `specs/`
(the standard's own text).
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import pathlib
import re
import sys
import tempfile

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402
import citation_corpus  # noqa: E402

REPO = citation_corpus.REPO
#: The other prose and data that cite the standard, beyond the audits' population (globs under the repository root).
EXTRA = ("scripts/**/*.md", ".claude/**/*.md", "*.md", ".github/**/*.md", ".github/**/*.yml",
         "conformance-suite/**/*.json", "tests/version-matrix/constructs.json",
         "tests/WiseOwl.Cobol.Tests.Characterization/Snapshots/**/*")
#: Never rewritten even when a glob above reaches them (see the docstring).
LEFT_OUT = ("DEVLOG/", "adjudication/", "tests/version-matrix/inventory/", "specs/", "specs-private/", "tools/") + tuple(
    f + "/" for f in citation_corpus.FROZEN)
#: A file's own numbered section headings (a Markdown document points at its own `§4.2`).
OWN_HEADING = re.compile(r"^#{1,6}\s+(\d+(?:\.\d+)+)")


@dataclasses.dataclass
class Tally:
    """What one run did (or would do), by directory and by edition."""
    rewritten: collections.Counter = dataclasses.field(default_factory=collections.Counter)    # directory -> count
    converted: collections.Counter = dataclasses.field(default_factory=collections.Counter)    # edition -> count
    left: collections.Counter = dataclasses.field(default_factory=collections.Counter)         # reason -> count
    files: int = 0
    hand: list[tuple[str, int, str, str]] = dataclasses.field(default_factory=list)            # (path, line, text, why)
    #: the 2023 clause numbers left bare beside a document's name, LISTED so a screen regression is visible (PB4887
    #: new defect 2: a count alone hid review R1-1's 1,258 genuine citations)
    beside: list[tuple[str, int, str, str]] = dataclasses.field(default_factory=list)          # (path, line, cite, text)


def population(repo: pathlib.Path = REPO) -> list[pathlib.Path]:
    """The files the rewrite reads: the audits' corpus and `EXTRA`, tracked or not ignored, never `LEFT_OUT`."""
    tracked = citation_corpus.repository_files()
    out = set(citation_corpus.all_files())
    for pattern in EXTRA:
        out.update(p for p in repo.glob(pattern) if p in tracked and p.is_file())
    return sorted(p for p in out if not p.relative_to(repo).as_posix().startswith(LEFT_OUT))


def _directory(rel: str) -> str:
    """The directory a count is filed under: two levels for the large trees, one otherwise."""
    parts = rel.split("/")
    return "/".join(parts[:2]) if len(parts) > 2 and parts[0] in ("src", "tests", "docs", "scripts", "kb") else parts[0]


def rewrite_line(line: str, own: frozenset[str], known: frozenset[str], rel: str, n: int, tally: Tally) -> str:
    """`line` with every bare citation of the standard in the edition form; what it left is counted in `tally`."""
    edits: list[tuple[int, int, str]] = []
    for c in citation.scan(line, bare=True):
        if not c.bare:
            continue
        at = line.index(c.locator, c.start)            # past the `§` and its optional space
        if citation.bare_standard(line, c, own, known):
            edits.append((c.start, at, "§2023:"))
            tally.rewritten[_directory(rel)] += 1
            continue
        edition = citation.other_edition(line[:c.start])
        if edition is not None:
            # ⛔ ANOTHER EDITION NAMED NEARBY DOES NOT MAKE THE NUMBER ITS OWN: "COBOL-2002 ROUNDED MODE (ISO §14.9.4)"
            # cites 2023's clause about a 2002 feature. The edition is decided only by ELIMINATION: a number 1985 has in
            # no section is 2023's; a number 2023 does not have is the named ISO edition's. Anything else is hand repair.
            homes = sorted(k for k in (citation.universe(edition) or ()) if k.split(".", 1)[-1] == c.locator)
            if edition == 1985 and c.locator in known and not homes:
                edits.append((c.start, at, "§2023:"))
                tally.converted["2023 (1985 has no such paragraph)"] += 1
            elif citation.EDITIONS[edition].locator == "clause" and c.locator not in known:
                edits.append((c.start, at, f"§{edition}:"))
                tally.converted[f"{edition} (2023 has no such clause)"] += 1
            else:
                why = (f"1985 sections {', '.join(homes[:6])} have paragraph {c.locator}" if edition == 1985
                       else f"{c.locator} is a 2023 clause, named beside {edition}")
                tally.hand.append((rel, n, line.strip()[:160], why))
                tally.left[f"{edition} named nearby (hand repair)"] += 1
        elif c.locator in own:
            tally.left["the document's own section"] += 1
        elif c.locator not in known:
            tally.left["not a 2023 clause (a document's section or a recorded phantom)"] += 1
        else:
            tally.left["a 2023 clause named beside a document (DOCUMENT_SECTION) or an item label"] += 1
            tally.beside.append((rel, n, '§' + c.locator, line.strip()[:160]))
    for start, end, text in reversed(edits):
        line = line[:start] + text + line[end:]
    return line


def rewrite_file(path: pathlib.Path, known: frozenset[str], tally: Tally, apply: bool, repo: pathlib.Path) -> None:
    rel = path.relative_to(repo).as_posix()
    try:
        text = path.read_bytes().decode("utf-8")
    except (OSError, UnicodeDecodeError) as e:
        tally.left[f"unreadable as UTF-8, not rewritten ({type(e).__name__})"] += 1   # counted, never skipped silently
        tally.hand.append((rel, 0, "", f"not read: {e}"))
        return
    if "§" not in text:
        return
    lines = text.splitlines(keepends=True)
    own = frozenset(m.group(1) for line in lines if (m := OWN_HEADING.match(line))) if path.suffix == ".md" else frozenset()
    out = "".join(rewrite_line(line, own, known, rel, n, tally) for n, line in enumerate(lines, 1))
    if out != text:
        tally.files += 1
        if apply:
            path.write_bytes(out.encode("utf-8"))


def left_out_counts(repo: pathlib.Path = REPO) -> collections.Counter:
    """The bare `§N.N` spellings in every repository file the rewrite does not read, by directory, so the record says
    what it did not touch (some are citations of the standard, some a document's own sections)."""
    counts: collections.Counter = collections.Counter()
    read = set(population(repo))
    for p in citation_corpus.repository_files():
        if p in read or not p.is_file():
            continue
        try:
            text = p.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        if "§" in text and (n := sum(1 for c in citation.scan(text, bare=True) if c.bare)):
            counts[_directory(p.relative_to(repo).as_posix())] += n
    return counts


def run(apply: bool, repo: pathlib.Path = REPO) -> Tally:
    known = citation.universe(2023, repo)
    if known is None:
        sys.exit("FATAL: the 2023 transcription is absent; the rewrite classifies against its clauses")
    tally = Tally()
    for path in population(repo):
        rewrite_file(path, known, tally, apply, repo)
    return tally


def report(tally: Tally, apply: bool, repo: pathlib.Path = REPO) -> None:
    verb = "rewrote" if apply else "would rewrite"
    print(f"{verb} {sum(tally.rewritten.values())} bare citations of the 2023 standard in {tally.files} files")
    for d, n in sorted(tally.rewritten.items(), key=lambda kv: -kv[1]):
        print(f"  {n:7d}  {d}")
    print("converted another ISO edition's free-text citation: "
          + (", ".join(f"{e}: {n}" for e, n in sorted(tally.converted.items())) or "none"))
    print("left as they are:")
    for why, n in sorted(tally.left.items(), key=lambda kv: -kv[1]):
        print(f"  {n:7d}  {why}")
    print("bare citations in the trees left out (never rewritten):")
    for prefix, n in sorted(left_out_counts(repo).items(), key=lambda kv: -kv[1]):
        print(f"  {n:7d}  {prefix}")
    print(f"a 2023 clause left bare beside a document's name or as an item label: {len(tally.beside)}")
    for rel, n, cite, text in tally.beside:
        print(f"  {rel}:{n}: {cite}")
        print(f"      {text}")
    print(f"hand repair: {len(tally.hand)}")
    for rel, n, text, why in tally.hand:
        print(f"  {rel}:{n}: {why}\n      {text}")


def self_test() -> int:
    """The classifier's every arm on planted lines, and idempotence."""
    known = citation.universe(2023)
    cases = [
        ("a bare citation of a 2023 clause is rewritten", "the MOVE statement (§14.9.25.4 GR6)", frozenset(),
         "the MOVE statement (§2023:14.9.25.4 GR6)"),
        ("its optional space goes", "ISO § 14.9.25.4", frozenset(), "ISO §2023:14.9.25.4"),
        ("an annex clause is rewritten", "A.1 item 14 (§A.1)", frozenset(), "A.1 item 14 (§2023:A.1)"),
        ("a document's section is left", "DESIGN-locale-facility §4.4.3 and plan §0", frozenset(),
         "DESIGN-locale-facility §4.4.3 and plan §0"),
        ("a word that contains a document's name is no document (review R1-1)",
         "the EVALUATE statement (§14.9.13) is evaluated per §8.8.1.5", frozenset(),
         "the EVALUATE statement (§2023:14.9.13) is evaluated per §2023:8.8.1.5"),
        ("a citation of the standard after a document's name and more prose is rewritten",
         "(docs/CONFORMANCE.md DOC-A.1-8, §12.3.7.4 GR2)", frozenset(), "(docs/CONFORMANCE.md DOC-A.1-8, §2023:12.3.7.4 GR2)"),
        ("a document's list of sections is left", "DESIGN-external-repository §4.5, §11.2", frozenset(),
         "DESIGN-external-repository §4.5, §11.2"),
        ("a rule range is no item label", "(§11.9.9.3 SR1-SR6)", frozenset(), "(§2023:11.9.9.3 SR1-SR6)"),
        ("the file's own section is left", "see §4.2 above", frozenset({"4.2"}), "see §4.2 above"),
        ("a recorded phantom is left", "the old comment cited §8.3.1.2", frozenset(), "the old comment cited §8.3.1.2"),
        ("an item label is left", "§9.10.1-C2 records it", frozenset(), "§9.10.1-C2 records it"),
        ("another ISO edition's clause is converted by elimination", "ISO 1989:2002 §14.8.4.9 SR2", frozenset(),
         "ISO 1989:2002 §2002:14.8.4.9 SR2"),
        ("a 2023 clause beside another edition is hand repair", "COBOL-2002 ROUNDED MODE (ISO §14.9.4)", frozenset(),
         "COBOL-2002 ROUNDED MODE (ISO §14.9.4)"),
        ("a 2023 clause 1985 has nowhere is 2023's", "COBOL-85 §8.9 does not reserve NULL", frozenset(),
         "COBOL-85 §2023:8.9 does not reserve NULL"),
        ("a 1985 free-text citation is left for hand repair", "COBOL-85 §5.3.2", frozenset(), "COBOL-85 §5.3.2"),
        ("an edition-form citation is left (idempotent)", "§2023:14.9.25.4 GR6", frozenset(), "§2023:14.9.25.4 GR6"),
        ("two on a line, both rewritten", "§14.9.25 and §14.9.24", frozenset(), "§2023:14.9.25 and §2023:14.9.24"),
    ]
    ok = True
    for name, line, own, want in cases:
        tally = Tally()
        got = rewrite_line(line, own, known, "x/y.md", 1, tally)
        again = rewrite_line(got, own, known, "x/y.md", 1, Tally())
        good = got == want and again == got
        ok &= good
        print(f"  {'ok  ' if good else 'FAIL'} {name}" + ("" if good else f"  (got {got!r}, then {again!r})"))
    hand = Tally()
    rewrite_line("COBOL-85 §5.3.2", frozenset(), known, "x/y.md", 7, hand)
    listed = len(hand.hand) == 1 and "VI.5.3.2" in hand.hand[0][3]
    ok &= listed
    print(f"  {'ok  ' if listed else 'FAIL'} a 1985 hand-repair item names the sections that have the paragraph")
    with tempfile.TemporaryDirectory() as td:
        p = pathlib.Path(td) / "a.md"
        p.write_bytes("crlf §14.9.25\r\nkept\r\n".encode("utf-8"))
        rewrite_file(p, known, Tally(), True, pathlib.Path(td))
        kept = p.read_bytes() == "crlf §2023:14.9.25\r\nkept\r\n".encode("utf-8")
    ok &= kept
    print(f"  {'ok  ' if kept else 'FAIL'} --apply keeps a file's line endings")
    print("SELF-TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--dry-run", action="store_true", help="report what --apply would change, change nothing")
    mode.add_argument("--apply", action="store_true", help="rewrite the tree in place")
    mode.add_argument("--self-test", action="store_true", help="the planted cases")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    tally = run(args.apply)
    report(tally, args.apply)
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
