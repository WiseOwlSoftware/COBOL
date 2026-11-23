## NEXT SENTENCE draws the archaic warning from 2002 (PB5723)

ISO/IEC 1989:2002 G.1 item 3 lists the NEXT SENTENCE phrase of IF and SEARCH as archaic, and 3.1.10 requires an optional
compile-time warning for archaic elements. The 2014 Annex F.1 item 3 and 2023 F.1 item 2 repeat the designation. The construct
row `next-sentence-archaic-2023` started its edge at 2023, so `--std 2002` and `--std 2014` compiled the phrase with no
COBOL0903.

Re-probe on this build before the fix: `--std 2002` printed DONE with no diagnostic; after it, both phrases of the note's
program (IF and SEARCH) draw COBOL0903 at 2002, 2014 and 2023, and `--std 85` stays silent (the 1985 text has no archaic
designation; that program is refused at 85 for an unrelated 2002-only sentence form).

Fix: the row is renamed `next-sentence-archaic-2002` with `obsoleteIn: 2002` and its citations rewritten
(`tests/version-matrix/constructs/next-sentence-archaic-2002.json`); `gen-constructs.ps1` regenerated the registry and the
`Constructs.NextSentenceArchaic2002` id; the one caller in `VersionConformancePass.VisitNextSentenceStatement`, the
construct catalogue, the version-change reference and four 1985 code-read records follow the rename. Goldens: two accepted-at-2002
cases (`pb5723_if_next_sentence_archaic`, `pb5723_search_next_sentence_archaic`) with a `warn-at: [2002]` COBOL0903 file each.
Rows `2002-FMT-14.8.18.1` and `2002-FMT-14.8.34.1` move GAP to CONFORMS (GAP 1147 to 1145).

Sibling sweep: EXIT PROGRAM (`exit-program-archaic-2023`) is the same shape; 2002 G.1 does not list it (item 1 is word
continuation), so its 2023 start is right. The other 2002 G.1 items (word continuation, alphanumeric figurative MOVE to numeric,
CALL ON OVERFLOW, COPY text-n) are separate mechanisms left to their own notes.
