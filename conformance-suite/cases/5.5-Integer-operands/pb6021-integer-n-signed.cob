*>--- conformance-case
*> id: pb6021-integer-n-signed
*> rules: [SR-5.5-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §2023:5.5 SR1: an integer-n "shall be unsigned and nonzero unless otherwise specified
      *> in the associated rules" (§2002:5.4 item 1 and §2014:5.5 item 1 the same). No associated rule of any
      *> format otherwise-specifies the sign, so a signed integer-n is refused, once, by the one integer-n screen,
      *> COBOL7737 (kb/Work PB6021), at a position whose rule permits ZERO too (the OCCURS lower bound,
      *> §2023:13.18.38.3 SR16): a permission of zero is none of a sign. It used to be the parse error COBOL0001
      *> with COBOL1941 naming the entry's closed clause list.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6021N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 1.
       01  T.
           05 E               PIC X OCCURS -0 TO 3 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHED"
           STOP RUN.