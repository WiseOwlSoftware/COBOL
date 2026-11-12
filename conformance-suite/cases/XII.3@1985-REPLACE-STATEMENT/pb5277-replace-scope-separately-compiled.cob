      *>--- conformance-case
      *> id: pb5277-replace-scope-separately-compiled
      *> rules: [1985-GR-XII.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR3 (cite.py --check: OK): "A given occurrence of the
      *> REPLACE statement is in effect from the point at which it is specified
      *> until the next occurrence of the statement or the end of the separately
      *> compiled program". SC34A holds the REPLACE, and SC34N is CONTAINED in
      *> SC34A, so it is inside the separately compiled program and is replaced
      *> too: REPLACED twice. SC34B is a separately compiled program of the same
      *> source, where the REPLACE has ended, so its own data item TAG stays a
      *> data item and displays OWN-TG. Why each leg can fail: a scope that runs
      *> to the end of the compilation group (the 2002 rule) turns SC34B's TAG
      *> into a literal and refuses its data description; a scope cut at every
      *> END PROGRAM header would leave SC34N unreplaced. (kb/Work PB5277)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC34A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       REPLACE ==TAG== BY =="REPLACED"==.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY TAG.
           CALL "SC34N".
           CALL "SC34B".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC34N.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY TAG.
           EXIT PROGRAM.
       END PROGRAM SC34N.
       END PROGRAM SC34A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC34B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TAG PIC X(6) VALUE "OWN-TG".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY TAG.
           EXIT PROGRAM.
       END PROGRAM SC34B.
