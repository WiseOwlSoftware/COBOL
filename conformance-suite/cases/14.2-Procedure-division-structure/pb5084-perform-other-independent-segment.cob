      *>--- conformance-case
      *> id: pb5084-perform-other-independent-segment
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.3.2: a PERFORM in an independent
      *>   segment has within its range "Sections and/or paragraphs wholly
      *>   contained in the same independent segment as that PERFORM
      *>   statement" or non-independent ones. The PERFORM in segment 50
      *>   names a section of segment 60. Expected: COBOL6877 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084PO.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           GO TO P-50.
       S-50 SECTION 50.
       P-50.
           PERFORM S-60.
           STOP RUN.
       S-60 SECTION 60.
       P-60.
           DISPLAY "60".
