      *>--- conformance-case
      *> id: pb5084-perform-two-independent-segments
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.3.2: a PERFORM in a section that is
      *>   not in an independent segment has within its range only
      *>   "Sections and/or paragraphs wholly contained in a single
      *>   independent segment" or non-independent ones. PERFORM S-60 THRU
      *>   S-70 spans segments 60 and 70. Expected: COBOL6877 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084P2.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM S-60 THRU S-70.
           STOP RUN.
       S-60 SECTION 60.
       P-60.
           DISPLAY "60".
       S-70 SECTION 70.
       P-70.
           DISPLAY "70".
