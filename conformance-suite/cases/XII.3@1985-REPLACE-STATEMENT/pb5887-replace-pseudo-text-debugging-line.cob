      *>--- conformance-case
      *> id: pb5887-replace-pseudo-text-debugging-line
      *> rules: [1985-GR-XII.3.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5887. "Debugging lines are permitted in pseudo-text."
      *> (§1985:XII.3.4 GR8), and "After all COPY and REPLACE statements
      *> have been processed, a debugging line will be considered to have
      *> all the characteristics of a comment line, if the WITH DEBUGGING
      *> MODE clause is not specified" (§1985:VI.7.3). §1985:XII.3.4 GR11
      *> gives "the same character as the line on which the text being
      *> replaced begins" to the lines a replacement introduces, the
      *> sentence the COPY statement's GR11 also carries beside its GR9's
      *> "only those text words that are specified on debugging lines
      *> where the debugging line is within pseudo-text-2 appear on
      *> debugging lines in the resultant program": a debugging line of
      *> pseudo-text-2 stays one. STMTS and ONLYD are replaced on source
      *> lines; the program has no WITH DEBUGGING MODE, so every debugging
      *> line of their pseudo-text-2 is a comment: S1, S2 and S3 are
      *> displayed, D1, D2 and D3 are not, and neither ADD runs (N=1).
      *> ONLYD's debugging line also ends the pseudo-text on its own line.
      *> Why it can fail: a REPLACE that gives a pseudo-text-2 debugging
      *> line the replaced line's indicator displays D1, D2, D3 and N=4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5887R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           REPLACE ==STMTS== BY ==DISPLAY "S1"
      D    DISPLAY "D1"
           DISPLAY "S2"
      D    ADD 1 TO WS-N
           DISPLAY "S3"==
                   ==ONLYD== BY ==
      D    DISPLAY "D2" DISPLAY "D3"
      D    ADD 2 TO WS-N ==.
       M-1.
           STMTS
           ONLYD
           DISPLAY "N=" WS-N.
           STOP RUN.
