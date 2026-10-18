*>--- conformance-case
*> id: pb870-start-first-last-relative-85
*> rules: [GR-14.9.41.4-11, GR-14.9.41.4-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB870 - START FIRST and START LAST on a RELATIVE file, the organization whose rules GR11 and
      *> GR12 (ISO 14.9.41.4) were dated 2014 while their INDEXED twins GR18/GR19 and the SEQUENTIAL twins
      *> GR20/GR21 were dated 2002. FIRST and LAST are ONE construct (14.9.41.2 prints one general format),
      *> start-first-last-2002 in tests/version-matrix/constructs.json, so the introduction gate speaks for
      *> every organization: below 2002 each of the two statements below is COBOLNET0900 ("START FIRST/LAST
      *> requires COBOL-2002"). The positive twin is l1_start_first_last_relative, accepted from 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB870N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFL ASSIGN TO "pb870n85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD RFL.
       01 RFL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT RFL
           START RFL FIRST
               INVALID KEY DISPLAY "FIRST-INVALID"
           END-START
           START RFL LAST
               INVALID KEY DISPLAY "LAST-INVALID"
           END-START
           CLOSE RFL
           STOP RUN.
