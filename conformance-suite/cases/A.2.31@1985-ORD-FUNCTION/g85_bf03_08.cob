      *>--- conformance-case
      *> id: g85_bf03_08
      *> rules: [1989-FMT-A.2.31.2, 1989-RV-A.2.31.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.31.2 - the format FUNCTION ORD ( argument-1 ), with a
      *> literal, an alphanumeric and an alphabetic argument
      *> (§1989:A.2.31.3 AR1). §1989:A.2.31.4 RV1 - the returned value
      *> is the ordinal position of argument-1 in the collating sequence
      *> for the program. AL puts "C" "B" "A" at positions 1 2 3; the
      *> other native characters follow in native order (§1985:VI.4.5.4
      *> GR4), so "D" is position 3 + 65 + 1 = 69 (65 native
      *> characters precede "A"). An ORD that used the native order
      *> prints 68 67 66 69 instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0308.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISEOWL
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "C" "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XB PIC X VALUE "B".
       01  AA PIC A VALUE "A".
       01  XD PIC X VALUE "D".
       01  N5 PIC 9(5).
       PROCEDURE DIVISION.
       P1.
           COMPUTE N5 = FUNCTION ORD ( "C" ).
           DISPLAY "ORD-C=" N5.
           COMPUTE N5 = FUNCTION ORD(XB).
           DISPLAY "ORD-B=" N5.
           COMPUTE N5 = FUNCTION ORD(AA).
           DISPLAY "ORD-A=" N5.
           COMPUTE N5 = FUNCTION ORD(XD).
           DISPLAY "ORD-D=" N5.
           STOP RUN.
