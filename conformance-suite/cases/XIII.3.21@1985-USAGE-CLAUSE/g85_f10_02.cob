      *>--- conformance-case
      *> id: g85_f10_02
      *> rules: [1985-SR-XIII.3.21.3-1, 1985-SR-XIII.3.21.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.21.3 SR1 - "The USAGE clause may be written in any
      *>   data description entry": USAGE on the 01 TYPE DE entry, on a
      *>   LINE group entry and on elementary COLUMN entries.
      *> §1985:XIII.3.21.3 SR2 - D1's group USAGE written again in the
      *>   subordinate group L1 and in the elementary entries at
      *>   COLUMN 5 and COLUMN 2.
      *> DERIVATION. WA = "ABC", WN = 42. Line 1: columns 1-3 "ABC"
      *>   (PIC X(3)), 5-7 "042" (PIC 999), 9-11 " 42" (PIC ZZ9):
      *>   "ABC 042  42". Line 2: column 2-3 "OK": " OK". The program
      *>   reads the report file back and DISPLAYs each nonblank line.
      *> Fails if: the pin refuses a USAGE clause on any of these
      *>   entries, or a printed line differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1002.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F1002.TXT".
           SELECT CHK ASSIGN TO "G85F1002.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WA PIC X(3) VALUE "ABC".
       01  WN PIC 99 VALUE 42.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES.
       01  D1 TYPE DE USAGE IS DISPLAY.
           03  L1 LINE PLUS 1 USAGE DISPLAY.
               05  COLUMN 1 PIC X(3) SOURCE WA.
               05  COLUMN 5 PIC 999 SOURCE WN USAGE IS DISPLAY.
               05  COLUMN 9 PIC ZZ9 SOURCE WN.
           03  LINE PLUS 1.
               05  COLUMN 2 PIC X(2) VALUE "OK" USAGE DISPLAY.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
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
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE(1:12) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
