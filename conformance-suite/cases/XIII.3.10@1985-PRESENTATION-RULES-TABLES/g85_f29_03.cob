      *>--- conformance-case
      *> id: g85_f29_03
      *> rules: [1985-GR-XIII.3.10.10-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.10 GR4 - final LINE-COUNTER setting rules of
      *> the report footing: "a. The final LINE-COUNTER setting is the
      *> line number on which the final print line of the report
      *> footing report group is presented." "b. LINE-COUNTER is
      *> unaffected by the processing of a nonprintable report group."
      *> Two unpaged reports (§1985:XIII.3.8.4 GR3). Table 5, row R,
      *> PAGE clause omitted: first print line 3d, final setting 4a;
      *> row blank (no LINE clause): 3e, 4b.
      *> R1 (rule 4a). GENERATE D1 (LINE PLUS 1): 0 + 1 = line 1
      *>   (§1985:XIII.3.10.8 GR4d), LC 1. TERMINATE presents RF1:
      *>   3d gives 1 + 2 = line 3; its second line is relative, so
      *>   3 + 1 = line 4 (§1985:XIII.3.15.4 GR4). 4a: LC = 4.
      *> R2 (rule 4b). Two GENERATE D2 put LC at 1 then 2. RF2 has
      *>   no LINE clause and no COLUMN clause, so it defines no
      *>   printable item: it is processed (its USE procedure runs,
      *>   §1985:XIII.3.20.4 GR9a) and not presented, and 4b leaves
      *>   LC at 2, both in its USE procedure and after TERMINATE.
      *> The read-back of R1's file shows the footing lines at 3, 4.
      *> Fails if the final setting is taken from the first footing
      *> line (3), stays at the detail's (1), or if the nonprintable
      *> footing moves R2's LINE-COUNTER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2903.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT1 ASSIGN TO "g85f2903a.txt".
           SELECT PRT2 ASSIGN TO "g85f2903b.txt".
           SELECT CHK  ASSIGN TO "g85f2903a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT1 REPORT IS R1.
       FD  PRT2 REPORT IS R2.
       FD  CHK.
       01  CHK-REC         PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-I            PIC 99    VALUE 0.
       01  WS-LN           PIC 99    VALUE 0.
       01  WS-LC           PIC 99    VALUE 0.
       01  WS-TAG          PIC X(4)  VALUE "TAG2".
       01  WS-LINE         PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R1.
       01  D1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "D1".
       01  RF1 TYPE RF.
           02  LINE PLUS 2.
               03  COLUMN 1 PIC X(4) VALUE "RF-A".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(4) VALUE "RF-B".
       RD  R2.
       01  D2 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "D2".
       01  RF2 TYPE RF.
           02  RF2-V PIC X(4) SOURCE WS-TAG.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-RF2 SECTION.
           USE BEFORE REPORTING RF2.
       D-RF2-P.
           MOVE LINE-COUNTER IN R2 TO WS-LC.
           DISPLAY "RF2 USE LC " WS-LC.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT1 PRT2.
           INITIATE R1 R2.
           GENERATE D1.
           GENERATE D2.
           GENERATE D2.
           TERMINATE R1.
           MOVE LINE-COUNTER IN R1 TO WS-LC.
           DISPLAY "R1 LC " WS-LC.
           TERMINATE R2.
           MOVE LINE-COUNTER IN R2 TO WS-LC.
           DISPLAY "R2 LC " WS-LC.
           CLOSE PRT1 PRT2.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-CR AND CHK-REC NOT = SYM-FF
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
