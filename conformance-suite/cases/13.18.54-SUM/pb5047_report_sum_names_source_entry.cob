      *>--- conformance-case
      *> id: pb5047_report_sum_names_source_entry
      *> rules: [SR-13.18.54.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5047 - the LEGAL side of a SUM data-name-1 naming a SOURCE entry. 1985:XIII.3.19.3 SR1 requires an identifier of the
      *> Report Section to reference a sum counter; 2023:13.18.54.4 GR6 adds, for an entry with a SOURCE clause, the operand of that clause.
      *> conformance:negative/pb5047-sum-addend-source-entry is the refused side at COBOL-85.
      *> DERIVATION. DA is a SOURCE entry of the detail; TOT, in the control footing, adds DA's operand WS-A (2) when the detail is
      *> processed (GR7 a)), once per GENERATE. The two details print 2 on lines 1 and 2, and the footing prints TOT = 04 on line 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5047P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5047P.TXT".
           SELECT CHK ASSIGN TO "PB5047P.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WS-A PIC 9 VALUE 2.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL PAGE LIMIT IS 10 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  DA COLUMN 1 PIC 9 SOURCE WS-A.
       01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  TOT COLUMN 1 PIC 99 SUM DA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           GENERATE DET.
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
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:11) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
