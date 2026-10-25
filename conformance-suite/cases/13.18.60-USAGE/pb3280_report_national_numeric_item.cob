*>--- conformance-case
*> id: pb3280_report_national_numeric_item
*> rules: [SR-13.18.60.3-7, SR-13.18.60.3-12, GR-13.18.60.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3280 - a numeric printable item of usage NATIONAL is legal and prints its digits.
      *> SR7: "Only the DISPLAY or NATIONAL phrase may be specified in any USAGE clause associated with a report group
      *>   item."   cite.py: OK  13.18.60.3 7)  (Syntax rules)
      *> SR12: "An elementary data item with usage national shall be described with a picture character-string that
      *>   describes a boolean, national, national-edited, numeric, or numeric-edited data item."
      *>   cite.py: OK  13.18.60.3 12)  (Syntax rules)
      *> GR1: "If the USAGE clause is specified or implied at a group level, it applies only to each elementary item in
      *>   the group."   cite.py: OK  13.18.60.4 1)  (General rules)
      *> USAGE NATIONAL is a COBOL 2002 introduction (conformance:negative/pb541-report-national-at-85 is the refused side).
      *> WA = 42, WN = -5.
      *>   DA: col 1 PIC 9(3) USAGE NATIONAL (its own clause) "042"; col 6 the DISPLAY twin "042".
      *>   DB (USAGE NATIONAL on the level 1 entry, GR1): col 1 PIC 9(3) "042"; col 6 PIC S9(3) SIGN LEADING SEPARATE
      *>     "-005" (13.18.52.3 SR2 admits usage national for the SIGN clause's subject, 13.18.52.4 GR6 a) and b) fix
      *>     the image).
      *> Before the fix the printable item's image mark admitted usage DISPLAY alone, so a national numeric item reached
      *> the line as a long and the backend refused the generated C# (CS1503) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3280RN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB3280RN.TXT".
           SELECT CHK ASSIGN TO "PB3280RN.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WA      PIC 9(3)  VALUE 42.
       01  WN      PIC S9(3) VALUE -5.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 9(3) USAGE NATIONAL SOURCE WA.
           02  COLUMN 6 PIC 9(3) SOURCE WA.
       01  DB TYPE DE LINE PLUS 1 USAGE NATIONAL.
           02  COLUMN 1 PIC 9(3) SOURCE WA.
           02  COLUMN 6 PIC S9(3) SOURCE WN SIGN LEADING SEPARATE.
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
