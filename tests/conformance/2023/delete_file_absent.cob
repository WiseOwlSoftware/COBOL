       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELABS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "df-absent-xyz.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           DELETE FILE F
               ON EXCEPTION DISPLAY "EXC"
               NOT ON EXCEPTION DISPLAY "NOEXC"
           END-DELETE.
           DISPLAY "ST=" WS-ST.
           STOP RUN.
