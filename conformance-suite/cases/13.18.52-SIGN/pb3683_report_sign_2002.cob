*>--- conformance-case
*> id: pb3683_report_sign_2002
*> rules: [SR-13.18.52.3-1, GR-13.18.52.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3683 - a SIGN clause in a REPORT GROUP description entry that COBOL-85 refused: one written WITHOUT
      *> the SEPARATE CHARACTER phrase, and one written on a report group entry that is a GROUP. ISO/IEC 1989:2002
      *> Annex F.2 item 123 introduces both: "SIGN clause in a report description entry. The SEPARATE phrase is no
      *> longer required in a report description entry and the SIGN clause may be specified at the group level."
      *> (post-ballot text v200112.txt line 44299; cite.py checks only the 2023 text). The 85 refusals are
      *> conformance:negative/pb3683-report-sign-without-separate-85 and conformance:negative/pb3683-report-group-sign-85.
      *> SR1: "The SIGN clause may be specified only for: ... a numeric report group description entry whose picture
      *>   character-string contains the symbol 'S' ... an alphanumeric group item, national group item, or
      *>   strongly-typed group item."   cite.py: OK  13.18.52.3 1)  (Syntax rules)
      *> GR5: "If the SEPARATE CHARACTER phrase is not specified, then:" the operational sign is fused onto the leading
      *>   (or trailing) digit position and the implementor defines the valid signs.   cite.py: OK  13.18.52.4 5)
      *>   (General rules); the representation is the implementor's, pinned by docs/CONFORMANCE.md DOC-A.1-177
      *>   (--sign-encoding ibm, the default): positive 0-9 fuse to "{ABCDEFGHI" and negative 0-9 to "}JKLMNOPQR", on the
      *>   LEADING digit for SIGN LEADING and the TRAILING digit for SIGN TRAILING.
      *> WN = -5 (digits 005), WP = +7 (digits 007):
      *>   DA - elementary entries, SIGN written without SEPARATE: col 1 SIGN LEADING, -5 -> "}05" (0 fuses to "}");
      *>        col 6 SIGN TRAILING, +7 -> "00G" (7 fuses to "G").
      *>   DB - SIGN TRAILING on the level 1 entry, a GROUP (SR1's group bullet), no SEPARATE; GR1 hands it to each
      *>        signed numeric item below: col 1 -5 -> "00N" (5 fuses to "N"), col 6 +7 -> "00G".
      *> The SEPARATE group case is conformance:cases/13.18.52-SIGN/pb493_report_group_sign_inherited.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3683RS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB3683RS.TXT".
           SELECT CHK ASSIGN TO "PB3683RS.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WN      PIC S9(3) VALUE -5.
       01  WP      PIC S9(3) VALUE +7.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC S9(3) SOURCE WN SIGN LEADING.
           02  COLUMN 6 PIC S9(3) SOURCE WP SIGN TRAILING.
       01  DB TYPE DE LINE PLUS 1 SIGN TRAILING.
           02  COLUMN 1 PIC S9(3) SOURCE WN.
           02  COLUMN 6 PIC S9(3) SOURCE WP.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           GENERATE DB.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:10) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
