# The COBOL standard, as a searchable transcription

`ISO_COBOL.md` is a transcription of **ISO/IEC 1989:2023(E)** — *Information technology — Programming languages,
their environments and system software interfaces — Programming language COBOL* (third edition, 2023-01).
© ISO/IEC 2023.

It is reproduced under the permission in the standard's own Introduction (page 28), which grants reproduction
"in whole or in part … for any other purpose" and asks that the acknowledgment paragraphs accompany it. **They
are carried in the file's Preface**, which is the first thing in the document.

**The standard is authoritative.** Where this transcription and ISO/IEC 1989:2023 differ, the published standard
governs. ISO sells the original; this is a working transcription, not a substitute for it.

## What is here, and what is not

| | |
|---|---|
| `ISO_COBOL.md` | the transcription — 1,261 pages, every clause, general format and rule |
| the source PDF | **not here.** It is licensed per copy and stays in a private submodule (`specs-private/`) |

Tools that MEASURE the printed page — underlining, bracket and choice-indicator geometry — need the PDF and will
say so if it is absent. Everything that works on the transcription alone runs without it.

## Corrections

The transcription is faithful to the printed standard except at three points where the standard itself is
defective and the defect would mislead a reader, or a tool built from the text — for example the reserved-word
list on page 236 prints `EMD-START`, which is not a COBOL word, where the rest of the standard says `END-START`.

**Every departure is listed in the Addendum at the end of the document, together with the printed form, so that
any correction can be reversed** if it later proves mistaken. Each is also flagged in place. Defects that are
doubtful rather than clear are transcribed AS PRINTED and listed in the Addendum too.

`scripts/spec/verify_publishable.py` enforces this: it fails if a correction is flagged in the text but missing
from the Addendum, or listed in the Addendum but referenced nowhere.

## Provenance

The transcription was produced by OCR of page images and has since been checked against the PDF by measurement
rather than by eye — underlines and delimiters are vector rectangles in the source, so a word's
required-or-optional status follows from geometry, not judgement. Whole-standard sweeps to date: choice
indicators 30/30 correct; underlining 0 defects across 2,215 measured tokens on 694 pages; figure words 1
discrepancy across 15,625 tokens on 820 pages, and that one is the standard's own typo (Addendum C3).
