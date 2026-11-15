      *>--- conformance-case
      *> id: g2002_open_sharing_all_phrase_no_lock_refused
      *> rules: [2002-SR-14.8.26.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.2 SR6, second disjunct: "... or if the ALL phrase
      *> is specified on the OPEN statement, the LOCK MODE clause shall be
      *> specified in the file control entry for file-name-1." F has no
      *> SHARING clause and no LOCK MODE clause; its OPEN says SHARING
      *> WITH ALL OTHER: refused at the OPEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN3Y.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2opn3y.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT SHARING WITH ALL OTHER F
           STOP RUN.
