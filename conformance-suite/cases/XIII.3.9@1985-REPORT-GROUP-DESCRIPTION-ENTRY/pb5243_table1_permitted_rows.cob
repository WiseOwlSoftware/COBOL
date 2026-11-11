      *>--- conformance-case
      *> id: pb5243_table1_permitted_rows
      *> rules: [1985-SR-XIII.3.9.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5243 - §1985:XIII.3.9.3 SR11 Table 1 (cite.py: OK), "Permissible Clause Combinations in Format 3 Entries": every row, as
      *> a program COBOL-85 must ACCEPT, including the two the 2023 screens used to refuse. "An 'M' indicates that the presence of the clause
      *> is mandatory. A 'P' ... permitted, but not required. A blank indicates that the clause is not permitted."
      *>   row 1  PIC M, SUM M, SIGN P, LINE P          U1: a SUM entry with no COLUMN clause (a counter that prints nowhere).
      *>   row 2  PIC, COLUMN, SUM M; BLANK WHEN ZERO, USAGE, SIGN, LINE P     S2.
      *>   row 3  PIC, SOURCE M; COLUMN, JUSTIFIED, GROUP INDICATE, USAGE, SIGN, LINE P   G1 prints (COLUMN 5); G2 and G3 have NO COLUMN
      *>          clause, "a 1985 entry with no COLUMN is not printed but is legal": the 2023 rule that JUSTIFIED (§2023:13.15.3 SR15) and
      *>          GROUP INDICATE (§2023:13.18.28.3 SR1) need a COLUMN clause refused both.
      *>   row 4  PIC, SOURCE M; COLUMN, BLANK WHEN ZERO, GROUP INDICATE, USAGE, SIGN, LINE P   B4.
      *>   row 5  PIC, COLUMN, VALUE M; JUSTIFIED, GROUP INDICATE, USAGE, SIGN, LINE P   V5.
      *> DERIVATION. One GENERATE with WS-C "QQ", WS-Z 0, WS-A 7. Detail line: G1 is the first presentation of its group (§1985:XIII.3.13.4
      *> GR1 a), so it presents WS-C, "QQ" moved to PIC X(5) JUSTIFIED RIGHT: "   QQ" in columns 5-9; B4 moves 0 to PIC 99 BLANK WHEN ZERO:
      *> two spaces in columns 11-12; V5 "ABC" in columns 14-16. The footing line has S2: the counter holds 7 (§1985:XIII.3.19.4 GR2 adds
      *> WS-A at the GENERATE), moved to PIC 99: "07".  The line is read back byte-wise, as conformance:pb541_report_national_item does.
      *> Each leg can fail: a binder that refuses a row (any of the five, or G2/G3) stops at the translation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5243P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5243P.TXT".
           SELECT CHK ASSIGN TO "PB5243P.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-C    PIC XX    VALUE "QQ".
       01  WS-Z    PIC 99    VALUE 0.
       01  WS-A    PIC 9     VALUE 7.
       REPORT SECTION.
       RD  R CONTROL IS FINAL.
       01  DET TYPE DE LINE PLUS 1.
           05  G1 COLUMN 5 PIC X(5) JUSTIFIED RIGHT SOURCE WS-C
               GROUP INDICATE.
           05  G2 PIC X(5) SOURCE WS-C JUSTIFIED RIGHT.
           05  G3 PIC X(5) SOURCE WS-C GROUP INDICATE.
           05  B4 COLUMN 11 PIC 99 BLANK WHEN ZERO SOURCE WS-Z.
           05  V5 COLUMN 14 PIC X(3) JUSTIFIED RIGHT VALUE "ABC".
       01  CFT TYPE CF FINAL LINE PLUS 1.
           05  U1 PIC 99 SUM WS-A.
           05  S2 COLUMN 1 PIC 99 BLANK WHEN ZERO USAGE DISPLAY
               SUM WS-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DET.
           TERMINATE R.
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
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
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
