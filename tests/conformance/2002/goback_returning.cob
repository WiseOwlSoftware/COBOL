      *> ISO §14.9.16 — COBOL-2002 GOBACK RETURNING. The RETURNING operand supplies the value returned to
      *> the activating element: GOBACK RETURNING x is equivalent to moving x into the PROCEDURE DIVISION
      *> RETURNING item and returning. The caller's CALL … RETURNING receives it. (GIVING is a synonym.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBRETMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 40.
       01 B PIC 9(4) VALUE 2.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           CALL "GBRETSUB" USING A B RETURNING R.
           DISPLAY "SUM=" R.
           MOVE 7 TO A.
           MOVE 6 TO B.
           CALL "GBRETSUB" USING A B RETURNING R.
           DISPLAY "SUM2=" R.
           STOP RUN.
       END PROGRAM GBRETMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBRETSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-SUM PIC 9(4).
       LINKAGE SECTION.
       01 LK-A PIC 9(4).
       01 LK-B PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-A LK-B RETURNING LK-R.
       P.
           COMPUTE WS-SUM = LK-A + LK-B.
           GOBACK RETURNING WS-SUM.
       END PROGRAM GBRETSUB.
