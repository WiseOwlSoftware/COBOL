      *>--- conformance-case
      *> id: pb5084-merge-output-other-independent-segment
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.3.3: "If a MERGE statement appears in
      *>   an independent segment, then any output procedure referenced by
      *>   that MERGE statement must be contained" totally within
      *>   non-independent segments or "Wholly within the same independent
      *>   segment as that MERGE statement." The MERGE in segment 60 names
      *>   an output procedure of segment 70. Expected: COBOL6877 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084MO.
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
           GO TO P-60.
       S-60 SECTION 60.
       P-60.
           MERGE SORT-FILE ON ASCENDING KEY SORT-REC
               USING IN-FILE OUT-FILE OUTPUT PROCEDURE IS S-70.
           STOP RUN.
       S-70 SECTION 70.
       P-70.
           RETURN SORT-FILE AT END GO TO P-70-X.
           GO TO P-70.
       P-70-X.
           EXIT.
