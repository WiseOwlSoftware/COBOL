      *>--- conformance-case
      *> id: pb5084-alter-from-another-segment
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.3.1: "A GO TO statement in a section
      *>   whose segment-number is greater than or equal to 50 must not be
      *>   referred to by an ALTER statement in a section with a different
      *>   segment-number." The ALTER in segment 0 names SW, a GO TO of
      *>   independent segment 60. Expected: COBOL6877 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084AL.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           ALTER SW TO PROCEED TO P-2.
           GO TO SW.
       IND-SEC SECTION 60.
       SW.
           GO TO P-1.
       P-1.
           STOP RUN.
       P-2.
           STOP RUN.
