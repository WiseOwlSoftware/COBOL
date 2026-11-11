      *>--- conformance-case
      *> id: pb5248_sum_size_error_presentation
      *> rules: [GR-13.18.54.4-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5248 - a SUM counter that took a size error, presented. §2023:13.18.54.4 GR4 (cite.py: OK; the 2002 text, 13.16.52.3 GR4, the
      *> same): "If the associated size error indicator is set, an EC-REPORT-SUM-SIZE exception condition is set to exist and the printable
      *> item is filled with spaces". §1985:XIII.3.19.4 GR4 (cite.py: OK) has no size error indicator: "the sum counter serves as a source
      *> data item. The report writer control system moves the data contained in the sum counter, according to the rules of the MOVE
      *> statement, to the printable item for presentation."
      *> DERIVATION. The counter S1 is PIC 9. GENERATE with WS-B 5 adds 5 (a counter of 5); GENERATE with WS-B 6 adds 6, a total of 11 that does
      *> not fit one digit: a size error. TERMINATE prints the control footing, whose column 1 is the counter.
      *>   COBOL-2002 and later: the indicator is set, so column 1 is a space (no exception is checked, the item is filled).
      *>   COBOL-85: the counter's content after the overflow is undefined, but it is MOVEd to the item, so column 1 is a digit.
      *> The test reads the three lines back (each ends in a line terminator, the empty one too) and says whether column 1 holds a digit or a space, so it states only what the standard states.
      *> Each leg can fail: spaces at COBOL-85 or a digit from 2002 on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5248S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5248S.TXT".
           SELECT CHK ASSIGN TO "PB5248S.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-B    PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROL IS FINAL.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE WS-B.
       01  CFT TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  S1 COLUMN 1 PIC 9 SUM WS-B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 5 TO WS-B. GENERATE DL.
           MOVE 6 TO WS-B. GENERATE DL.
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
           ADD 1 TO WS-LN
           IF WS-LINE(1:1) IS NUMERIC
               DISPLAY "LINE " WS-LN " DIGIT"
           ELSE
               DISPLAY "LINE " WS-LN " SPACE"
           END-IF
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-I.
