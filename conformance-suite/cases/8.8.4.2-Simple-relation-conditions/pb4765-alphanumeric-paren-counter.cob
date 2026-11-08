*>--- conformance-case
*> id: pb4765-alphanumeric-paren-counter
*> rules: [GR-8.8.4.2.1-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4765: 8.8.4.2.5 admits a numeric operand opposite an alphanumeric one only as "an integer literal or an
      *> integer numeric data item of usage display or national". (LINAGE-COUNTER) is an arithmetic expression enclosed in
      *> parentheses (8.8.1.1), so it is neither, while LINAGE-COUNTER written sole is the usage-display integer item.
      *>   cite.py --check 8.8.4.2.5 "The numeric integer operand shall be" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4765C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "n4765c.tmp"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD PRT LINAGE IS 66 LINES.
       01 PRT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 A PIC X(2) VALUE "00".
       PROCEDURE DIVISION.
           IF A = (LINAGE-COUNTER)
               DISPLAY "EQ"
           END-IF
           STOP RUN.
