      *>--- conformance-case
      *> id: pb3990_report_source_prohibited_keeps_the_item
      *> rules: [GR-13.18.53.4-2, GR-14.7.4.3-7, GR-13.18.53.4-4]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3990 (review finding R1-1) - ROUNDED MODE IS PROHIBITED in a
      *> report SOURCE clause, whose implicit COMPUTE has no SIZE ERROR
      *> phrase and runs with EC-SIZE checking off.
      *>
      *> ISO 13.18.53.4 GR2: "Arithmetic-expression-1 specifies the
      *> operand of an implicit COMPUTE statement that is executed
      *> implicitly whenever the associated item is printed. If the
      *> ROUNDED phrase is specified, the implicit COMPUTE statement has
      *> the corresponding ROUNDED phrase." 14.7.4.3 rule 7: "If the
      *> PROHIBITED phrase is specified, and the arithmetic value cannot
      *> be represented exactly in the resultant identifier, the
      *> EC-SIZE-TRUNCATION exception condition is set to exist, the
      *> size error condition exists, and the content of the resultant
      *> identifier is unchanged". The resultant is the printable item;
      *> with checking off 14.6.13.1.3 item 8 leaves whether execution
      *> continues to the implementor, and CONFORMANCE.md DOC-A.1-70
      *> continues, exactly as the COMPUTE statement does (golden
      *> 14.7.4-ROUNDED/pb3990_prohibited_unchecked_keeps_the_resultant).
      *> The unchanged printable item is SPACES: 13.8.6.2.3 makes a
      *> printable item a horizontal division of a report line whose
      *> value "is determined solely by a SOURCE, SUM, or VALUE clause",
      *> and each presentation composes the line from spaces. The leg
      *> fails if the raise ends the run unit (the branch before the
      *> fix), if the value is stored cut (main: 001 and 1.5 on line
      *> 2), or if a kept item leaks the previous presentation's value.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE RULE. WS-A = 100,
      *> WS-B = 23. GENERATE 1 with WS-F = 2.00 (every value exact),
      *> GENERATE 2 with WS-F = 1.55, so a kept item that leaked the
      *> first presentation's value would show it on line 2.
      *>   COL 1  9(3)  WS-A + WS-B PROHIBITED: 123 exact => 123 / 123
      *>   COL 5  9(3)  WS-F PROHIBITED  => 002 / 1.55 inexact: spaces
      *>   COL 9  9.9   WS-F PROHIBITED  => 2.0 / 1.55 inexact: spaces
      *>   COL 13 9(3)  WS-F, no phrase: the GR1 MOVE  => 002 / 001
      *>   COL 17 9.99  WS-F PROHIBITED: exact        => 2.00 / 1.55
      *>   COLS 22 26 9(3) SOURCES (WS-F) (WS-A) PROHIBITED, GR4 one
      *>          operand per repetition: 002 100 / spaces, then 100.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3990RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb3990rp.txt".
           SELECT CHK ASSIGN TO "pb3990rp.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-PX.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-A    PIC 999   VALUE 100.
       01  WS-B    PIC 999   VALUE 23.
       01  WS-F    PIC 9V99  VALUE 2.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R-PX PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A + WS-B
                   ROUNDED MODE IS PROHIBITED.
           03  COLUMN 5  PIC 9(3) SOURCE IS WS-F
                   ROUNDED MODE IS PROHIBITED.
           03  COLUMN 9  PIC 9.9  SOURCE IS WS-F
                   ROUNDED MODE IS PROHIBITED.
           03  COLUMN 13 PIC 9(3) SOURCE IS WS-F.
           03  COLUMN 17 PIC 9.99 SOURCE IS WS-F
                   ROUNDED MODE IS PROHIBITED.
           03  COLUMNS ARE 22 26 PIC 9(3)
                   SOURCES ARE (WS-F) (WS-A)
                   ROUNDED MODE IS PROHIBITED.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-PX.
           GENERATE DET.
           MOVE 1.55 TO WS-F.
           GENERATE DET.
           TERMINATE R-PX.
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
