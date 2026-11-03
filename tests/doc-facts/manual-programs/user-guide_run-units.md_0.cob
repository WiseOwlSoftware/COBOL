       IDENTIFICATION DIVISION.
       PROGRAM-ID. STOPPER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CALLS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO CALLS
           DISPLAY "call number " CALLS
           IF CALLS = 2
               STOP RUN WITH ERROR STATUS 7
           END-IF
           GOBACK.
