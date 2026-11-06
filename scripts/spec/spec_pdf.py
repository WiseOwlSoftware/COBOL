"""THE ONE LOCATOR OF THE LICENSED STANDARD PDFS (kb/Work PB3208).

The bought PDFs of the COBOL standard are licensed per copy, so they live in the PRIVATE submodule `specs-private/`, one
per edition, each named after the public Markdown transcription of the same edition (`specs/ISO_COBOL_2023.md` ->
`specs-private/ISO_COBOL_2023.pdf`). Every tool that measures a printed page asks THIS module for the file: the locator
and its absent message were written 16 times under four rules before (a sorted glob of `*COBOL*.pdf`, a sorted glob of
`*.pdf`, a literal file name carrying the order number), and a glob stopped naming one file once a second edition's PDF
arrived beside the first.

    path(edition)     the PDF of that edition, or None when the submodule is not checked out or lacks it
    require(edition)  the same path, or exit naming the edition and the command that fetches the submodule

Run `python scripts/spec/spec_pdf.py --self-test`.
"""
from __future__ import annotations

import pathlib
import re
import sys
import tempfile

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
PRIVATE = REPO / "specs-private"

#: Each edition's PDF, named after its public transcription in specs/.
EDITIONS: dict[int, str] = {
    1985: "ANSI_COBOL_1985.pdf",
    2002: "ISO_COBOL_2002.pdf",
    2023: "ISO_COBOL_2023.pdf",
}


def path(edition: int = 2023, private: pathlib.Path = PRIVATE) -> pathlib.Path | None:
    """The licensed PDF of `edition`, or None when it is absent. An edition this module does not name is a KeyError."""
    p = private / EDITIONS[edition]
    return p if p.is_file() else None


def absent_message(edition: int = 2023) -> str:
    return (f"FATAL: the licensed PDF of the {edition} COBOL standard (specs-private/{EDITIONS[edition]}) was not found. "
            "It is licensed per copy and lives in a PRIVATE submodule; the public repository carries only the Markdown "
            "transcription in specs/. This tool measures the printed page, so it needs the PDF: "
            "git submodule update --init specs-private")


def require(edition: int = 2023, private: pathlib.Path = PRIVATE) -> pathlib.Path:
    p = path(edition, private)
    if p is None:
        sys.exit(absent_message(edition))
    return p


#: A line that finds a licensed PDF by itself: it builds a path from the submodule's name as its own quoted segment
#: (`REPO / "specs-private"`, `os.path.join(.., 'specs-private', ..)`), or names a .pdf file in it. A message that merely
#: mentions the submodule (`git submodule update --init specs-private`) quotes a longer string and is not one.
_STRAY = re.compile(r"""["']specs-private/?["']|specs-private/[^\s"']*\.pdf""", re.I)


def locator_copies_in(files: dict[str, str]) -> list[str]:
    """`path:line` of every line in `files` (path -> text) that locates a licensed PDF without this module."""
    # Only in a file that handles a PDF at all: a list of the repository's submodules names the directory too.
    return [f"{path}:{n}" for path, text in sorted(files.items()) if ".pdf" in text.lower()
            for n, line in enumerate(text.splitlines(), 1)
            if _STRAY.search(line) and not line.lstrip().startswith("#")]


def locator_copies(repo: pathlib.Path = REPO) -> list[str]:
    """The drift sweep (kb/Work PB3208, CLAUDE.md rule 5): every tracked script under scripts/ but this one."""
    import subprocess
    listed = subprocess.run(["git", "-C", str(repo), "ls-files", "scripts"], capture_output=True, text=True, check=True)
    here = pathlib.Path(__file__).resolve().relative_to(repo).as_posix()
    files = {p: (repo / p).read_text(encoding="utf-8", errors="replace") for p in listed.stdout.split()
             if p.endswith((".py", ".ps1", ".sh")) and p != here and (repo / p).is_file()}
    return locator_copies_in(files)


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
            (d / EDITIONS[e]).write_bytes(b"%PDF-1.4")
        (d / "zz-other COBOL.pdf").write_bytes(b"%PDF-1.4")
        check("each edition resolves to its own file, whatever else the directory holds",
              all(path(e, d) == d / EDITIONS[e] for e in EDITIONS))
        (d / EDITIONS[2002]).unlink()
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
    stray = locator_copies()
    check("this tree: no script outside spec_pdf.py locates a licensed PDF itself (a specs-private glob or file name)"
          + (f" -- found {stray[:5]}" if stray else ""), not stray)
    planted = locator_copies_in({"scripts/x.py": 'PDF = next((REPO / "specs-private").glob("*.pdf"))'})
    check("the sweep finds a planted glob", planted == ["scripts/x.py:1"])
    split = locator_copies_in({"scripts/y.py": 'PDF_DIR = REPO / "specs-private"\npdf = next(PDF_DIR.glob("*.pdf"))',
                               "scripts/z.py": 'sys.exit("absent: git submodule update --init specs-private")'})
    check("the sweep finds a directory named on one line and globbed on the next, and passes a message",
          split == ["scripts/y.py:1"])
    print(f"=== SPEC PDF LOCATOR SELF-TEST: {'FAIL - ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    for e in sorted(EDITIONS):
        print(e, path(e) or "absent")
