## PB5268: the 2014 standard transcribed from its text layer, and the first 2014 edge checked against it (PB5270)

kb/Work PB5268. `specs/ISO_COBOL_2014.md` is new: ISO/IEC 1989:2014 (INCITS/ISO/IEC 1989:2014 (R2019)), converted from the
PDF's own text layer with no model and no API, by the route PB4813 built for 2002. The PDF is `specs-private/ISO_COBOL_2014.pdf`;
the submodule pointer moves to 0727a70, the commit that added it.

**Measured.** 952 pages, every one written by `transcribe_pdf.py text --edition 2014`; 470 general formats on 319 pages, every one
drawn by the vector arm from the measured page; 39,256 lines. `verify_publishable.py --edition 2014` PUBLISHABLE,
`verify_acknowledgment.py --edition 2014` verbatim in the Preface and in position, `lint_rendering.py --edition 2014` LINT CLEAN,
`sweep_figures.py --edition 2014 --check` clean (464 figures located, 458 byte-identical to the drawn ones), and
`cite.py --check "§2014:14.9.23.2 SR2" "File-name-1 shall be described in a sort-merge file description entry"` OK.

**Read from the PDF, not guessed.** The reseller's stamp is one line on all 952 pages, of the 2002 line's shape; every shape of
the 2002 rows matches it on every page and no other line, so `verify_publishable` names one shape set for both editions. The
folio offset is 24 on all 926 arabic-numbered pages. Unlike 2002, 2014 prints the CODASYL grant and acknowledgment, on its INCITS
cover page (PDF page 2); the Preface reproduces the paragraphs as printed, `OI` for "or" included, after the rendered page showed
that is the printed text.

**A sibling fixed at its root.** The cover page's footer, `ii © ITIC 2014 – All rights reserved`, survived as text in 2014 and,
as `ii © ITIC 2003`, in the 2002 transcription: `text_layer.FOOTER` knew only ISO/IEC's line, and the boxed page puts the
footer above the foot band. Converting all 1,834 pages of both PDFs with the old and the new module changes exactly page 2 of
each; the 2002 file loses that one line.

**Not placed by the tools (reported in PB5268, nothing typed by hand).** Two clauses whose figure count the generator and the
text layer read differently (`§2014:11.2.1`, `§2014:13.16.1`), and 55 pages the text layer reports structure-ambiguous: the cover
image, 32 pages of strokes outside every table and format, 34 with unruled-table rows, 3 with a format glyph outside a General
format clause. The page-by-page reconciliation is still owed, as for 2002.

**A defect found by the new text.** PB5077 gated the FROM/INTO record-area rule (COBOL6827) at every edition before 2023,
because no 2014 text was held. `§2014:E.3` items 21, 24, 25, 26 and 38 say all five phrases are "no longer prohibited" at
2014, so `--std 2014` refuses legal source: filed as kb/Work PB5270 (open). The docs that said the repository holds no 2014 text
now name the file and say which derivations predate it.
