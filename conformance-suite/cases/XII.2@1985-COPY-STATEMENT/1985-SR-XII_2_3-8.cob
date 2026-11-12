      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-8
      *> rules: [1985-SR-XII.2.3-8]
      *> accept: [85]
      *> files: [LONGLIT.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR8 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SR8TEST.
      * LIBRARY TEXT LONGLIT (SEPARATE LIBRARY FILE) IS ITS LINES BELOW:
      * 01 LONG-REC. / 05 LONG-LIT PIC X(160) VALUE / 160-CHAR LITERAL
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           COPY LONGLIT.
       PROCEDURE DIVISION.
       P1.
           DISPLAY LONG-LIT.
           STOP RUN.
      *---- CONTENTS OF LIBRARY TEXT LONGLIT ----
      *01 LONG-REC.
      *    05 LONG-LIT PIC X(160) VALUE
      *    "012345678901234567890123456789012345678901234567890123456789
      *-   "012345678901234567890123456789012345678901234567890123456789
      *-   "0123456789012345678901234567890123456789".
      * (IN THE LIBRARY FILE THESE LINES HAVE A SPACE IN COLUMN 7, THE
      * CONTINUATION LINES A HYPHEN IN COLUMN 7, AND EACH OPENING QUOTE
      * IN COLUMN 12; THE 162-CHARACTER LITERAL IS ONE TEXT WORD)
