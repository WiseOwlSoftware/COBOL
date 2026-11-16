      *>--- conformance-case
      *> id: g2002_open_sharing_all_clause_no_lock_refused
      *> rules: [2002-SR-14.8.26.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.2 SR6, first disjunct: "If the SHARING phrase is
      *> omitted from the OPEN statement and the ALL phrase is specified
      *> in the SHARING clause of the file control entry for file-name-1
      *> ... the LOCK MODE clause shall be specified in the file control
      *> entry for file-name-1." F's clause says ALL OTHER, F has no LOCK
      *> MODE clause, and the OPEN omits the phrase: refused at the OPEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN3X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2opn3x.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F
           STOP RUN.
