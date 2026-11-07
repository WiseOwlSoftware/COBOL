"""THE ONE EDITION TABLE OF THE SPEC TOOLS (kb/Work PB3208 for the PDFs, PB4815 for the editions).

The bought PDFs of the COBOL standard are licensed per copy, so they live in the PRIVATE submodule `specs-private/`, one
per edition, each named after the public Markdown transcription of the same edition (`specs/ISO_COBOL_2023.md` ->
`specs-private/ISO_COBOL_2023.pdf`). Every tool that reads a transcription or measures a printed page asks THIS module for
the files: the locator and its absent message were written 16 times under four rules before (a sorted glob of
`*COBOL*.pdf`, a sorted glob of `*.pdf`, a literal file name carrying the order number), and a glob stopped naming one
file once a second edition's PDF arrived beside the first. The transcription's path was spelled by every tool the same
way, so the three editions (1985, 2002, 2023) would have needed one edit per tool; now `EDITIONS` is the one table.

    path(edition)       the PDF of that edition, or None when the submodule is not checked out or lacks it
    require(edition)    the same path, or exit naming the edition and the command that fetches the submodule
    markdown(edition)   the public Markdown transcription of that edition (tracked in this repository)
    geometry(edition)   "vector" or "raster": how that PDF's general formats are measured (figure_geometry.py dispatches)
    require_vector(ed, tool)  exit unless the edition's general formats are vector strokes (a tool that reads underlines)
    folio_offset(ed)    PDF page minus printed folio, or exit naming the missing table
    add_edition_argument(parser)        the one `--edition {1985,2002,2023}` option (default 2023), read as `args.edition`
    edition_data(table, edition, what)  a tool's per-edition DATA, or exit naming the table that lacks the edition

Run `python scripts/spec/spec_pdf.py --self-test`.
"""
from __future__ import annotations

import argparse
import contextlib
import dataclasses
import io
import pathlib
import re
import sys
import tempfile
from typing import TypeVar

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
PRIVATE = REPO / "specs-private"

DEFAULT_EDITION = 2023


@dataclasses.dataclass(frozen=True)
class Edition:
    """Everything the spec tools need to know about one edition, in one row."""
    pdf: str                    # file name under specs-private/
    markdown: str               # file name under specs/
    geometry: str               # "vector": the general formats are strokes in the PDF; "raster": they are scanned pixels
    folio_offset: int | None    # PDF page minus printed folio; None until measured from that edition's PDF


#: Each edition's files, named after its public transcription in specs/. 1985 is a scan (PB4812: 0 vector drawings and
#: about 33 images on a general-format page), so its underlines, brackets, braces and choice bars exist only as pixels
#: (PB4817 measures them); 2002 is a true vector PDF like 2023 (PB4813). The folio offset of 1985 and 2002 is measured
#: when their runs begin (PB4812, PB4813), so a tool that needs it exits naming this table until then.
EDITIONS: dict[int, Edition] = {
    1985: Edition("ANSI_COBOL_1985.pdf", "ANSI_COBOL_1985.md", "raster", None),
    2002: Edition("ISO_COBOL_2002.pdf", "ISO_COBOL_2002.md", "vector", None),
    2023: Edition("ISO_COBOL_2023.pdf", "ISO_COBOL_2023.md", "vector", 30),
}


def path(edition: int = DEFAULT_EDITION, private: pathlib.Path = PRIVATE) -> pathlib.Path | None:
    """The licensed PDF of `edition`, or None when it is absent. An edition this module does not name is a KeyError."""
    p = private / EDITIONS[edition].pdf
    return p if p.is_file() else None


def markdown(edition: int = DEFAULT_EDITION, repo: pathlib.Path = REPO) -> pathlib.Path:
    """The public Markdown transcription of `edition`. The file need not exist: an earlier edition's is being
    transcribed. An edition this module does not name is a KeyError."""
    return repo / "specs" / EDITIONS[edition].markdown


def geometry(edition: int = DEFAULT_EDITION) -> str:
    """How `edition`'s general formats are measured: "vector" strokes or "raster" pixels."""
    return EDITIONS[edition].geometry


def require_vector(edition: int, tool: str) -> None:
    """Exit unless `edition`'s general formats are vector strokes. A tool that reads underlines from a PDF page's drawings
    (`figure_extract.extract`) finds none in a scan, and would report every word un-underlined as a clean result. A scan
    is measured from pixels against the converted figures' words (`figure_raster.measure_figures`), which only the
    tools that read those words do: `sweep_figures.py` and `render_figure.py` (kb/Work PB4849)."""
    if geometry(edition) != "vector":
        sys.exit(f"FATAL: {tool} reads underlines from a PDF page's vector drawings, and the {edition} PDF is a scan whose "
                 "underlines are pixels. A scan is measured by scripts/spec/figure_raster.py (PB4817) against the converted "
                 "figures' words, which sweep_figures.py and render_figure.py read (PB4849); this tool has no raster arm "
                 f"yet and refuses the {edition} edition rather than report a clean page.")


def folio_offset(edition: int = DEFAULT_EDITION) -> int:
    """PDF page minus printed folio for `edition`; exits naming this table when that PDF has not been measured yet."""
    n = EDITIONS[edition].folio_offset
    if n is None:
        sys.exit(f"FATAL: spec_pdf.EDITIONS has no folio offset for the {edition} edition yet - measure it from that "
                 "edition's PDF (printed folio against PDF page) and record it there; no tool borrows another edition's.")
    return n


def add_edition_argument(parser: argparse.ArgumentParser) -> None:
    """The ONE spelling of the edition option every spec tool takes; read it back as `args.edition`."""
    parser.add_argument("--edition", type=int, choices=sorted(EDITIONS), default=DEFAULT_EDITION,
                        help=f"which edition of the standard to read (default {DEFAULT_EDITION})")


T = TypeVar("T")


def edition_data(table: dict[int, T], edition: int, what: str) -> T:
    """A tool's per-edition DATA (clause tables, the acknowledgment text, stamp shapes). An edition the table lacks exits
    naming the table; it never falls back to another edition's row (kb/Work PB4815)."""
    if edition not in table:
        sys.exit(f"FATAL: no {what} for the {edition} edition: the table is keyed by edition and has rows for "
                 f"{sorted(table)} only. Add the {edition} row from that edition's own text; never reuse another's.")
    return table[edition]


def absent_message(edition: int = DEFAULT_EDITION) -> str:
    return (f"FATAL: the licensed PDF of the {edition} COBOL standard (specs-private/{EDITIONS[edition].pdf}) was not found. "
            "It is licensed per copy and lives in a PRIVATE submodule; the public repository carries only the Markdown "
            "transcription in specs/. This tool measures the printed page, so it needs the PDF: "
            "git submodule update --init specs-private")


def require(edition: int = DEFAULT_EDITION, private: pathlib.Path = PRIVATE) -> pathlib.Path:
    p = path(edition, private)
    if p is None:
        sys.exit(absent_message(edition))
    return p


#: A line that finds a licensed PDF by itself: it builds a path from the submodule's name as its own quoted segment
#: (`REPO / "specs-private"`, `os.path.join(.., 'specs-private', ..)`), or names a .pdf file in it. A message that merely
#: mentions the submodule (`git submodule update --init specs-private`) quotes a longer string and is not one.
_STRAY = re.compile(r"""["']specs-private/?["']|specs-private/[^\s"']*\.pdf""", re.I)

#: A line that BUILDS or QUOTES the path of a transcription by itself, in either spelling: pathlib segments
#: (`REPO / "specs" / "ISO_COBOL_2023.md"`) or one string literal holding the whole path (`"specs/ISO_COBOL_2023.md"`, a
#: `--path` default, a PowerShell `Join-Path $repo 'specs/...'`). Prose that names the file (a docstring, a message, a
#: prompt) quotes a longer string with spaces and is not one; a tool that needs the file asks `markdown(edition)`, so the
#: next tool cannot weld itself to one edition's file again (kb/Work PB4815).
_STRAY_MARKDOWN = re.compile(r"""["']specs["']\s*/\s*["'][^"']*COBOL[^"']*\.md["']"""
                             r"""|["'][^"'\s]*specs/[^"'\s/]*COBOL[^"'\s/]*\.md["']""")


#: A trailing comment that marks a line as NAMING the transcription as repository data (a path classification key, a rename
#: map, a fixture), not reading it: the sweep passes such a line. Each use is visible and greppable.
NAMES_THE_FILE = "# spec_pdf: names-the-file"


def locator_copies_in(files: dict[str, str]) -> list[str]:
    """`path:line` of every line in `files` (path -> text) that locates a licensed PDF without this module."""
    # Only in a file that handles a PDF at all: a list of the repository's submodules names the directory too.
    return [f"{path}:{n}" for path, text in sorted(files.items()) if ".pdf" in text.lower()
            for n, line in enumerate(text.splitlines(), 1)
            if _STRAY.search(line) and not line.lstrip().startswith("#")]


def markdown_copies_in(files: dict[str, str]) -> list[str]:
    """`path:line` of every line in `files` (path -> text) that builds a transcription's path without this module."""
    return [f"{path}:{n}" for path, text in sorted(files.items())
            for n, line in enumerate(text.splitlines(), 1)
            if _STRAY_MARKDOWN.search(line) and not line.lstrip().startswith("#") and NAMES_THE_FILE not in line]


def _tracked_sources(repo: pathlib.Path, suffixes: tuple[str, ...]) -> dict[str, str]:
    """Every tracked file under scripts/ and .claude/ with one of `suffixes`, but this module."""
    import subprocess
    listed = subprocess.run(["git", "-C", str(repo), "ls-files", "scripts", ".claude"], capture_output=True, text=True,
                            check=True)
    here = pathlib.Path(__file__).resolve().relative_to(repo).as_posix()
    return {p: (repo / p).read_text(encoding="utf-8", errors="replace") for p in listed.stdout.splitlines()
            if p.endswith(suffixes) and p != here and (repo / p).is_file()}


#: The PyMuPDF module's deprecated alias (kb/Work PB3244): PyMuPDF warns on every `import fitz` and is dropping the name.
_FITZ = re.compile(r"\bfitz\b")

#: An import of PyMuPDF at a module's top level. CI's runners do not install PyMuPDF, so a tool that imports it there
#: fails its `--help` and usage-error probes (standard_streams.py rule 3) before argparse runs; every tool imports it
#: inside the function that opens the PDF (kb/Work PB4815's bounce of pull request #1237).
_TOP_LEVEL_PYMUPDF = re.compile(r"^(?:import|from)\s+pymupdf\b")


def pymupdf_imports_in(files: dict[str, str]) -> list[str]:
    """`path:line: why` of every line in `files` (path -> text) that names `fitz`, or imports PyMuPDF at the top level
    of a Python file."""
    found = []
    for path, text in sorted(files.items()):
        for n, line in enumerate(text.splitlines(), 1):
            if line.lstrip().startswith("#"):
                continue
            if _FITZ.search(line):
                found.append(f"{path}:{n}: fitz (the deprecated alias; say pymupdf)")
            elif path.endswith(".py") and _TOP_LEVEL_PYMUPDF.match(line):
                found.append(f"{path}:{n}: a top-level PyMuPDF import (import it inside the function that opens the PDF)")
    return found


def pymupdf_imports(repo: pathlib.Path = REPO) -> list[str]:
    """The PyMuPDF import sweep over every tracked Python, PowerShell and shell file under scripts/ and .claude/."""
    return pymupdf_imports_in(_tracked_sources(repo, (".py", ".ps1", ".sh")))


def locator_copies(repo: pathlib.Path = REPO) -> list[str]:
    """The drift sweep (kb/Work PB3208, CLAUDE.md rule 5): every tracked script under scripts/ but this one."""
    files = {p: t for p, t in _tracked_sources(repo, (".py", ".ps1", ".sh")).items() if p.startswith("scripts/")}
    return locator_copies_in(files)


def markdown_copies(repo: pathlib.Path = REPO) -> list[str]:
    """The same sweep for the transcription's path, over every tracked Python, PowerShell and shell file under scripts/
    and .claude/."""
    return markdown_copies_in(_tracked_sources(repo, (".py", ".ps1", ".sh")))


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(("  ok    " if cond else "  FAIL  ") + name)
        if not cond:
            failures.append(name)

    with tempfile.TemporaryDirectory() as td:
        d = pathlib.Path(td)
        check("an absent submodule gives None", path(2023, d / "missing") is None)
        for e in EDITIONS:
            (d / EDITIONS[e].pdf).write_bytes(b"%PDF-1.4")
        (d / "zz-other COBOL.pdf").write_bytes(b"%PDF-1.4")
        check("each edition resolves to its own file, whatever else the directory holds",
              all(path(e, d) == d / EDITIONS[e].pdf for e in EDITIONS))
        (d / EDITIONS[2002].pdf).unlink()
        check("a missing edition is None, never another edition's file", path(2002, d) is None)
        try:
            require(2002, d)
            check("require exits on a missing edition", False)
        except SystemExit as ex:
            check("require exits naming the edition and the fetch command",
                  "2002" in str(ex.code) and "git submodule update --init specs-private" in str(ex.code))
        try:
            path(1999, d)
            check("an unnamed edition is an error", False)
        except KeyError:
            check("an unnamed edition is an error", True)
    check("each edition names its own transcription in specs/",
          [markdown(e, pathlib.Path("r")).as_posix() for e in sorted(EDITIONS)]
          == ["r/specs/ANSI_COBOL_1985.md", "r/specs/ISO_COBOL_2002.md", "r/specs/ISO_COBOL_2023.md"])
    check("each transcription is named after its PDF",
          all(pathlib.Path(v.pdf).stem == pathlib.Path(v.markdown).stem for v in EDITIONS.values()))
    check("the scanned edition is measured from pixels, the others from strokes",
          [geometry(e) for e in sorted(EDITIONS)] == ["raster", "vector", "vector"])
    require_vector(2002, "a planted tool")
    require_vector(2023, "a planted tool")
    try:
        require_vector(1985, "a planted tool")
        check("require_vector exits for the scanned edition", False)
    except SystemExit as ex:
        check("require_vector exits for the scanned edition, naming the tool, the edition and PB4817",
              all(w in str(ex.code) for w in ("a planted tool", "1985", "PB4817")))
    check("2023's folio offset is recorded", folio_offset(2023) == 30)
    try:
        folio_offset(1985)
        check("an unmeasured folio offset exits", False)
    except SystemExit as ex:
        check("an unmeasured folio offset exits naming the table and the edition",
              "EDITIONS" in str(ex.code) and "1985" in str(ex.code))
    parser = argparse.ArgumentParser()
    add_edition_argument(parser)
    check("--edition defaults to 2023 and takes each named edition",
          parser.parse_args([]).edition == 2023
          and [parser.parse_args(["--edition", str(e)]).edition for e in sorted(EDITIONS)] == sorted(EDITIONS))
    try:
        with contextlib.redirect_stderr(io.StringIO()):
            parser.parse_args(["--edition", "1999"])
        check("--edition refuses an unnamed edition", False)
    except SystemExit:
        check("--edition refuses an unnamed edition", True)
    check("edition_data returns the row of the edition asked", edition_data({2023: "a", 2002: "b"}, 2002, "x") == "b")
    try:
        edition_data({2023: "a"}, 1985, "acknowledgment text")
        check("edition_data exits on a missing row", False)
    except SystemExit as ex:
        check("edition_data exits naming the table and the edition, never falling back",
              "acknowledgment text" in str(ex.code) and "1985" in str(ex.code))
    stray = locator_copies()
    check("this tree: no script outside spec_pdf.py locates a licensed PDF itself (a specs-private glob or file name)"
          + (f" -- found {stray[:5]}" if stray else ""), not stray)
    planted = locator_copies_in({"scripts/x.py": 'PDF = next((REPO / "specs-private").glob("*.pdf"))'})
    check("the sweep finds a planted glob", planted == ["scripts/x.py:1"])
    split = locator_copies_in({"scripts/y.py": 'PDF_DIR = REPO / "specs-private"\npdf = next(PDF_DIR.glob("*.pdf"))',
                               "scripts/z.py": 'sys.exit("absent: git submodule update --init specs-private")'})
    check("the sweep finds a directory named on one line and globbed on the next, and passes a message",
          split == ["scripts/y.py:1"])
    welded = markdown_copies()
    check("this tree: no script or workflow outside spec_pdf.py builds a transcription's path itself"
          + (f" -- found {welded[:5]}" if welded else ""), not welded)
    planted_md = markdown_copies_in({
        "scripts/a.py": 'SPEC_MD = REPO / "specs" / "ISO_COBOL_2023.md"',
        "scripts/b.py": "SPEC = ROOT / 'specs' / 'ANSI_COBOL_1985.md'",
        "scripts/c.py": '"""Reads specs/ISO_COBOL_2002.md"""\nmsg = "no page anchors in specs/ISO_COBOL_2023.md"',
        "scripts/d.py": '# SPEC = REPO / "specs" / "ISO_COBOL_2023.md"\nREADME = REPO / "specs" / "README.md"',
        "scripts/e.py": 'SPEC = "specs/ISO_COBOL_2023.md"',
        "scripts/f.py": "p = REPO / 'specs/ISO_COBOL_2002.md'",
        "scripts/g.py": 'ap.add_argument("--path", default="specs/ISO_COBOL_2023.md")',
        "scripts/h.ps1": "$specPath = Join-Path $repo 'specs/ISO_COBOL_2023.md'",
        "scripts/i.py": 'KEY = {"specs/ISO_COBOL_2023.md": "oracle"}  ' + NAMES_THE_FILE})
    check("the transcription sweep finds a built path in either quote style, as pathlib segments or one literal, in "
          "Python and PowerShell, and passes prose, a comment and the README",
          planted_md == ["scripts/a.py:1", "scripts/b.py:1", "scripts/e.py:1", "scripts/f.py:1", "scripts/g.py:1",
                         "scripts/h.ps1:1"])
    imports = pymupdf_imports()
    check("this tree: no script names fitz or imports PyMuPDF at its top level"
          + (f" -- found {imports[:5]}" if imports else ""), not imports)
    planted_imports = pymupdf_imports_in({
        "scripts/a.py": "import fitz\n",
        "scripts/b.py": "def main():\n    import fitz\n    doc = fitz.open(p)\n",
        "scripts/c.py": "import os\nimport pymupdf\n",
        "scripts/d.py": "def main():\n    import pymupdf  # late\n    doc = pymupdf.open(p)\n# import pymupdf\n",
        "scripts/e.sh": "python -c 'import fitz; print(fitz.VersionBind)'\nimport pymupdf\n"})
    check("the import sweep finds fitz anywhere and a top-level PyMuPDF import in Python, and passes a late import, "
          "a comment and a shell line",
          [f.split(": ")[0] for f in planted_imports]
          == ["scripts/a.py:1", "scripts/b.py:2", "scripts/b.py:3", "scripts/c.py:2", "scripts/e.sh:1"])
    print(f"=== SPEC PDF LOCATOR SELF-TEST: {'FAIL - ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    ap = argparse.ArgumentParser(description="List the editions, their transcriptions and whether each licensed PDF is present.")
    ap.add_argument("--self-test", action="store_true", help="run the planted self-test")
    ap.add_argument("--markdown", type=int, choices=sorted(EDITIONS), metavar="EDITION",
                    help="print the absolute path of that edition's transcription and nothing else (for a PowerShell script)")
    args = ap.parse_args()
    if args.markdown:
        print(markdown(args.markdown))
        sys.exit(0)
    if args.self_test:
        sys.exit(self_test())
    for e in sorted(EDITIONS):
        print(e, markdown(e).relative_to(REPO).as_posix(), geometry(e), path(e) or "absent")
