# PB6135 - UNCOVERED rows take the second reading too: 807 to 284 (2026-11-24)

**Finding.** `text_layer_diff.py` sent only UNEXPLAINED rows to the tesseract arbitration; an UNCOVERED row (Markdown words the
text layer lacks) was "unchecked by this tool", so 807 rows on main had no witness either way. A Markdown insertion error (a
word or run the print does not carry) lands in that class and would never have surfaced, and the layer's real drops (table
cells, a line of prose) waited for a reader although a second OCR reads them.

**Change.** `classify_page` hands an UNCOVERED row its differing block (`_src`), and `arbitrate_rows` weighs it with the
UNEXPLAINED rule, unchanged and positional: tesseract holding the Markdown's words at the place makes it OCR-ARBITRATED;
tesseract's neighbouring words touching without them (two readings without the words) makes it LIKELY-ERROR; both, neither, or
the words only elsewhere on the page (a table read by column) keep it UNCOVERED. Self-test case 20.

**Measured** (`text_layer_diff.py --arbitrate`, all 976 pages, the JSON committed with this change; tesseract 5.4.0):

| Class | Before | After |
|---|---|---|
| OCR-ARBITRATED | 1344 | 1757 |
| UNCOVERED | 807 | 284 |
| LIKELY-ERROR | 88 | 198 |
| UNEXPLAINED | 554 | 554 |

Rows a reader still opens: 1449 to 1036; pages carrying one: 445 to 336. Every other class is unchanged.

**The 110 new LIKELY-ERROR rows** are not trusted as errors: a first look (p7, p18-23, p130-169, p248, p323, p345, p406-500,
p563-572, p721-806) shows Amendment text the Markdown applies in place where the print page has the base text, the class the
59-row verdict found (`2026-11-24-likely-error-classes.md`: 45 of 59 AMENDMENT, 0 MD-ERROR). The rest are module-table rows
(p729-744, `1 NUC`, `1 RPW`) and the report-group tables (p610, p613, p620) that both readers read across columns. They join
the page review. The next mechanical class is the Amendment insertion whose note names the change but whose entry is among
the 93 not located.

**Also measured, not committed:** a page-bag sort of the 1361 UNEXPLAINED/UNCOVERED rows before this change (each row's words
counted on both sides of its page, then with tesseract) explained 635 as reading order. It is a bag, not a place, so it is no
class of the tool (`E:/COBOL-coord/scratch/w20262169/pb6135-residual/`).
