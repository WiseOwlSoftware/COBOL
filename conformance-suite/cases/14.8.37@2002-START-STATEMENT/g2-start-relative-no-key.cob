      *>--- conformance-case
      *> id: g2-start-relative-no-key
      *> rules: [2002-GR-14.8.37.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.37.3 GR8 - "If the KEY phrase is omitted, the
      *> START statement behaves as though KEY IS EQUAL TO data-name-1
      *> had been specified, where data-name-1 is the name of the key
      *> specified in the RELATIVE KEY clause associated with
      *> file-name-1."
      *> The file holds relative records 1, 3 and 4 (2 is absent).
      *> EXPECTED, derived from GR8 with GR9 a)/c) and 9.1.12:
      *>  A1 S=00, A2 R003 - RK=3, START RELF (no KEY) = KEY IS
      *>     EQUAL TO RK: record 3 satisfies it, the file position
      *>     indicator is set to 3 (GR9 a)), I-O status 00
      *>     (§2002:9.1.12.1); READ NEXT returns record 3.
      *>  B1 IK S=23 - RK=2: no record has key EQUAL to 2, so the
      *>     invalid key condition exists (GR9 c)); of the invalid-key
      *>     statuses (§2002:9.1.12.4) only 23 a) (record does not
      *>     exist) applies. A START that defaulted to GREATER OR EQUAL
      *>     would succeed at record 3 instead; one that defaulted to
      *>     FIRST would succeed at record 1 in A1/A2 (R001).
      *>  DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STR08.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "g2str08.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RELF.
           MOVE 1 TO RK. MOVE "R001" TO RREC. WRITE RREC.
           MOVE 3 TO RK. MOVE "R003" TO RREC. WRITE RREC.
           MOVE 4 TO RK. MOVE "R004" TO RREC. WRITE RREC.
           CLOSE RELF.
           OPEN INPUT RELF.
           MOVE 3 TO RK.
           START RELF
               INVALID KEY DISPLAY "A1 IK S=" FS
               NOT INVALID KEY DISPLAY "A1 S=" FS
           END-START.
           READ RELF NEXT.
           DISPLAY "A2 " RREC.
           MOVE 2 TO RK.
           START RELF
               INVALID KEY DISPLAY "B1 IK S=" FS
               NOT INVALID KEY DISPLAY "B1 S=" FS
           END-START.
           CLOSE RELF.
           DISPLAY "DONE".
           STOP RUN.
