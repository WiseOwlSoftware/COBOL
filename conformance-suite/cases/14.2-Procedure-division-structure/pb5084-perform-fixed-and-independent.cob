      *>--- conformance-case
      *> id: pb5084-perform-fixed-and-independent
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.3.2 (1) and (2): the range holds
      *>   either non-independent sections or one independent segment, not
      *>   both. PERFORM S-20 THRU S-60 in segment 10 spans segments 20
      *>   and 60. Expected: COBOL6877 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084PF.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 10.
       M-1.
           PERFORM S-20 THRU S-60.
           STOP RUN.
       S-20 SECTION 20.
       P-20.
           DISPLAY "20".
       S-60 SECTION 60.
       P-60.
           DISPLAY "60".
