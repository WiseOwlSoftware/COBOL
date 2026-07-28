# Transcription state — what is done, what is next

> Live state for `specs/ISO_COBOL.md` specifically. Plan §0 remains the SSOT for the project; this is the
> detail behind its spec-reconciliation bullets, kept here so a new session can resume without re-deriving it.

## Run these first

```
python scripts/spec/lint_rendering.py          # legibility — currently RED at 1, deliberately
python scripts/spec/sweep_figures.py --check   # the 484 general formats, regenerated and diffed
python scripts/spec/verify_publishable.py      # no licence data, acknowledgment in the preface
python scripts/spec/verify_acknowledgment.py   # verbatim against the PDF (needs specs-private/)
```

Last known: lint 1 · sweep clean 484/484 · publishable at 47,142 lines · acknowledgment verbatim · 3,811
internal links, zero dangling.

## ⛔ NEXT: Figure D.6, the last undrawn Annex D illustration

`lint_rendering.py` is red at exactly this and goes green when it lands. D.6 is an example PAGE LAYOUT —
rectangles and labels, not a flowchart — so it belongs in `repairs/annex_d_structure.py` beside D.3, not in
either flowchart generator. Its printed original is in Annex D; find it with the caption search below, and
note the standard prints `Figure  D.6` with TWO spaces, which defeats a naive `Figure D\.\d+` regex.

```python
# locating a printed figure
import fitz, re
for pno in range(doc.page_count):
    if re.search(r"Figure\s+D\.6\s*—", doc[pno].get_text()): print(pno + 1)
```
Captions sit ABOVE their figure, and a figure may flow onto the NEXT page — p1185 carries D.11's caption and
chart plus D.12's caption, with D.12's chart on p1186. Render with `python scripts/render-spec-page.py <page>`
and look at it; do not infer the shape from the text layer.

## The Annex D figures — 8 of 9 drawn

| figure | state | generator |
|---|---|---|
| D.1 SEARCH decision chart | drawn | `annex_d_flowcharts.py` |
| D.2 compilation group sample | already fine | — |
| D.3 compilation group / run unit structure | drawn | `annex_d_structure.py` |
| D.4 manager class | already fine | — |
| D.5 banking hierarchy | Markdown TABLES — a fair representation of a class hierarchy, deliberately left | — |
| **D.6 example page layout** | **NOT DRAWN — the remaining work** | → `annex_d_structure.py` |
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

## Already closed, do not redo

Pages removed entirely (anchors, running headers, and the 758 leftover `---` rules) · every page reference
re-keyed to a clause link, none left bare · index, TOC and the tables/figures lists rebuilt as real nested
lists, with the index's sub-entry levels MEASURED off the printed page (`data/index-levels.json`) · 18
page-break table joints merged and Table 10's 24×24 matrix rebuilt from geometry · 137 literal `*` escaped ·
all 484 figures carrying `line-height:1` · `figure_extract.is_underlined` widened to catch underlines drawn
inside a word's descender space (changes no figure — verified).
