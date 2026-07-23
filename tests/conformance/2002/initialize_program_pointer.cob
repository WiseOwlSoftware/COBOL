      *> CA2 (CONFORMANCE-FIX-QUEUE): INITIALIZE of a data-pointer / program-pointer / object-reference item
      *> emits an implicit SET ... TO the predefined NULL (ISO 14.9.20.4 GR4/GR6c), NOT a MOVE. Pre-fix,
      *> INITIALIZE of these categories emitted nothing, so the item kept its prior value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITPP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ENTRY "INITSUB".
           IF PP = NULL DISPLAY "BEFORE-NULL" ELSE DISPLAY "BEFORE-SET" END-IF.
           INITIALIZE PP.
           IF PP = NULL DISPLAY "AFTER-NULL" ELSE DISPLAY "AFTER-SET" END-IF.
           STOP RUN.
       END PROGRAM INITPP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITSUB.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM INITSUB.
