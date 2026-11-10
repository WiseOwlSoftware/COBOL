      *>--- conformance-case
      *> id: pb5084-sort-input-spans-segments
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.3.4: "If a SORT statement appears in a
      *>   section that is not an independent segment, then any input
      *>   procedures or output procedures referenced by that SORT
      *>   statement must appear" totally within non-independent segments
      *>   or "Wholly contained in a single independent segment." INPUT
      *>   PROCEDURE S-60 THRU S-70 spans segments 60 and 70. Expected:
      *>   COBOL6877 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084SI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "PB5084WK".
           SELECT IN-FILE ASSIGN TO "PB5084IN".
           SELECT OUT-FILE ASSIGN TO "PB5084OT".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC PIC X(5).
       FD  IN-FILE.
       01  IN-REC PIC X(5).
       FD  OUT-FILE.
       01  OUT-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           SORT SORT-FILE ON ASCENDING KEY SORT-REC
               INPUT PROCEDURE IS S-60 THRU S-70 GIVING OUT-FILE.
           STOP RUN.
       S-60 SECTION 60.
       P-60.
           MOVE "A" TO SORT-REC.
           RELEASE SORT-REC.
       S-70 SECTION 70.
       P-70.
           MOVE "B" TO SORT-REC.
           RELEASE SORT-REC.
