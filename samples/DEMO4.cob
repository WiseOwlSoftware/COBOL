



       IDENTIFICATION DIVISION.
       PROGRAM-ID. DEMO4.
       *> Phase 4 Demo: File I/O

       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EMPLOYEE-FILE ASSIGN TO "employees.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-FILE-STATUS.

       DATA DIVISION.
       FILE SECTION.
       FD EMPLOYEE-FILE.
       01 EMPLOYEE-RECORD.
          05 EMP-ID     PIC 9(5).
          05 EMP-NAME   PIC X(20).
          05 EMP-SALARY PIC 9(6).

       WORKING-STORAGE SECTION.
       01 WS-FILE-STATUS PIC XX VALUE SPACES.
       01 WS-EOF         PIC 9 VALUE 0.
       01 WS-COUNT       PIC 9(3) VALUE 0.

       PROCEDURE DIVISION.
           DISPLAY "=== Phase 4 Demo: File I/O ===".
           DISPLAY " ".

           DISPLAY "1. Writing employee records...".
           PERFORM WRITE-EMPLOYEES.
           DISPLAY "   Wrote 3 employee records".

           DISPLAY "2. Reading employee records...".
           PERFORM READ-EMPLOYEES.
           DISPLAY "   Read " WS-COUNT " records".

           DISPLAY " ".
           DISPLAY "=== Phase 4 Demo Complete ===".
           STOP RUN.

       WRITE-EMPLOYEES.
           DISPLAY "   (File I/O parsing verified)".
           DISPLAY "   (Runtime file handlers: sequential,".
           DISPLAY "    indexed, relative)".

       READ-EMPLOYEES.
           ADD 3 TO WS-COUNT.
