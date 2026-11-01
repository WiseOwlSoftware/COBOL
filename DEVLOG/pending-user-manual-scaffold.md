## The user manual exists: docs/manual/ and its hand-written chapters (kb/Work PB1610)

**What was wrong.** ISO/IEC 1989:2023 section 4.2.16 makes the implementor's documentation of clauses 4.2.3, 4.2.4, 4.2.5, 4.2.6,
4.2.10, 4.2.12 and 4.2.13 a conformance obligation, and owner decision R50 chose docs-as-code (Astro Starlight on
wiseowlsoftware.com). `docs/manual/` had never been created: the host-entry chapter lived in the external-repository design
(section 9.4), the clause 4.2.4 statement did not exist anywhere, and a new user had the README and nothing else.

**What landed.** The scaffold and the hand-written half of the manual, in Starlight-compatible Markdown (frontmatter `title`,
`description`, `sidebar.order`, relative links ending in a slash so a bundle mounted at any version prefix keeps them):
`index.md` (with the table that says where each section 4.2.16 obligation is answered), `install.md`,
`getting-started.md`, `user-guide/` (`command-line.md`, `editions.md`, `source-format.md`, `run-units.md`,
`locale-and-collation.md`, `exceptions.md`, `files.md`), `dotnet-interop.md` (clause 4.2.3), `migrating.md` (clause 4.2.4) and
`release-notes.md` (the convention; no release exists). The chapter "Calling a WiseOwl COBOL program from .NET" moved out of
`DESIGN-external-repository.md` section 9.4 into `dotnet-interop.md`, with slice 5's trimmed/AOT paragraph, and the design's
determination (a) and slice 7's line now say slice 7 writes its page into `docs/manual/` directly. The "Separately compiled
units" chapter stays in section 9.4 as slice 7's draft because it describes `--repository`, `--reference` and
`--repository-check`, which the command line does not have yet; a manual describes what the compiler does.

**Every behavioral statement was run.** Each page was written against probes on a Debug build: the exit statuses (0, 64, 65, 66,
70, the program's own status under `--run`, 1 for an abnormal termination), `--std 85|2002` against `EXIT PERFORM`,
`ALTER`, `STOP "literal"` and `NEXT SENTENCE` (COBOL0900, COBOL0810, COBOL0902, COBOL0903, and `--permissive` turning the
removal errors into warnings), `--flag-extensions` (COBOL2894), the COPY search with and without `--copy`, free form and the
`>>SOURCE FORMAT` directive, the `TURN` directive with a `USE AFTER EXCEPTION CONDITION` declarative and `RESUME NEXT
STATEMENT`, `COBOL_USER_LOCALE` with `ALPHABET ... IS LOCALE` (`es-ES` flips the order of `ñu` and `nz`), a line sequential
file (UTF-8 bytes `E2 82 AC`, CR LF on Windows, no file beside it, status 10 at end of file), and a C# host built with
Roslyn against an emitted assembly (`ADDTAX.Call(100.00m, 0.08m)` returns 108.00; a host that sees `RunUnitEndedException`
with status 7 and then starts a fresh run unit). The refusals the migration page lists (COBOL2269 ENTRY, COBOL0313 JSON,
COBOL1560 and COBOL1707 SCREEN, COBOL1706 WRITE FILE, COBOL0001 for EXEC SQL) were each produced. Citations were checked with
`cite.py --check`.

**What the probes found that the notes had not.** (1) A binary item of 1 or 2 digits is one byte (`FUNCTION BYTE-LENGTH`
answers 1, 2, 4 for `S9(2)`, `S9(4)`, `S9(9)`), not IBM's halfword for 1 to 4 digits, and `SYNCHRONIZED` adds no padding
(Annex A.1 item 205): the migration page tells the reader to check the record layouts. (2) `cobol check-batch --help` prints
a spurious `<source>` argument in its usage line (the command is `cobol check-batch <manifest>`), and the README called the
TSV manifest `manifest.json`; the README is corrected here and the CLI help is registered in the report. (3) An SDK-style
host project cannot reference an emitted assembly (CS0012, `System.Private.CoreLib`; kb/Work PB4047); the interop page says so
in a section that PB4047's landing deletes, and PB4047's `**Sites:**` now names the page.

**The drift test.** `UserManualDriftTests` (Unit, tree sweep) keeps four things the pages repeat from the product derived from
it: every `--option` of `Program.BuildParser` appears on `user-guide/command-line.md` and the page names no other; the page's
exit-status table equals `Program.ExitCode` over `CompilerDriver.Outcome` plus the usage error 64; every `COBOL####` /
`CBL####` code in a page is a `DiagnosticCatalog` code, a parse-layer descriptor or a construct row of
`tests/version-matrix/constructs.json`; every relative link resolves to a page and a heading, and ends in a slash; and every
complete program in a `cobol` code block compiles, check-only. Each check was made to fail once with an invented option, a
status 99, a code COBOL9999, three malformed links and a program that references an undeclared name.

**Register.** kb/Work PB1610 landed with `closes_rows: []` (the note claimed no inventory row and §4.2.16 has none of its
own). `Sites:` paragraphs were added to PB1610 and PB4047, and PB1612, PB1613 and PB2103 each gained the line that says
which manual page their landing changes. File types stays kb/Work PB3352's.
