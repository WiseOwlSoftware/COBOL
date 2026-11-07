## The 1985 transcription's converter, and the raster measurer calibrated on twelve real pages

Wave 20261886, group A (kb/Work PB4816, PB4848), after the owner lifted the hold on running the tools against the licensed 1985 scan ("yes, go ahead with all the steps").
No API call was made and nothing was run against the 2002 PDF.

**PB4816, one converter for every edition (`scripts/spec/transcribe_pdf.py`).** The 2023 conversion lived in five private scripts with a retired checkout's paths, a retired
model and one synchronous call per page. The new tool takes `--edition` and a state directory and has `submit`, `status`, `collect`, `retry`, `pages <list>` (a batch, or
`--local MODEL`; `--replace` keeps the old text in `orig/`), `check` (word agreement with the PDF's text layer, numbers only) and `assemble`. It is restartable: the state is written
atomically (a temporary file, then a rename) before a batch is created (the intent) and after (its id), so a crash between the two is reconciled from the server's batch list;
a page is written once and never overwritten; `retry` takes errored, expired, cut-off and missing pages. The key reader moved to one place (`scripts/api_key.py`) and
`fuzz_claude.py`, `fuzz_batch.py` and the new tool all use it. The tool ADOPTED the owner's run as it stood (976 of 976 pages, 8 of them local): `status` reads 976/976 and
`submit` finds nothing to submit. `assemble` wrote `specs/ANSI_COBOL_1985.md`: 976 `## Page N` sections under 21 `# ` parts (front matter, Sections I to XVIII, the two
amendments). Two raw pages (351, 477) had the reseller's footer on a line of its own; assembly drops stamp lines and refuses a page that still matches, so the raw pages stay in
the private state directory and only the assembly is tracked. The self-test plants a stand-in PDF and a fake batches client (it needs no PDF library, which CI's self-test jobs
do not install) and makes no API call. Not done, so the note stays open: the `specs-private` commit (delete the five scripts, bump the pointer) is a push to the private
repository, which a worktree does not make; the scratch scripts of the 1985 run are deleted once the owner has seen the tool adopt the run.

**PB4848, the raster measurer run on real pages for the first time.** It was wrong in six ways, each found by rendering the page and counting by eye, each fixed at its root.
Printed braces have no straight halves (the old `BRACE_STRAIGHT` and `BRACE_ALIGNED` rejected every brace over two lines, and are deleted): a brace is now a departure of the
edge's middle from its shoulders, its side the hooks' direction. A tall brace is overstruck from stacked glyphs with a gap, so pieces that are no brace alone are stacked. A
five-line brace has a straight middle and hooks that read as a bracket, and a 72 pt bracket leans 1.8 pt, so stems take the run's full extent, feet anchor on the stroke's
own edge per row, and a line fit that may lean decides bracket against brace. Underlines: the 0.7 fill test refused ragged tilted rules (real boxes fill 0.42 to 0.76), hyphens
were read as underlines and removed from their words, and an underline fused to its word was never found (a band erased, the letters made components again, a descender's tail
rejoined). Boxes (the standard boxes level-2 specifications, PDF page 23 and 147; amendments delete boxes) were read as underlines and choice bars; `find_boxes` finds four
rules even when a brace touches an edge and lists the words each encloses. `BRACE_GLYPH_H` was 13 (the 2023 vector value) and is 10.2, the median of 120 braces. Twelve regions
(PDF pages 196, 217, 252, 389, 396, 426, 444, 493, 568, 684, 693, 845) were counted by eye and cropped, stamp margins excluded, into `scripts/spec/figure_raster_pages/`;
`test_figure_raster.py` measures every crop with its page's skew and asserts the counts, and holds a planted case per mechanism. The table is in PB4848's body.
Registered and not fixed: Amendment 1's bold face (pages 841, 844: a 54 pt closing bracket with 1.3 pt feet reads as a brace), boxes that wrap a line, and the measurer's
fitness for monospaced bands only.

**Self-review** (architecture, full code, performance, duplication). One mechanism per job: the key reader, the stamp shapes (from `verify_publishable`), the edition table
(`spec_pdf`) and the underline tests (`figure_extract`) are each read from their one home; no wrapper or alias was added and the old key reader is deleted from `fuzz_claude.py`.
Two defects found in files already read were fixed in place: the planted-only fill test, and a stem's median extent that lost the first 4.7 pt of a leaning bracket.
Gate: see the report.
