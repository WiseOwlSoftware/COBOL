## Entry 3930 — 2026-11-22 15:24 PST — 1985 code reads re-read after #1953 corrected six rules' text (slice 1985-c07)

#1953, the second accuracy pass of the 1985 transcription, corrected the text of six rules. Each inventory row's rule
digest changed, so their stored code reads went stale, and `edition_status.py --edition 1985` gained 6 code-read terms
at a012d50751. Slice 1985-c07 re-read all six from the corrected text at a012d50751. A cobol-adjudicator read them and a
cobol-refuter probed each with its own programs. All six came out CONFORMS and upheld: 1985-FMT-VI.2.4.2 (END PROGRAM),
1985-GR-VI.6.27.4-19 and -20 (END-UNSTRING scope, NOT ON OVERFLOW), 1985-GR-VIII.4.6.4-13 (relative REWRITE invalid key),
1985-GR-IX.4.6.4-16 (indexed REWRITE invalid key) and 1985-GR-X.5.2.4-4 (CALL resolution through COMMON programs).
They were imported with `code_reads.py import --replace`. `code_reads.py --check 1985` is GREEN and the code-read term
is 0. The landing-check gap that let a text change go stale silently is PB6090 (wave 20262138 group F).

