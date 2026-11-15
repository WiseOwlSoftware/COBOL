*>--- conformance-case
*> id: pb5602-start-key-other-relative-occurrence
*> rules: [2002-SR-14.8.37.2-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - the RELATIVE KEY is RK(2), so START KEY IS = RK(1) names another data item: §2002:14.8.37.2
      *> SR5, "For relative files, data-name-1, if specified, shall be the data item specified in the RELATIVE KEY clause".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602N7.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-start-key-other-relative-occurrence.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK(2).
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(3).
       WORKING-STORAGE SECTION.
       01 RKT.
          05 RK PIC 9(2) OCCURS 3.
       PROCEDURE DIVISION.
           OPEN INPUT F1
           START F1 KEY IS EQUAL TO RK(1)
           CLOSE F1
           STOP RUN.
