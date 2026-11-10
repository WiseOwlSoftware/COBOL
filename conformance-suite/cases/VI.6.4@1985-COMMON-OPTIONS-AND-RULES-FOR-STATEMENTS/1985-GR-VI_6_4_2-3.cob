      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_2-3
      *> rules: [1985-GR-VI.6.4.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.2 GR3 (COMMON OPTIONS AND RULES FOR STATEMENTS).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SZERR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 99 VALUE 90.
       01 E PIC 99 VALUE 10.
       01 B PIC 99 USAGE BINARY VALUE 50.
       01 C PIC 99 VALUE 99.
       01 D PIC 99.
       01 G1.
           05 X PIC 99 VALUE 60.
           05 Y PIC 99 VALUE 10.
       01 G2.
           05 X PIC 99 VALUE 50.
           05 Y PIC 99 VALUE 20.
       PROCEDURE DIVISION.
       P1.
           ADD 20 TO A E
               ON SIZE ERROR DISPLAY "SIZE ERROR 1"
               NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR 1"
           END-ADD.
           DISPLAY A.
           DISPLAY E.
           ADD 60 TO B
               ON SIZE ERROR DISPLAY "SIZE ERROR 2"
           END-ADD.
           MOVE B TO D.
           DISPLAY D.
           ADD 0.6 TO C ROUNDED
               ON SIZE ERROR DISPLAY "SIZE ERROR 3"
           END-ADD.
           DISPLAY C.
           ADD 1 TO D
               ON SIZE ERROR DISPLAY "SIZE ERROR 4"
               NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR 4"
           END-ADD.
           DISPLAY D.
           ADD CORRESPONDING G1 TO G2
               ON SIZE ERROR
                   DISPLAY "SIZE ERROR 5 " X OF G2 " " Y OF G2
           END-ADD.
           STOP RUN.
