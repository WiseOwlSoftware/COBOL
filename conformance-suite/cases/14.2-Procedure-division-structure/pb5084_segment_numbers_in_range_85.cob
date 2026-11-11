      *>--- conformance-case
      *> id: pb5084_segment_numbers_in_range_85
      *> rules: [FMT-14.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - the X3.23-1985 Segmentation module ranges:
      *>   SEGMENT-LIMIT 1 through 49 (XVI.2.3.3 SR1), a section 0
      *>   through 99 (XVI.3.2.3 SR1), a declarative section less than
      *>   50 (SR3). Each bound is written at its limit. Expected: "OK".
      *>   It fails if a number at its limit is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084SR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS 49.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION 49.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       ERR-P.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION 99.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
