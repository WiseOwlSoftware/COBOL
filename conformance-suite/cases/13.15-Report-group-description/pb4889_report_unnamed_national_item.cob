*>--- conformance-case
*> id: pb4889_report_unnamed_national_item
*> rules: [FMT-13.15.2, SR-13.15.3-2, SR-13.18.60.3-7]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ⛔ AN UNNAMED REPORT ITEM MAY BEGIN WITH ITS USAGE WORD (kb/Work PB4889). ISO/IEC 1989:2023 §2023:13.15.2
      *> prints the report group description entry as `level-number [ entry-name-clause ]` followed by its
      *> clauses, one of them `[ USAGE IS ] { DISPLAY | NATIONAL }`: both the entry-name and the words USAGE IS
      *> are optional, so `03 NATIONAL COLUMN 1 PIC N(3) VALUE N"ABC".` is an unnamed USAGE NATIONAL item.
      *>   cite.py: OK  §2023:13.15.2  (General format) "entry-name-clause"
      *> §2023:13.15.3 SR2: "If the entry-name clause is specified, the data-name format or filler format shall be
      *> specified. The entry-name clause shall immediately follow the level-number."
      *>   cite.py: OK  §2023:13.15.3 2)  (Syntax rules)
      *> §2023:8.9 reserves NATIONAL from 2002, and §2023:8.3.2.1 1) bars a reserved word from a user-defined word, so
      *> the word cannot be the entry's name: the clause reading is the only one. The name slot admits a
      *> reservation-gated word, and the parser took NATIONAL as the name and refused it (COBOL0901) — legal
      *> source refused. The data description entry's entry-name predicate (kb/Work PB845) now guards this
      *> entry's name slot too.
      *> §2023:13.18.60.3 SR7: "Only the DISPLAY or NATIONAL phrase may be specified in any USAGE clause associated
      *> with a report group item."
      *>   cite.py: OK  §2023:13.18.60.3 7)  (Syntax rules)
      *>
      *> DERIVATION (one detail group, three relative lines):
      *>   LINE 1 - an elementary item whose first clause is NATIONAL, then COLUMN, PIC and VALUE: "ABC" at column 1,
      *>     and a DISPLAY item "D" at column 5 (§2023:13.18.14.4 GR3, the printable-size is the columns its
      *>     characters take, one each here as conformance:cases/13.18.14-COLUMN/pb541_report_national_item
      *>     pins) -> "ABC D".
      *>   cite.py: OK  §2023:13.18.14.4 3)  (General rules)
      *>   LINE 2 - a GROUP entry whose first clause is NATIONAL; its subordinate item has no USAGE clause and is
      *>     national by §2023:13.18.60.4 GR1 (a group's usage applies to each elementary item): SOURCE WN = "PQ".
      *>   LINE 3 - USAGE IS NATIONAL written in full after no name, the same item spelt the long way: "MN".
      *> The national characters are all 7-bit, so the print stream writes them unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4889RUN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb4889run.txt".
           SELECT CHK ASSIGN TO "pb4889run.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-N.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WN      PIC N(2)  VALUE N"PQ".
       REPORT SECTION.
       RD  R-N PAGE LIMIT IS 10 LINES.
       01  DET-N TYPE DE.
           02  LINE PLUS 1.
               03  NATIONAL COLUMN 1 PIC N(3) VALUE N"ABC".
               03  COLUMN 5 PIC X VALUE "D".
           02  NATIONAL LINE PLUS 1.
               03  COLUMN 1 PIC N(2) SOURCE WN.
           02  LINE PLUS 1.
               03  USAGE IS NATIONAL COLUMN 1 PIC N(2) VALUE N"MN".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-N.
           GENERATE DET-N.
           TERMINATE R-N.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
