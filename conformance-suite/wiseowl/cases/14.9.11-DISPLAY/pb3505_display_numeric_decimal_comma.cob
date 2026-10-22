*>--- conformance-case
*> id: pb3505_display_numeric_decimal_comma
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [85, 2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR1 (Annex A.1 item 56; kb/Work PB3505, owner decision PB3467) under the default
      *> `--numeric-display decimal` and DECIMAL-POINT IS COMMA: the separator of the shown value is
      *> the PROGRAM'S, the comma, exactly as a numeric literal's is (§12.3.7.4 GR14 a: the comma is
      *> "the character written in numeric literals to represent the decimal separator"; DISPLAY of a
      *> literal shows it as written, kb/Work PB1643). Expected values from the declarations:
      *> -1,5 in S9(3)V99 is -001,50; 1,5 in 9(3)V99 COMP is 001,50; a V9(3) item holding ,5 shows
      *> ,500; an integer item has no separator.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3505C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DC PIC S9(3)V99 VALUE -1,5.
       01 DCU PIC 9(3)V99 COMP VALUE 1,5.
       01 DV PIC V9(3) VALUE ,5.
       01 DI PIC S9(3) VALUE -1.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DC=" DC "|"
           DISPLAY "DCU=" DCU "|"
           DISPLAY "DV=" DV "|"
           DISPLAY "DI=" DI "|"
           DISPLAY "LIT=" 12,50 "|"
           STOP RUN.
