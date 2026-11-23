# The text-layer diff is built; three class checks are not gated (2026-11-23)

**Built.** `scripts/spec/text_layer_diff.py` compares the word sequence of each PDF page's text layer (pymupdf) with that
page's slice of `specs/ANSI_COBOL_1985.md` and writes `docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json`
(committed; `--check` reads it without the licensed PDF). Cases: `scripts/selftests/spec/text_layer_diff/`. Measured over
all 976 pages: OCR-NOISE 2272, CORRECTION 647, FIGURE 818, UNCOVERED 667, UNEXPLAINED 128 on 87 pages.

`UNCOVERED` is a class beyond the four the note named: Markdown words with no text-layer counterpart (the layer omits a
table, a figure's labels or a block of prose; the Markdown may also add them). It is not explained, only unchecked by this
tool, and it is reported apart so that "0 unexplained" cannot be read as "all verified". The two pages seen whole are 674
(error-key table) and 721 (history prose).

**The 128 unexplained** are listed per page in the JSON (`differences`, class `UNEXPLAINED`). The sampled ones are OCR
(`6.2.2` read `8.2.2`, checked on the rendered page 284: the Markdown is right) or amended text applied in place without a
quotable note (p7 preface terms, p323 `explicitly terminated`, p459/p501 `associated with the file connector referenced by`).
None was changed in this branch. PB6051 and PB6135 stay open: the first condition (0 unexplained) does not hold.

**Class checks, not built (no precise, ungated-noise-free rule):**
- (a) Summary-table indentation: the text layer's x0 for a level-0 row ranges 57 to 80 across the table pages (scan skew and
  margin), and one level is about 6 pt with a jitter of 3 pt, so neither an absolute nor a per-page relative level is
  reliable; a lint on it would fire at random. Probe: rows matched by their first two words, x0 counted per `&nbsp;` depth.
- (b) Brace/bracket continuation column: no text-layer words for the braces and brackets (the layer is OCR of the glyphs, and
  the figures' columns are re-drawn in `<pre>`), so the alternatives' column has no measured source.
- (c) A boxed figure that ends its page: the class is named only in the fifth-pass note, which this tree does not hold, so
  the precise defect (a box left open at the page end, or a closing row on the next page) is not defined enough to gate.

They stay reported here, not gated.
