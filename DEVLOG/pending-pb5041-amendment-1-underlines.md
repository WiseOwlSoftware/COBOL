## PB5041: Amendment 1's underlines measured on every page

Pages 842-906 of the 1985 transcription had been checked for underlined heads on 20 samples. `figure_raster.py` gained a prose-page detector (`underlined_hints`, CLI `--underlined`) that judges each OCR word by the rows below its baseline: a printed rule inks the whole word width for three pixel rows (1.00), the feet of bold Times at most 0.94, so threshold 0.96 separates them; words under 14 pt are not judged and the 8 pt Courier face uses 0.85. A planted self-test case pins it.

Run over all 65 pages and compared with the `<u>` marks: the Amendment's headings are bold, not underlined, and the general formats agree. Eight table heads the page underlines were bare in the transcription: `Argument Type | Function Type` (pages 882, 886, 895, 901) and `Character Positions | Contents` (869, 870, 905, 906). Repaired, with ledger sweep `table-head-underlines(PB5041)` x8. Page 861/862 table borders read as rules and are not underlines. Known limit: a word with a displaced OCR box is missed (905 `Contents`). Closes no inventory row.

The two 1985 store records whose rule text the new marks changed (1989-RV-A.2.11.3-1, 1989-RV-A.2.46.3-1) were re-recorded with record_verdicts (same change record, new digest); no GAP moved.
