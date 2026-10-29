      *>--- conformance-case
      *> id: pb3990_report_source_rounded_edited
      *> rules: [GR-13.18.53.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3990 (review finding R1-1's sibling) - a SOURCE clause's
      *> ROUNDED phrase into a NUMERIC-EDITED printable item.
      *>
      *> ISO 13.18.53.4 GR2: "If the ROUNDED phrase is specified, the
      *> implicit COMPUTE statement has the corresponding ROUNDED
      *> phrase." With no MODE phrase 14.7.4.3 rule 1 defers to the
      *> DEFAULT ROUNDED clause, and 11.9.6.3 rule 2: "If the DEFAULT
      *> ROUNDED clause is not specified, DEFAULT ROUNDED MODE IS
      *> NEAREST-AWAY-FROM-ZERO is implied"; 14.7.4.3 rule 4 rounds
      *> "to the nearest value that can be represented in the resultant
      *> identifier. If two such values are equally near, the value
      *> farther from zero is chosen." A floating-point edited item
      *> represents its significand's digits (kb/Work PB2638). Without
      *> the phrase GR1's MOVE truncates (14.6.8.2 rule 4). The leg
      *> fails if an edited item truncates under ROUNDED: the native
      *> lane handed the edit its sender unrounded, so 1.55 printed 1
      *> into Z9 and 1.5 into Z9.9, and the floating-point form always
      *> truncated.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE RULE. WS-F = 1.55,
      *> WS-G = 1.2355.
      *>   COL 1  Z9        WS-F ROUNDED: 1.55 to scale 0      =>  2
      *>   COL 4  Z9.9      WS-F ROUNDED: a tie, away from zero =>  1.6
      *>   COL 9  Z9        WS-F, no phrase: truncated         =>  1
      *>   COL 12 +9.99E+99 WS-G ROUNDED: three digits, 1.2355 => +1.24E+00
      *>   COL 23 +9.99E+99 WS-G, no phrase: truncated         => +1.23E+00
      *>   COL 34 Z9.9      WS-G ROUNDED: 1.2355 is nearer 1.2 =>  1.2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3990RE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb3990re.txt".
           SELECT CHK ASSIGN TO "pb3990re.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-RE.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X       VALUE "N".
       01  WS-F    PIC 9V99    VALUE 1.55.
       01  WS-G    PIC 9V9(4)  VALUE 1.2355.
       01  WS-I    PIC 99      VALUE 0.
       01  WS-LINE PIC X(40)   VALUE SPACES.
       REPORT SECTION.
       RD  R-RE PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC Z9        SOURCE IS WS-F ROUNDED.
           03  COLUMN 4  PIC Z9.9      SOURCE IS WS-F ROUNDED.
           03  COLUMN 9  PIC Z9        SOURCE IS WS-F.
           03  COLUMN 12 PIC +9.99E+99 SOURCE IS WS-G ROUNDED.
           03  COLUMN 23 PIC +9.99E+99 SOURCE IS WS-G.
           03  COLUMN 34 PIC Z9.9      SOURCE IS WS-G ROUNDED.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-RE.
           GENERATE DET.
           TERMINATE R-RE.
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
