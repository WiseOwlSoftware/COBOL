# PB6135 - the 128 LIKELY-ERROR rows by class, and the verdict on the 59 candidate rows: no Markdown error

**Classes (2026-11-24, mechanical rules over `TEXT-LAYER-DIFF-1985.json`; script and rows at
`E:/COBOL-coord/scratch/w20262166/pb6135-classes/`).** TABLE-HEADER-OR-CELL 47 (the print repeats a table header per page, the
Markdown holds it once), FOOTNOTE-RENUMBER 22 (pp. 328-332, 428-429), LONG-RUN-REWORDED 15, SHORT-PHRASE-SUBST 14,
SINGLE-WORD-SUBST 14, CHAR-CONFUSION 8, LAYER-ONLY-RUN 6, SECTION-NUMBER-SHIFT 2; 128 in all.

**Verdict on the 59 candidate rows** (every row of the last five classes, plus the O/0 and digit CHAR-CONFUSION rows; each page
rendered from the PDF, one pass): AMENDMENT 45 (each sits under a CORRECTED note on its page whose Errata number matches the
change, e.g. C177 Preface, C202 the 4.3.8.3 to 4.3.8.4 renumbering, C246 PERFORM), LAYER-ERROR 9 (the text layer misreads the
print: p118, p152, p246, p262, p393, p784, p861), TABLE/FOOTNOTE 5, **MD-ERROR 0**. Row by row:
`2026-11-24-likely-error-verdicts-rows.md` beside this file.

**What it means for the tool.** LIKELY-ERROR, as classified today, found no transcription error in these 59 rows: 45 are Amendment
text the classifier will not accept without a word-for-word confirming entry (fail closed by design, PB6144). The 64 unread
TABLE-HEADER-OR-CELL and FOOTNOTE-RENUMBER rows are next, by a mechanical check per class (a header run repeated on the
previous page of the same table; a footnote marker whose text matches the print after renumbering), not a read.
