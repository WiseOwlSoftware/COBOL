# Transcription state — what is done, what is next

> Live state for `specs/ISO_COBOL_2023.md`, and for `specs/ANSI_COBOL_1985.md` in its own section below ("The 1985
> transcription"). Plan §0 remains the SSOT for the project; this is the detail behind its spec-reconciliation
> bullets, kept here so a new session can resume without re-deriving it.

## Run these first

```
python scripts/spec/lint_rendering.py                      # legibility — currently CLEAN
python scripts/spec/sweep_figures.py --check               # the 484 general formats, regenerated and diffed
python scripts/spec/verify_publishable.py                  # no licence data, acknowledgment in the preface
python scripts/spec/verify_acknowledgment.py               # verbatim against the PDF (needs specs-private/)
python scripts/spec/repairs/reserved_word_gaps.py --check  # §8.9 == the printed reserved words (needs the PDF)
```

Last known: **lint CLEAN** · sweep clean 484/484 · publishable at 47,187 lines · acknowledgment verbatim ·
§8.9 == the printed page (409 words) · 3,811 internal links, zero dangling.

## ⛔ Markdown re-parses plain text — the class that produced the most damage

Everything below was found in one sweep after the owner noticed `>>IF` rendering as a blue bar. The words were
all present and every text-level audit was green; what was broken was that **Markdown treated the standard's
own notation as its syntax.** When touching prose, assume this class first.

- **A rule label is not a list marker.** `1)` at column 0 is an ordered-list item, so the printed `1)` rendered
  `1.` — colliding with the standard's THIRD level, which really is printed `1.` — while `a)`, not being a
  marker, stayed literal. Worse, a list item's content is re-parsed as BLOCK syntax, so eight rules opening
  with `>` LOST it: `9) >= is an abbreviation for GREATER THAN OR EQUAL TO.` rendered as a claim about `=`.
  Labels are escaped now (`1\)`, `1\.`) and `lint_rendering.py`'s RULE LABEL check gates it.
- **A sub-rule indented 4+ spaces is a CODE BLOCK** once its parent list is gone. This only surfaced by
  rendering the candidate fix, which came out in monospace.
- **A bare-line list is one run-on paragraph.** §8.9's 409 reserved words had no markers. The existing RUN-ON
  LIST check missed it because that check keys on a cross-reference link per line, and a word list has none.
- **Character-sequence words must be inline code.** §8.9's fifteen special-character words were destroyed —
  `+` became an empty bullet, `>`/`>>` VANISHED, `>=` rendered as `=`, and `<`/`<>` became an `<h1>` because
  the `=` beneath them read as a setext underline. As `` `>>` `` they are immune and need no escape soup.
- **Escape `<`/`>` at write time inside `<pre>`** — see rule 7 below.

**Render, do not reason.** Pandoc is on PATH; `pandoc -f commonmark -t html` on a slice is the cheapest way to
settle any of these, and it is what proved every one above rather than a reading of the CommonMark spec.

## ✅ NOTHING OUTSTANDING on the transcription

Figure D.6 landed 2026-07-28 and `lint_rendering.py` is green. Every Annex D illustration is now drawn or
deliberately left (see the table), every general format is generated from the printed page, and the
reconciliation itself closed at 210/210. The next spec-side work is not here — it is the **grammar ↔ spec
audit**, plan §0 NEXT item 3.

Should a further figure ever need drawing: captions sit ABOVE their figure, and a figure may flow onto the
NEXT page — p1185 carries D.11's caption and chart plus D.12's caption, with D.12's chart on p1186; D.6's
caption is on p1161 and its body on p1162. The standard prints `Figure  D.6` with TWO spaces, which defeats a
naive `Figure D\.\d+` regex.

```python
# locating a printed figure
import pymupdf, re
for pno in range(doc.page_count):
    if re.search(r"Figure\s+D\.6\s*—", doc[pno].get_text()): print(pno + 1)
```
Render with `python scripts/render-spec-page.py <page>` and look at it; do not infer the shape from the text
layer.

## The Annex D figures — all 9 accounted for

| figure | state | generator |
|---|---|---|
| D.1 SEARCH decision chart | drawn | `annex_d_flowcharts.py` |
| D.2 compilation group sample | already fine | — |
| D.3 compilation group / run unit structure | drawn | `annex_d_structure.py` |
| D.4 manager class | already fine | — |
| D.5 banking hierarchy | Markdown TABLES — a fair representation of a class hierarchy, deliberately left | — |
| D.6 example page layout | drawn | `annex_d_structure.py` |
| D.7 AND chain | drawn | `annex_d_truth_charts.py` |
| D.8 OR chain | was correct; regenerated | `annex_d_truth_charts.py` |
| D.9, D.10 mixed / two-column | drawn | `annex_d_truth_charts.py` |
| D.11–D.14 VARYING charts | drawn | `annex_d_flowcharts.py` |

## ⛔ Rules for drawing one

1. **The collision guard is not optional.** `put` refuses to overwrite a non-blank cell; a separate
   `junction()` handles the one legitimate overwrite (a border glyph becoming `├`/`┤` where a branch attaches),
   and fails if the expected glyph is not there. It has caught seven defects that would each have rendered as a
   plausible picture. I wrote the third generator without it and it produced `Fromtotherucomp.tgroup`.
2. **Compute the geometry; never count characters.** A box is sized from its content and every connector sits
   at a computed column. Hand-counting is what produced the ragged figures in the first place.
3. **The house style is settled by Figure D.8** — centred text, connectors meeting a border at a `┬`/`┴`/`├`
   junction rather than a floating arrowhead, branch labels beside their connector. Match it; do not invent.
4. **`str.center` is not centring.** For an odd margin it puts the extra space on the LEFT
   (`marg // 2 + (marg & width & 1)`). Use the local `centre()`.
5. **A splice must not swallow prose.** Replacing a figure's body replaces everything between the caption and
   the next anchor — which for D.1 also held the standard's two footnotes, deleted silently. Both generators
   now stop at the first line that reads as a sentence.
6. **Labels decide the width**, not the other way round: `P-1-2-1`/`P-1-2-2` are seven characters each and did
   not fit the column they were meant to label.
7. **Escape `<` and `>` at WRITE time, never on the canvas.** A `<pre>` block is raw HTML, so D.6's own
   notation — `<blank>`, `<Detail lines>` — is a TAG, and a sanitizing renderer drops an unknown tag and the
   words with it, leaving a blank line under a green audit. The canvas holds the real characters so the
   geometry and the collision guard measure what the standard prints; `escape()` converts on the way out.
   `lint_rendering.py`'s SWALLOWED check is the gate (14 findings on the unescaped form).
8. **Vertical distance can BE the content.** D.6 stood as a three-column Markdown table, which gives every row
   the same height — and the figure is about where lines fall on a page. Place rows from the measured printed
   y at the page's own pitch (8.7 pt for D.6): that is what keeps the void mid-body ("and further body
   groups") and the void between the logical and the physical bottom of form. Column positions cannot come
   from the page the same way when a wrapped printed label is drawn on one line; size those from content.

## ISO's own defects — transcribed AS PRINTED, deliberately

Confirmed by rendering the page at 600 dpi, not by reading the text layer. Do not "fix" them silently; each is
an Addendum candidate, an owner call.

| printed | should be | where |
|---|---|---|
| `i-O` (lower-case i) | `I-O` | §8.9 reserved words |
| `I-OICONTROL` | `I-O-CONTROL` | §8.9 reserved words |
| `EMD-START` | `END-START` | §8.9 — **already corrected**, Addendum C1 |
| `END-PERFORM, END-RECEIVE, END-READ` | alphabetical order | §8.9 — the printed list is out of order here |

**`>>IF` vs `>> IF` is NOT a defect and belongs in no such table** — the standard prints both because **BOTH ARE
LEGAL, and it says so twice.** §7.3 SR5: "A compiler directive is composed of the compiler directive indicator,
*optionally followed by the COBOL character space*, followed by compiler-instruction. The compiler directive
indicator shall be treated as though it were followed by a space if no space is specified after the
indicator." And the character-set table: "`>>` … a compiler-directive line when followed by a
compiler-directive word — *with or without an intervening space*". Standard-wide the transcription carries 29
spaced and 42 glued, faithfully. A conforming compiler must accept BOTH; ours does — all seven recognizers in
`Frontend/Preprocessor/` slice `>>` then `TrimStart()` (or match `>>\s*`).

✅ **OWNER DECISION 2026-07-28 — FOLLOW THE PDF; do not normalise either way.** Normalising to one spelling was
considered and declined. It would make the figure GENERATOR deviate from the page it measures — either baked
in, which blunts `sweep_figures --check` exactly where we deviate, or as a post-pass every sweep re-undoes —
and it would need one mechanism for the generated figures and a second for the hand-written syntax rules. It
would also be a deliberate 42-instance departure requiring an Addendum entry, for a spelling that is not a
defect. The ambiguity is removed instead by the marked editorial note at §7.3, which names the rule, quotes
both citations, and states that neither spelling is preferred. **A reader who finds `>>IF` on one line and
`>> IF` on another is seeing the standard, not an error — leave it.**

⚠ The underline defect this question grew out of is a separate matter and IS fixed: five figures underlined the
`>>` itself, making it look like a required WORD. `figure_extract` now measures the underline per CHARACTER, and
that measurement may only ever TRIM a word the per-word test accepted — never promote one, because a bracket
foot is a short rule that cannot cover 55% of a word but trivially covers one glyph.

## Already closed, do not redo

Pages removed entirely (anchors, running headers, and the 758 leftover `---` rules) · every page reference
re-keyed to a clause link, none left bare · index, TOC and the tables/figures lists rebuilt as real nested
lists, with the index's sub-entry levels MEASURED off the printed page (`data/index-levels.json`) · 18
page-break table joints merged and Table 10's 24×24 matrix rebuilt from geometry · 137 literal `*` escaped ·
all 484 figures carrying `line-height:1` · `figure_extract.is_underlined` widened to catch underlines drawn
inside a word's descender space (changes no figure — verified).

## The 1985 transcription

`specs/ANSI_COBOL_1985.md` is ANSI X3.23-1985 with its amendments X3.23a-1989 and X3.23b-1993, each under its own
`AMENDMENT` heading. Unlike 2023's, it keeps its pages: `## Page N` opens the text of PDF page N, because the source is a
scan and every repair is read against the rendered page (`render-spec-page.py --edition 1985 <page>`).

```
python scripts/spec/lint_rendering.py --edition 1985         # legibility
python scripts/spec/sweep_figures.py --edition 1985 --check  # every general format in its decided form (about 12 min)
python scripts/spec/verify_publishable.py --edition 1985     # no stamp, the Preface, and the reconciliation ledger
python scripts/spec/verify_acknowledgment.py --edition 1985  # the acknowledgment verbatim against PDF page 3
```

**The reconciliation is recorded in `LEDGER-1985.json`** (kb/Work PB4850): every claim of the batch passes with its
decision, every repair with the text it wrote, every sweep of a repair's siblings, and every level-2 box. The batch
passes compared all 976 pages, so a page with no claim was found to agree. `verify_publishable.py --edition 1985`
holds the transcription to the ledger (a gate audit and a CI step run it): a repair lost, a claim left undecided or a
box side the ledger does not account for is refused. A new repair adds its ledger entry in the same change.

**A misprint of the printed page is corrected and listed in the Errata** (owner 2026-11-08, kb/Work PB4972), the 2023
convention (specs/README.md "Corrections") under the name 1985 needs, because its text already has an Addendum of its
own (`verify_publishable.CORRECTIONS_SECTION`). A misspelling or a damaged letter is corrected in place, flagged
with a `> ⚠ **CORRECTED — see the Errata (Cn).**` note that quotes the printed form and names the PDF page (and the
folio), and listed under `# Errata` at the end of the file; `verify_publishable.py --edition 1985` holds the three
together. The ledger records how the transcription reads the page; the Errata records where it departs from the page.
The acknowledgment is never corrected (its slips are Errata `D` entries). A correction inside a general format changes
its figure verdict's `figure_lines` too, and the verdict becomes `neither` (sweep_figures.load_verdicts: a figure that
reads differently from the batch's drawing is neither drawing), with the Errata entry named in its reason (C4, page 217).
The 17 corrections of 2026-11-08 came from a scan for rare words one edit from a common word or carrying a letter no
English word has, each read on the rendered page; the scan finds a misprint that makes a non-word, not one that makes
another real word.

**A level-2 box in prose is a `║` at each end of each boxed block** (`spec_pdf.BOX_SIDE`; the 1985 standard boxes every
specification unique to level 2 of a module, §1985:I.1.3 and §1985:IV.1). One markup, used everywhere:

- a boxed heading, paragraph, rule or list item opens and closes with `║`, after any heading or list marker
  (`### ║2.7 THE PADDING CHARACTER CLAUSE║`, `║(4) Any necessary conversion …║`);
- boxed words inside a sentence are enclosed where they stand (`opened in the output ║or extend║ mode`), and a box
  opens before the underline or bold that opens its words (`║<u>FORMAT 2</u>:║`);
- a boxed table row opens in its first cell and closes in its last; a box never sits outside an HTML cell;
- a box holding several paragraphs marks each of them;
- a general format draws its boxes as columns (FIGURE-STYLE.md rule 9), and a drawn flowchart carries none.

Every reader of a 1985 heading reads it through the box side (`citation._MARK_85`, `sweep_figures.CLAUSE_LINE` and
`MD_HEADING`, `audit_grammar_optional_words.MD_HEADING`): a boxed heading once hid 30 of the 213 general formats from
the grammar audit and a clause from `cite.py`. A new reader of the headings does the same.
