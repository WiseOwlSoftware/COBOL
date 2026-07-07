      *> ISO §11.7 / §13.18.44 — REDEFINES in a METHOD's WORKING-STORAGE (M2-OO-1h step 3). Method WS is STATIC
      *> (one copy per class, persistent across activations, §11.7; pre-2023). A Tier-B REDEFINES (CCHARS X(4) over
      *> CNT 9(4)) — its string backing is emitted STATIC, so the redefine window survives between INVOKEs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOREDW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS AGG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE AGG.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE AGG "NEW" RETURNING A.
           INVOKE A "DOIT".
           INVOKE A "DOIT".
           STOP RUN.
       END PROGRAM OOREDW.

       IDENTIFICATION DIVISION.
       CLASS-ID. AGG.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(4) VALUE 0.
       01 CCHARS REDEFINES CNT PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO CNT.
           DISPLAY "CNT=" CNT " CH=" CCHARS.
           GOBACK.
       END METHOD DOIT.
       END OBJECT.
       END CLASS AGG.
