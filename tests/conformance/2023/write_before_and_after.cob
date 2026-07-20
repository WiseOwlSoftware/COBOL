      *> ISO §14.9.51 GR25e/GR25f — WRITE … BEFORE ADVANCING … AFTER ADVANCING … (COBOL-2023): both advancing
      *> phrases on one WRITE. The record is presented once at the current line, then the page advances by the
      *> BEFORE amount and by the AFTER amount (both after presentation; SR17 forbids PAGE). Observable via
      *> LINAGE-COUNTER (§13.18.34 GR7c): it increments by before+after. OPEN sets it to 1 (GR7d); a
      *> BEFORE 1 + AFTER 2 write leaves it at 1+1+2 = 4 (a single BEFORE 1 would give 2; a single AFTER 2, 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WBA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "wba-conf.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF LINAGE IS 20 LINES.
       01 P-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 LC PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRTF.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "OPEN LC=" LC.
           MOVE "AAA" TO P-REC.
           WRITE P-REC BEFORE ADVANCING 1 AFTER ADVANCING 2.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "WRITE LC=" LC.
           CLOSE PRTF.
           STOP RUN.
