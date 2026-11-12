      *>--- conformance-case
      *> id: pb415_initialize_multi_category_85
      *> rules: [FMT-14.9.20.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB415, re-verdicted by kb/Work PB5180: the edition-gate
      *> witness of ISO §2023:14.9.20.2's category-name SET. That set is
      *> the CHOICE INDICATORS of the format (§2023:5.2.6.4), and they
      *> are COBOL-2002's: §1985:VI.6.17.2 prints the five category words
      *> of a REPLACING item in plain braces, and §1985:IV.2.1.4 says
      *> "When braces, { } , enclose a portion of a general format, one
      *> of the options contained within the braces must be either
      *> explicitly specified or implicitly selected." So at 85 each
      *> REPLACING item names ONE category, and the two-category items
      *> on lines 32 and 34 are refused (COBOL0900, construct row
      *> initialize-replacing-category-set-2002). This file had pinned
      *> the opposite reading ("conforming COBOL-85 source"); the 1985
      *> text overturns it. The 2002+ behaviour is witnessed by
      *> pb415_initialize_category_set.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415MC85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 AB PIC A(3) VALUE "abc".
          05 AN PIC X(3) VALUE "xyz".
          05 NU PIC 9(3) VALUE 123.
          05 NE PIC ZZZ9 VALUE "  42".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING ALPHABETIC ALPHANUMERIC DATA BY "Q".
           DISPLAY "T1=[" AB "][" AN "][" NU "][" NE "]".
           INITIALIZE G REPLACING NUMERIC NUMERIC-EDITED DATA BY 7.
           DISPLAY "T2=[" AB "][" AN "][" NU "][" NE "]".
           INITIALIZE G.
           DISPLAY "T3=[" AB "][" AN "][" NU "][" NE "]".
           STOP RUN.
