*>--- conformance-case
*> id: pb4763-class-condition-paren-counter-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4763: a class condition prints identifier-1 alone (ISO 8.8.4.4.2, general format "identifier-1 IS [ NOT ]");
      *> (LINAGE-COUNTER) is an arithmetic expression enclosed in parentheses (8.8.1.1), while LINAGE-COUNTER written sole is
      *> a data item (8.4.3.14.4 GR1). The expression binder drops the parentheses, so the screen that asked the TREE's shape
      *> admitted it. COBOL3318 at every edition.
      *>   cite.py --check 8.8.4.4.2 "identifier-1" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4763B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "F.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 20 LINES.
       01 FR PIC X(10).
       WORKING-STORAGE SECTION.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           IF (LINAGE-COUNTER) IS NUMERIC
               MOVE 1 TO R
           END-IF
           DISPLAY R
           STOP RUN.
