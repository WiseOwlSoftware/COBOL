      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_2-p1
      *> rules: [1985-GR-VI.6.3.1.2-p1, 1985-GR-VI.6.3.1.2-p4, 1985-GR-VI.6.3.1.2-p5, 1985-SR-VI.6.3.1.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.2 p1; §1985:VI.6.3.1.2 p4; §1985:VI.6.3.1.2 p5;
      *> §1985:VI.6.3.1.2 p3 (Simple Conditions). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-10.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10CLS.
      * 1985 VI.6.3.1.2: class condition results, NOT, signs,
      * zero-length group item.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXD IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X4       PIC X(4).
       01  U3       PIC 9(3).
       01  U3X REDEFINES U3 PIC X(3).
       01  S3       PIC S9(3).
       01  S3X REDEFINES S3 PIC X(3).
       01  SS       PIC S9(3) SIGN LEADING SEPARATE.
       01  SSX REDEFINES SS PIC X(4).
       01  ST       PIC S9(3) SIGN TRAILING SEPARATE.
       01  STX REDEFINES ST PIC X(4).
       01  AL       PIC A(4).
       01  N        PIC 9 VALUE 0.
       01  ZG.
           05  ZE   PIC X OCCURS 0 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       P0.
           MOVE "1234" TO X4.
           IF X4 IS NUMERIC DISPLAY "C1 T" ELSE DISPLAY "C1 F".
           MOVE "12A4" TO X4.
           IF X4 IS NUMERIC DISPLAY "C2 T" ELSE DISPLAY "C2 F".
           IF X4 IS NOT NUMERIC DISPLAY "C3 T" ELSE DISPLAY "C3 F".
           MOVE 123 TO U3.
           IF U3 IS NUMERIC DISPLAY "C4 T" ELSE DISPLAY "C4 F".
           MOVE -123 TO S3.
           IF S3 IS NUMERIC DISPLAY "C5 T" ELSE DISPLAY "C5 F".
           MOVE S3X TO U3X.
           IF U3 IS NUMERIC DISPLAY "C6 T" ELSE DISPLAY "C6 F".
           MOVE "+123" TO SSX.
           IF SS IS NUMERIC DISPLAY "C7 T" ELSE DISPLAY "C7 F".
           MOVE "-123" TO SSX.
           IF SS IS NUMERIC DISPLAY "C8 T" ELSE DISPLAY "C8 F".
           MOVE " 123" TO SSX.
           IF SS IS NUMERIC DISPLAY "C9 T" ELSE DISPLAY "C9 F".
           MOVE "123*" TO STX.
           IF ST IS NUMERIC DISPLAY "CA T" ELSE DISPLAY "CA F".
           MOVE "123-" TO STX.
           IF ST IS NUMERIC DISPLAY "CB T" ELSE DISPLAY "CB F".
           MOVE "Ab z" TO AL.
           IF AL IS ALPHABETIC DISPLAY "A1 T" ELSE DISPLAY "A1 F".
           MOVE "Ab1z" TO X4.
           IF X4 IS ALPHABETIC DISPLAY "A2 T" ELSE DISPLAY "A2 F".
           MOVE "ab z" TO X4.
           IF X4 IS ALPHABETIC-LOWER DISPLAY "L1 T"
               ELSE DISPLAY "L1 F".
           MOVE "aB z" TO X4.
           IF X4 IS ALPHABETIC-LOWER DISPLAY "L2 T"
               ELSE DISPLAY "L2 F".
           MOVE "AB Z" TO X4.
           IF X4 IS ALPHABETIC-UPPER DISPLAY "U1 T"
               ELSE DISPLAY "U1 F".
           MOVE "AbZ " TO X4.
           IF X4 IS ALPHABETIC-UPPER DISPLAY "U2 T"
               ELSE DISPLAY "U2 F".
           IF X4 IS NOT ALPHABETIC-UPPER DISPLAY "U3 T"
               ELSE DISPLAY "U3 F".
           MOVE "09AF" TO X4.
           IF X4 IS HEXD DISPLAY "H1 T" ELSE DISPLAY "H1 F".
           MOVE "09AG" TO X4.
           IF X4 IS HEXD DISPLAY "H2 T" ELSE DISPLAY "H2 F".
           IF X4 IS NOT HEXD DISPLAY "H3 T" ELSE DISPLAY "H3 F".
           MOVE 0 TO N.
           IF ZG IS NUMERIC DISPLAY "Z1 T" ELSE DISPLAY "Z1 F".
           IF ZG IS ALPHABETIC DISPLAY "Z2 T" ELSE DISPLAY "Z2 F".
           IF ZG IS NOT NUMERIC DISPLAY "Z3 T" ELSE DISPLAY "Z3 F".
           IF ZG IS NOT ALPHABETIC DISPLAY "Z4 T" ELSE DISPLAY "Z4 F".
           IF ZG IS HEXD DISPLAY "Z5 T" ELSE DISPLAY "Z5 F".
           IF ZG IS NOT HEXD DISPLAY "Z6 T" ELSE DISPLAY "Z6 F".
           STOP RUN.
