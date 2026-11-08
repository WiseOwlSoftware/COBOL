*>--- conformance-case
*> id: pb4765-index-name-paren-counter
*> rules: [GR-8.8.4.2.13-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4765: 8.8.4.2.13 row 2 admits an index-name against "a numeric data item or numeric literal". (LINAGE-COUNTER) is
      *> "an arithmetic expression enclosed in parentheses" (8.8.1.1), not the data item (8.4.3.2.1; 8.4.3.14.4 GR1) it encloses, and the
      *> expression binder drops the parentheses, so the tree is the sole form's. The relation of IX with the plain (N) was refused;
      *> this one was accepted. COBOL2533 at every edition.
      *>   cite.py --check 8.8.4.2.13 "Relation tests may be made only between" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4765B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "n4765b.tmp"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD PRT LINAGE IS 66 LINES.
       01 PRT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 N PIC S9(3) VALUE -5.
       01 T.
          05 E PIC X OCCURS 9 INDEXED BY IX.
       PROCEDURE DIVISION.
           SET IX TO 5
           IF IX = (LINAGE-COUNTER)
               DISPLAY "EQ"
           END-IF
           STOP RUN.
