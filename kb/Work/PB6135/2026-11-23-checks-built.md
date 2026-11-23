# The text-layer diff is built; three class checks are not gated (2026-11-23)

**Built.** `scripts/spec/text_layer_diff.py` compares the word sequence of each PDF page's text layer (pymupdf) with that
page's slice of `specs/ANSI_COBOL_1985.md` and writes `docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json`
(committed). Cases: `scripts/selftests/spec/text_layer_diff/`. Gate audit TEXT LAYER DIFF 1985 (`--check`, no PDF): RED when
the Markdown, the ledger or the script changed since the JSON was written (their SHA-256 are stored in it), naming the
re-run command. It prints what is unexplained and does not fail on it; `--require-zero` is PB6051's closing condition.

**Round-1 review (R1-1 to R1-5) fixed.** The first version folded OCR confusions out of every word and so hid real errors
(`identifier-i`, `file-name-l`, `comer`, `date-name-1`, `B-1`/`B-2`, `of`/`or`). Now: words align on letters and digits only,
nothing folded; OCR-NOISE needs the PDF word to be the damaged side and the Markdown word the well-formed one (known: a
reserved word, or spelled twice in the Markdown, or a digit suffix on a stem spelled twice; not malformed such as a
hyphenated word ending in a lone `l`/`i`/`o`/`s`/`z`), the PDF word absent from the Markdown, and the two a confusion apart.
CORRECTION needs one ledger `was`/`now` pair or note quote of that page to hold this hunk (PDF words in `was`, Markdown
words in `now`, with a neighbouring word unless the hunk is three words or the whole entry). Punctuation and case are classes
PUNCT and CASE. Words cancel only as a run of two or more moved together; a dropped `not` against a gained one stays two
differences. The verdict line names unexplained, uncovered and punctuation/case.

**Measured over all 976 pages (head of this change):** OCR-NOISE 5269, CORRECTION 823, FIGURE 1186, PUNCT 2322, CASE 423,
UNCOVERED 823, UNEXPLAINED 2092 on 528 pages (was 2272, 647, 818, 667 and 128 on 87 pages before the review; the Markdown
and ledger also moved with main). The rise is the point: the 128 hid the other 1964 behind folds. Of the 2092, by reason:
1040 a different word, 539 a single word moved, 513 text-layer words the Markdown lacks. Sampled, most are the layer's own
damage beyond the tolerated confusions (`meferenced`, `normumeric`, `Commun .cation`, a lone `I` speck, running-foot labels
read as `X1-19`), a few are real candidates; none is called OCR by this tool, so each page goes to a scoped agent, never to
a whole pass. The Markdown is unchanged in this branch. PB6051 and PB6135 stay open.

**Fold-classes still tolerated, and why.** (1) l/1/I/|, O/0, rn/m, Z/2, S/5, ligatures, case-free letters of a PDF word that
is absent from the Markdown against a well-formed Markdown word: the layer's systematic misreadings, tolerated only on the
PDF side. (2) One letter lost, one confusable letter or glyph swapped, one stray mark added, in a word of four letters or more
with no digit, same conditions: single-glyph damage. (3) A stray mark (a glyph of no letter, or a short fragment with no
digit) that the Markdown does not spell: scan specks. (4) A split or join by the layer's spacing or a line-end hyphen:
`FILE - CONTROL`/`FILE-CONTROL` (a hyphen against a space is therefore not seen). (5) A run of two or more words the layer
reads in another place (columns). (6) Tokens of no letter or digit (a lone `-`, a box glyph) are dropped from both sides. Not
tolerated, and visible: any difference of digits, punctuation, case, or a word the document also spells.

**Second reading (`--arbitrate`, tesseract).** Tesseract 5 reads each page with an UNEXPLAINED row (300 dpi render,
`--psm 4`; the per-page text is cached outside the tree, `--ocr-cache`, and only the result is committed; the version is
stored in the JSON). A row is OCR-ARBITRATED when the second reading holds the Markdown hunk's words in order beside the
neighbouring word and does not hold the layer's, and LIKELY-ERROR when it holds the layer's and not the Markdown's (two
independent readings against the transcription); both or neither stays UNEXPLAINED. The match is positional, never a bag of
the page's words. Two reading-order cases are never a witness for the layer: a moved single word, and Markdown words the
layer lacks that the second reading has elsewhere on the page (a table read by column); they stay UNEXPLAINED. The planted
cases for every outcome are case_10. `--require-zero` counts UNEXPLAINED and LIKELY-ERROR.

**Measured after arbitration (all 976 pages, the tool as of 2026-11-23's second review):** OCR-NOISE 5258, OCR-ARBITRATED 1343,
CORRECTION 823, AMENDMENT-APPLIED 88, FIGURE 1186, PUNCT 2322, CASE 423, UNCOVERED 773, LIKELY-ERROR 114, UNEXPLAINED 599
(from 2092; the residual is on 244 pages). The LIKELY-ERROR rows are differences between the Markdown and the print that two
readings agree on: some are errors, some are Amendment text applied in place that no cited Amendment page confirms word for
word, some the transcription's own footnote renumbering, and the repeated table header that the Markdown holds once. Each is a
page for one scoped agent, never a pass. AMENDMENT-APPLIED needs the page's CORRECTED note to hold the print and the cited
Amendment page (or the note's quote) to hold the Markdown's words (kb/Work PB6135 2026-11-23-errata-amendment-class).

**Every LIKELY-ERROR** is in the committed result; list it with
`python scripts/spec/text_layer_diff.py --check --likely` (page, line of the Markdown: layer / Markdown / tesseract). The
list is not copied here: it would go stale at the next re-run and would carry the standard's text.

**Class checks, not built (no precise, ungated-noise-free rule):**
- (a) Summary-table indentation: the text layer's x0 for a level-0 row ranges 57 to 80 across the table pages (scan skew and
  margin), and one level is about 6 pt with a jitter of 3 pt, so neither an absolute nor a per-page relative level is
  reliable; a lint on it would fire at random. Probe: rows matched by their first two words, x0 counted per `&nbsp;` depth.
- (b) Brace/bracket continuation column: no text-layer words for the braces and brackets (the layer is OCR of the glyphs, and
  the figures' columns are re-drawn in `<pre>`), so the alternatives' column has no measured source.
- (c) A boxed figure that ends its page: the class is named only in the fifth-pass note, which this tree does not hold, so
  the precise defect (a box left open at the page end, or a closing row on the next page) is not defined enough to gate.

They stay reported here, not gated.
